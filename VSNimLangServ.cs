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

    public class VSNimLangServ: LanguageService {
        private VSNimScanner m_scanner;
        private LanguagePreferences languageprefs;

        public override string GetFormatFilterList() {
            return "Nim file(*.nim)";
        }

        public override LanguagePreferences GetLanguagePreferences() {
            if (languageprefs == null) {
                languageprefs = new LanguagePreferences(this.Site, typeof(VSNimLangServ).GUID, this.Name);
                if (this.languageprefs != null)
                    this.languageprefs.Init();
                this.languageprefs.ParameterInformation = true;
            }
            return this.languageprefs;
        }

        public override IScanner GetScanner(IVsTextLines buffer) {
            if (m_scanner == null)
                m_scanner = new VSNimScanner(buffer);
            return m_scanner;
        }


        void UpdateViewInfo(IVsTextView textView, int line, int col) {
            if (textView != null) {
                var source = GetSource(textView);
                if (source != null) {
                    if (line >= 0 && col >= 0 || !ErrorHandler.Failed(textView.GetCaretPos(out line, out col)))
                        System.Diagnostics.Debug.Write("Activated : " + source.GetFilePath());
                        //source.GetEngine().SetTextCursorLocation(source.FileIndex, line + 1, col + 1);
                }
            }
        }

        public override void OnActiveViewChanged(IVsTextView textView) {
            UpdateViewInfo(textView, -1, -1);
            base.OnActiveViewChanged(textView);
        }


        public override string Name {
            get { return VSNConst.LangName; }
        }

        public override AuthoringScope ParseSource(ParseRequest req) {
            var c1 = 1;
            return req.Scope;
        }

    }

}
