using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using dcto = System.Collections.Generic.Dictionary<string, object>;

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
        private Dictionary<int,IVsColorableItem> m_colorable_items = new Dictionary<int,IVsColorableItem>();


        public NSLangServ():base() {
            m_colorable_items.Add((int)TokenColor.Comment, new ColorableItem("Comment", COLORINDEX.CI_DARKGREEN, COLORINDEX.CI_USERTEXT_BK, false, false));
            m_colorable_items.Add((int)TokenColor.Identifier, new ColorableItem("Identifier", COLORINDEX.CI_SYSPLAINTEXT_FG, COLORINDEX.CI_USERTEXT_BK, false, false));
            m_colorable_items.Add((int)TokenColor.Keyword,  new ColorableItem("Keyword",  COLORINDEX.CI_BLUE, COLORINDEX.CI_USERTEXT_BK,  false, false));
            m_colorable_items.Add((int)TokenColor.Number,  new ColorableItem("Number", COLORINDEX.CI_USERTEXT_FG, COLORINDEX.CI_USERTEXT_BK,  false, false));
            m_colorable_items.Add((int)TokenColor.String,  new ColorableItem("String", COLORINDEX.CI_BROWN, COLORINDEX.CI_USERTEXT_BK,  false, false));
            //m_colorable_items.Add((int)TokenColor.Text,  new ColorableItem("Text",  COLORINDEX.CI_SYSPLAINTEXT_FG, COLORINDEX.CI_USERTEXT_BK,  false, false)); 
            m_colorable_items.Add((int)TokenColor.Number+1,  new ColorableItem("NimLang Procedure",  COLORINDEX.CI_RED, COLORINDEX.CI_USERTEXT_BK,  false, false)); 
            m_colorable_items.Add((int)TokenColor.Number+2,  new ColorableItem("NimLang DataType",  COLORINDEX.CI_BROWN, COLORINDEX.CI_USERTEXT_BK,  false, false)); 
            m_colorable_items.Add((int)TokenColor.Number+3,  new ColorableItem("NimLang Punctuation",  COLORINDEX.CI_SYSPLAINTEXT_FG, COLORINDEX.CI_USERTEXT_BK,  false, false)); 
        }

        public override void Initialize() {
            _serviceprovider_sys = this.Site;
            IVsFontAndColorCacheManager mgr = this.GetService(typeof(SVsFontAndColorCacheManager)) as IVsFontAndColorCacheManager;
            mgr.ClearAllCaches();
        }

        public override string GetFormatFilterList() {
            return "Nim file(*.nim)";
        }

        public override LanguagePreferences GetLanguagePreferences() {
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

        //public override CodeWindowManager CreateCodeWindowManager(IVsCodeWindow codewindow, Source source) {
        //    NSCodeWindow nscw = new NSCodeWindow(codewindow, source);
        //    return nscw;
        //}

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
            return;
            //CodeWindowManager cwm = GetCodeWindowManagerForSource(source);
            //if (cwm != null) {
            //    IVsTextView tv;
            //    cwm.CodeWindow.GetLastActiveView(out tv);
            //    if (tv != null)
            //        tv.CloseView();
            //}
            //source.Close();
            //base.OnCloseSource(source);
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
            NSUtil.DebugPrintAlways("OnActiveViewChanged");
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
                base.OnActiveViewChanged(textview);
            }
        }

        public override Source CreateSource(IVsTextLines buffer) {       
            return new NSSource(this, buffer, GetColorizer(buffer));
        }


        public override Colorizer GetColorizer(IVsTextLines buffer) {
            NSColorizer colorizer;
            colorizer = new NSColorizer(this, buffer, (NSScanner)GetScanner(buffer));
            return colorizer;
        }

        public override int GetItemCount(out int count) {
            count = m_colorable_items.Count;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public override int GetColorableItem(int index, out Microsoft.VisualStudio.TextManager.Interop.IVsColorableItem item) {
            item = m_colorable_items[index];
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public override AuthoringScope ParseSource(ParseRequest req) {
            NSUtil.DebugPrintAlways("ParseSource:" + req.Reason);
            return new NSAuthoringScope(req, req.FileName, "");
            //return req.Scope;
        }

        public override string Name {
            get { return NSConst.LangName; }
        }

        public static dcto CaretPosGet() {
            int caretline=0, caretcol=0;
            if (textview_current != null) { 
                textview_current.GetCaretPos(out caretline, out caretcol);
                caretline++;
            }
            var retval = new dcto(StringComparer.OrdinalIgnoreCase){ {"col",caretcol}, {"line",caretline} };
            return retval;
        }

        public class ColorableItem: Microsoft.VisualStudio.TextManager.Interop.IVsColorableItem {
            private string displayName;
            private COLORINDEX background;
            private COLORINDEX foreground;
            private uint fontFlags = (uint)FONTFLAGS.FF_DEFAULT;

            public ColorableItem(string displayName, COLORINDEX foreground, COLORINDEX background, bool bold, bool strikethrough) {
                this.displayName = displayName;
                this.background = background;
                this.foreground = foreground;
                if (bold)
                    this.fontFlags = this.fontFlags | (uint)FONTFLAGS.FF_BOLD;
                if (strikethrough)
                    this.fontFlags = this.fontFlags | (uint)FONTFLAGS.FF_STRIKETHROUGH;
            }

            public int GetDefaultColors(COLORINDEX[] piForeground, COLORINDEX[] piBackground) {
                piForeground[0] = foreground;
                piBackground[0] = background;
                return Microsoft.VisualStudio.VSConstants.S_OK;
            }

            public int GetDefaultFontFlags(out uint pdwFontFlags) {
                pdwFontFlags = this.fontFlags;
                return Microsoft.VisualStudio.VSConstants.S_OK;
            }

            public int GetDisplayName(out string pbstrName) {
                pbstrName = displayName;
                return Microsoft.VisualStudio.VSConstants.S_OK;
            }
        }
    }

}
