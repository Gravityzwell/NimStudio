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
        //public List<List<string>> sugs = new List<List<string>>();
        //public SortedDictionary<string, List<string>> sugdct = new SortedDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public SortedDictionary<string, SortedDictionary<string, string>> sugdct = new SortedDictionary<string, SortedDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        //private Thread thread = null;
        private bool queryfinished = false;
        //private HashSet<string> filelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public static class Qtype {
            /// <summary>nimsuggest sug</summary>
            public const string sug = "sug";
            /// <summary>nimsuggest def</summary>
            public const string def = "def";
            /// <summary>nimsuggest con</summary>
            public const string con = "con";
        }

        private static class Sugspl {
            /// <summary>query type: sug|def|con [0]</summary>
            public const int qtype = 0;
            /// <summary>symbol kind: skProc|skVar|skIterator [1]</summary>
            public const int kind = 1;
            /// <summary>symbol name: os.walkFile|module1.thisproc [2]</summary>
            public const int name = 2;
            /// <summary>symbol definition/type: int|string|proc (string,string) [3]</summary>
            public const int type = 3;
            /// <summary>code file path [4]</summary>
            public const int path = 4;
            /// <summary>[5]</summary>
            public const int line = 5;
            /// <summary>[6]</summary>
            public const int col = 6;
            /// <summary>help text [7]</summary>
            public const int help = 7;   // 7
        }

        static NimSuggestProc() {
            // static constructor
        }

        public NimSuggestProc() {
            // instance constructor
        }


        /// <summary>Queries nimsuggest</summary><param name='qtype'>use qtype class constants</param><param name='qline'>code line number</param><param name='qcol'>code column number</param>
        public void Query(string qtype, object qline, object qcol) {

            Microsoft.VisualStudio.TextManager.Interop.IVsTextLines lines;
            NSLangServ.textview_current.GetBuffer(out lines);
            int numlines, column;
            lines.GetLineCount(out numlines);
            lines.GetLengthOfLine(numlines - 1, out column);
            String strbuff;
            NSLangServ.textview_current.GetTextStream(0, 0, numlines - 1, column, out strbuff);
            File.WriteAllText(NSLangServ.filelist[NSLangServ.codefile_path_current], strbuff, new UTF8Encoding(false));

            //string fstr = Path.GetFileNameWithoutExtension(NSLangServ.codefile_path_current);

            if (NSPackage.quickinfo || NSPackage.memberlist) {
                NSPackage.quickinfo=false;
                NSPackage.memberlist=false;
                //NSUtil.SaveIfDirty(NSLangServ.codefile_path_current);
            }

            //if (fstr != filepath_prev) {
            //    Close();
            //    Init();
            //}
            //if (!NSLangServ.filelist.ContainsKey(NSLangServ.codefile_path_current)) {
                

            //    // create or update nimstudio_base.nim, which contains: import openfile1, openfile2, ..
            //    // nimstudio_base.nim is the initial file passed to nimsuggest
            //    filelist.Add(fstr);
            //    string basefile = "import " + string.Join(",", filelist);
            //    StreamWriter sw1 = new StreamWriter(Path.GetDirectoryName(NSLangServ.codefile_path_current) + @"\nimstudio_base.nim");
            //    sw1.WriteLine(basefile);
            //    sw1.Close();
            //}

            //if (!filelist.Contains(fstr)) {
            //    // create or update nimstudio_base.nim, which contains: import openfile1, openfile2, ..
            //    // nimstudio_base.nim is the initial file passed to nimsuggest
            //    filelist.Add(fstr);
            //    string basefile = "import " + string.Join(",", filelist);
            //    StreamWriter sw1 = new StreamWriter(Path.GetDirectoryName(NSLangServ.codefile_path_current) + @"\nimstudio_base.nim");
            //    sw1.WriteLine(basefile);
            //    sw1.Close();
            //}

            if (proc == null) {
                Init();
            }
            conout.Clear();
            sugdct.Clear();
            queryfinished = false;
            //string qstr = qtype + " " + fstr + ".nim:" + qline.ToString() + ":" + qcol.ToString();
            string qstr = 
                String.Format(@"{0} ""{1}"";""{2}"":{3}:{4}", 
                    qtype, // 0
                    NSLangServ.codefile_path_current,  // 1
                    NSLangServ.filelist[NSLangServ.codefile_path_current], // 2
                    qline, // 3
                    qcol // 4
                    );

            // + NSLangServ.codefile_path_current + ".nim\";" + 
            //    NSLangServ.filelist[NSLangServ.codefile_path_current]
             //":" + qline.ToString() + ":" + qcol.ToString();

            //string qstr = qtype + " \"" + NSLangServ.codefile_path_current + ".nim\";" + 
            //    NSLangServ.filelist[NSLangServ.codefile_path_current]
            // ":" + qline.ToString() + ":" + qcol.ToString();

            
            NSUtil.DebugPrintAlways("NimStudio - query:" + qstr);
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
                string[] sugsplit = cstr.Split(new char[] { '\t' });
                if (sugsplit.Length < 2) continue;
                if (sugsplit[Sugspl.kind] == "skProc" || sugsplit[Sugspl.kind] == "skVar") {

                    if (qtype == Qtype.def) {
                        if (sugsplit[Sugspl.kind] == "skVar")
                            sugsplit[Sugspl.type] = "(" + sugsplit[Sugspl.type] + ")";
                        sugsplit[Sugspl.type] = Regex.Replace(sugsplit[Sugspl.type], @"{.*?}", "");
                        sugsplit[Sugspl.name] = sugsplit[Sugspl.name].Replace(fnamestrip, "");
                        sugsplit[Sugspl.help] = sugsplit[Sugspl.help].Replace(@"\x0D\x0A", "\n");
                        sugsplit[Sugspl.help] = sugsplit[Sugspl.help].Trim(new char[] { '"' });
                        if (sugsplit[Sugspl.help] != "")
                            sugsplit[Sugspl.help] = "\n\n" + sugsplit[Sugspl.help];
                        sugdct.Add(sugsplit[Sugspl.name], new SortedDictionary<string,string>(){ {"kind",sugsplit[Sugspl.kind]}, {"type",sugsplit[Sugspl.type]}, {"help",sugsplit[Sugspl.help]} });

                    } else if (qtype == Qtype.con) {
                        sugsplit[Sugspl.name] = sugsplit[Sugspl.name].Replace(fnamestrip, "");
                        sugsplit[Sugspl.type] = Regex.Replace(sugsplit[Sugspl.type], @"{.*?}", "");
                        sugsplit[Sugspl.help] = sugsplit[Sugspl.help].Replace(@"\x0D\x0A", "\n");
                        sugsplit[Sugspl.help] = sugsplit[Sugspl.help].Trim(new char[] { '"' });
                        if (sugsplit[Sugspl.help] != "")
                            sugsplit[Sugspl.help] = "\n\n" + sugsplit[Sugspl.help];
                        if (sugdct.ContainsKey(sugsplit[Sugspl.type])) {
                            Debug.Print("Error");
                        } else {
                            sugdct.Add(sugsplit[Sugspl.type], new SortedDictionary<string, string>() 
                                { { "kind", sugsplit[Sugspl.kind] }, { "name", sugsplit[Sugspl.name] }, { "help", sugsplit[Sugspl.help] } });
                        }
                    } else {
                        sugsplit[Sugspl.name] = sugsplit[Sugspl.name].Replace(fnamestrip, "");
                        sugsplit[Sugspl.type] = Regex.Replace(sugsplit[Sugspl.type], @"{.*?}", "");
                        sugsplit[Sugspl.help] = sugsplit[Sugspl.help].Replace(@"\x0D\x0A", "\n");
                        sugsplit[Sugspl.help] = sugsplit[Sugspl.help].Trim(new char[] { '"' });
                        if (sugsplit[Sugspl.help] != "")
                            sugsplit[Sugspl.help] = "\n\n" + sugsplit[Sugspl.help];
                        if (sugdct.ContainsKey(sugsplit[Sugspl.name])) {
                            if (sugsplit[Sugspl.kind] != "skVar" && sugdct[sugsplit[Sugspl.name]]["type"] != sugsplit[Sugspl.type])
                                // add overload
                                sugdct[sugsplit[Sugspl.name]]["type"] += "\n" + sugsplit[Sugspl.type];
                        } else {
                            sugdct.Add(sugsplit[Sugspl.name], new SortedDictionary<string, string>() 
                                { { "kind", sugsplit[Sugspl.kind] }, { "type", sugsplit[Sugspl.type] }, { "help", sugsplit[Sugspl.help] } });
                        }
                    }

                }
            }
            NSUtil.DebugPrint("NimStudio - suggs count:" + sugdct.Count.ToString());

        }

        //public void conwriteold(string str) {
        //    conout.Clear();
        //    sugs.Clear();
        //    queryfinished=false;
        //    proc.StandardInput.WriteLine(str);
        //    int waitcount=0;
        //    while (!queryfinished) {
        //        if (proc == null || proc.HasExited) break;
        //        Thread.Sleep(50);
        //        waitcount++;
        //        if (waitcount > 100) // 5 second wait cap
        //            break;
        //    }
        //    queryfinished = true;
        //    if (proc == null || proc.HasExited) {
        //        proc=null;
        //        filepath_prev="";
        //        return;
        //    }
        //    foreach (string qstr in conout) {
        //        string[] qwords = qstr.Split(new char[] { '\t' });
        //        if (qwords.Length < 2) break;
        //        if (qwords[1] == "skProc" || qwords[1] == "skVar") {
        //            //new List<string>(new string[]{"G","H","I"})
        //            qwords[2] = qwords[2].Replace("htmlarc.","");
        //            qwords[7] = qwords[7].Replace(@"\x0D\x0A", "\n");
        //            qwords[7] = qwords[7].Trim(new char[]{'"'});
        //            if (qwords[7] != "")
        //                qwords[7] = "\n\n" + qwords[7];
        //            sugs.Add(new List<string>(new string[] { qwords[2], qwords[3] + qwords[7] }));
        //        }
        //    }
        //    sugs.Add(new List<string>(new string[] { "func1", "func1 help" }));
        //    NSUtil.DebugPrint("NimStudio - suggs count:" + sugs.Count.ToString());
        //}

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
