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
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;


namespace NimStudio.NimStudio {

    /*  ====================================
            TEXTVIEW CREATION LISTENER
        ==================================== */
    [Export(typeof(IVsTextViewCreationListener))]
    [Name("NimStudio TextViewCreation Listener")]
    [ContentType(NSConst.LangName)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class NSTextViewCreationListener: IVsTextViewCreationListener {
        //[Import] internal ISignatureHelpBroker _signaturehelpbroker = null;
        //[Import] internal ITextDocumentFactoryService _textdocumentfactoryservice = null;
        //[Import] internal IEditorOperationsFactoryService _editoperationsfactory = null;
        //[Import] internal ICompletionBroker _completionbroker = null;
        [Import] internal IVsEditorAdaptersFactoryService _adaptersfactory = null;
        [Import] internal SVsServiceProvider _serviceprovider_vs = null;
        //[Import] internal IQuickInfoBroker _quickinfobroker = null;
        //[Import] internal NSIntellisenseControllerProvider nsicp = null;

        public void VsTextViewCreated(IVsTextView textviewadapter) {

            ITextView textview = _adaptersfactory.GetWpfTextView(textviewadapter);
            if (textview == null) {
                return;
            }
            //NSIntellisenseControllerProvider nsicp = null;
            //Func<NSIntellisenseController> createCommandHandler =
            //    () => new NSIntellisenseController(nsicp, textview );
            //textview.Properties.GetOrCreateSingletonProperty(createCommandHandler);
            NSIntellisenseController controller;
            if (textview.Properties.TryGetProperty<NSIntellisenseController>(typeof(NSIntellisenseController), out controller)) {
                controller.AttachKeyboardFilter();
            };
            //textview.Properties.GetOrCreateSingletonProperty(() =>new NSIntellisenseController(nsicp(
            //    _serviceprovider_vs
            //    ), textview));

            //NSIntellisenseController nsintillisensectrl = new NSIntellisenseController(new NSIntellisenseControllerProvider(_serviceprovider_vs), textview);
            //IOleCommandTarget next;
            //textviewadapter.AddCommandFilter(nsintillisensectrl, out next);
            //nsintillisensectrl.m_commandhandler_next = next;

        }
    }


    /*  =======================================
            INTELLISENSE CONTROLLER PROVIDER
        ======================================= */
    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("NimStudio Intellisense Controller")]
    [ContentType(NSConst.LangName)]
    internal class NSIntellisenseControllerProvider: IIntellisenseControllerProvider  {

        internal ISignatureHelpBroker _signaturehelpbroker = null;
        //internal ITextDocumentFactoryService _textdocumentfactoryservice = null;
        internal IEditorOperationsFactoryService _editoperationsfactory = null;
        internal ICompletionBroker _completionbroker = null;
        internal IVsEditorAdaptersFactoryService _adaptersfactory = null;
        internal IQuickInfoBroker _quickinfobroker = null;
        //internal System.IServiceProvider _serviceprovider_sys = null;
        internal SVsServiceProvider _serviceprovider_vs = null;
        internal IEditorOperations _editops = null;

        [ImportingConstructor]
        public NSIntellisenseControllerProvider(
            [Import(typeof(SVsServiceProvider))]SVsServiceProvider spvs, 
            ICompletionBroker icb, 
            IQuickInfoBroker iqib, 
            ISignatureHelpBroker ish,
            IVsEditorAdaptersFactoryService ieafs,
            IEditorOperationsFactoryService ieofs
            ) {
                _serviceprovider_vs = spvs;
                //_serviceprovider_sys = NSLangServ._serviceprovider_sys;
                //_serviceprovider_sys = serviceProvider;
                _editoperationsfactory = ieofs;
                _signaturehelpbroker = ish;
                //_textdocumentfactoryservice = itdfs;
                _completionbroker = icb;
                _adaptersfactory = ieafs;
                _quickinfobroker = iqib;
        }

        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers) {
            NSIntellisenseController nsiscontroller;
            if (!textView.Properties.TryGetProperty<NSIntellisenseController>(typeof(NSIntellisenseController), out nsiscontroller)) {
                nsiscontroller = new NSIntellisenseController(this, textView);
            }
            return nsiscontroller;
        }

        //internal NSIntellisenseController GetOrCreateController(System.IServiceProvider serviceProvider, IComponentModel model, ITextView textView ) {
        //    NSIntellisenseController controller;
        //    if (!textView.Properties.TryGetProperty<NSIntellisenseController>(typeof(NSIntellisenseController), out controller)) {
        //        //var intellisenseControllerProvider = (
        //        //   from export in model.DefaultExportProvider.GetExports<IIntellisenseControllerProvider>()
        //        //   from exportedContentType in export.Metadata.ContentTypes
        //        //   where exportedContentType == PythonCoreConstants.ContentType && export.Value.GetType() == typeof(IntellisenseControllerProvider)
        //        //   select export.Value
        //        //).First();
        //        controller = new NSIntellisenseController(this, textView);
        //    }
        //    return controller;
        //}

    }

    /*  ====================================
            INTELLISENSE CONTROLLER
        ==================================== */
    internal class NSIntellisenseController: IIntellisenseController, IOleCommandTarget {
        readonly ITextView _textview;
        public IVsTextView _ivstextview;
        private ITextDocument _textdocument;
        private NSIntellisenseControllerProvider _nsicprovider;
        //private readonly System.IServiceProvider _serviceprovider;
        private SVsServiceProvider _serviceprovider;
        //private readonly Microsoft.VisualStudio.OLE.Interop.IServiceProvider _serviceprovider;
        internal ICompletionSession _session_completion;
        private ISignatureHelpSession _session_sighelp;
        private IQuickInfoSession _session_quickinfo;
        public IOleCommandTarget m_commandhandler_next;
        internal IEditorOperationsFactoryService _editoperationsfactory;
        private IEditorOperations _editops;
        private ISignatureHelpSession session;

        public NSIntellisenseController(NSIntellisenseControllerProvider nsicprovider, ITextView textview) {
            _nsicprovider = nsicprovider;
            _serviceprovider = nsicprovider._serviceprovider_vs;
            _textview = textview;
            _ivstextview = _nsicprovider._adaptersfactory.GetViewAdapter(_textview);
            _editops = nsicprovider._editoperationsfactory.GetEditorOperations(textview);
            //nsicprovider._textdocumentfactoryservice.TryGetTextDocument(_textview.TextDataModel.DocumentBuffer, out _textdocument);
            _textview.Properties.AddProperty(typeof(NSIntellisenseController), this);  
            //_ivstextview.AddCommandFilter(this, out m_commandhandler_next);
        }

       internal void AttachKeyboardFilter() {
            if (m_commandhandler_next == null) {
                if (_ivstextview != null) {
                    ErrorHandler.ThrowOnFailure(_ivstextview.AddCommandFilter(this, out m_commandhandler_next));
                }
            }
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
            return m_commandhandler_next.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
            //if (VsShellUtilities.IsInAutomationFunction(_nsicprovider._serviceprovider_sys)) {
            //    return m_commandhandler_next.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            //}
            uint commandID = nCmdID;
            char typedChar = char.MinValue;
            // test input is a char
            if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR) {
                typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            }
            if (typedChar != char.MinValue)
                Debug.Print("EXEC {0}", typedChar);
            else
                Debug.Print("EXEC");

            if (pguidCmdGroup == VSConstants.VSStd2K) {
                switch ((VSConstants.VSStd2KCmdID)nCmdID) {
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                        Debug.Print("AUTOCOMPLETE");
                        break;
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                        Debug.Print("COMPLETEWORD");
                        break;
                    case VSConstants.VSStd2KCmdID.RETURN:
                        Debug.Print("RETURN");
                        break;
                    case VSConstants.VSStd2KCmdID.TAB:
                        Debug.Print("TAB");
                        break;
                    case VSConstants.VSStd2KCmdID.CANCEL:
                        Debug.Print("CANCEL");
                        break;
                    case VSConstants.VSStd2KCmdID.SHOWMEMBERLIST:
                        Debug.Print("SHOWMEMBERLIST");
                        NSPackage.memberlist = true;
                        break;
                    case VSConstants.VSStd2KCmdID.PARAMINFO:
                        Debug.Print("PARAMINFO");
                        break;
                    case VSConstants.VSStd2KCmdID.QUICKINFO:
                        Debug.Print("QUICKINFO");
                        NSPackage.quickinfo = true;
                        break;
                    default:
                        break;
                }
            }

            // completion commit
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
            //if ((VSConstants.VSStd2KCmdID)nCmdID == VSConstants.VSStd2KCmdID.SHOWMEMBERLIST || (!typedChar.Equals(char.MinValue) && char.IsLetterOrDigit(typedChar))) {
            if ((VSConstants.VSStd2KCmdID)nCmdID == VSConstants.VSStd2KCmdID.SHOWMEMBERLIST) {
                if (_session_completion == null || _session_completion.IsDismissed) {
                    this.CompletionTrigger();
                    if (_session_completion == null) {
                        Debug.Print("NimStudio - Completion session not created.");
                        return retVal;
                    } else {
                        Debug.Print("NimStudio - Completion session created. Tot:" + _session_completion.CompletionSets.Count.ToString());
                        _session_completion.Filter();
                        return VSConstants.S_OK;
                    }
                } else {
                    _session_completion.Filter(); // session active
                    return VSConstants.S_OK;
                }
            } 

            if ((VSConstants.VSStd2KCmdID)nCmdID == VSConstants.VSStd2KCmdID.PARAMINFO) {
                if (_session_sighelp == null || _session_sighelp.IsDismissed) {
                    this.SigHelpTrigger();
                    if (_session_sighelp == null) {
                        Debug.Print("NS - _session_sighelp not created.");
                        return retVal;
                    } else {
                        Debug.Print("NS - _session_sighelp created.");
                        return VSConstants.S_OK;
                    }
                } else {
                    _session_sighelp.Recalculate(); // session active
                    return VSConstants.S_OK;
                }
            } 

            if (commandID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE || commandID == (uint)VSConstants.VSStd2KCmdID.DELETE) {
                if (_session_completion != null && !_session_completion.IsDismissed) {
                    // update completion
                    _session_completion.Filter();
                    return VSConstants.S_OK;
                }
            }

            return retVal;

        }

        private bool SigHelpTrigger() {

            if (_session_completion != null)
                _session_completion.Dismiss();

            SnapshotPoint? caretPoint = _textview.Caret.Position.Point.GetPoint(textBuffer => (!textBuffer.ContentType.IsOfType("projection")), PositionAffinity.Predecessor);
            CaretPosition curPosition = _textview.Caret.Position;
            var curTrackPoint = _textview.TextSnapshot.CreateTrackingPoint(curPosition.BufferPosition.Position, Microsoft.VisualStudio.Text.PointTrackingMode.Positive);
            if (!caretPoint.HasValue)
                return false;
            _session_sighelp = _nsicprovider._signaturehelpbroker.CreateSignatureHelpSession(_textview, caretPoint.Value.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive), false);
            _session_sighelp.Dismissed += SigHelpDismiss;
            _session_sighelp.Start();
            Debug.Print("NS - SigHelpTrigger");
            return true;
        }

        private void SigHelpDismiss(object sender, EventArgs e) {
            if (_session_sighelp != null) {
                _session_sighelp.Dismissed -= CompletionDismiss;
                _session_sighelp = null;
                Debug.Print("NS - SigHelpDismiss");
            }
        }

        private bool CompletionTrigger() {
            if (_session_sighelp != null)
                _session_sighelp.Dismiss();

            if (_session_completion != null)
                _session_completion.Dismiss();

            SnapshotPoint? caretPoint = _textview.Caret.Position.Point.GetPoint(textBuffer => (!textBuffer.ContentType.IsOfType("projection")), PositionAffinity.Predecessor);
            CaretPosition curPosition = _textview.Caret.Position;
            var curTrackPoint = _textview.TextSnapshot.CreateTrackingPoint(curPosition.BufferPosition.Position, Microsoft.VisualStudio.Text.PointTrackingMode.Positive);
            if (!caretPoint.HasValue)
                return false;
            _session_completion = _nsicprovider._completionbroker.CreateCompletionSession(_textview, caretPoint.Value.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive), false);
            _session_completion.Dismissed += CompletionDismiss;
            _session_completion.Committed += CompletionDismiss;
            _session_completion.Start();
            Debug.Print("NS - CompletionTrigger");
            return true;
        }

        private void CompletionDismiss(object sender, EventArgs e) {
            if (_session_completion != null) {
                _session_completion.Dismissed -= CompletionDismiss;
                _session_completion.Committed -= CompletionDismiss;
                _session_completion = null;
                Debug.Print("NS - CompletionDismiss");
            }
        }

        void OnDocumentDirtyStateChanged(object sender, EventArgs e) {
            //if (!textDocument.IsDirty) {
                //TriggerSignatureHelp();
            //}
        }

        void TriggerSignatureHelp() {
            //var point = _textview.Caret.Position.BufferPosition;
            //var triggerPoint = point.Snapshot.CreateTrackingPoint(point.Position, PointTrackingMode.Positive);
            //if (!_nsicprovider._signaturehelpbroker.IsSignatureHelpActive(_textview)) {
            //    //textView.Properties.AddProperty(NSSigSource.SessionKey, null);
            //    session = _nsicprovider._signaturehelpbroker.TriggerSignatureHelp(_textview, triggerPoint, true);
            //    //textView.Properties.RemoveProperty(NSSigSource.SessionKey);
            //}
            if (_session_completion != null)
                _session_completion.Dismiss();

            if (_session_sighelp != null)
                _session_sighelp.Dismiss();

            //_session_completion = _nsicprovider._completionbroker.CreateCompletionSession(_textview, caretPoint.Value.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive), false);

            _session_sighelp = _nsicprovider._signaturehelpbroker.TriggerSignatureHelp(_textview);

            //if (_sigHelpSession != null) {
            //    _sigHelpSession.Dismiss();
            //}

            //_sigHelpSession = SignatureBroker.TriggerSignatureHelp(_textView);

            //if (_sigHelpSession != null) {
            //    _sigHelpSession.Dismissed += OnSignatureSessionDismissed;

            //    ISignature sig;
            //    if (_sigHelpSession.Properties.TryGetProperty(typeof(PythonSignature), out sig)) {
            //        _sigHelpSession.SelectedSignature = sig;

            //        IParameter param;
            //        if (_sigHelpSession.Properties.TryGetProperty(typeof(PythonParameter), out param)) {
            //            ((PythonSignature)sig).SetCurrentParameter(param);
            //        }
            //    }
            //}

        }

        /*
        internal ICompletionBroker CompletionBroker {
            get {
                return _nsicprovider._completionbroker;
            }
        }

        internal IVsEditorAdaptersFactoryService AdaptersFactory {
            get {
                return _nsicprovider._adaptersfactory;
            }
        }

        internal ISignatureHelpBroker SignatureBroker {
            get {
                return _nsicprovider._signaturehelpbroker;
            }
        }
        */

        public void Detach(ITextView textView) {
            if (_textview == null) {
                throw new InvalidOperationException("Already detached from text view");
            }
            if (textView != _textview) {
                throw new ArgumentException("Not attached to specified text view", "textView");
            }

            //_textview.MouseHover -= _textview;
            _textview.Properties.RemoveProperty(typeof(NSIntellisenseController));

            if (m_commandhandler_next != null) {
                ErrorHandler.ThrowOnFailure(_ivstextview.RemoveCommandFilter(this));
                m_commandhandler_next = null;
            }
            //_bufferParser = null;
            //_textdocument.DirtyStateChanged -= OnDocumentDirtyStateChanged;
        }

        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer) {
        }

        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer) {
        }
    }


}
