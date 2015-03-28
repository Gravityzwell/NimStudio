using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace NimStudio.NimStudio {

    [Export(typeof(IQuickInfoSourceProvider)), ContentType(NSConst.LangName), Order, Name("NimStudio Quick Info Source")]
    class QuickInfoSourceProvider: IQuickInfoSourceProvider {
        internal readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public QuickInfoSourceProvider([Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
        }

        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer) {
            return new QuickInfoSource(this, textBuffer);
        }
    }

    internal class QuickInfoSource: IQuickInfoSource {
        private readonly ITextBuffer _textBuffer;
        private readonly QuickInfoSourceProvider _provider;
        private IQuickInfoSession _curSession;

        public QuickInfoSource(QuickInfoSourceProvider provider, ITextBuffer textBuffer) {
            _textBuffer = textBuffer;
            _provider = provider;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan) {
            if (_curSession != null && !_curSession.IsDismissed) {
                _curSession.Dismiss();
                _curSession = null;
            }

            SnapshotPoint currentPoint = (session.TextView.Caret.Position.BufferPosition) - 1;
            var startpoint = session.GetTriggerPoint(session.TextView.TextBuffer).GetPosition(currentPoint.Snapshot);
            applicableToSpan = currentPoint.Snapshot.CreateTrackingSpan(startpoint, 0, SpanTrackingMode.EdgeInclusive);

            int caretline, caretcol;
            NSLangServ.textview_current.GetCaretPos(out caretline, out caretcol);
            caretline++;
            if (caretline == 0 && caretcol == 0) {
                applicableToSpan = null;
                return;
            } 

            NSPackage.nimsuggest.Query(NimSuggestProc.qtype_enum.def, caretline, caretcol);

            _curSession = session;
            _curSession.Dismissed += CurSessionDismissed;

            //List<List<string>> defs = new List<List<string>>();
            string qinfostr = "";
            foreach (List<string> def in NSPackage.nimsuggest.sugs) {
                if (def[0] == "skVar") {
                    qinfostr = def[1] + " " + def[2];
                } else if (def[0] == "skProc") {
                    qinfostr = def[1] + "\n" + def[2] + def[3];
                }
                //qinfostr = def[0] + " " + def[1] + def[2] + "-" + caretline.ToString() + ":" + caretcol.ToString();
                quickInfoContent.Add(qinfostr);
            }

        }

        private void CurSessionDismissed(object sender, EventArgs e) {
            _curSession = null;
        }


        public void Dispose() {
        }

    }

}
