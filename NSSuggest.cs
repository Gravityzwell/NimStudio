using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using con = System.Console;

namespace NimStudio.NimStudio {

    public class NimSuggestProc {
        private Process proc = null;
        private List<string> conout = new List<string>();
        public List<List<string>> sugs = new List<List<string>>();
        //private Thread thread = null;
        private bool queryfinished = false;
        private HashSet<string> filelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string filepath_prev="";
        public enum qtype_enum {
            sug,
            def,
            con
        }

        static NimSuggestProc() {
            // static constructor
        }

        public NimSuggestProc() {
            // instance constructor
        }


        public void Query(qtype_enum qtype, int qline, int qcol) {
            string fstr = Path.GetFileNameWithoutExtension(NSLangServ.codefile_path_current);
            NSUtil.SaveIfDirty(NSLangServ.codefile_path_current);
            //if (fstr != filepath_prev) {
            //    Close();
            //    Init();
            //}
            if (!filelist.Contains(fstr)) {
                filelist.Add(fstr);
                string basefile = "import " + string.Join(",", filelist);
                StreamWriter sw1 = new StreamWriter(Path.GetDirectoryName(NSLangServ.codefile_path_current) + @"\nimstudio_base.nim");
                sw1.WriteLine(basefile);
                sw1.Close();
            }

            if (proc == null) {
                Init();
            }
            filepath_prev=fstr;
            conout.Clear();
            sugs.Clear();
            queryfinished = false;
            string qstr = qtype.ToString() + " " + fstr + ".nim:" + qline.ToString() + ":" + qcol.ToString();
            NSUtil.DebugPrint("NimStudio - query:" + qstr);
            proc.StandardInput.WriteLine(qstr);
            int waitcount = 0;
            while (!queryfinished) {
                Thread.Sleep(25);
                waitcount++;
                if (waitcount > 200) {
                    // 5 second wait cap
                    //VSNimUtil.DebugPrint("NimStudio - querywait max hit");
                    con.Write("NimStudio - querywait max hit");
                    queryfinished = true;
                    break;
                }
            }
            string fnamestrip = Path.GetFileNameWithoutExtension(NSLangServ.codefile_path_current) + ".";
            NSUtil.DebugPrint("NimStudio - conout.count:" + conout.Count.ToString());
            foreach (string cstr in conout) {
                if (cstr==null) continue;
                NSUtil.DebugPrint("NimStudio - conout:" + cstr);
                string[] qwords = cstr.Split(new char[] { '\t' });
                if (qwords.Length < 2) continue;
                if (qwords[1] == "skProc" || qwords[1] == "skVar") {
                    //new List<string>(new string[]{"G","H","I"})
                    qwords[2] = qwords[2].Replace(fnamestrip, "");
                    qwords[7] = qwords[7].Replace(@"\x0D\x0A", "\n");
                    qwords[7] = qwords[7].Trim(new char[] { '"' });
                    if (qwords[7] != "")
                        qwords[7] = "\n\n" + qwords[7];
                    sugs.Add(new List<string>(new string[] { qwords[2], qwords[3], qwords[3] + qwords[7] }));
                }
            }
            sugs.Add(new List<string>(new string[] { "func1", "proc", "func1 help" }));
            NSUtil.DebugPrint("NimStudio - suggs count:" + sugs.Count.ToString());

        }

        public void conwriteold(string str) {
            conout.Clear();
            sugs.Clear();
            queryfinished=false;
            proc.StandardInput.WriteLine(str);
            int waitcount=0;
            while (!queryfinished) {
                if (proc == null || proc.HasExited) break;
                Thread.Sleep(50);
                waitcount++;
                if (waitcount > 100) // 5 second wait cap
                    break;
            }
            queryfinished = true;
            if (proc == null || proc.HasExited) {
                proc=null;
                filepath_prev="";
                return;
            }
            foreach (string qstr in conout) {
                string[] qwords = qstr.Split(new char[] { '\t' });
                if (qwords.Length < 2) break;
                if (qwords[1] == "skProc" || qwords[1] == "skVar") {
                    //new List<string>(new string[]{"G","H","I"})
                    qwords[2] = qwords[2].Replace("htmlarc.","");
                    qwords[7] = qwords[7].Replace(@"\x0D\x0A", "\n");
                    qwords[7] = qwords[7].Trim(new char[]{'"'});
                    if (qwords[7] != "")
                        qwords[7] = "\n\n" + qwords[7];
                    sugs.Add(new List<string>(new string[] { qwords[2], qwords[3] + qwords[7] }));
                }
            }
            sugs.Add(new List<string>(new string[] { "func1", "func1 help" }));
            NSUtil.DebugPrint("NimStudio - suggs count:" + sugs.Count.ToString());
        }

        public void Close() {
            NSUtil.DebugPrint("NimStudio - Nimsuggest close:");
            if (proc == null) 
                return;

            try {
                if (proc.HasExited) {
                    proc.Dispose();
                    proc=null;
                }
            } catch (Exception ex) {
                NSUtil.DebugPrint("NimStudio - Nimsuggest close:" + ex.Message);
                proc = null;
                return;
            }

            try {
                proc.StandardInput.WriteLine("quit");
            } catch (Exception ex) {
                NSUtil.DebugPrint("NimStudio - Nimsuggest close:" + ex.Message);
                proc = null;
            }

            int waitcount = 0;
            while (true) {
                if (proc == null || proc.HasExited) break;
                Thread.Sleep(25);
                waitcount++;
                if (waitcount > 200) { // 5 second wait cap
                    proc.Kill();
                    Thread.Sleep(25);
                }
                break;
            }

            proc=null;
            return;
        }

        public void Init() {
            NSUtil.DebugPrint("NimStudio - Nimsuggest init:");
            string nimsuggestexe = NSIni.Get("Main", "nimsuggest.exe");
            if (nimsuggestexe == "") {
                NSUtil.DebugPrint("Nimsuggest.exe not set in nimstudio.ini!");
                return;
            }
            if (!File.Exists(nimsuggestexe)) {
                NSUtil.DebugPrint("Nimsuggest.exe not found!");
                return;
            }
            
            proc = new Process();
            proc.StartInfo.CreateNoWindow = true;
            //proc.StartInfo.WorkingDirectory = @"c:\MyProgs\Nim";
            //proc.StartInfo.Arguments = @"--stdin c:\MyProgs\Nim\htmlarc.nim";
            //proc.StartInfo.Arguments = @"--stdin " + VSNimLangServ.codefile_path_current;
            proc.StartInfo.Arguments = @"--stdin " + Path.GetDirectoryName(NSLangServ.codefile_path_current) + @"\nimstudio_base.nim";
            //proc.StartInfo.Arguments = @"--stdin " + Path.GetDirectoryName(VSNimLangServ.codefile_path_current) + "\\";
            proc.StartInfo.FileName = NSIni.Get("Main", "nimsuggest.exe");
            proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(NSLangServ.codefile_path_current);
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = false;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.UseShellExecute = false;
            NSUtil.DebugPrint("NimStudio - Nimsuggest load:" + NSLangServ.codefile_path_current);
            NSUtil.DebugPrint("NimStudio - Nimsuggest working dir:" + proc.StartInfo.WorkingDirectory);

            proc.EnableRaisingEvents = true;
            proc.OutputDataReceived += new DataReceivedEventHandler(NSDRHandler);
            //proc.ErrorDataReceived += new DataReceivedEventHandler(NSDRHandler);
            proc.Exited += new EventHandler(NSExitHandler);
            proc.Start();
            Thread.Sleep(100);
            proc.StandardInput.WriteLine("");
            proc.BeginOutputReadLine();
            Thread.Sleep(300); // allow time for nimsuggest startup lines to be processed
        }

        private void NSExitHandler(object sender, EventArgs e) {
            NSUtil.DebugPrint("NimStudio NSExitHandler ");
            //proc.Dispose();
            proc = null;
        }

        private void NSDRHandler(object sender, DataReceivedEventArgs e) {
            string line = e.Data;
            //VSNimUtil.dp(line + "<conout");
            if (line == "")
                queryfinished=true;
            else
                conout.Add(line);
        }

    }

}
