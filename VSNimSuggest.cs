using System;
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
        public Process proc = null;
        public List<string> conout = new List<string>();
        //public List<string> sugs = new List<string>();
        public List<List<string>> sugs = new List<List<string>>();
        public Thread thread = null;
        public bool queryfinished = false;

        static NimSuggestProc() {
            // static constructor
        }

        public NimSuggestProc() {
            // instance constructor
        }

        public void conwrite(string str) {
            conout.Clear();
            sugs.Clear();
            queryfinished=false;
            proc.StandardInput.WriteLine(str);
            int waitcount=0;
            while (!queryfinished) {
                Thread.Sleep(50);
                waitcount++;
                if (waitcount > 100) { 
                    // 5 second wait cap
                    queryfinished=true;
                    break;
                }
            }
            foreach (string qstr in conout) {
                string[] qwords = qstr.Split(new char[] { '\t' });
                //List<string> qwords = new List<string>(qstr.Split(new Char[] { '\t' }));
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

        public void quit() {
            proc.StandardInput.WriteLine("quit");
        }

        public void init() {
            proc = new Process();
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.WorkingDirectory = @"c:\MyProgs\Nim";
            proc.StartInfo.Arguments = @"--stdin c:\MyProgs\Nim\htmlarc.nim";
            proc.StartInfo.FileName = @"c:\MyProgs\Nim\bin\nimsuggest.exe";
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.UseShellExecute = false;

            proc.EnableRaisingEvents = true;
            proc.OutputDataReceived += new DataReceivedEventHandler(DRHandler);
            proc.ErrorDataReceived += new DataReceivedEventHandler(DRHandler);
            proc.Start();
            proc.BeginOutputReadLine();
            proc.StandardInput.WriteLine("");
            Thread.Sleep(300); // allow time for nimsuggest startup lines to be processed
        }

        private void DRHandler(object sender, DataReceivedEventArgs e) {
            string line = e.Data;
            if (line == "")
                queryfinished=true;
            else {
                conout.Add(line);
            }
        }

    }

}
