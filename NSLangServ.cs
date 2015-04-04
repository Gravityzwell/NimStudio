using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace NimStudio.NimStudio {

    public class NSLangServ: LanguageService {
        private NSScanner m_scanner;
        private LanguagePreferences languageprefs;
        public static IVsTextView textview_current;
        public static string codefile_path_current;
        public static System.IServiceProvider _serviceprovider_sys;
        public static SortedDictionary<string, string> filelist = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public override string GetFormatFilterList() {
            return "Nim file(*.nim)";
        }

        public override LanguagePreferences GetLanguagePreferences() {
            _serviceprovider_sys = this.Site; // getting this here as it doesn't work in constructor
            //IVsShell shell = _serviceprovider_sys.GetService(typeof(SVsShell)) as IVsShell;
            //if (shell == null) {
            //    throw new InvalidOperationException();
            //}

            if (languageprefs == null) {
                languageprefs = new LanguagePreferences(this.Site, typeof(NSLangServ).GUID, this.Name);
                if (this.languageprefs != null)
                    this.languageprefs.Init();
                languageprefs.ParameterInformation = true;
                languageprefs.EnableQuickInfo = true;
            }
            return this.languageprefs;
        }

        public override IScanner GetScanner(IVsTextLines buffer) {
            if (m_scanner == null)
                m_scanner = new NSScanner(buffer);
            return m_scanner;
        }

        public override void OnCloseSource(Source source) {
            string codefile_path = source.GetFilePath();
            if (filelist.ContainsKey(codefile_path)) {
                string dirty_file = filelist[codefile_path];
                filelist.Remove(codefile_path);
                if (File.Exists(dirty_file)) {
                    try {
                        File.Delete(dirty_file);
                    } catch (Exception ex) {
                        NSUtil.DebugPrintAlways("Error deleting temp file:" + ex.Message);
                    }
                }
                NimBaseFileCreate();
            }
            base.OnCloseSource(source);
        }

        private void NimBaseFileCreate() {
            string basefile = "import ";
            foreach (string fstr in filelist.Keys) {
                basefile += Path.GetFileNameWithoutExtension(fstr) + ",";
            }
            basefile = basefile.TrimEnd(new char[] {','});
            StreamWriter sw1 = new StreamWriter(Path.GetDirectoryName(NSLangServ.codefile_path_current) + @"\nimstudio_base.nim");
            sw1.WriteLine(basefile);
            sw1.Close();
        }

        public override void OnActiveViewChanged(IVsTextView textview) {
            // also called for new textviews
            if (textview != null) {
                textview_current = textview;
                var source = GetSource(textview);
                codefile_path_current = source.GetFilePath();
                NSUtil.DebugPrintAlways("OnActiveViewChanged:"+codefile_path_current);
                if (!filelist.ContainsKey(codefile_path_current)) {
                    //string fstr = Path.GetFileNameWithoutExtension(codefile_path_current);
                    string fdirtystr = Path.GetTempFileName();
                    filelist.Add(codefile_path_current,fdirtystr);
                    NimBaseFileCreate();
                }
            }
            base.OnActiveViewChanged(textview);
        }

        public override string Name {
            get { return NSConst.LangName; }
        }

        public override AuthoringScope ParseSource(ParseRequest req) {
            //var c1 = 1;
            return new NSAuthoringScope(req, "", "");
            //return req.Scope;
        }

    }

}
