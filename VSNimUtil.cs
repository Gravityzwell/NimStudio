using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Collections;

namespace NimStudio.NimStudio {

    class VSNimUtil {
        public static bool debug=false;

        public static void NimExeFind(NimStudioPackage pkg) {
            string[] nimexes = { "nim.exe", "nimsuggest.exe" };

            var patharr = new List<string>((Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'));
            patharr.Add(@"c:\myprogs\nim\bin");
            foreach (string nimexe in nimexes) {
                foreach (string spathi in patharr) {
                    if (VSNimINI.Get("Main", nimexe) == "") {
                        string sfullpath = Path.Combine(spathi.Trim(), nimexe);
                        if (File.Exists(sfullpath)) {
                            VSNimINI.Add("Main", nimexe, Path.GetFullPath(sfullpath));
                            VSNimINI.Write();
                            pkg.GetType().GetProperty(nimexe.Replace(".", "")).SetValue(nimexe.Replace(".", ""), sfullpath);
                            break;
                        }
                    }
                }
            }
        }

        public static void DebugPrint(string debugmsg, params object[] args) {
            if (debug)
                Console.WriteLine(string.Format(debugmsg, args));
        }

    }

    public class VSNimINI {
        private static SortedDictionary<string, string> inidct = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static string inifilepath;

        public static void Init(string inipath) {
            inidct.Clear();
            TextReader srfile = null;
            string linestr = null;
            string inisectcurrent = "MAIN";
            string[] keyvalarr = null;
            inifilepath = inipath;
            if (File.Exists(inipath)) {
                try {
                    srfile = new StreamReader(inipath);
                    linestr = srfile.ReadLine();
                    while (linestr != null) {
                        linestr = linestr.Trim();
                        if (linestr != "") {
                            if (linestr.StartsWith("[") && linestr.EndsWith("]")) {
                                inisectcurrent = linestr.Substring(1, linestr.Length - 2);
                            } else {
                                keyvalarr = linestr.Split(new char[] { '=' }, 2);
                                if (keyvalarr.Length > 1)
                                    inidct.Add(inisectcurrent + "|" + keyvalarr[0], keyvalarr[1]);
                                else
                                    inidct.Add(inisectcurrent + "|" + keyvalarr[0], "");
                            }
                        }
                        linestr = srfile.ReadLine();
                    }
                } catch (Exception ex) {
                    throw ex;
                } finally {
                    if (srfile != null)
                        srfile.Close();
                }
            }
        }

        public static string Get(string inisect, string inikey) {
            string dkey = inisect + "|" + inikey;
            if (inidct.ContainsKey(dkey))
                return inidct[dkey];
            else
                return "";
        }

        // return the section as list of arraylist of inikeys+inivalues
        public static ArrayList Get(string inisect) {
            ArrayList arrlst = new ArrayList();
            foreach (string dkey in inidct.Keys) {
                string[] sectkeyarr = dkey.Split(new char[] { '|' }, 2);
                if (sectkeyarr[0].ToUpper() == inisect.ToUpper())
                    arrlst.Add(new[] { sectkeyarr[1], inidct[dkey] });
            }
            return arrlst;
        }

        public static void Add(string inisect, string inikey, string settingValue) {
            string dkey = inisect + "|" + inikey;
            if (inidct.ContainsKey(dkey))
                inidct.Remove(dkey);
            inidct.Add(dkey, settingValue);
        }

        public static void Add(string inisect, string inikey) {
            Add(inisect, inikey, "");
        }

        public static void Delete(string inisect, string inikey) {
            string dkey = inisect + "|" + inikey;
            if (inidct.ContainsKey(dkey))
                inidct.Remove(dkey);
        }

        public static void Write(string inifilepathnew) {
            ArrayList sections = new ArrayList();
            string inivalue = "";
            string inifilebuff = "";
            foreach (string dkey in inidct.Keys) {
                string[] sectkeyarr = dkey.Split(new char[] { '|' }, 2);
                if (!sections.Contains(sectkeyarr[0]))
                    sections.Add(sectkeyarr[0]);
            }
            foreach (string inisect in sections) {
                inifilebuff += ("[" + inisect + "]\r\n");
                foreach (string dkey in inidct.Keys) {
                    string[] inisectkeyarr = dkey.Split(new char[] { '|' }, 2);
                    if (inisectkeyarr[0] == inisect) {
                        inivalue = (string)inidct[dkey];
                        inifilebuff += (inisectkeyarr[1] + "=" + inivalue + "\r\n");
                    }
                }
                inifilebuff += "\r\n";
            }

            try {
                TextWriter tw = new StreamWriter(inifilepathnew);
                tw.Write(inifilebuff);
                tw.Close();
            } catch (Exception ex) {
                throw ex;
            }
        }

        public static void Write() {
            Write(inifilepath);
        }
    }
}
