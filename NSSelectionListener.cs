
using System;
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ShellConstants = Microsoft.VisualStudio.Shell.Interop.Constants;

namespace NimStudio.NimStudio {

    [CLSCompliant(false)]
    public abstract class SelectionListener: IVsSelectionEvents, IDisposable {
        private uint eventsCookie;
        private IVsMonitorSelection monSel;
        private ServiceProvider serviceProvider;
        private bool isDisposed;
        private static volatile object Mutex = new object();

        protected SelectionListener(ServiceProvider serviceProviderParameter) {
            if (serviceProviderParameter == null) {
                throw new ArgumentNullException("serviceProviderParameter");
            }

            this.serviceProvider = serviceProviderParameter;
            this.monSel = this.serviceProvider.GetService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;

            if (this.monSel == null) {
                throw new InvalidOperationException();
            }
        }

        protected uint EventsCookie {
            get {
                return this.eventsCookie;
            }
        }

        protected IVsMonitorSelection SelectionMonitor {
            get {
                return this.monSel;
            }
        }

        protected ServiceProvider ServiceProvider {
            get {
                return this.serviceProvider;
            }
        }

        public virtual int OnCmdUIContextChanged(uint dwCmdUICookie, int fActive) {
            return VSConstants.E_NOTIMPL;
        }

        public virtual int OnElementValueChanged(uint elementid, object varValueOld, object varValueNew) {
            int hr = VSConstants.S_OK;
            if (elementid == VSConstants.DocumentFrame) {
                IVsWindowFrame pWindowFrame = varValueOld as IVsWindowFrame;
                if (pWindowFrame != null) {
                    object document;
                    hr = pWindowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out document); // get doc name
                    if (ErrorHandler.Succeeded(hr)) {
                        //uint itemid;
                        //IVsHierarchy hier = projMgr as IVsHierarchy;
                        //hr = hier.ParseCanonicalName((string)document, out itemid);
                        //PythonFileNode node = projMgr.NodeFromItemId(itemid) as PythonFileNode;
                        //if (null != node) {
                        //    node.RunGenerator();
                        //}
                    }
                }
            }
            return hr;
        }

        public virtual int OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew) {
            return VSConstants.E_NOTIMPL;
        }

        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Init() {
            if (this.SelectionMonitor != null) {
                ErrorHandler.ThrowOnFailure(this.SelectionMonitor.AdviseSelectionEvents(this, out this.eventsCookie));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Microsoft.VisualStudio.Shell.Interop.IVsMonitorSelection.UnadviseSelectionEvents(System.UInt32)")]
        protected virtual void Dispose(bool disposing) {
            if (!this.isDisposed) {
                lock (Mutex) {
                    if (disposing && this.eventsCookie != (uint)ShellConstants.VSCOOKIE_NIL && this.SelectionMonitor != null) {
                        this.SelectionMonitor.UnadviseSelectionEvents((uint)this.eventsCookie);
                        this.eventsCookie = (uint)ShellConstants.VSCOOKIE_NIL;
                    }
                    this.isDisposed = true;
                }
            }
        }
    }
}
