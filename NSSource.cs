using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System;
using VsCommands2K = Microsoft.VisualStudio.VSConstants.VSStd2KCmdID;
using System.Text;

// BUILD = NONE

namespace NimStudio.NimStudio {
    public partial class NSSource : Source {

        public NSSource(NSLangServ service, IVsTextLines textLines, Colorizer colorizer)
        : base(service, textLines, colorizer) {
            string path = GetFilePath();

            Service = service;

            //Scanner = colorizer.Scanner as NSScanner;
            LastDirtyTime = DateTime.Now;
        }

        public DateTime LastDirtyTime {
            get;
            private set;
        }

        public NSLangServ Service {
            get;
            private set;
        }
        //public NSScanner Scanner {
        //    get;
        //    private set;
        //}
        public MethodData MethodData {
            get;
            private set;
        }
        public int TimeStamp {
            get;
            private set;
        }
        public bool RegionsLoaded {
            get;
            set;
        }

        public IVsTextLines TextLines {
            get {
                return GetTextLines();
            }
        }

        int _fileIndex = -1;


        public override void OnChangeLineText(TextLineChange[] lineChange, int last) {
            base.OnChangeLineText(lineChange, last);
            TimeStamp++;

            //if (Scanner != null && Scanner.GetLexer().ClearHoverHighlights()) {
            //    int lineCount;
            //    GetTextLines().GetLineCount(out lineCount);
            //    Recolorize(1, lineCount);
            //}
        }

        public override TextSpan UncommentLines(TextSpan span, string lineComment) {
            // Remove line comments
            int clen = lineComment.Length;
            var editMgr = new EditArray(this, null, true, "UncommentLines");

            for (int line = span.iStartLine; line <= span.iEndLine; line++) {
                int i = this.ScanToNonWhitespaceChar(line);
                string text = base.GetLine(line);

                if ((i + clen) <= text.Length && text.Substring(i, clen) == lineComment) {
                    var es = new EditSpan(new TextSpan() {
                        iEndLine = line,
                        iStartLine = line,
                        iStartIndex = i,
                        iEndIndex = i + clen
                    }, "");
                    editMgr.Add(es); // remove line comment.

                    if (line == span.iStartLine && span.iStartIndex != 0)
                        span.iStartIndex = i;
                }
            }

            editMgr.ApplyEdits();

            span.iStartIndex = 0;
            return span;
        }

        public void Completion(IVsTextView textView, int lintIndex, int columnIndex, bool byTokenTrigger) {
            //var result = GetEngine().Completion(this, lintIndex + 1, columnIndex + 1);

            //var decls = new NemerleDeclarations(result.CompletionElems, result.ComlitionLocation);
            //CompletionSet.Init(textView, decls, !byTokenTrigger);
        }

        public override bool IsDirty {
            get {
                return base.IsDirty;
            } set {
                Debug.WriteLine("IsDirty = " + value);
                base.IsDirty = value;
                if (value)
                    LastDirtyTime = DateTime.Now;
                //else
                //  LastParseTime = System.DateTime.Now;
            }
        }

        public override void OnIdle(bool periodic) {
        }

        public IVsTextView GetView() {
            if (Service == null)
                throw new InvalidOperationException("The Service property of NSSource is null!");

            return Service.GetPrimaryViewForSource(this);
        }

        public override void MethodTip(IVsTextView textView, int line, int index, TokenInfo info) {
            //var result = BeginGetMethodTipInfo(this, line + 1, index + 1);
            //result.AsyncWaitHandle.WaitOne();
            //if (result.Stop)
            //    return;
            //if (result.MethodTipInfo == null || !result.MethodTipInfo.HasTip) {
            //    MethodData.Dismiss();
            //    return;
            //}

            //var methods = new NemerleMethods(result.MethodTipInfo);

            //var span = result.MethodTipInfo.StartName.Combine(result.MethodTipInfo.EndParameters).ToTextSpan();
            //MethodData.Refresh(textView, methods, result.MethodTipInfo.ParameterIndex, span);
            Debug.WriteLine("MethodTip");
        }

        public void Goto(IVsTextView view, bool gotoDefinition, int lineIndex, int colIndex) {
            /*
                IVsUIShell shell = _project.ProjectNode.Package.GetService<IVsUIShell, SVsUIShell>();

                Guid           guid = new Guid(ToolWindowGuids.ObjectSearchResultsWindow);
                IVsWindowFrame frame;

                shell.FindToolWindow(
                (uint)__VSFINDTOOLWIN.FTW_fForceCreate,
                ref guid,
                out frame);

                if (frame != null)
                {
                object obj;
                frame.GetProperty((int)__VSFPROPID.VSFPROPID_ExtWindowObject, out obj);

                obj.ToString();

                EnvDTE.Window window = (EnvDTE.Window)obj;

                guid = typeof(IVsObjectListOwner).GUID;
                IntPtr ptr;
                frame.QueryViewInterface(ref guid, out ptr);

                IVsObjectListOwner lst = Marshal.GetObjectForIUnknown(ptr) as IVsObjectListOwner;

                int isv = lst.IsVisible();

                lst.ClearCachedData((int)_VSOBJLISTOWNERCACHEDDATAKINDS.LOCDK_SELECTEDNAVINFO);

                guid = typeof(IVsObjectSearchPane).GUID;
                frame.QueryViewInterface(ref guid, out ptr);

                IVsObjectSearchPane pane = Marshal.GetObjectForIUnknown(ptr) as IVsObjectSearchPane;

                frame.Show();
                }
            */

            /*
            var engine = GetEngine();
            var line = lineIndex + 1;
            var col = colIndex + 1;

            GotoInfo[] infos = gotoDefinition
                               ? engine.GetGotoInfo(this, line, col, GotoKind.Definition)
                               : engine.GetGotoInfo(this, line, col, GotoKind.Usages);

            if (infos == null || infos.Length == 0)
                return;

            string captiopn = null;

            if (!infos[0].HasLocation && infos[0].Member != null) {
                Debug.Assert(infos.Length == 1, "Multiple unknown locations are unexpected");
                var inf = infos[0];
                GotoInfo[] infoFromPdb = TryFindGotoInfoByDebugInfo(engine, inf);

                if (infoFromPdb.Length == 0) {
                    if (inf.Member != null) {
                        var res = ProjectInfo.FindProjectByOutput(inf.FilePath);

                        if (res != null)
                            infos = res.Engine.GetGotoInfoForMember(inf.Member.GetFullName(), false, GotoKind.Definition);

                        if (infos.Length <= 0 || res == null)
                            infos = NemerleGoto.GenerateSource(infos, engine, out captiopn);
                    } else
                        infos = NemerleGoto.GenerateSource(infos, engine, out captiopn);
                } else
                    infos = infoFromPdb;
            }

            var langSrvc = (NSLangServ)LanguageService;

            if (infos.Length == 1)
                langSrvc.GotoLocation(infos[0].Location, captiopn, captiopn != null);
            else if (infos.Length > 0) {
                var textEditorWnd = NativeWindow.FromHandle(view.GetWindowHandle());

                using(var popup = new GotoUsageForm(infos))
                if ((textEditorWnd == null ? popup.ShowDialog() : popup.ShowDialog(textEditorWnd)) == DialogResult.OK)
                    langSrvc.GotoLocation(popup.Result.Location, captiopn, captiopn != null);
            }
             */
        }

        /*
        private GotoInfo[] TryFindGotoInfoByDebugInfo(IIdeEngine engine, GotoInfo inf) {
            GotoInfo[] infoFromPdb = ProjectInfo.LookupLocationsFromDebugInformation(inf);
            var result = new List<GotoInfo>();

            foreach (GotoInfo item in infoFromPdb) {
                var cu = engine.ParseCompileUnit(new FileNemerleSource(item.Location.FileIndex));
                var res = TryGetGotoInfoForMemberFromSource(inf.Member, item.Location, cu);

                if (res.Length > 0)
                    result.AddRange(res);
            }

            return result.ToArray();
        }

        private static GotoInfo[] TryGetGotoInfoForMemberFromSource(IMember member, Location loc, CompileUnit cu) {
            Trace.Assert(member != null);
            var ty = member as TypeInfo;
            var soughtIsType = ty != null;

            if (ty == null)
                // ReSharper disable PossibleNullReferenceException
                ty = member.DeclaringType;
            // ReSharper restore PossibleNullReferenceException

            var td = FindTopDeclaration(ty, cu);

            if (td == null)
                return new[] { new GotoInfo(loc, UsageType.GeneratedDefinition) };

            if (soughtIsType)
                return new[] { new GotoInfo(Location.GetFileName(cu.FileIndex), td.NameLocation) };

            var name = member.Name;
            var file = Location.GetFileName(cu.FileIndex);
            var members = td.GetMembers().Where(m => string.Equals(m.Name, name, StringComparison.Ordinal)).ToArray();

            if (members.Length == 1)
                return new[] { new GotoInfo(file, members[0].NameLocation) };

            var isProp = member is IProperty;

            members = td.GetMembers().Where(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase)
                                            // Макро [Accessor] может изменять имя свойства. Учитываем это...
                                            || (isProp && string.Equals(m.Name.Replace("_", ""), name, StringComparison.OrdinalIgnoreCase))).ToArray();

            if (members.Length > 0) {
                if (loc.Column > 0) {
                    var members2 = members.Where(m => m.Location.Contains(loc)).ToArray();

                    if (members2.Length > 0)
                        return members2.Select(m => new GotoInfo(file, m.NameLocation)).ToArray();

                    return new[] { new GotoInfo(file, td.NameLocation) };
                }

                if (members.Length == 1)
                    return new[] { new GotoInfo(file, members[0].NameLocation) };

                return FindBastMember(members, member).Select(m => new GotoInfo(file, m.NameLocation)).ToArray();
            }

            // ничего не нашли

            if (td != null) // но у нас есть тип в котором объявлен член...
                return new[] { new GotoInfo(td.NameLocation) }; // возвращаем его имя!

            if (loc.IsEmpty)
                return new GotoInfo[0];

            return new[] { new GotoInfo(loc, UsageType.GeneratedDefinition) }; // если все обломалось, вернем хотя бы что-то (правда, вряд ли это будте корректным результатом)
        }
        */


        public override ParseRequest BeginParse(int line, int idx, TokenInfo info, ParseReason reason, IVsTextView view, ParseResultHandler callback) {
            //return base.BeginParse(line, idx, info, reason, view, callback);
            switch (reason) {
            case ParseReason.Autos:
                break;
            case ParseReason.Check:
                break;
            case ParseReason.CodeSpan:
                break;
            case ParseReason.CompleteWord:
                break;
            case ParseReason.DisplayMemberList:
                break;
            case ParseReason.Goto:
                break;
            case ParseReason.MemberSelect:
                break;
            case ParseReason.MemberSelectAndHighlightBraces:
                break;
            case ParseReason.MethodTip:
                break;
            case ParseReason.None:
                break;
            case ParseReason.QuickInfo:
                break;
            case ParseReason.HighlightBraces:
            case ParseReason.MatchBraces:
                Trace.Assert(false);
                break;
            default:
                break;
            }

            Debug.WriteLine("Soutce.BeginParse: " + reason);
            return null;
        }

        public override void Dispose() {
            base.Dispose();
        }


        public override CommentInfo GetCommentFormat() {
            return new CommentInfo { UseLineComments = true, LineStart = "//", BlockStart = "/*", BlockEnd = "*/" };
        }

        public override TextSpan CommentLines(TextSpan span, string lineComment) {
            // Calculate minimal position of non-space char
            // at lines in selected span.
            var minNonEmptyPosition = 0;

            for (var i = span.iStartLine; i <= span.iEndLine; ++i) {
                var line = base.GetLine(i);

                if (line.Trim().Length <= 0)
                    continue;

                var spaceLen = line.Replace(line.TrimStart(), "").Length;

                if (minNonEmptyPosition == 0 || spaceLen < minNonEmptyPosition)
                    minNonEmptyPosition = spaceLen;
            }

            // insert line comment at calculated position.
            var editMgr = new EditArray(this, null, true, "CommentLines");

            for (var i = span.iStartLine; i <= span.iEndLine; ++i) {
                var text = base.GetLine(i);

                if (minNonEmptyPosition <= text.Length && text.Trim().Length > 0) {
                    var commentSpan = new TextSpan();

                    commentSpan.iStartLine = commentSpan.iEndLine = i;
                    commentSpan.iStartIndex = commentSpan.iEndIndex = minNonEmptyPosition;

                    editMgr.Add(new EditSpan(commentSpan, lineComment));
                }
            }

            editMgr.ApplyEdits();

            // adjust original span to fit comment symbols
            span.iEndIndex += lineComment.Length;
            return span;
        }

        public override AuthoringSink CreateAuthoringSink(ParseReason reason, int line, int col) {
            Trace.Assert(false, "We don't using MS infrastructure of background parsing now. This code should not be called!");
            throw new NotImplementedException("This should not be heppen!");
        }

        public override TokenInfo GetTokenInfo(int line, int col) {
            //get current line
            var info = new TokenInfo();
            /*
            var colorizer = GetColorizer() as NScol;

            if (colorizer == null)
                return info;

            colorizer.SetCurrentLine(line);

            //get line info
            TokenInfo[] lineInfo = colorizer.GetLineInfo(GetTextLines(), line, ColorState);

            if (lineInfo != null) {
                //get character info
                if (col > 0)
                    col--;

                GetTokenInfoAt(lineInfo, col, ref info);
            }
            */
            return info;
        }

        public override void ProcessHiddenRegions(System.Collections.ArrayList hiddenRegions) {
            // TranslateMe:ss
            //VladD2: Приходится переписывать реализацию от МС, так как она практически не расширяется как нужно нам.
            throw new NotImplementedException();
        }

        class TextSpanEqCmp : IEqualityComparer<TextSpan> {
            public bool Equals(TextSpan x, TextSpan y) {
                return x.iStartLine == y.iStartLine && x.iEndLine == y.iEndLine
                       && x.iEndIndex == y.iEndIndex && x.iStartIndex == y.iStartIndex;
            }

            public int GetHashCode(TextSpan x) {
                return x.iStartLine ^ x.iEndLine ^ x.iEndIndex ^ x.iStartIndex;
            }

            public static readonly TextSpanEqCmp Instance = new TextSpanEqCmp();
        }

        bool _processingOfHiddenRegions;

        public override MethodData CreateMethodData() {
            return MethodData = base.CreateMethodData();
        }

        public override void OnCommand(IVsTextView textView, VsCommands2K command, char ch) {
            if (textView == null || Service == null || !Service.Preferences.EnableCodeSense)
                return;

            int line, idx;
            textView.GetCaretPos(out line, out idx);

            TokenInfo tokenBeforeCaret = GetTokenInfo(line, idx);

            TryHighlightBraces(textView, command, line, idx, tokenBeforeCaret);

            //VladD2: We do not trigger MethodTip on type because it's very slow!

            // This code open completion list if user enter '.'.
            if ((tokenBeforeCaret.Trigger & TokenTriggers.MemberSelect) != 0 && (command == VsCommands2K.TYPECHAR)) {
                var spaces = new[] { '\t', ' ', '\u000B', '\u000C' };
                var str = GetText(line, 0, line, idx - 1).Trim(spaces);

                while (str.Length <= 0 && line > 0) { // skip empy lines
                    line--;
                    str = GetLine(line + 1).Trim(spaces);
                }

                if (str.Length > 0) {
                    var lastChar = str[str.Length - 1];

                    // Don't show completion list if previous char not one of following:
                    if (char.IsLetterOrDigit(lastChar) || lastChar == ')' || lastChar == ']')
                        Completion(textView, line, idx, true);
                }
            }
        }

        public override void GetPairExtents(IVsTextView view, int line, int col, out TextSpan span) {
            var spanAry = GetMatchingBraces(false, line, col);

            if (spanAry.Length == 2)
                span = TextSpanHelper.ContainsInclusive(spanAry[0], line, col) ? spanAry[1] : spanAry[0];
            else
                span = new TextSpan();
        }

        /// <summary>
        /// Match paired tokens. Run in GUI thread synchronously!
        /// </summary>
        /// <param name="view">Current view</param>
        /// <param name="line">zero based index of line</param>
        /// <param name="index">zero based index of char</param>
        public bool HighlightBraces(IVsTextView view, int line, int index) {
            try {
                var spanAry = GetMatchingBraces(false, line, index);
                if (spanAry.Length == 2 && TextSpanHelper.ValidSpan(this, spanAry[0]) && TextSpanHelper.ValidSpan(this, spanAry[1])) {
                    // No check result!
                    view.HighlightMatchingBrace((uint)Service.Preferences.HighlightMatchingBraceFlags, (uint)spanAry.Length, spanAry);
                    return true;
                }

                return false;
            } finally {
            }
        }

        /// <summary>
        /// Match paired tokens. Run in GUI thread synchronously!
        /// </summary>
        /// <param name="isMatchBraces">match or highlight mraces</param>
        /// <param name="line">zero based index of line</param>
        /// <param name="index">zero based index of char</param>
        public TextSpan[] GetMatchingBraces(bool isMatchBraces, int line, int index) {
            var nline = line + 1; // one based number of line
            var ncol = index + 1; // one based number of column

            // Steps:
            // 1. Find token under text caret.
            // 2. Determine that it is a paired token.
            // 3. Determine paired token.
            // 4. Find paired token in the source file.
            // 5. Set info about paired tokens Sink and return it in AuthoringScope.

            var source = this;
            IVsTextColorState colorState = source.ColorState;
            Colorizer colorizer = source.GetColorizer();
            var scanner = (NSScanner)colorizer.Scanner;
            string lineText = source.GetLine(nline);
            scanner.SetSource(lineText, 0);

            // Steps: 1-3
            //var bracketFinder = new BracketFinder(source, nline, ncol, scanner, colorState);

            // 4. Find paired token in the source file.
            //var matchBraceInfo = bracketFinder.FindMatchBraceInfo();

            //if (matchBraceInfo != null) {
                // 5. Set info about paired tokens Sink and return it in AuthoringScope.

                // Fix a bug in MPF: Correct location of left token.
                // It need for correct navigation (to left side of token).
                //
                
                //Token matchToken = matchBraceInfo.Token;
                
                //Location matchLocation = isMatchBraces && !BracketFinder.IsOpenToken(matchToken)
                //  ? matchToken.Location.FromEnd() : matchToken.Location;
                
                //Location matchLocation = matchToken.Location;

                // Set tokens position info

                //var startSpan = Utils.SpanFromLocation(bracketFinder.StartBraceInfo.Token.Location);
                //var endSpan = Utils.SpanFromLocation(matchLocation);

                //return new[] { startSpan, endSpan };
            //}

            return new TextSpan[0];
        }

        public void CaretChanged(IVsTextView textView, int lineIdx, int colIdx) {
        }

        private void TryHighlightBraces(IVsTextView textView, VsCommands2K command, int line, int idx, TokenInfo tokenInfo) {
            // Highlight brace to the left from the caret
            if ((tokenInfo.Trigger & TokenTriggers.MatchBraces) != 0 && Service.Preferences.EnableMatchBraces) {
                if ((command != VsCommands2K.BACKSPACE) &&
                        (/*(command == VsCommands2K.TYPECHAR) ||*/
                            Service.Preferences.EnableMatchBracesAtCaret)) {
                    //if (!this.LanguageService.IsParsing)
                    HighlightBraces(textView, line, idx);
                    return;
                }
            }

            return;
        }

        int _oldLine = -1;
        int _oldIdx = -1;
        IVsTextView _oldTextView;

        public void TryHighlightBraces1(IVsTextView textView) {
            if (_processingOfHiddenRegions)
                return;

            if (textView == null)
                return;

            if (Service == null || !Service.Preferences.EnableMatchBraces || !Service.Preferences.EnableMatchBracesAtCaret)
                return;

            int line, idx;
            textView.GetCaretPos(out line, out idx);

            _oldTextView = textView;
            _oldLine = line;
            _oldIdx = idx;

            TokenInfo tokenBeforeCaret = GetTokenInfo(line, idx);
            TokenInfo tokenAfterCaret = GetTokenInfo(line, idx + 1);

            if ((tokenAfterCaret.Trigger & TokenTriggers.MatchBraces) != 0)
                HighlightBraces(textView, line, idx + 1);
            else if ((tokenBeforeCaret.Trigger & TokenTriggers.MatchBraces) != 0)
                HighlightBraces(textView, line, idx);
        }

        private static bool IsInContent(string lileText, int col) {
            col--;

            if (col > lileText.Length)
                return false;

            for (int i = 0; i < col; i++)
                if (!char.IsWhiteSpace(lileText[i]))
                    return true;

            return false;
        }

        private static int IndexOfNextToken(string lileText, int startIndex) {
            if (startIndex >= lileText.Length)
                return -1;

            for (int i = startIndex; i < lileText.Length; i++) {
                if (!char.IsWhiteSpace(lileText[i]))
                    continue;

                for (; i < lileText.Length; i++)
                    if (!char.IsWhiteSpace(lileText[i]))
                        break;

                return i;
            }

            return lileText.Length - 1;
        }

        bool IsAllWhiteSpace(string text) {
            for (int i = 0; i < text.Length; i++)
                if (!char.IsWhiteSpace(text[i]))
                    return false;

            return true;
        }

        int GetPrevNotEmpryLineIndex(int line) {
            if (--line <= 1)
                return 1;

            while (IsAllWhiteSpace(GetLine(line)))
                if (--line <= 1)
                    return 1;

            return line;
        }

        int GetNextNotEmpryLineIndex(int line) {
            var len = LineCount;

            if (++line > len)
                return len;

            while (IsAllWhiteSpace(GetLine(line)))
                if (++line > len)
                    return len;

            return line;
        }

        public void OnSetFocus(IVsTextView view) {
            _oldLine = -1; // we should reset it. otherwise the TryHighlightBraces don't highlight braces
            _oldIdx = -1;

            TryHighlightBraces1(view);
        }


        public void DeleteEmptyStatementAt(int lineIndex) {
            var txt = GetLine(lineIndex);
            if (txt.Trim() == ";") {
                //var len = GetLineLength(lineIndex);
                SetText(lineIndex, 0, lineIndex + 1, 0, "");
            }
        }
        /// <summary>Get text of line frome text bufer of IDE.</summary>
        /// <param name="line">Line position (first line is 1).</param>
        /// <returns>The text of line.</returns>
        public new string GetLine(int line) {
            line--; // Convert to zero based index.

            #if DEBUG
            //int lineCount = LineCount;

            //if (line >= lineCount) // just for debugging purpose.
            //  Debug.Assert(line < lineCount);
            #endif

            return base.GetLine(line);
        }

        public int LineCount {
            get {
                int lineCount;
                int hr1 = GetTextLines().GetLineCount(out lineCount);
                ErrorHandler.ThrowOnFailure(hr1);
                return lineCount;
            }
        }

        public new int GetPositionOfLineIndex(int line, int col) {
            return base.GetPositionOfLineIndex(line - 1, col - 1);
        }

        public Tuple<int, int> GetLineIndexOfPosition(int pos) {
            int line, col;

            base.GetLineIndexOfPosition(pos, out line, out col);

            return new Tuple<int, int>(line + 1, col + 1);
        }


        /// <summary>
        /// Return information about token which coordinates intersect with point (line, index)
        /// </summary>
        /// <param name="line">zero based index of line</param>
        /// <param name="index">zero based index of char</param>
        /// <returns>Token coordinate or span initialised with -1, if no token intersect with point</returns>
        public TextSpan GetTokenSpan(int line, int index) {
            var token = GetTokenInfo(line, index + 1);  // GetTokenInfo() выдает информацию о предыдущем токене! +1 заставляет ее брать следующий
            if (token == null)
                return new TextSpan { iEndIndex = -1, iStartLine = -1, iStartIndex = -1, iEndLine = -1 };

            var start = token.StartIndex;
            var end = token.EndIndex + 1; //VladD2: Неизвесно из каких соображений GetTokenInfo() вычитает еденицу из EndIndex. Учитываем это!
            var hintSpan = new TextSpan { iStartLine = line, iStartIndex = start, iEndLine = line, iEndIndex = end };

            return hintSpan;
        }

        internal int GetDataTipText(IVsTextView view, TextSpan[] textSpan, out string hintText) {
            hintText = null;

            /*
            if (Service.IsSmartTagActive)
                return (int)TipSuccesses2.TIP_S_NODEFAULTTIP;

            var loc = Utils.LocationFromSpan(FileIndex, textSpan[0]);

            if (_tipAsyncRequest == null || _tipAsyncRequest.Line != loc.Line || _tipAsyncRequest.Column != loc.Column) {
                //if (_typeNameMarker != null && _typeNameMarker.Location.Contains(loc.Line, loc.Column))
                //  ShowTypeNameSmartTag(view, false);
                _tipAsyncRequest = GetEngine().BeginGetQuickTipInfo(this, loc.Line, loc.Column);
                return VSConstants.E_PENDING;
            }
            if (!_tipAsyncRequest.IsCompleted)
                return VSConstants.E_PENDING;

            var tipInfo = _tipAsyncRequest.QuickTipInfo;
            _tipAsyncRequest = null;

            if (LanguageService.IsDebugging) {
                if (NeedDebugDataTip(tipInfo, textSpan)) {
                    hintText = "";
                    return (int)TipSuccesses2.TIP_S_NODEFAULTTIP;
                }
            }

            var span = textSpan[0];

            //QuickTipInfo tipInfo = engine.GetQuickTipInfo(FileIndex, loc.Line, loc.Column);

            //Debug.WriteLine(loc.ToVsOutputStringFormat() + "GetDataTipText()");
            var projectInfo = ProjectInfo;

            if (projectInfo == null)
                return (int)TipSuccesses.TIP_S_ONLYIFNOMARKER;

            var taskLocation = IsSecondarySource ? MapSecondaryToPrimaryLocation(loc) : loc;

            var tasks = projectInfo.FindTaks(t => t.CompilerMessage.Location.Contains(taskLocation) && !t.CompilerMessage.IsRelated).ToList();

            if (tasks.Count == 0 && tipInfo == null)
                return (int)TipSuccesses.TIP_S_ONLYIFNOMARKER;

            var hintSpan = GetTokenSpan(span.iStartLine, span.iStartIndex);

            if (tipInfo != null) {
                hintText = tipInfo.Text;

                if (TextSpanHelper.IsEmpty(hintSpan))
                    hintSpan = Utils.SpanFromLocation(tipInfo.Location);
            }

            if (tasks.Count > 0) {
                var locAgg = tasks.Aggregate(Location.Default, (loc1, t) => loc1.Combine(t.CompilerMessage.Location));
                var tasksMsgs = tasks.Select(t => NemerleErrorTaskToString(t));//.ToArray();

                //Debug.WriteLine(token.Type.ToString());
                if (TextSpanHelper.IsEmpty(hintSpan))
                    hintSpan = Utils.SpanFromLocation(locAgg);

                if (hintText != null)
                    hintText += Environment.NewLine;

                hintText += "<font color=\"Green\"><font face=\"Webdings\" size=\"22\">!</font> <b>Compiler messages:</b></font>"
                            + Environment.NewLine + tasksMsgs.Join(Environment.NewLine);
            }

            textSpan[0] = hintSpan; // если не задать не пустой span пересекающийся с исхдным, VS не покажет hint.

            //Debug.WriteLine(Utils.LocationFromSpan(FileIndex, span).ToVsOutputStringFormat() + "result GetDataTipText() text:");
            //Debug.WriteLine(hintText);

            if (hintText != null)
                Service.ShowHint(view, hintSpan, tipInfo == null ? null : tipInfo.GetHintContent, hintText);

             * return (int)TipSuccesses2.TIP_S_NODEFAULTTIP;
            */
            return 1;
        }


    }
}