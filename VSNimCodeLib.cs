using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


/*  
    Placeholder for code samples
    Build Action = None, so uncompileable code will not be a problem

*/
namespace NimStudio.NimStudio {
    class VSNimCodeLib {

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



    }
}