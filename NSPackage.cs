﻿
//#define ivslang

using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using VSLangProj;

namespace NimStudio.NimStudio {

    [PackageRegistration(UseManagedResourcesOnly = true)]

    // Help/About dialog
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]

    // Menus
    [ProvideMenuResource("Menus.ctmenu", 1)]

    // GUID
    //[Guid("B98EABF4-9F60-432D-885D-73BD12CC638F")]
    [Guid(GuidList.NSPkgGUIDStr)]

    //[ProvideLanguageServiceAttribute(typeof(VSNimLangServ), "VSNimLang",

    [ProvideLanguageService(typeof(NSLangServ), NSConst.LangName,
        106,                          // resource ID of localized language name
        CodeSense = true,             // IntelliSense
        DefaultToInsertSpaces = true,
        RequestStockColors = false,   // Custom colors
        EnableCommenting = true,
        MatchBraces = true,
        ShowMatchingBrace = true,
        AutoOutlining = true,
        EnableAsyncCompletion = false  // Background parsing
        )]

    [ProvideLanguageExtension(typeof(NSLangServ), NSConst.FileExt)]

    [ProvideLanguageExtensionAttribute(typeof(NSLangServ), NSConst.FileExt)]

    //[ProvideLanguageCodeExpansionAttribute(typeof(VSNimLangServ), VSNConst.LangName,
    //             106,           // Resource ID
    //             "testlanguage", // key for snippet templates
    //             @"%InstallRoot%\Test Language\SnippetsIndex.xml",  // Path to snippets index
    //             SearchPaths = @"%InstallRoot%\Test Language\Snippets\%LCID%\Snippets\;" +
    //                           @"%TestDocs%\Code Snippets\Test Language\Test Code Snippets"
    //             )]

    [ProvideLanguageEditorOptionPageAttribute(
             typeof(NSOptions),
             NSConst.LangName,  // Registry key name for language
             "Options",      // Registry key name for property page
             "Page1",
             "#242"
             )]
    [ProvideLanguageEditorOptionPageAttribute(
             typeof(NSOptions),
             NSConst.LangName,  // Registry key name for language
             "Advanced",     // Registry key name for node
             "Page2",
             "#243"
             )]
    [ProvideLanguageEditorOptionPageAttribute(
             typeof(NSOptions),
             NSConst.LangName,  // Registry key name for language
             @"Advanced\Indenting",     // Registry key name for property page
             "Page3",         // Localized name of property page
             "#244"
             )]

    [ProvideOptionPageAttribute(typeof(NSOptions),
    "NimStudioPackage", "OptionsPage", 113, 114, true)]

    [Microsoft.VisualStudio.Shell.ProvideService(typeof(NSLangServ))]
    public sealed class NSPackage: Package, IOleComponent {

        #if ivslang
        private VSNLanguageInfo LangInfo;
        #endif

        private uint m_ComponentID;
        public static NimSuggestProc nimsuggest;
        public static NSPackage nspackage;
        public static bool quickinfo=false; // 
        public static bool memberlist = false; // 
        public static string nimsettingsini;
        public static Dictionary<int, string> menucmds;

        public NSPackage() {
            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        protected override void Initialize() {

            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();
            nspackage = this;

            #if ivslang
            LangInfo = new VSNLanguageInfo();
            ((IServiceContainer)this).AddService(typeof(VSNLanguageInfo), LangInfo, true);
            #endif

            menucmds = new Dictionary<int, string>() { 
                { 0x0100, "NSMenuCmdOptionsEdit"},
                { 0x0101, "NSMenuCmdOptionsLoad"} 
            };

            // menu commands - .vsct file
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs != null) {
                foreach (int dkey in menucmds.Keys) {
                    CommandID cmdid = new CommandID(GuidList.NSMenuCmdTopGUID, dkey);
                    MenuCommand menucmd = new MenuCommand(MenuItemCallback, cmdid);
                    mcs.AddCommand(menucmd);
                }
            }

            IServiceContainer ServiceCnt = this as IServiceContainer;
            NSLangServ ServiceLng = new NSLangServ();
            ServiceLng.SetSite(this);
            ServiceCnt.AddService(typeof(NSLangServ), ServiceLng, true);

            IOleComponentManager mgr = GetService(typeof(SOleComponentManager)) as IOleComponentManager;
            if (m_ComponentID == 0 && mgr != null) {
                OLECRINFO[] crinfo = new OLECRINFO[1];
                crinfo[0].cbSize = (uint)Marshal.SizeOf(typeof(OLECRINFO));
                crinfo[0].grfcrf = (uint)_OLECRF.olecrfNeedIdleTime | (uint)_OLECRF.olecrfNeedPeriodicIdleTime;
                crinfo[0].grfcadvf = (uint)_OLECADVF.olecadvfModal | (uint)_OLECADVF.olecadvfRedrawOff | (uint)_OLECADVF.olecadvfWarningsOff;
                crinfo[0].uIdleTimeInterval = 1000;
                int hr = mgr.FRegisterComponent(this, crinfo, out m_ComponentID);
            }

            nimsettingsini = System.IO.Path.Combine(UserDataPath, "nimstudio.ini");
            NSIni.Init(nimsettingsini);
            NSSugInit();

            if (NSIni.Get("Main", "exttoolsadded") != "true") {
                NSIni.Add("Main", "exttoolsadded", "true");
                NSIni.Write();
                string reg_keyname = "HKEY_CURRENT_USER\\Software\\Microsoft\\VisualStudio\\12.0\\External Tools";
                object reg_ret;
                bool regstateok=false;
                reg_ret = Registry.GetValue(reg_keyname, "ToolNumKeys", -1);
                while (true) {
                    if (reg_ret==null) break;
                    if (reg_ret.GetType()!=typeof(int)) break;
                    int totkeys = (int)reg_ret;
                    if (totkeys==-1) break;
                    for (int rloop = 0; rloop < totkeys; rloop++) {
                        reg_ret = Registry.GetValue(reg_keyname, "ToolTitle" + rloop.ToString(), null);
                        if (reg_ret==null) break;
                        if (reg_ret.GetType()!=typeof(string)) break;
                        if (reg_ret=="NimStudio Compile+Run") break;
                        if (rloop==totkeys-1) regstateok=true;
                    }
                    if (regstateok) {
                        Registry.SetValue(reg_keyname, "ToolTitle" + totkeys.ToString(), "NimStudio Compile+Run", RegistryValueKind.String);
                        Registry.SetValue(reg_keyname, "ToolSourceKey" + totkeys.ToString(), "", RegistryValueKind.String);
                        Registry.SetValue(reg_keyname, "ToolOpt" + totkeys.ToString(), 26, RegistryValueKind.DWord);
                        Registry.SetValue(reg_keyname, "ToolDir" + totkeys.ToString(), "$(ItemDir)", RegistryValueKind.String);
                        Registry.SetValue(reg_keyname, "ToolCmd" + totkeys.ToString(), "cmd.exe", RegistryValueKind.String);
                        Registry.SetValue(reg_keyname, "ToolArg" + totkeys.ToString(), @"/c """"c:\Program Files (x86)\Microsoft Visual Studio 12.0\VC\bin\vcvars32.bat"" & del $(ItemDir)$(ItemFileName).exe 2>nul & ""c:\Nim\bin\nim.exe"" c $(ItemPath) & echo ****Running**** & $(ItemDir)$(ItemFileName).exe""", RegistryValueKind.String);
                        totkeys++;
                        Registry.SetValue(reg_keyname, "ToolTitle" + totkeys.ToString(), "NimStudio Compile", RegistryValueKind.String);
                        Registry.SetValue(reg_keyname, "ToolSourceKey" + totkeys.ToString(), "", RegistryValueKind.String);
                        Registry.SetValue(reg_keyname, "ToolOpt" + totkeys.ToString(), 26, RegistryValueKind.DWord);
                        Registry.SetValue(reg_keyname, "ToolDir" + totkeys.ToString(), "$(ItemDir)", RegistryValueKind.String);
                        Registry.SetValue(reg_keyname, "ToolCmd" + totkeys.ToString(), "cmd.exe", RegistryValueKind.String);
                        Registry.SetValue(reg_keyname, "ToolArg" + totkeys.ToString(), @"/c """"c:\Program Files (x86)\Microsoft Visual Studio 12.0\VC\bin\vcvars32.bat"" & del $(ItemDir)$(ItemFileName).exe 2>nul & ""c:\Nim\bin\nim.exe"" c $(ItemPath)""", RegistryValueKind.String);
                        totkeys++;
                        Registry.SetValue(reg_keyname, "ToolNumKeys", totkeys, RegistryValueKind.DWord);
                    }
                }
            }
        }

        public void NSSugInit() {
            string[] nimexes = { "nim.exe", "nimsuggest.exe" };
            for (int lexe = 0; lexe < 2; lexe++) {
                if (NSIni.Get("Main", nimexes[lexe]) == "") {
                    // nim exe not found in INI - try to find it in path, or c:\nim\bin
                    var patharr = new List<string>((Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'));
                    patharr.Add(@"c:\nim\bin");
                    foreach (string spathi in patharr) {
                        string sfullpath = Path.Combine(spathi.Trim(), nimexes[lexe]);
                        if (File.Exists(sfullpath)) {
                            NSIni.Add("Main", nimexes[lexe], Path.GetFullPath(sfullpath));
                            NSIni.Write();
                            break;
                        }
                    }
                } else {
                    if (!File.Exists(NSIni.Get("Main", nimexes[lexe]))) {
                        System.Diagnostics.Debug.Print("NimStudio warning:" + nimexes[lexe] + " not found!");
                    }
                }
            }

            if (NSIni.Get("Main", nimexes[0]) == "" || NSIni.Get("Main", nimexes[1]) == "") {
                string msg = "";
                if (NSIni.Get("Main", nimexes[0]) == "" && NSIni.Get("Main", nimexes[1]) == "")
                    msg = "Path to nim.exe and nimsuggest.exe not found.";
                else
                    msg = String.Format("Path to {0} not found.", (NSIni.Get("Main", nimexes[0]) == "") ? nimexes[0] : nimexes[1]);

                msg += "\nEnter path(s) via:\n\n";
                msg += "Tools->NimStudio->Options Edit";
                msg += "\n   Then\n";
                msg += "Tools->NimStudio->Options Load";

                // write INI so user can edit
                NSIni.Add("Main", nimexes[0], "");
                NSIni.Add("Main", nimexes[1], "");
                NSIni.Write();

                System.Windows.Forms.MessageBox.Show(msg, "NimStudio configuration", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
            }
            if (NSIni.Get("Main", nimexes[1]) != "") { 
                if (nimsuggest==null)
                    nimsuggest = new NimSuggestProc();
                //nimsuggest.Init();
            }

        }

        public int FDoIdle(uint grfidlef) {
            bool bPeriodic = (grfidlef & (uint)_OLEIDLEF.oleidlefPeriodic) != 0;
            LanguageService service = GetService(typeof(NSLangServ)) as LanguageService;
            if (service != null) {
                service.OnIdle(bPeriodic);
            }
            return 0;
        }

        public int FContinueMessageLoop(uint uReason, IntPtr pvLoopData, MSG[] pMsgPeeked) {
            return 1;
        }

        public int FPreTranslateMessage(MSG[] pMsg) {
            return 0;
        }

        public int FQueryTerminate(int fPromptUser) {
            return 1;
        }

        public int FReserved1(uint dwReserved, uint message, IntPtr wParam, IntPtr lParam) {
            return 1;
        }

        public IntPtr HwndGetWindow(uint dwWhich, uint dwReserved) {
            return IntPtr.Zero;
        }

        public void OnActivationChange(IOleComponent pic, int fSameComponent, OLECRINFO[] pcrinfo, int fHostIsActivating, OLECHOSTINFO[] pchostinfo, uint dwReserved) {
        }

        public void OnAppActivate(int fActive, uint dwOtherThreadID) {
        }

        public void OnEnterState(uint uStateID, int fEnter) {
        }

        public void OnLoseActivation() {
        }

        public void Terminate() {
        }

        protected override void Dispose(bool disposing) {
            if (m_ComponentID != 0) {
                IOleComponentManager mgr = GetService(typeof(SOleComponentManager)) as IOleComponentManager;
                if (mgr != null) {
                    int hr = mgr.FRevokeComponent(m_ComponentID);
                }
                m_ComponentID = 0;
            }
            base.Dispose(disposing);
        }

        // menu callback
        private void MenuItemCallback(object sender, EventArgs e) {

            //IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            //Guid clsid = Guid.Empty;

            MenuCommand menucmd = (MenuCommand)sender;

            if (menucmds[menucmd.CommandID.ID] == "NSMenuCmdOptionsEdit") {
                Process.Start("notepad.exe", NSIni.inifilepath);
            }

            if (menucmds[menucmd.CommandID.ID] == "NSMenuCmdOptionsLoad") {
                NSIni.Init(NSIni.inifilepath);
                NSSugInit();
                System.Windows.Forms.MessageBox.Show("NimStudio INI loaded.", "NimStudio", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }


        }
    }

    #if ivslang
    [Guid(GuidList.NSPkgGUIDStr)]
    internal class NSLanguageInfo: IVsLanguageInfo {

        private readonly System.IServiceProvider _serviceProvider;
        private readonly IComponentModel _componentModel;

        public NSLanguageInfo(System.IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
            _componentModel = serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
        }

        public int GetCodeWindowManager(IVsCodeWindow pCodeWin, out IVsCodeWindowManager ppCodeWinMgr) {
            ppCodeWinMgr = null;
            return VSConstants.E_NOTIMPL;

            var model = _serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            var service = model.GetService<IVsEditorAdaptersFactoryService>();
            
            IVsTextView textView;
            if (ErrorHandler.Succeeded(pCodeWin.GetPrimaryView(out textView))) {
                ppCodeWinMgr = new CodeWindowManager(_serviceProvider, pCodeWin, service.GetWpfTextView(textView));

                return VSConstants.S_OK;
            }

            ppCodeWinMgr = null;
            return VSConstants.E_FAIL;

        }

        public int GetColorizer(IVsTextLines pBuffer, out IVsColorizer ppColorizer) {
            ppColorizer = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetFileExtensions(out string pbstrExtensions) {
            pbstrExtensions = NSConst.FileExt;
            return VSConstants.S_OK;
        }

        public int GetLanguageName(out string bstrName) {
            bstrName = NSConst.LangName;
            return VSConstants.S_OK;
        }
    }
    #endif

    public class NSOptions: DialogPage {
        bool Option1 = true;
        public bool Option {
            get {
                return this.Option1;
            }
            set {
                this.Option1 = value;
            }
        }
    }
    public static class NSConst {
        public const string PkgGUIDStr = "ef6a91e8-dd7d-48aa-a77c-69b7b8e9229a";
        public const string LangName      = "NimLang";
        public const string FileExt     = ".nim";
        public const string ProjExt  = ".nimproj";
        public const string ProductName       = "NimStudio";
        public const string ProductDetails    = "Nim Language extension for Visual Studio\r\nVersion 1.0";
        public const bool ivslang = false;
        //public static class ImageListIndex {
        //    public const int NemerleSource  = 0;
        //    public const int NemerleProject = 1;
        //    public const int NemerleForm = 2;
        //    public const int NemerleMacroReferences = 3;
        //}
    }

}
