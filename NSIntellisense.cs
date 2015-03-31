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

    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("NimStudio Intellisense Controller")]
    [ContentType(NSConst.LangName), Order]
    internal class NSIntellisenseControllerProvider: IIntellisenseControllerProvider {

        [Import] internal ISignatureHelpBroker _signaturehelpbroker = null; // set via MEF
        [Import] internal ITextDocumentFactoryService _textdocumentfactoryservice { get; set; }
        [Import] internal IEditorOperationsFactoryService _editoperationsfactory = null; // Set via MEF
        [Import] internal ICompletionBroker _completionbroker = null; // Set via MEF
        [Import] internal IVsEditorAdaptersFactoryService _adaptersfactory { get; set; }
        [Import] internal System.IServiceProvider _serviceprovider;
        [Import] internal IQuickInfoBroker _quickinfobroker = null; // Set via MEF

        public NSIntellisenseControllerProvider(System.IServiceProvider serviceProvider) {
            _serviceprovider = serviceProvider;
            //PythonService = serviceProvider.GetPythonToolsService();
        }

        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers) {
            //ITextDocument textDocument;
            NSIntellisenseController nsiscontroller;
            if (!textView.Properties.TryGetProperty<NSIntellisenseController>(typeof(NSIntellisenseController), out nsiscontroller)) {
                nsiscontroller = new NSIntellisenseController(this, textView);
            }

            return nsiscontroller;
        }

        internal NSIntellisenseController GetOrCreateController(System.IServiceProvider serviceProvider, IComponentModel model, ITextView textView ) {
            NSIntellisenseController controller;
            if (!textView.Properties.TryGetProperty<NSIntellisenseController>(typeof(NSIntellisenseController), out controller)) {
                //var intellisenseControllerProvider = (
                //   from export in model.DefaultExportProvider.GetExports<IIntellisenseControllerProvider>()
                //   from exportedContentType in export.Metadata.ContentTypes
                //   where exportedContentType == PythonCoreConstants.ContentType && export.Value.GetType() == typeof(IntellisenseControllerProvider)
                //   select export.Value
                //).First();
                controller = new NSIntellisenseController(this, textView);
            }
            return controller;
        }


    }

    internal class NSIntellisenseController: IIntellisenseController, IOleCommandTarget {
        readonly ITextView _textview;
        public IVsTextView _ivstextview;
        readonly ITextDocument _textdocument;
        readonly NSIntellisenseControllerProvider _nsicprovider;
        private readonly System.IServiceProvider _serviceprovider;
        private ICompletionSession _session_completion;
        private ISignatureHelpSession _session_sighelp;
        private IQuickInfoSession _session_quickinfo;
        public IOleCommandTarget m_commandhandler_next;
        private IEditorOperations _editops;
        ISignatureHelpSession session;

        public NSIntellisenseController(NSIntellisenseControllerProvider nsicprovider, ITextView textview) {
        //public NSIntellisenseController(NSIntellisenseControllerProvider nsicprovider, ITextView textview, System.IServiceProvider servprovider) {
            _nsicprovider = nsicprovider;
            _serviceprovider = nsicprovider._serviceprovider;
            _textview = textview;
            _editops = nsicprovider._editoperationsfactory.GetEditorOperations(textview);
            _ivstextview = nsicprovider._adaptersfactory.GetViewAdapter(textview);
            nsicprovider._textdocumentfactoryservice.TryGetTextDocument(_textview.TextDataModel.DocumentBuffer, out _textdocument);
            _ivstextview.AddCommandFilter(this, out m_commandhandler_next);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
            return m_commandhandler_next.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
            if (VsShellUtilities.IsInAutomationFunction(_serviceprovider)) {
                return m_commandhandler_next.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
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
                        NSPackage.memberlist = true;
                        break;
                    case VSConstants.VSStd2KCmdID.PARAMINFO:
                        Debug.Print("PARAMINFO");
                        break;
                    case VSConstants.VSStd2KCmdID.QUICKINFO:
                        //Debug.Print("QUICKINFO");
                        NSPackage.quickinfo = true;
                        break;
                    default:
                        //Debug.Print("Cancel");
                        break;
                }
            }

            // check for a commit character 
            if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN || nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB || (char.IsWhiteSpace(typedChar) || char.IsPunctuation(typedChar))) {
                if (_session_completion != null && !_session_completion.IsDismissed) {
                    // if selection is fully selected, commit
                    if (_session_completion.SelectedCompletionSet.SelectionStatus.IsSelected) {
                        _session_completion.Commit();
                        // don't add to the buffer 
                        return VSConstants.S_OK;
                    } else {
                        // no selection, dismiss
                        _session_completion.Dismiss();
                    }
                }
            }

            int retVal = m_commandhandler_next.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            bool handled = false;
            if ((VSConstants.VSStd2KCmdID)nCmdID == VSConstants.VSStd2KCmdID.SHOWMEMBERLIST ||
                    (!typedChar.Equals(char.MinValue) && char.IsLetterOrDigit(typedChar))) {
                if (_session_completion == null || _session_completion.IsDismissed) {
                    this.TriggerCompletion();
                    if (_session_completion == null) {
                        handled = false;
                        Debug.Print("NimStudio - Completion session not created.");
                    } else {
                        Debug.Print("NimStudio - Completion session created. Tot:" + _session_completion.CompletionSets.Count.ToString());
                        _session_completion.Filter();
                        handled = true;
                    }
                } else {
                    _session_completion.Filter(); // session active
                    handled = true;
                }
            } else if (commandID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE || commandID == (uint)VSConstants.VSStd2KCmdID.DELETE) {
                // redo the filter if there is a deletion
                if (_session_completion != null && !_session_completion.IsDismissed)
                    _session_completion.Filter();
                handled = true;
            }
            if (handled) return VSConstants.S_OK;
            return retVal;

        }

        private bool TriggerCompletion() {
            // test caret is in a projection location
            SnapshotPoint? caretPoint = _textview.Caret.Position.Point.GetPoint(textBuffer => (!textBuffer.ContentType.IsOfType("projection")), PositionAffinity.Predecessor);
            CaretPosition curPosition = _textview.Caret.Position;
            var curTrackPoint = _textview.TextSnapshot.CreateTrackingPoint(curPosition.BufferPosition.Position, Microsoft.VisualStudio.Text.PointTrackingMode.Positive);
            if (!caretPoint.HasValue)
                return false;
            //m_session = m_provider.CompletionBroker.CreateCompletionSession(m_textView, caretPoint.Value.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive), true);
            _session_completion = _nsicprovider._completionbroker.CreateCompletionSession(_textview, caretPoint.Value.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive), false);
            _session_completion.Dismissed += this.OnSessionDismissed;  //subscribe to dismissed event
            _session_completion.Start();
            return true;
        }

        private void OnSessionDismissed(object sender, EventArgs e) {
            _session_completion.Dismissed -= this.OnSessionDismissed;
            _session_completion = null;
            Debug.Print("NimStudio - Completion session dismissed.");
        }

        void OnDocumentDirtyStateChanged(object sender, EventArgs e) {
            //if (!textDocument.IsDirty) {
                //TriggerSignatureHelp();
            //}
        }

        void TriggerSignatureHelp() {
            var point = _textview.Caret.Position.BufferPosition;
            var triggerPoint = point.Snapshot.CreateTrackingPoint(point.Position, PointTrackingMode.Positive);
            if (!_nsicprovider._signaturehelpbroker.IsSignatureHelpActive(_textview)) {
                //textView.Properties.AddProperty(NSSigSource.SessionKey, null);
                session = _nsicprovider._signaturehelpbroker.TriggerSignatureHelp(_textview, triggerPoint, true);
                //textView.Properties.RemoveProperty(NSSigSource.SessionKey);
            }
        }

        public void Detach(ITextView tview) {
            if (_textview == tview) {
                _textdocument.DirtyStateChanged -= OnDocumentDirtyStateChanged;
            }
        }

        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer) {
        }

        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer) {
        }
    }

    [Export(typeof(IVsTextViewCreationListener))]
    [Name("NimStudio TextView Listener")]
    [ContentType(NSConst.LangName)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    class NSTextViewListener: IVsTextViewCreationListener {
        internal readonly IVsEditorAdaptersFactoryService _adaptersFactory;

        [ImportingConstructor]
        public NSTextViewListener(IVsEditorAdaptersFactoryService adaptersFactory) {
            _adaptersFactory = adaptersFactory;
        }

        public void VsTextViewCreated(IVsTextView textViewAdapter) {

            var textView = _adaptersFactory.GetWpfTextView(textViewAdapter);
            NSIntellisenseControllerProvider nsicp = null;
            NSIntellisenseController controller = new NSIntellisenseController(nsicp, textView);
            //bool created = textView.Properties.TryGetProperty<NSIntellisenseController>(typeof(NSIntellisenseController), out controller);
            //var completionController = textView.Properties.GetProperty<NSIntellisenseController>(typeof(NSIntellisenseController));

            //NSIntellisenseController filter = new NSIntellisenseController(view, CompletionBroker);

            IOleCommandTarget next;
            var retval = textViewAdapter.AddCommandFilter(controller, out next);
            controller.m_commandhandler_next = next;


            //Func<NSIntellisenseController> createCommandHandler = delegate() {
            //    return new NSIntellisenseController(NSIntellisenseControllerProvider, textView, textViewAdapter, this);
            //};
            //textView.Properties.GetOrCreateSingletonProperty(createCommandHandler);

            //Func<RustCompletionCommandHandler> createCommandHandler =
            //    () => new RustCompletionCommandHandler(textViewAdapter, textView, CompletionBroker);
            //textView.Properties.GetOrCreateSingletonProperty(createCommandHandler);

            //NSIntellisenseController nsic = new NSIntellisenseController()
            NSUtil.DebugPrintAlways("Created {0}", retval);
            NSUtil.DebugPrintAlways("Created {0}", retval);
            NSUtil.DebugPrintAlways("Created {0}", retval);
            //var textView = _adaptersFactory.GetWpfTextView(textViewAdapter);

            //Func<NSIntellisenseController> createCommandHandler = delegate() {
            //    return new NSIntellisenseController(textView, textViewAdapter, this);
            //};
            //textView.Properties.GetOrCreateSingletonProperty(createCommandHandler);
            //NSIntellisenseController controller = new NSIntellisenseController(textView, textViewAdapter, this);
            //controller._ivstextview = textViewAdapter;
            //bool tvcontrolleradded = textView.Properties.TryGetProperty<NSIntellisenseController>(typeof(NSIntellisenseController), out controller);
            //NSUtil.DebugPrintAlways("textview controller added {0}", tvcontrolleradded);
            //if (textView.Properties.TryGetProperty<NSIntellisenseController>(typeof(NSIntellisenseController), out controller)) {
            //    controller.AttachKeyboardFilter();
            //}
        }

    }


}
