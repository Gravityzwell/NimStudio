using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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









    internal class NSCompletionSource: ICompletionSource {

        private NSCompletionSourceProvider m_sourceProvider;
        private ITextBuffer m_textBuffer;
        private List<Completion> m_compList;
        private bool m_isDisposed;
        private IGlyphService m_glyphservice;
        private static Dictionary<string, BitmapImage> m_glyphdct = new Dictionary<string, BitmapImage>();


        // IVsTextView textViewAdapter
        // textView  = adapterFactory.CreateVsTextViewAdapter(GetService(typeof(IOleServiceProvider)) as IOleServiceProvider);
        public NSCompletionSource(NSCompletionSourceProvider sourceProvider, ITextBuffer textBuffer, IGlyphService glyphService){ 
            m_sourceProvider = sourceProvider;
            m_textBuffer = textBuffer;
            m_glyphservice = glyphService;
            GlyphAdd("int");
            GlyphAdd("float");
            GlyphAdd("string");
        }

        private void GlyphAdd(string glyph) {
            if (!m_glyphdct.ContainsKey(glyph)) { 
                m_glyphdct.Add(glyph, new BitmapImage());
                m_glyphdct[glyph].BeginInit();
                m_glyphdct[glyph].UriSource = new Uri("pack://application:,,,/NimStudio;component/Resources/glyph-" + glyph + ".png");
                m_glyphdct[glyph].EndInit();
            }
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

            int caretlineline2, caretcol2;
            NSLangServ.textview_current.GetCaretPos(out caretlineline2, out caretcol2);
            caretlineline2++;
            int caretline = NSCompletionCommandHandler.caretline;
            int caretcol = NSCompletionCommandHandler.caretcol;
            //textview.GetCaretPos(out line, out idx);

            //String nimsugcmd = String.Format("sug htmlarc.nim:{0}:{1}",caretline,caretcol);
            //NimStudioPackage.nimsuggest.conwrite(nimsugcmd);
            NSPackage.nimsuggest.Query(NimSuggestProc.qtype_enum.sug, caretline, caretcol);

            m_compList = new List<Completion>();

            foreach (string skey in NSPackage.nimsuggest.sugdct.Keys) {
                var suglst = NSPackage.nimsuggest.sugdct[skey];
                if (m_glyphdct.ContainsKey(suglst[1]))
                    m_compList.Add(new Completion(skey, skey, suglst[1] + suglst[2], m_glyphdct[suglst[1]], "icon text"));
                else
                    m_compList.Add(new Completion(skey, skey, suglst[1] + suglst[2], null, "icon text"));
            }


            //foreach (List<string> sugglst in NSPackage.nimsuggest.sugs) {
            //    //m_compList.Add(new Completion(str[0], str[0], str[1], m_glyphservice.GetGlyph(StandardGlyphGroup.GlyphGroupConstant, StandardGlyphItem.GlyphItemPublic), "icon text"));
            //    if (m_glyphdct.ContainsKey(sugglst[1]))
            //        m_compList.Add(new Completion(sugglst[0], sugglst[0], sugglst[1] + sugglst[2], m_glyphdct[sugglst[1]], "icon text"));
            //    else
            //        m_compList.Add(new Completion(sugglst[0], sugglst[0], sugglst[1] + sugglst[2], null, "icon text"));
            //}

            SnapshotPoint currentPoint = (session.TextView.Caret.Position.BufferPosition) - 1;
            ITextStructureNavigator navigator = m_sourceProvider.NavigatorService.GetTextStructureNavigator(m_textBuffer);
            TextExtent extent = navigator.GetExtentOfWord(currentPoint);
            string w1 = currentPoint.Snapshot.GetText(extent.Span);
            var startpoint = session.GetTriggerPoint(session.TextView.TextBuffer).GetPosition(currentPoint.Snapshot);

            ITrackingSpan applicableTo;
            if (w1==".")
                applicableTo = currentPoint.Snapshot.CreateTrackingSpan(startpoint, 0, SpanTrackingMode.EdgeInclusive);
            else
                applicableTo = currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);


            //var applicableTo = m_textBuffer.CurrentSnapshot.CreateTrackingSpan(session.GetTriggerPoint(session.TextView.TextBuffer).GetPosition(currentPoint.Snapshot), 0, SpanTrackingMode.EdgeInclusive);

            //completionSets.Add(new CompletionSet("Tokens", "Tokens", applicableTo, m_compList, null));
            completionSets.Add(new CompletionSet("Tokens", "Tokens", applicableTo, m_compList, null));
            //completionSets.Add(new CompletionSet(m_compList));

            //completionSets.Add(new CompletionSet("Tokens", "Tokens", FindTokenSpanAtPosition(session.GetTriggerPoint(m_textBuffer), session), m_compList, null));
        }

        private enum TokenType {
            Struct,
            Module,
            Function,
            Crate,
            Let,
            StructField,
            Impl,
            Enum,
            EnumVariant,
            Type,
            FnArg,
            Trait,
            Static,
        }

        private ImageSource GetCompletionIcon(TokenType elType) {
            switch (elType) {
                case TokenType.Struct:
                    return m_glyphservice.GetGlyph(StandardGlyphGroup.GlyphGroupStruct, StandardGlyphItem.GlyphItemPublic);
                case TokenType.Module:
                    return m_glyphservice.GetGlyph(StandardGlyphGroup.GlyphAssembly, StandardGlyphItem.GlyphItemPublic);
                case TokenType.Function:
                    return m_glyphservice.GetGlyph(StandardGlyphGroup.GlyphExtensionMethod, StandardGlyphItem.GlyphItemPublic);
                case TokenType.Crate:
                    return m_glyphservice.GetGlyph(StandardGlyphGroup.GlyphAssembly, StandardGlyphItem.GlyphItemPublic);
                case TokenType.Let:
                    return m_glyphservice.GetGlyph(StandardGlyphGroup.GlyphGroupConstant, StandardGlyphItem.GlyphItemPublic);
                case TokenType.StructField:
                    return m_glyphservice.GetGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPublic);
                case TokenType.Impl:
                    return m_glyphservice.GetGlyph(StandardGlyphGroup.GlyphGroupTypedef, StandardGlyphItem.GlyphItemPublic);
                case TokenType.Enum:
                    return m_glyphservice.GetGlyph(StandardGlyphGroup.GlyphGroupEnum, StandardGlyphItem.GlyphItemPublic);
                case TokenType.EnumVariant:
                    return m_glyphservice.GetGlyph(StandardGlyphGroup.GlyphGroupEnumMember, StandardGlyphItem.GlyphItemPublic);
                case TokenType.Type:
                    return m_glyphservice.GetGlyph(StandardGlyphGroup.GlyphGroupTypedef, StandardGlyphItem.GlyphItemPublic);
                case TokenType.Trait:
                    return m_glyphservice.GetGlyph(StandardGlyphGroup.GlyphGroupInterface, StandardGlyphItem.GlyphItemPublic);
                case TokenType.Static:
                    return null;
                case TokenType.FnArg:
                    return null;
                default:
                    NSUtil.DebugPrint("Completion type not found: {0}", elType);
                    return null;
            }
        }



        public void Dispose() {
            if (!m_isDisposed) {
                GC.SuppressFinalize(this);
                m_isDisposed = true;
            }
        }
    }

    [Export(typeof(ICompletionSourceProvider))]
    [ContentType(NSConst.LangName)]
    [Name("nim completion")]
    internal class NSCompletionSourceProvider: ICompletionSourceProvider {
        [Import] internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }
        [Import] private IGlyphService GlyphService { get; set; }

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer) {
            return new NSCompletionSource(this, textBuffer, GlyphService);
        }
    }

    [Export(typeof(IVsTextViewCreationListener))]
    [Name("nim completion handler")]
    [ContentType(NSConst.LangName)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class NSCompletionHandlerProvider: IVsTextViewCreationListener {
        [Import] internal IVsEditorAdaptersFactoryService AdapterService = null;
        [Import] internal ICompletionBroker CompletionBroker { get; set; }
        [Import] internal SVsServiceProvider ServiceProvider { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter) {
            ITextView textView = AdapterService.GetWpfTextView(textViewAdapter);
            if (textView == null)
                return;

            Func<NSCompletionCommandHandler> createCommandHandler = delegate() { 
                return new NSCompletionCommandHandler(textViewAdapter, textView, this);
            };
            textView.Properties.GetOrCreateSingletonProperty(createCommandHandler);
        }

    }

    internal class NSCompletionCommandHandler: IOleCommandTarget {
        private IOleCommandTarget m_nextCommandHandler;
        private ITextView m_textView;
        private IVsTextView m_textViewAdapter;
        private NSCompletionHandlerProvider m_provider;
        private ICompletionSession m_session;
        public static int caretline=0;
        public static int caretcol=0;

        internal NSCompletionCommandHandler(IVsTextView textViewAdapter, ITextView textView, NSCompletionHandlerProvider provider) {
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
                    case VSConstants.VSStd2KCmdID.QUICKINFO:
                        Debug.Print("QUICKINFO");
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
