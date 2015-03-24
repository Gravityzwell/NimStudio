﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Windows;
//using System.Windows.Media;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Utilities;

namespace NimStudio.NimStudio {


    //internal static class VSNContentType {
    //    [Export, Name("nim"), BaseDefinition("text")]
    //    public static ContentTypeDefinition NimContentType = null;

    //    [Export, FileExtension(".nim"), ContentType("nim")]
    //    public static FileExtensionToContentTypeDefinition NimFileType = null;
    //}

    //[Export(typeof(IClassifierProvider))]
    //[ContentType("nim")]
    //[Name("NimSyntaxProvider")]
    //internal sealed class MyLangSyntaxProvider: IClassifierProvider {
    //    [Import]
    //    internal IClassificationTypeRegistryService ClassificationRegistry = null;

    //    public IClassifier GetClassifier(ITextBuffer buffer) {
    //        return buffer.Properties.GetOrCreateSingletonProperty(() => new VSNLangSyntax(ClassificationRegistry, buffer));
    //    }
    //}

    //internal sealed class VSNLangSyntax: IClassifier {
    //    private ITextBuffer buffer;
    //    private IClassificationType identifierType;
    //    private IClassificationType keywordType;

    //    public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

    //    internal VSNLangSyntax(IClassificationTypeRegistryService registry, ITextBuffer buffer) {
    //        this.identifierType = registry.GetClassificationType(PredefinedClassificationTypeNames.Identifier);
    //        this.keywordType = registry.GetClassificationType(PredefinedClassificationTypeNames.Keyword);
    //        this.buffer = buffer;
    //        this.buffer.Changed += OnBufferChanged;
    //    }

    //    public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan snapshotSpan) {
    //        var classifications = new List<ClassificationSpan>();
    //        string text = snapshotSpan.GetText();
    //        var span = new SnapshotSpan(snapshotSpan.Snapshot, snapshotSpan.Start.Position, text.Length);
    //        classifications.Add(new ClassificationSpan(span, keywordType));
    //        return classifications;
    //    }

    //    private void OnBufferChanged(object sender, TextContentChangedEventArgs e) {
    //        foreach (var change in e.Changes)
    //            ClassificationChanged(this, new ClassificationChangedEventArgs(new SnapshotSpan(e.After, change.NewSpan)));
    //    }
    //}









    internal class VSNCompletionSource: ICompletionSource {

        private VSNCompletionSourceProvider m_sourceProvider;
        private ITextBuffer m_textBuffer;
        private List<Completion> m_compList;
        private bool m_isDisposed;

        // IVsTextView textViewAdapter
        // textView  = adapterFactory.CreateVsTextViewAdapter(GetService(typeof(IOleServiceProvider)) as IOleServiceProvider);
        public VSNCompletionSource(VSNCompletionSourceProvider sourceProvider, ITextBuffer textBuffer){ 
            m_sourceProvider = sourceProvider;
            m_textBuffer = textBuffer;
            //textBuffer.Properties.Pr
        }

        private ITrackingSpan FindTokenSpanAtPosition(ITrackingPoint point, ICompletionSession session) {
            SnapshotPoint currentPoint = (session.TextView.Caret.Position.BufferPosition) - 1;
            ITextStructureNavigator navigator = m_sourceProvider.NavigatorService.GetTextStructureNavigator(m_textBuffer);
            TextExtent extent = navigator.GetExtentOfWord(currentPoint);
            string w1 = currentPoint.Snapshot.GetText(extent.Span);
            return currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
        }

        void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets) {
            List<string> strList = new List<string>();

            // should pull this from VSNimLangServ.textview_current
            int caretlineline2, caretcol2;
            VSNimLangServ.textview_current.GetCaretPos(out caretlineline2, out caretcol2);
            caretlineline2++;
            int caretline = VSNCompletionCommandHandler.caretline;
            int caretcol = VSNCompletionCommandHandler.caretcol;
            //textview.GetCaretPos(out line, out idx);

            //String nimsugcmd = String.Format("sug htmlarc.nim:{0}:{1}",caretline,caretcol);
            //NimStudioPackage.nimsuggest.conwrite(nimsugcmd);
            NimStudioPackage.nimsuggest.Query(NimSuggestProc.qtype_enum.sug, caretline, caretcol);

            //strList.Add("addition");
            //strList.Add("adaptation");
            //strList.Add("subtraction");
            //strList.Add("summation");
            m_compList = new List<Completion>();
            foreach (List<string> str in NimStudioPackage.nimsuggest.sugs) {
                m_compList.Add(new Completion(str[0], str[0], str[1], null, null));
            }

            SnapshotPoint currentPoint = (session.TextView.Caret.Position.BufferPosition) - 1;
            ITextStructureNavigator navigator = m_sourceProvider.NavigatorService.GetTextStructureNavigator(m_textBuffer);
            TextExtent extent = navigator.GetExtentOfWord(currentPoint);
            string w1 = currentPoint.Snapshot.GetText(extent.Span);
            var startpoint = session.GetTriggerPoint(session.TextView.TextBuffer).GetPosition(currentPoint.Snapshot);

            ITrackingSpan applicableTo;
            if (w1=="." || true)
                applicableTo = currentPoint.Snapshot.CreateTrackingSpan(startpoint, 0, SpanTrackingMode.EdgeInclusive);
            else
                applicableTo = currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);


            //var applicableTo = m_textBuffer.CurrentSnapshot.CreateTrackingSpan(session.GetTriggerPoint(session.TextView.TextBuffer).GetPosition(currentPoint.Snapshot), 0, SpanTrackingMode.EdgeInclusive);

            //completionSets.Add(new CompletionSet("Tokens", "Tokens", applicableTo, m_compList, null));
            completionSets.Add(new CompletionSet("Tokens", "Tokens", applicableTo, m_compList, null));
            //completionSets.Add(new CompletionSet(m_compList));

            //completionSets.Add(new CompletionSet("Tokens", "Tokens", FindTokenSpanAtPosition(session.GetTriggerPoint(m_textBuffer), session), m_compList, null));
        }

        public void Dispose() {
            if (!m_isDisposed) {
                GC.SuppressFinalize(this);
                m_isDisposed = true;
            }
        }
    }

    [Export(typeof(ICompletionSourceProvider))]
    [ContentType(VSNConst.LangName)]
    [Name("nim completion")]
    internal class VSNCompletionSourceProvider: ICompletionSourceProvider {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }
        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer) {
            return new VSNCompletionSource(this, textBuffer);
        }
    }

    [Export(typeof(IVsTextViewCreationListener))]
    [Name("nim completion handler")]
    [ContentType(VSNConst.LangName)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class VSNCompletionHandlerProvider: IVsTextViewCreationListener {
        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService = null;
        [Import]
        internal ICompletionBroker CompletionBroker { get; set; }
        [Import]
        internal SVsServiceProvider ServiceProvider { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter) {
            ITextView textView = AdapterService.GetWpfTextView(textViewAdapter);
            if (textView == null)
                return;

            Func<VSNCompletionCommandHandler> createCommandHandler = delegate() { 
                return new VSNCompletionCommandHandler(textViewAdapter, textView, this);
            };
            textView.Properties.GetOrCreateSingletonProperty(createCommandHandler);
        }

    }

    internal class VSNCompletionCommandHandler: IOleCommandTarget {
        private IOleCommandTarget m_nextCommandHandler;
        private ITextView m_textView;
        private IVsTextView m_textViewAdapter;
        private VSNCompletionHandlerProvider m_provider;
        private ICompletionSession m_session;
        public static int caretline=0;
        public static int caretcol=0;

        internal VSNCompletionCommandHandler(IVsTextView textViewAdapter, ITextView textView, VSNCompletionHandlerProvider provider) {
            this.m_textView = textView;
            this.m_provider = provider;
            this.m_textViewAdapter = textViewAdapter;
            //add the command to the command chain
            textViewAdapter.AddCommandFilter(this, out m_nextCommandHandler);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
            return m_nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
            if (VsShellUtilities.IsInAutomationFunction(m_provider.ServiceProvider)) {
                return m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }
            uint commandID = nCmdID;
            char typedChar = char.MinValue;
            // test input is a char
            if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR) {
                typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            }

            if (pguidCmdGroup == VSConstants.VSStd2K) {
                switch ((VSConstants.VSStd2KCmdID)nCmdID) {
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                        //Debug.Print("Cancel");
                        break;
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                        //Debug.Print("Cancel");
                        break;
                    case VSConstants.VSStd2KCmdID.RETURN:
                        //Debug.Print("Cancel");
                        break;
                    case VSConstants.VSStd2KCmdID.TAB:
                        //Debug.Print("Cancel");
                        break;
                    case VSConstants.VSStd2KCmdID.CANCEL:
                        //Debug.Print("Cancel");
                        break;
                    case VSConstants.VSStd2KCmdID.SHOWMEMBERLIST:
                        //Debug.Print("Cancel");
                        break;
                    default:
                        //Debug.Print("Cancel");
                        break;

                }
            }


            // check for a commit character 
            if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN || nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB || (char.IsWhiteSpace(typedChar) || char.IsPunctuation(typedChar))) {
                if (m_session != null && !m_session.IsDismissed) {
                    // if selection is fully selected, commit
                    if (m_session.SelectedCompletionSet.SelectionStatus.IsSelected) {
                        m_session.Commit();
                        // don't add to the buffer 
                        return VSConstants.S_OK;
                    } else {
                        // no selection, dismiss
                        m_session.Dismiss();
                    }
                }
            }

            int retVal = m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut); // add to buffer
            bool handled = false;
            if ( (VSConstants.VSStd2KCmdID)nCmdID == VSConstants.VSStd2KCmdID.SHOWMEMBERLIST || 
                    (!typedChar.Equals(char.MinValue) && char.IsLetterOrDigit(typedChar)) ) {
                if (m_session == null || m_session.IsDismissed) {
                    this.TriggerCompletion();
                    if (m_session == null) {
                        handled = false;
                        Debug.Print("NimStudio - Completion session not created.");
                    } else {
                        Debug.Print("NimStudio - Completion session created. Tot:" + m_session.CompletionSets.Count.ToString());
                        m_session.Filter();
                        handled = true;
                    }
                } else {
                    m_session.Filter(); // session active
                    handled = true;
                }
            } else if (commandID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE || commandID == (uint)VSConstants.VSStd2KCmdID.DELETE) {
                // redo the filter if there is a deletion
                if (m_session != null && !m_session.IsDismissed)
                    m_session.Filter();
                handled = true;
            }
            if (handled) return VSConstants.S_OK;
            return retVal;
        }

        private bool TriggerCompletion() {
            // test caret is in a projection location
            SnapshotPoint? caretPoint = m_textView.Caret.Position.Point.GetPoint(textBuffer => (!textBuffer.ContentType.IsOfType("projection")), PositionAffinity.Predecessor);
            CaretPosition curPosition = m_textView.Caret.Position;
            var curTrackPoint = m_textView.TextSnapshot.CreateTrackingPoint(curPosition.BufferPosition.Position, Microsoft.VisualStudio.Text.PointTrackingMode.Positive);
            //int line, idx;
            m_textViewAdapter.GetCaretPos(out caretline, out caretcol);
            caretline++;
            if (!caretPoint.HasValue)
                return false;
            //m_session = m_provider.CompletionBroker.CreateCompletionSession(m_textView, caretPoint.Value.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive), true);
            m_session = m_provider.CompletionBroker.CreateCompletionSession(m_textView, caretPoint.Value.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive), false);
            m_session.Dismissed += this.OnSessionDismissed;  //subscribe to dismissed event
            m_session.Start();
            return true;
        }

        private void OnSessionDismissed(object sender, EventArgs e) {
            m_session.Dismissed -= this.OnSessionDismissed;
            m_session = null;
            Debug.Print("NimStudio - Completion session dismissed.");
        }
    }
}
