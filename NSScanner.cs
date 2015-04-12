using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Diagnostics;

namespace NimStudio.NimStudio {

    static class LangConstants {
        public static readonly HashSet<string> keywords = new HashSet<string> {
            "addr", "and", "as", "asm", "atomic",
            "bind", "block", "break",
            "case", "cast", "const", "continue", "converter",
            "discard", "distinct", "div", "do",
            "elif", "else", "end", "enum", "except", "export",
            "finally", "for", "float", "float,", "from",
            "generic",
            "if", "import", "in", "include", "int", "int,", "interface", "is", "isnot", "iterator",
            "lambda", "let",
            "macro", "method", "mixin", "mod",
            "nil", "not", "notin",
            "object", "of", "or", "out",
            "proc", "ptr",
            "raise", "ref", "return",
            "shl", "shr", "static", "string", "string,",
            "template", "try", "tuple", "type",
            "using",
            "var",
            "when", "while", "with", "without",
            "xor",
            "yield"
        };
    }

    enum TStringTypes {
        stNone,
        stNormal,
        stRaw
    }

    //public interface ILineScanner: IScanner {
    //    void SetLine(int line);
    //}

    class NSColorizer : Colorizer, IDisposable {
        private NSScanner m_scanner;

        public NSColorizer(NSLangServ ls, IVsTextLines buffer, NSScanner scanner): base(ls, buffer, scanner) {
            m_scanner = scanner;
        }

        public bool IsClosed { get; private set; }

        public override void CloseColorizer() {
            base.CloseColorizer();
            IsClosed = true;
        }

        void IDisposable.Dispose() {
            base.Dispose();
            IsClosed = true;
        }

        public void SetCurrentLine(int line) {
            //NSScanner scanner = (NSScanner)Scanner;
            m_scanner.m_linenum_curr = line;
        }

        public override int ColorizeLine(int line, int length, IntPtr ptr, int state, uint[] attrs) {
            //NSScanner scanner = (NSScanner)Scanner;
            m_scanner.m_linenum_curr = line;
            int ret;
            ret = base.ColorizeLine(line, length, ptr, state, attrs);
            return ret;
        }
    }

    /*
    public class NSLineColorizer: Colorizer {
        public NSLineColorizer(NSLangServ svc, IVsTextLines buffer, NSScanner scanner): base(svc, buffer, scanner) {
            Debug.Print("NSLineColorizer constructor");
        }

        public override int ColorizeLine(int line, int length, IntPtr ptr, int state, uint[] attrs) {
            Debug.Print("ColorizeLine");
            if (Scanner is ILineScanner) {
                ((ILineScanner)Scanner).SetLine(line);
                Debug.Print("setline");
            } else {
                Debug.Print("nosetline");
            }
            return base.ColorizeLine(line, length, ptr, state, attrs);
        }

        public override int GetColorInfo(string line, int length, int state) {
            Debug.Print("GetColorInfo");
            return base.GetColorInfo(line, length, state);
        }

        public override TokenInfo[] GetLineInfo(IVsTextLines buffer, int line, IVsTextColorState colorState) {
            Debug.Print("GetLineInfo");
            if (Scanner is ILineScanner)
                ((ILineScanner)Scanner).SetLine(line);
            return base.GetLineInfo(buffer, line, colorState);
        }
    }
    */

    class NSScanner: IScanner {
        private IVsTextBuffer m_buffer;
        public NSSource m_nssource;
        public string m_source_line_str;
        private NSTokenizer m_tokenizer;
        public int m_linenum_curr;

        public NSScanner(IVsTextBuffer buffer) {
            m_buffer = buffer;
            Debug.Print("NSScanner");
        }

        public void SetSource(string source, int offset) {
            // source is a line
            Debug.Print("SetSource:" + source + ":" + DateTime.Now.Millisecond.ToString());
            m_source_line_str = source.Substring(offset);
            m_tokenizer = new NSTokenizer(m_source_line_str);
        }

        public bool ScanTokenAndProvideInfoAboutIt(TokenInfo tokenInfo, ref int state) {
            NSScanState flags = (NSScanState)state;
            //Debug.Print("ScanTokenAndProvideInfoAboutIt " + DateTime.Now.Second.ToString());
            //Debug.Print("ScanTokenAndProvideInfoAboutIt " + DateTime.Now.Millisecond.ToString());
            var lastToken = m_tokenizer.Kind;
            switch (m_tokenizer.Kind) {
                case TkType.Eof:
                    return false;
                case TkType.None:
                    tokenInfo.Type = TokenType.Unknown;
                    tokenInfo.Color = TokenColor.Text;
                    break;
                case TkType.Space:
                    tokenInfo.Type = TokenType.WhiteSpace;
                    tokenInfo.Color = TokenColor.Text;
                    break;
                case TkType.NumberInt:
                case TkType.NumberBin:
                case TkType.NumberHex:
                case TkType.NumberOct:
                case TkType.NumberFloat:
                    tokenInfo.Type = TokenType.Literal;
                    tokenInfo.Color = TokenColor.Number;
                    break;
                case TkType.Identifier:
                    tokenInfo.Type = TokenType.Identifier;
                    tokenInfo.Color = TokenColor.Identifier;

                    break;
                case TkType.Keyword:
                    tokenInfo.Type = TokenType.Keyword;
                    tokenInfo.Color = TokenColor.Keyword;
                    break;
                case TkType.StringLit:
                    tokenInfo.Type = TokenType.String;
                    tokenInfo.Color = TokenColor.String;
                    if (!flags.HasFlag(NSScanState.RawStringLit)) {
                        flags ^= NSScanState.NormalStringLit;
                    }
                    break;
                case TkType.StringLitLong:
                    tokenInfo.Type = TokenType.String;
                    tokenInfo.Color = TokenColor.String;
                    flags ^= NSScanState.RawStringLit;
                    break;
                case TkType.CharLit:
                    tokenInfo.Type = TokenType.String;
                    tokenInfo.Color = TokenColor.String;
                    break;
                case TkType.Escape:
                    tokenInfo.Type = TokenType.Unknown;
                    tokenInfo.Color = TokenColor.Text;
                    break;
                case TkType.Operator:
                    tokenInfo.Type = TokenType.Operator;
                    tokenInfo.Color = TokenColor.Text;
                    break;
                case TkType.Punctuation:
                    tokenInfo.Type = TokenType.Text;
                    tokenInfo.Color = TokenColor.Text;
                    break;
                case TkType.Comment:
                    tokenInfo.Type = TokenType.Comment;
                    tokenInfo.Color = TokenColor.Comment;
                    break;
                case TkType.CommentLong:
                    tokenInfo.Type = TokenType.LineComment;
                    tokenInfo.Color = TokenColor.Comment;
                    break;
                case TkType.RegEx:
                case TkType.TagStart:
                case TkType.TagEnd:
                case TkType.Key:
                case TkType.Value:
                case TkType.RawData:
                case TkType.Assembler:
                case TkType.Preprocessor:
                case TkType.Directive:
                case TkType.Command:
                case TkType.Rule:
                case TkType.Hyperlink:
                case TkType.Label:
                case TkType.Reference:
                case TkType.Other:
                    tokenInfo.Type = TokenType.Unknown;
                    tokenInfo.Color = TokenColor.Text;
                    break;
                case TkType.CurlyDotLeft:
                    tokenInfo.Type = TokenType.Delimiter;
                    tokenInfo.Color = TokenColor.Text;
                    break;
                case TkType.CurlyDotRight:
                    tokenInfo.Type = TokenType.Delimiter;
                    tokenInfo.Color = TokenColor.Text;
                    break;
                case TkType.Dot:
                    tokenInfo.Type = TokenType.Operator;
                    tokenInfo.Color = TokenColor.Text;
                    tokenInfo.Trigger = TokenTriggers.MemberSelect;

                    break;
                case TkType.ParenLeft:
                    tokenInfo.Type = TokenType.Delimiter;
                    tokenInfo.Color = TokenColor.Text;
                    tokenInfo.Trigger = TokenTriggers.ParameterStart;
                    break;
                case TkType.ParenRight:
                    tokenInfo.Type = TokenType.Delimiter;
                    tokenInfo.Color = TokenColor.Text;
                    tokenInfo.Trigger = TokenTriggers.ParameterEnd;
                    break;
                case TkType.Comma:
                    tokenInfo.Type = TokenType.Text;
                    tokenInfo.Color = TokenColor.Text;
                    tokenInfo.Trigger = TokenTriggers.ParameterNext;
                    break;
            }
            if (flags.HasFlag(NSScanState.NormalStringLit) || flags.HasFlag(NSScanState.RawStringLit)) {
                tokenInfo.Color = TokenColor.String;
                tokenInfo.Type = TokenType.String;

            }
            state = (int)flags;
            tokenInfo.StartIndex = m_tokenizer.m_linepos_start;
            tokenInfo.EndIndex = m_tokenizer.m_linepos_end;
            m_tokenizer.TokenNext(flags);
            return true;
        }
    }

    class NSTokenizer {
        private TkType m_token_type; // rename token_kind
        private string m_source;
        public int m_linepos_start;
        public int m_linepos_end;
        private int m_token_pos_end;
        //public TStringTypes inString = TStringTypes.stNone;
        private string m_token_next;
        //public int Start {
        //    get {
        //        return m_start;
        //    }
        //}
        //public int End {
        //    get {
        //        return tokenEnd - 1;
        //    }
        //}
        public string NextToken {
            get {
                return m_token_next;
            }
        }
        public TkType Kind {
            get {
                return m_token_type;
            }
        }
        public NSTokenizer(string source) {
            //inString = TStringTypes.stNone;
            m_source = source;
            //Debug.Print("NSTokenizer:" + source + ":" + DateTime.Now.Millisecond.ToString());
            m_linepos_start = 0;
            m_linepos_end = 0;
            TokenNext(NSScanState.None);
        }
        private static int SkipChar(string str, char chr, int idx) {
            if (idx == -1) {
                return idx;
            }
            while (idx + 1 < str.Length && str[idx + 1] == chr) {
                idx++;
            }
            if (idx >= str.Length) {
                return -1;
            }
            return idx;
        }

        private bool CheckEqual(int position, char chr) {
            if (position >= m_source.Length) {
                return false;
            } else {
                return m_source[position] == chr;
            }
        }

        private bool CheckNotEqual(int position, char chr) {
            if (position >= m_source.Length) {
                return true;
            } else {
                return m_source[position] != chr;
            }
        }

        private bool Peek(string teststr) {
            if (m_linepos_start + teststr.Length > m_source.Length)
                return false;
            return false;
            //m_source[m_start

        }


        public void TokenNext(NSScanState flags) {
            /*  if (m_source.Contains("else:"))
                {
                Debugger.Break();
                }*/
            m_linepos_start = m_linepos_end;
            if (m_linepos_end >= m_source.Length) {
                m_token_type = TkType.Eof;
                return;
            }
            if (m_linepos_start >= m_source.Length) {
                m_token_type = TkType.Eof;
                return;
            }
            if (m_source[m_linepos_start] == '#' && flags == NSScanState.None) {
                m_token_type = TkType.Comment;
                m_linepos_end = m_source.Length;
                m_token_pos_end = m_source.Length;
            } else if (m_source[m_linepos_start] == '\'') {
                m_token_type = TkType.CharLit;
                if (m_linepos_start + 2 < m_source.Length && m_source[m_linepos_start + 2] == '\'') {
                    m_linepos_end = m_linepos_start + 3;
                    m_token_pos_end = m_linepos_start + 3;
                } else {
                    m_linepos_end = m_linepos_start + 1;
                    m_token_pos_end = m_linepos_start + 1;
                }
            } else if (m_source[m_linepos_start] == '"') {
                if (m_linepos_start + 2 < m_source.Length && m_source.Substring(m_linepos_start, 3) == "\"\"\"") {
                    m_linepos_end = m_linepos_start + 3;
                    m_token_pos_end = m_linepos_start + 3;
                    m_token_type = TkType.StringLitLong;
                    return;
                } else {
                    m_linepos_end = m_linepos_start + 1;
                    m_token_pos_end = m_linepos_start + 1;
                    m_token_type = TkType.StringLit;
                    return;
                }
            } else if (m_source[m_linepos_start] == '{') {
                if (CheckEqual(m_linepos_start + 1, '.') && CheckNotEqual(m_linepos_start + 2, '.')) {
                    m_linepos_end = m_linepos_start + 2;
                    m_token_pos_end = m_linepos_start + 2;
                    m_token_type = TkType.CurlyDotLeft;
                } else {
                    m_linepos_end = m_linepos_start + 1;
                    m_token_pos_end = m_linepos_start + 1;
                    m_token_type = TkType.Punctuation;

                }

            } else if (m_source[m_linepos_start] == '.') {
                if (CheckEqual(m_linepos_start + 1, '}')) {
                    m_linepos_end = m_linepos_start + 2;
                    m_token_pos_end = m_linepos_start + 2;
                    m_token_type = TkType.CurlyDotRight;
                } else {
                    m_linepos_end = m_linepos_start + 1;
                    m_token_pos_end = m_linepos_start + 1;
                    m_token_type = TkType.Dot;
                }
            } else if (m_source[m_linepos_start] == '(') {
                m_token_type = TkType.ParenLeft;
                m_linepos_end = m_linepos_start + 1;
                m_token_pos_end = m_linepos_start + 1;
            } else if (m_source[m_linepos_start] == ')') {
                m_token_type = TkType.ParenRight;
                m_linepos_end = m_linepos_start + 1;
                m_token_pos_end = m_linepos_start + 1;
            } else if (m_source[m_linepos_start] == ',') {
                m_token_type = TkType.Comma;
                m_linepos_end = m_linepos_start + 1;
                m_token_pos_end = m_linepos_start + 1;
            } else if (m_source[m_linepos_start] == '*') {
                m_token_type = TkType.Punctuation;
                m_linepos_end = m_linepos_start + 1;
                m_token_pos_end = m_linepos_start + 1;
            } else if (m_source[m_linepos_start] == ':') {
                m_token_type = TkType.Punctuation;
                m_linepos_end = m_linepos_start + 1;
                m_token_pos_end = m_linepos_start + 1;
            } else if (m_source[m_linepos_start] == ' ') {
                m_token_type = TkType.Space;
                m_linepos_end = m_linepos_start + 1;
                m_token_pos_end = m_linepos_start + 1;
            } else if (m_source[m_linepos_start] == '[') {
                m_token_type = TkType.BracketLeft;
                m_linepos_end = m_linepos_start + 1;
                m_token_pos_end = m_linepos_start + 1;
            } else if (m_source[m_linepos_start] == ']') {
                m_token_type = TkType.BracketRight;
                m_linepos_end = m_linepos_start + 1;
                m_token_pos_end = m_linepos_start + 1;
            } else {

                if (m_linepos_start >= m_source.Length) {
                    m_token_type = TkType.Eof;
                    return;
                }
                m_token_type = TkType.Other;
                var spaceIdx = m_source.IndexOf(' ', m_linepos_start); // idx of next whitespace after start
                m_token_pos_end = spaceIdx;
                spaceIdx = SkipChar(m_source, ' ', spaceIdx); // ????
                var searchStart = m_linepos_start;
                var quoteIdx = m_source.IndexOf('"', searchStart);
                var parenIdx = m_source.IndexOf('(', searchStart);
                var closeParenIdx = m_source.IndexOf(')', searchStart);
                var starIdx = m_source.IndexOf('*', searchStart);
                var colonIdx = m_source.IndexOf(':', searchStart);
                var dotidx = m_source.IndexOf('.', searchStart);
                var squareIdx = m_source.IndexOf('[', searchStart);
                var closeSquareIdx = m_source.IndexOf(']', searchStart);
                m_linepos_end = spaceIdx;
                if (m_linepos_end == -1) {
                    m_token_next = m_source.Substring(m_linepos_start);
                    m_linepos_end = m_source.Length;
                    m_token_pos_end = m_source.Length;
                }
                if (squareIdx != -1 && squareIdx < m_linepos_end) {
                    m_linepos_end = squareIdx;
                    m_token_pos_end = squareIdx;
                }
                if (closeSquareIdx != -1 && closeSquareIdx < m_linepos_end) {
                    m_linepos_end = closeSquareIdx;
                    m_token_pos_end = closeSquareIdx;
                }
                if (parenIdx != -1 && parenIdx < m_linepos_end) {
                    m_token_type = TkType.Identifier;
                    m_linepos_end = parenIdx;
                    m_token_pos_end = parenIdx;
                }
                if (closeParenIdx != -1 && closeParenIdx < m_linepos_end) {
                    m_linepos_end = closeParenIdx;
                    m_token_pos_end = closeParenIdx;
                }
                if (starIdx != -1 && starIdx < m_linepos_end) {
                    m_linepos_end = starIdx;
                    m_token_pos_end = starIdx;
                    m_token_type = TkType.Identifier;
                }
                if (quoteIdx != -1 && quoteIdx < m_linepos_end) {
                    m_linepos_end = quoteIdx;
                    m_token_pos_end = quoteIdx;
                }
                if (colonIdx != -1 && colonIdx < m_linepos_end) {
                    m_linepos_end = colonIdx;
                    m_token_pos_end = colonIdx;
                }
                if (dotidx != -1 && dotidx < m_linepos_end) {
                    m_linepos_end = dotidx;
                    m_token_pos_end = dotidx;
                }

                m_token_next = m_source.Substring(m_linepos_start, (m_linepos_end - m_linepos_start));

                if (LangConstants.keywords.Contains(m_token_next)) {
                    m_token_type = TkType.Keyword;
                }
            }
        }

    }
    [Flags]
    enum NSScanState: int {
        None = 0,
        RawStringLit = 1,
        NormalStringLit = 2,
        Pragma = 4
    }


    public enum TkType: int {
        Assembler,
        BracketLeft, 
        BracketRight,
        CharLit, 
        Comma,
        Command, 
        Comment, 
        CommentLong, 
        CurlyDotLeft, 
        CurlyDotRight, 
        Directive, 
        Dot, 
        Eof, 
        Escape,
        Hyperlink, 
        Identifier, 
        Key, 
        Keyword, 
        Label,
        None, 
        NumberBin, 
        NumberFloat, 
        NumberHex,
        NumberInt, 
        NumberOct, 
        Operator, 
        Other, 
        ParenLeft, 
        ParenRight,
        Preprocessor, 
        Punctuation, 
        RawData, 
        Reference, 
        RegEx,
        Rule, 
        Space, 
        StringLit,
        StringLitLong, 
        TagEnd, 
        TagStart, 
        Value, 
    }

    /*
    tkInvalid, tkEof,
    tkSymbol, # keywords:
    tkAddr, tkAnd, tkAs, tkAsm, tkAtomic,
    tkBind, tkBlock, tkBreak, tkCase, tkCast,
    tkConst, tkContinue, tkConverter,
    tkDefer, tkDiscard, tkDistinct, tkDiv, tkDo,
    tkElif, tkElse, tkEnd, tkEnum, tkExcept, tkExport,
    tkFinally, tkFor, tkFrom, tkFunc,
    tkGeneric, tkIf, tkImport, tkIn, tkInclude, tkInterface,
    tkIs, tkIsnot, tkIterator,
    tkLet,
    tkMacro, tkMethod, tkMixin, tkMod, tkNil, tkNot, tkNotin,
    tkObject, tkOf, tkOr, tkOut,
    tkProc, tkPtr, tkRaise, tkRef, tkReturn, tkShl, tkShr, tkStatic,
    tkTemplate,
    tkTry, tkTuple, tkType, tkUsing,
    tkVar, tkWhen, tkWhile, tkWith, tkWithout, tkXor,
    tkYield, # end of keywords
    tkIntLit, tkInt8Lit, tkInt16Lit, tkInt32Lit, tkInt64Lit,
    tkUIntLit, tkUInt8Lit, tkUInt16Lit, tkUInt32Lit, tkUInt64Lit,
    tkFloatLit, tkFloat32Lit, tkFloat64Lit, tkFloat128Lit,
    tkStrLit, tkRStrLit, tkTripleStrLit,
    tkGStrLit, tkGTripleStrLit, tkCharLit, tkParLe, tkParRi, tkBracketLe,
    tkBracketRi, tkCurlyLe, tkCurlyRi,
    tkBracketDotLe, tkBracketDotRi, # [. and  .]
    tkCurlyDotLe, tkCurlyDotRi, # {.  and  .}
    tkParDotLe, tkParDotRi,   # (. and .)
    tkComma, tkSemiColon,
    tkColon, tkColonColon, tkEquals, tkDot, tkDotDot,
    tkOpr, tkComment, tkAccent,
    tkSpaces, tkInfixOpr, tkPrefixOpr, tkPostfixOpr,

    // sorted

    tkAccent,
    tkAddr,
    tkAnd,
    tkAs,
    tkAsm,
    tkAtomic,
    tkBind,
    tkBlock,
    tkBracketDotLe,
    tkBracketDotRi,
    tkBracketLe,
    tkBracketRi,
    tkBreak,
    tkCase,
    tkCast,
    tkCharLit,
    tkColon,
    tkColonColon,
    tkComma,
    tkComment,
    tkConst,
    tkContinue,
    tkConverter,
    tkCurlyDotLe,
    tkCurlyDotRi,
    tkCurlyLe,
    tkCurlyRi,
    tkDefer,
    tkDiscard,
    tkDistinct,
    tkDiv,
    tkDo,
    tkDot,
    tkDotDot,
    tkElif,
    tkElse,
    tkEnd,
    tkEnum,
    tkEof,
    tkEquals,
    tkExcept,
    tkExport,
    tkFinally,
    tkFloat128Lit,
    tkFloat32Lit,
    tkFloat64Lit,
    tkFloatLit,
    tkFor,
    tkFrom,
    tkFunc,
    tkGStrLit,
    tkGTripleStrLit,
    tkGeneric,
    tkIf,
    tkImport,
    tkIn,
    tkInclude,
    tkInfixOpr,
    tkInt16Lit,
    tkInt32Lit,
    tkInt64Lit,
    tkInt8Lit,
    tkIntLit,
    tkInterface,
    tkInvalid,
    tkIs,
    tkIsnot,
    tkIterator,
    tkLet,
    tkMacro,
    tkMethod,
    tkMixin,
    tkMod,
    tkNil,
    tkNot,
    tkNotin,
    tkObject,
    tkOf,
    tkOpr,
    tkOr,
    tkOut,
    tkParDotLe,
    tkParDotRi,
    tkParLe,
    tkParRi,
    tkPostfixOpr,
    tkPrefixOpr,
    tkProc,
    tkPtr,
    tkRStrLit,
    tkRaise,
    tkRef,
    tkReturn,
    tkSemiColon,
    tkShl,
    tkShr,
    tkSpaces,
    tkStatic,
    tkStrLit,
    tkSymbol,
    tkTemplate,
    tkTripleStrLit,
    tkTry,
    tkTuple,
    tkType,
    tkUInt16Lit,
    tkUInt32Lit,
    tkUInt64Lit,
    tkUInt8Lit,
    tkUIntLit,
    tkUsing,
    tkVar,
    tkWhen,
    tkWhile,
    tkWith,
    tkWithout,
    tkXor,
    tkYield,

     */

}
