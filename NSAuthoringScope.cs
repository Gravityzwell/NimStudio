using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;

namespace NimStudio.NimStudio {
    public class NSAuthoringScope : AuthoringScope, IDisposable {
        private string m_filename;
        private string m_dirtyname;
        private string m_projectfile;
        private AuthoringSink m_sink;
        bool disposed = false;

        public NSAuthoringScope(AuthoringSink sink, string filename, string dirtyname, string projectfile) : base() {
            m_sink = sink;
            m_filename = filename;
            m_dirtyname = dirtyname;
            m_projectfile = projectfile;
        }

        public NSAuthoringScope(ParseRequest req, string dirtyFile, string project): base() {
            m_sink = req.Sink;
            m_projectfile = project;
            m_dirtyname = dirtyFile;
            m_filename = req.FileName;
        }

        public override string GetDataTipText(int line, int col, out TextSpan span) {
            span = new TextSpan();

            string qinfostr = "";
            if (NSPackage.quickinfo == true) {
                NSPackage.quickinfo=false;
                span.iStartLine = line - 1;
                span.iEndLine = line;
                span.iStartIndex = col - 1;
                span.iEndIndex = col + 1;

                NSPackage.nimsuggest.Query(NimSuggestProc.Qtype.def, line+1, col);

                foreach (string dkey in NSPackage.nimsuggest.sugdct.Keys) {
                    var def = NSPackage.nimsuggest.sugdct[dkey];
                    if (def["kind"] == "skVar") {
                        qinfostr += dkey + " " + def["type"] + " " + def["help"];
                    } else if (def["kind"] == "skProc") {
                        qinfostr += def["type"] + "\n" + def["type"] + def["help"];
                    }
                }

                //foreach (SortedDictionary<string, string> def in NSPackage.nimsuggest.sugdct.Values) {
                //    if (def["kind"] == "skVar") {
                //        qinfostr += def["type"] + " " + def["help"];
                //    } else if (def["kind"] == "skProc") {
                //        qinfostr += def["type"] + "\n" + def["type"] + def["help"];
                //    }
                //}
            }
            //System.Diagnostics.Debug.Print("GetDataTipNext");
            return qinfostr;
        }


        public override Declarations GetDeclarations(IVsTextView view, int line, int col, TokenInfo info, ParseReason reason) {
            return null;
        }

        public override Methods GetMethods(int line, int col, string name) {
            return null;
        }

        public override string Goto(Microsoft.VisualStudio.VSConstants.VSStd97CmdID cmd, IVsTextView textView, int line, int col, out TextSpan span) {
            span = new TextSpan();
            switch (cmd) {
            case VSConstants.VSStd97CmdID.GotoDefn:
                //var def = idetoolsfuncs.GetDef(m_filename, line, col, m_projectfile);
                //if (def.type == symTypes.none) {
                //    break;
                //} else {
                //    span = new TextSpan();
                //    span.iStartLine = def.line - 1;
                //    span.iEndIndex = def.col;
                //    span.iStartIndex = 0;
                //    span.iEndLine = def.line - 1;
                //    return def.filePath;
                //}
            default:
                break;
            }
            return null;
        }

        public void Dispose() {
        }
    }
}
