using System;
using System.Collections.Generic;
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


        public override void OnActiveViewChanged(IVsTextView textview) {
            if (textview != null) {
                textview_current = textview;
                var source = GetSource(textview);
                codefile_path_current = source.GetFilePath();
                //if (source != null) {
                //    int line, col;
                //    textview.GetCaretPos(out line, out col);
                //    System.Diagnostics.Debug.Write("Activated : " + source.GetFilePath());
                //}
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
