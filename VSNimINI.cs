using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Collections;

namespace NimStudio.NimStudio {

    public class VSNimINI {
        public SortedDictionary<string, string> inidct = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private string inifilepath;

        public VSNimINI(string inipath) {
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

        public string Get(string inisect, string inikey) {
            string dkey = inisect + "|" + inikey;
            if (inidct.ContainsKey(dkey))
                return inidct[dkey];
            else
                return "";
        }

        // return the section as list of arraylist of inikeys+inivalues
        public ArrayList Get(string inisect) {
            ArrayList arrlst = new ArrayList();
            foreach (string dkey in inidct.Keys) {
                string[] sectkeyarr = dkey.Split(new char[] { '|' }, 2);
                if (sectkeyarr[0].ToUpper() == inisect.ToUpper())
                    arrlst.Add(new[] { sectkeyarr[1], inidct[dkey] });
            }
            return arrlst;
        }

        public void Add(string inisect, string inikey, string settingValue) {
            string dkey = inisect + "|" + inikey;
            if (inidct.ContainsKey(dkey))
                inidct.Remove(dkey);
            inidct.Add(dkey, settingValue);
        }

        public void Add(string inisect, string inikey) {
            Add(inisect, inikey, "");
        }

        public void Delete(string inisect, string inikey) {
            string dkey = inisect + "|" + inikey;
            if (inidct.ContainsKey(dkey))
                inidct.Remove(dkey);
        }

        public void Write(string inifilepathnew) {
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

        public void Write() {
            Write(inifilepath);
        }
    }
}

