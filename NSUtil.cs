using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using Microsoft.VisualStudio.Shell;



namespace NimStudio.NimStudio {

    class NSUtil {
        public static bool debug=false;

        public static void NimExeFind(NSPackage pkg) {
            string[] nimexes = { "nim.exe", "nimsuggest.exe" };

            var patharr = new List<string>((Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'));
            patharr.Add(@"c:\myprogs\nim\bin");
            foreach (string nimexe in nimexes) {
                foreach (string spathi in patharr) {
                    if (NSIni.Get("Main", nimexe) == "") {
                        string sfullpath = Path.Combine(spathi.Trim(), nimexe);
                        if (File.Exists(sfullpath)) {
                            NSIni.Add("Main", nimexe, Path.GetFullPath(sfullpath));
                            NSIni.Write();
                            pkg.GetType().GetProperty(nimexe.Replace(".", "")).SetValue(nimexe.Replace(".", ""), sfullpath);
                            break;
                        }
                    }
                }
            }
        }

        public static void DebugPrintAlways(string debugmsg, params object[] args) {
                System.Diagnostics.Debug.Print(string.Format(debugmsg, args));
        }

        public static void DebugPrint(string debugmsg, params object[] args) {
            if (debug)
                System.Diagnostics.Debug.Print(string.Format(debugmsg, args));
        }

        //public void SaveIfDirty2(IVsTextView view, int line, int col) {
        //    string text;
        //    int numLines;
        //    int lastCol;
        //    IVsTextLines buf = null;
        //    var hr = view.GetBuffer(out buf);
        //    Marshal.ThrowExceptionForHR(hr);
        //    hr = buf.GetLineCount(out numLines);
        //    Marshal.ThrowExceptionForHR(hr);
        //    hr = buf.GetLengthOfLine(numLines - 1, out lastCol);
        //    Marshal.ThrowExceptionForHR(hr);
        //    hr = buf.GetLineText(0, 0, numLines - 1, lastCol, out text);
        //    Marshal.ThrowExceptionForHR(hr);
        //    File.WriteAllText(m_dirtyname, text, new UTF8Encoding(false));
        //    var reply = idetoolsfuncs.GetDirtySuggestions(m_dirtyname, m_filename, line + 1, col + 1, m_projectfile);
        //    decls = new IntelliSense.NimrodDeclarations(reply);
        //}


        public static bool SaveIfDirty(string fpath) {
            bool dirty=false;

            Microsoft.VisualStudio.Shell.Interop.IVsPersistDocData persistDocData;
            Microsoft.VisualStudio.Shell.Interop.IVsHierarchy hierarchy;
            uint vsitemid = Microsoft.VisualStudio.VSConstants.VSITEMID_NIL;
            uint doccook;
            VsShellUtilities.GetRDTDocumentInfo(ServiceProvider.GlobalProvider, fpath, out hierarchy, out vsitemid, out persistDocData, out doccook);
            if (hierarchy == null || doccook == (uint)Microsoft.VisualStudio.Shell.Interop.Constants.VSDOCCOOKIE_NIL) {
                dirty = false;
                return dirty;
            }
            if (persistDocData != null) {
                int docdirty;
                persistDocData.IsDocDataDirty(out docdirty);
                dirty = (docdirty != 0);
                if (!dirty)
                    return false;
            }

            Microsoft.VisualStudio.Shell.VsShellUtilities.SaveFileIfDirty(ServiceProvider.GlobalProvider, fpath);
            System.Diagnostics.Debug.Print(string.Format("NS saved file {0}", fpath));
            return dirty;

        }


        //public static bool SaveDirtyFiles() {
        //    var rdt = ServiceProvider.GlobalProvider.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
        //    if (rdt != null) {
        //        // Consider using (uint)(__VSRDTSAVEOPTIONS.RDTSAVEOPT_SaveIfDirty | __VSRDTSAVEOPTIONS.RDTSAVEOPT_PromptSave)
        //        // when VS settings include prompt for save on build
        //        var saveOpt = (uint)__VSRDTSAVEOPTIONS.RDTSAVEOPT_SaveIfDirty;
        //        var hr = rdt.SaveDocuments(saveOpt, null, VSConstants.VSITEMID_NIL, VSConstants.VSCOOKIE_NIL);
        //        if (hr == VSConstants.E_ABORT) {
        //            return false;
        //        }
        //    }

        //    return true;
        //}

    }



    public class NSIni {
        private static SortedDictionary<string, string> inidct = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public static string inifilepath;

        public static void Init(string inipath) {
            inidct.Clear();
            TextReader srfile = null;
            string linestr = null;
            string inisectcurrent = "MAIN";
            string[] keyvalarr = null;
            inifilepath=inipath;
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
