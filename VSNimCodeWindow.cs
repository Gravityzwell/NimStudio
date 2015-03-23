using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudioTools;
using Microsoft.VisualStudio.Utilities;
using IServiceProvider = System.IServiceProvider;

namespace NimStudio.NimStudio {
    class VSNimCodeWindow: IVsCodeWindowManager, IVsCodeWindowEvents {

        private readonly IServiceProvider _serviceProvider;
        private readonly IVsCodeWindow _window;
        private readonly ITextBuffer _textBuffer;
        private IWpfTextView _curView;
        private static readonly HashSet<VSNimCodeWindow> _windows = new HashSet<VSNimCodeWindow>();
        private uint _cookieVsCodeWindowEvents;
        private static IVsEditorAdaptersFactoryService _vsEditorAdaptersFactoryService = null;

        public VSNimCodeWindow(IServiceProvider serviceProvider, IVsCodeWindow codeWindow, IWpfTextView textView) {
            _serviceProvider = serviceProvider;
            _window = codeWindow;
            _textBuffer = textView.TextBuffer;
        }

        public int AddAdornments() {
            _windows.Add(this);

            IVsTextView textView;

            if (ErrorHandler.Succeeded(_window.GetPrimaryView(out textView))) {
                ((IVsCodeWindowEvents)this).OnNewView(textView);
            }

            if (ErrorHandler.Succeeded(_window.GetSecondaryView(out textView))) {
                ((IVsCodeWindowEvents)this).OnNewView(textView);
            }

            return VSConstants.S_OK;
        }

        private int AddDropDownBar() {
            return VSConstants.S_OK;
        }

        private int RemoveDropDownBar() {
            return VSConstants.S_OK;
        }

        public int OnNewView(IVsTextView pView) {
            return VSConstants.S_OK;
        }

        public int RemoveAdornments() {
            return VSConstants.S_OK;
        }

        public static void ToggleNavigationBar(bool fEnable) {
        }

        int IVsCodeWindowEvents.OnNewView(IVsTextView vsTextView) {
            var wpfTextView = VsEditorAdaptersFactoryService.GetWpfTextView(vsTextView);
            if (wpfTextView != null) {
                var factory = ComponentModel.GetService<IEditorOperationsFactoryService>();
                var editFilter = new EditFilter(wpfTextView, factory.GetEditorOperations(wpfTextView), ServiceProvider.GlobalProvider);
                editFilter.AttachKeyboardFilter(vsTextView);
                wpfTextView.GotAggregateFocus += OnTextViewGotAggregateFocus;
                wpfTextView.LostAggregateFocus += OnTextViewLostAggregateFocus;
            }
            return VSConstants.S_OK;
        }

        int IVsCodeWindowEvents.OnCloseView(IVsTextView vsTextView) {
            var wpfTextView = VsEditorAdaptersFactoryService.GetWpfTextView(vsTextView);
            if (wpfTextView != null) {
                wpfTextView.GotAggregateFocus -= OnTextViewGotAggregateFocus;
                wpfTextView.LostAggregateFocus -= OnTextViewLostAggregateFocus;
            }
            return VSConstants.S_OK;
        }

        private void OnTextViewGotAggregateFocus(object sender, EventArgs e) {
            var wpfTextView = sender as IWpfTextView;
            if (wpfTextView != null) {
                _curView = wpfTextView;
            }
        }

        private void OnTextViewLostAggregateFocus(object sender, EventArgs e) {
            var wpfTextView = sender as IWpfTextView;
            if (wpfTextView != null) {
                _curView = null;
            }
        }

        private IComponentModel ComponentModel {
            get {
                return (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));
            }
        }

        private IVsEditorAdaptersFactoryService VsEditorAdaptersFactoryService {
            get {
                if (_vsEditorAdaptersFactoryService == null) {
                    _vsEditorAdaptersFactoryService = ComponentModel.GetService<IVsEditorAdaptersFactoryService>();
                }
                return _vsEditorAdaptersFactoryService;
            }
        }

    }

   internal sealed class EditFilter : IOleCommandTarget {
        private readonly ITextView _textView;
        private readonly IEditorOperations _editorOps;
        private readonly IServiceProvider _serviceProvider;
        private readonly IComponentModel _componentModel;
        private IOleCommandTarget _next;

        public EditFilter(ITextView textView, IEditorOperations editorOps, IServiceProvider serviceProvider) {
            _textView = textView;
            _textView.Properties[typeof(EditFilter)] = this;
            _editorOps = editorOps;
            _serviceProvider = serviceProvider;
            //_componentModel = _serviceProvider.GetComponentModel();

            //BraceMatcher.WatchBraceHighlights(textView, _componentModel);
        }

        internal void AttachKeyboardFilter(IVsTextView vsTextView) {
            if (_next == null) {
                ErrorHandler.ThrowOnFailure(vsTextView.AddCommandFilter(this, out _next));
            }
        }

        private int GotoDefinition() {

            return VSConstants.S_OK;
        }

        private void GotoLocation() {

        }

        private IVsTextView GetViewAdapter() {
            var adapterFactory = _componentModel.GetService<IVsEditorAdaptersFactoryService>();
            var viewAdapter = adapterFactory.GetViewAdapter(_textView);
            return viewAdapter;
        }

        private int FindAllReferences() {

            return VSConstants.S_OK;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
            // preprocessing
            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97) {
                switch ((VSConstants.VSStd97CmdID)nCmdID) {
                    case VSConstants.VSStd97CmdID.Paste:
                    case VSConstants.VSStd97CmdID.GotoDefn: return GotoDefinition();
                    case VSConstants.VSStd97CmdID.FindReferences: return FindAllReferences();
                }
            };
            return _next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        private void ExtractMethod() {
        }

        private void FormatCode(SnapshotSpan span, bool selectResult) {
        }

        internal void RefactorRename() {
        }

        private const uint CommandDisabledAndHidden = (uint)(OLECMDF.OLECMDF_INVISIBLE | OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_DEFHIDEONCTXTMENU);

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97) {
                for (int i = 0; i < cCmds; i++) {
                    switch ((VSConstants.VSStd97CmdID)prgCmds[i].cmdID) {
                        case VSConstants.VSStd97CmdID.GotoDefn:
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                            return VSConstants.S_OK;
                        case VSConstants.VSStd97CmdID.FindReferences:
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                            return VSConstants.S_OK;
                    }
                }
            } 
            return _next.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        private void QueryStatusExtractMethod(OLECMD[] prgCmds, int i) {
            var activeView = CommonPackage.GetActiveTextView(_serviceProvider);

        }

        private void QueryStatusRename(OLECMD[] prgCmds, int i) {
            IWpfTextView activeView = CommonPackage.GetActiveTextView(_serviceProvider);
        }

        internal void DoIdle(IOleComponentManager compMgr) {
        }

    }

}
