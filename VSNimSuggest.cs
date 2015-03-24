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
        public List<string> conout = new List<string>();
        public List<List<string>> sugs = new List<List<string>>();
        //private Thread thread = null;
        private bool queryfinished = false;
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
            string fstr = Path.GetFileName(VSNimLangServ.codefile_path_current);
            if (fstr != filepath_prev) {
                Close();
            }
            filepath_prev=fstr;
            conout.Clear();
            sugs.Clear();
            queryfinished = false;
            string qstr = qtype.ToString() + " " + fstr + ":" + qline.ToString() + ":" + qcol.ToString();
            proc.StandardInput.WriteLine(qstr);
            int waitcount = 0;
            while (!queryfinished) {
                Thread.Sleep(50);
                waitcount++;
                if (waitcount > 100) {
                    // 5 second wait cap
                    queryfinished = true;
                    break;
                }
            }
            foreach (string qstr in conout) {
                string[] qwords = qstr.Split(new char[] { '\t' });
                //List<string> qwords = new List<string>(qstr.Split(new Char[] { '\t' }));
                if (qwords.Length < 2) break;
                if (qwords[1] == "skProc" || qwords[1] == "skVar") {
                    //new List<string>(new string[]{"G","H","I"})
                    qwords[2] = qwords[2].Replace("htmlarc.", "");
                    qwords[7] = qwords[7].Replace(@"\x0D\x0A", "\n");
                    qwords[7] = qwords[7].Trim(new char[] { '"' });
                    if (qwords[7] != "")
                        qwords[7] = "\n\n" + qwords[7];
                    sugs.Add(new List<string>(new string[] { qwords[2], qwords[3] + qwords[7] }));
                }
            }
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
        }

        public void Close() {
            if (proc==null) return;
            proc.StandardInput.WriteLine("quit");
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
        }

        public void Init() {
            string nimsuggestexe = VSNimINI.Get("Main", "nimsuggest.exe");
            if (nimsuggestexe == "") {
                System.Diagnostics.Debug.Print("Nimsuggest.exe not set in nimstudio.ini!");
                return;
            }
            if (!File.Exists(nimsuggestexe)) {
                System.Diagnostics.Debug.Print("Nimsuggest.exe not found!");
                return;
            }
            
            proc = new Process();
            proc.StartInfo.CreateNoWindow = true;
            //proc.StartInfo.WorkingDirectory = @"c:\MyProgs\Nim";
            proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(VSNimLangServ.codefile_path_current);
            //proc.StartInfo.Arguments = @"--stdin c:\MyProgs\Nim\htmlarc.nim";
            proc.StartInfo.Arguments = @"--stdin " + VSNimLangServ.codefile_path_current + ":1:1";
            proc.StartInfo.FileName = VSNimINI.Get("Main", "nimsuggest.exe");
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.UseShellExecute = false;

            proc.EnableRaisingEvents = true;
            proc.OutputDataReceived += new DataReceivedEventHandler(NSDRHandler);
            proc.ErrorDataReceived += new DataReceivedEventHandler(NSDRHandler);
            proc.Exited += new EventHandler(NSExitHandler);
            proc.Start();
            proc.BeginOutputReadLine();
            proc.StandardInput.WriteLine("");
            Thread.Sleep(300); // allow time for nimsuggest startup lines to be processed
        }

        private void NSExitHandler(object sender, EventArgs e) {
            proc.Dispose();
            proc = null;
        }

        private void NSDRHandler(object sender, DataReceivedEventArgs e) {
            string line = e.Data;
            if (line == "")
                queryfinished=true;
            else {
                conout.Add(line);
            }
        }

    }

}
