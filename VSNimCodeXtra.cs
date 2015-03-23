using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*  
    Placeholder for code samples/snippets
    Build Action = None, so uncompileable code will not be a problem
*/

namespace NimStudio.NimStudio {
    class VSNimCodeXtra {

        // Get IVsTextView from filepath
        internal static Microsoft.VisualStudio.TextManager.Interop.IVsTextView IVsTextView_FromFilePath_Get(string filePath) {
            var dte2 = (EnvDTE80.DTE2)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE));
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider sp = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte2;
            Microsoft.VisualStudio.Shell.ServiceProvider serviceProvider = new Microsoft.VisualStudio.Shell.ServiceProvider(sp);
            Microsoft.VisualStudio.Shell.Interop.IVsUIHierarchy uiHierarchy;
            uint itemID;
            Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame windowFrame;
            Microsoft.VisualStudio.Text.Editor.IWpfTextView wpfTextView = null;
            if (Microsoft.VisualStudio.Shell.VsShellUtilities.IsDocumentOpen(serviceProvider, filePath, Guid.Empty,
                                            out uiHierarchy, out itemID, out windowFrame)) {
                return Microsoft.VisualStudio.Shell.VsShellUtilities.GetTextView(windowFrame);
            }
            return null;
        }

        // Get IVsTextView and text in WindowActivated event
        void WindowEvents_WindowActivated(EnvDTE.Window GotFocus, EnvDTE.Window LostFocus) {
            if (GotFocus.Document != null) {
                EnvDTE.Document curDoc = GotFocus.Document;
                System.Diagnostics.Debug.Write("Activated : " + curDoc.FullName);
                Microsoft.VisualStudio.TextManager.Interop.IVsTextView itv = IVsTextView_FromFilePath_Get(curDoc.FullName);
                if (itv != null) {
                    Microsoft.VisualStudio.TextManager.Interop.IVsTextLines tl;
                    itv.GetBuffer(out tl); // get textlines
                }
            }
        }

        public static Microsoft.VisualStudio.Text.Editor.IWpfTextView GetActiveTextView(System.IServiceProvider serviceProvider) {
            var monitorSelection = (Microsoft.VisualStudio.Shell.Interop.IVsMonitorSelection)serviceProvider.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SVsShellMonitorSelection));
            if (monitorSelection == null) {
                return null;
            }
            object curDocument;
            if (Microsoft.VisualStudio.ErrorHandler.Failed(monitorSelection.GetCurrentElementValue((uint)Microsoft.VisualStudio.VSConstants.VSSELELEMID.SEID_DocumentFrame, out curDocument))) {
                return null;
            }

            Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame frame = curDocument as Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame;
            if (frame == null) {
                return null;
            }

            object docView = null;
            if (Microsoft.VisualStudio.ErrorHandler.Failed(frame.GetProperty((int)Microsoft.VisualStudio.Shell.Interop.__VSFPROPID.VSFPROPID_DocView, out docView))) {
                return null;
            }

            if (docView is Microsoft.VisualStudio.TextManager.Interop.IVsCodeWindow) {
                Microsoft.VisualStudio.TextManager.Interop.IVsTextView textView;
                if (Microsoft.VisualStudio.ErrorHandler.Failed(((Microsoft.VisualStudio.TextManager.Interop.IVsCodeWindow)docView).GetPrimaryView(out textView))) {
                    return null;
                }

                var model = (Microsoft.VisualStudio.ComponentModelHost.IComponentModel)serviceProvider.GetService(typeof(Microsoft.VisualStudio.ComponentModelHost.SComponentModel));
                var adapterFactory = model.GetService<Microsoft.VisualStudio.Editor.IVsEditorAdaptersFactoryService>();
                var wpfTextView = adapterFactory.GetWpfTextView(textView);
                return wpfTextView;
            }
            return null;
        }

        // LanguageService;
        // LanguageService.OnCaretMoved;
        protected bool ParseThreadIsAlive { get; }
        protected virtual void OnChangesCommitted(uint flags, TextSpan[] ptsChanged);
        public CodeWindowManager GetCodeWindowManagerForSource(Source src);
        public CodeWindowManager GetCodeWindowManagerForView(IVsTextView view);
        public Guid GetLanguageServiceGuid();
        public IAsyncResult BeginInvoke(Delegate method, object[] args);
        public IAsyncResult BeginParse(ParseRequest request, ParseResultHandler handler);
        public IAsyncResult GetParseResult();
        public IEnumerable GetSources();
        public IVsDebugger GetIVsDebugger();
        public IVsTextMacroHelper GetIVsTextMacroHelperIfRecordingOn();
        public IVsTextView GetPrimaryViewForSource(Source src);
        public IVsTextView LastActiveTextView { get; }
        public LanguagePreferences Preferences { get; set; }
        public Source GetOrCreateSource(IVsTextLines buffer);
        public Source GetSource(IVsTextLines buffer);
        public Source GetSource(IVsTextView view);
        public Source GetSource(string fname);
        public System.IServiceProvider Site { get; }
        public abstract AuthoringScope ParseSource(ParseRequest req);
        public abstract IScanner GetScanner(IVsTextLines buffer);
        public abstract LanguagePreferences GetLanguagePreferences();
        public abstract string GetFormatFilterList();
        public abstract string Name { get; }
        public bool InvokeRequired { get; }
        public bool IsActive { get; }
        public bool IsDebugging { get; }
        public bool IsMacroRecordingOn();
        public bool IsParsing { get; set; }
        public bool ParseThreadPaused { get; set; }
        public int DispatchCommand(Guid cmdGuid, uint cmdId, IntPtr pvaIn, IntPtr pvaOut);
        public int DispatchCommand(Guid cmdGuid, uint cmdId, uint cmdExecOpt, IntPtr pvaIn, IntPtr pvaOut);
        public int GetCodeWindowManager(IVsCodeWindow codeWindow, out IVsCodeWindowManager mgr);
        public int GetColorizer(IVsTextLines buffer, out IVsColorizer result);
        public int MainThreadId { get; }
        public object EndInvoke(IAsyncResult result);
        public object GetService(Type serviceType);
        public object Invoke(Delegate method, object[] args);
        public virtual CodeWindowManager CreateCodeWindowManager(IVsCodeWindow codeWindow, Source source);
        public virtual Colorizer GetColorizer(IVsTextLines buffer);
        public virtual DocumentProperties CreateDocumentProperties(CodeWindowManager mgr);
        public virtual ExpansionFunction CreateExpansionFunction(ExpansionProvider provider, string functionName);
        public virtual ExpansionProvider CreateExpansionProvider(Source src);
        public virtual ImageList GetImageList();
        public virtual ParseRequest CreateParseRequest(Source s, int line, int idx, TokenInfo info, string sourceText, string fname, ParseReason reason, IVsTextView view);
        public virtual Source CreateSource(IVsTextLines buffer);
        public virtual TypeAndMemberDropdownBars CreateDropDownHelper(IVsTextView forView);
        public virtual ViewFilter CreateViewFilter(CodeWindowManager mgr, IVsTextView newView);
        public virtual bool CanStopThread(Source src);
        public virtual bool IsSourceOpen(Source src);
        public virtual bool QueryInvalidEncoding(__VSTFF format, out string errorMessage);
        public virtual int CurFileExtensionFormat(string fileName);
        public virtual int GetColorableItem(int index, out IVsColorableItem item);
        public virtual int GetFileExtensions(out string extensions);
        public virtual int GetItemCount(out int count);
        public virtual int GetLanguageID(IVsTextBuffer buffer, int line, int col, out Guid langId);
        public virtual int GetLanguageName(out string name);
        public virtual int GetLocationOfName(string name, out string pbstrMkDoc, TextSpan[] spans);
        public virtual int GetNameOfLocation(IVsTextBuffer buffer, int line, int col, out string name, out int lineOffset);
        public virtual int GetProximityExpressions(IVsTextBuffer buffer, int line, int col, int cLines, out IVsEnumBSTR ppEnum);
        public virtual int IsMappedLocation(IVsTextBuffer buffer, int line, int col);
        public virtual int OnModeChange(DBGMODE dbgmodeNew);
        public virtual int QueryService(ref Guid guidService, ref Guid iid, out IntPtr obj);
        public virtual int QueryWaitForAutoOutliningCallback(out int fWait);
        public virtual int ResolveName(string name, uint flags, out IVsEnumDebugName ppNames);
        public virtual int ValidateBreakpointLocation(IVsTextBuffer buffer, int line, int col, TextSpan[] pCodeSpan);
        public virtual void Dispose();
        public virtual void Initialize();
        public virtual void OnActiveViewChanged(IVsTextView textView);
        public virtual void OnCaretMoved(CodeWindowManager mgr, IVsTextView textView, int line, int col);
        public virtual void OnCloseSource(Source source);
        public virtual void OnIdle(bool periodic);
        public virtual void OnParseAborted();
        public virtual void OnParseComplete(ParseRequest req);
        public virtual void SynchronizeDropdowns();
        public virtual void UpdateLanguageContext(LanguageContextHint hint, IVsTextLines buffer, TextSpan[] ptsSelection, IVsUserContext context);
        public void AbortBackgroundParse();
        public void AddCodeWindowManager(CodeWindowManager m);
        public void GetSite(ref Guid iid, out IntPtr ptr);
        public void OpenDocument(string path);
        public void RemoveCodeWindowManager(CodeWindowManager m);
        public void ScrollToEnd(IVsTextView view);
        public void ScrollToEnd(IVsWindowFrame frame);
        public void SetSite(object site);
        public void SetUserContextDirty(string fileName);

    }
}