using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Diagnostics;
using VSTkColor = Microsoft.VisualStudio.Package.TokenColor;

namespace NimStudio.NimStudio {

    static class LangConst {
        public static readonly HashSet<string> keywords = new HashSet<string> {
            "addr", "and", "as", "asm", "atomic",
            "bind", "block", "break",
            "case", "cast", "const", "continue", "converter",
            "discard", "distinct", "div", "do",
            "elif", "else", "end", "except", "export",
            "finally", "for", "from",
            "generic",
            "if", "import", "in", "include", "interface", "is", "isnot", "iterator",
            "lambda", "let",
            "macro", "method", "mixin", "mod",
            "nil", "not", "notin",
            "object", "of", "or", "out",
            "proc", "ptr",
            "raise", "ref", "return",
            "shl", "shr", "static",
            "template", "try", "tuple", "type",
            "using",
            "var",
            "when", "while", "with", "without",
            "xor",
            "yield"
        };

        public static readonly HashSet<string> datatypes = new HashSet<string> {
            "bool",
            "char",
            "enum",
            "float","float32", "float64",
            "int", "int8", "int16", "int32", "int64", "uint", "uint8", "uint16", "uint32", "uint64",
            "seq",
            "string", "cstring",
            "tuple",
            "void",
        };

        // workaround to colorize some system procs that are often used without parens, until import scanner is implemented
        public static readonly HashSet<string> proctypes = new HashSet<string> {
            "echo",
        };


    }

    enum TStringTypes {
        stNone,
        stNormal,
        stRaw
    }

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
            m_scanner.m_linenum_curr = line;
        }

        public override int ColorizeLine(int line, int length, IntPtr ptr, int state, uint[] attrs) {
            m_scanner.m_linenum_curr = line;
            int ret;
            ret = base.ColorizeLine(line, length, ptr, state, attrs);
            return ret;
        }
    }

    class NSScanner: IScanner {
        private IVsTextBuffer m_buffer;
        public NSSource m_nssource;
        public string m_source;
        public int m_linenum_curr;
        // lexer.nim OpChars {'+', '-', '*', '/', '\\', '<', '>', '!', '?', '^', '.', '|', '=', '%', '&', '$', '@', '~', ':'}
        public static char[] token_delims = new char[] { ' ', '"', '(', ')', '*', ':', '.', '[', ']', ',', '=', ';', '+', '-', '/', '<', '>', '!', '?', '^', '|', '%', '&', '$', '@', '~'};
        public static string token_nums = "0123456789xX_'iIuUaAbBcCdDeEfF";
        public int m_tokenpos_start;
        public int m_tokenpos_start_next;
        public TkType m_token_type;
        public TkType m_token_type_prev;
        public string m_token_keyword_prev;
        public char m_token_delim_prev='\0';
        public List<TkType> m_tokens_type=new List<TkType>();
        public List<int> m_tokens_delims=new List<int>();

        public NSScanner(IVsTextBuffer buffer) {
            m_buffer = buffer;
            Debug.Print("NSScanner");
        }

        // IVsColorizer->ColorizeLine (base) calls SetSource with text for the line
        // SetSource calls TokenNextGet(state=none) which processes the first token
        // IVsColorizer->ColorizeLine calls ScanTokenAndProvideInfoAboutIt(state) repeatedly until it returns false
        // ScanTokenAndProvideInfoAboutIt calls TokenNextGet(state) to process subsequent tokens
        public void SetSource(string source, int offset) {
            // source is a line
            m_source = source.Substring(offset);
            m_tokenpos_start = 0;
            m_tokenpos_start_next = 0;
            m_tokens_type.Clear();
            m_tokens_delims.Clear();
            for (int lspot=0; lspot < m_source.Length; lspot++) {
                if (token_delims.Contains(m_source[lspot]))
                    m_tokens_delims.Add(lspot);
            }
            TokenNextGet(NSScanState.None);
        }

        private bool CharNext(char ctest, int pos=1) {
            if (m_tokenpos_start + pos >= m_source.Length)
                return false;
            if (m_source[m_tokenpos_start+pos]==ctest)
                return true;
            return false;
        }

        private char CharNext() {
            if (m_tokenpos_start + 1 >= m_source.Length)
                return '\0';
            return m_source[m_tokenpos_start+1];
        }

        private char DelimNext(int from_pos) {
            if (from_pos >= m_source.Length)
                return '\0';
            int delimpos = m_source.IndexOfAny(NSScanner.token_delims, from_pos);
            if (delimpos!=-1)
                return m_token_delim_prev=m_source[delimpos];
            else
                return '\0';
        }

        public void TokenNextGet(NSScanState flags) {

            m_tokenpos_start = m_tokenpos_start_next;
            if (m_tokenpos_start_next >= m_source.Length) {
                m_token_type = TkType.Eof;
                return;
            }

            switch (m_source[m_tokenpos_start]) {
                case '#':
                    if (flags == NSScanState.None) {
                        m_token_type = TkType.Comment;
                        m_tokenpos_start_next = m_source.Length;
                        return;
                    }
                    break;

                case '\'':
                    m_token_type = TkType.CharLit;
                    // 'a', '\"', 'xAA', '\9', '\32', '\255'  ,''=should not compile
                    if (CharNext('\'',2)) { // 'a'
                        m_tokenpos_start_next = m_tokenpos_start + 3;
                    } else if (CharNext('\'',3)){ // '\"'
                        m_tokenpos_start_next = m_tokenpos_start + 4;
                    } else if (CharNext('\'',4)){ // 'xAA'
                        m_tokenpos_start_next = m_tokenpos_start + 5;
                    } else if (CharNext('\'',5)){ // '\255'
                        m_tokenpos_start_next = m_tokenpos_start + 6;
                    } else { // unfinished char literal?
                        m_token_type = TkType.Other;
                        m_tokenpos_start_next = m_tokenpos_start + 1;
                    }
                    return;

                case '"':
                    if (m_tokenpos_start + 2 < m_source.Length && m_source.Substring(m_tokenpos_start, 3) == "\"\"\"") {
                        m_tokenpos_start_next = m_tokenpos_start + 3;
                        m_token_type = TkType.StringLitLong;
                    } else {
                        m_tokenpos_start_next = m_tokenpos_start + 1;
                        m_token_type = TkType.StringLit;
                    }
                    return;

                case '{':
                    if (CharNext('.') && !CharNext('.',2)) {
                        m_tokenpos_start_next = m_tokenpos_start + 2;
                        m_token_type = TkType.CurlyDotLeft;
                    } else {
                        m_tokenpos_start_next = m_tokenpos_start + 1;
                        m_token_type = TkType.Punctuation;
                    }
                    return;

                case '.':
                    // float literals can't be declared/assigned as .1 (compile error)
                    if (CharNext('}')) {
                        m_tokenpos_start_next = m_tokenpos_start + 2;
                        m_token_type = TkType.CurlyDotRight;
                    } else {
                        m_tokenpos_start_next = m_tokenpos_start + 1;
                        m_token_type = TkType.Dot;
                    }
                    return;

                case '(':
                    m_token_type = TkType.ParenLeft;
                    m_tokenpos_start_next = m_tokenpos_start + 1;
                    return;

                case ')':
                    m_token_type = TkType.ParenRight;
                    m_tokenpos_start_next = m_tokenpos_start + 1;
                    return;

                case ',':
                    m_token_type = TkType.Comma;
                    m_tokenpos_start_next = m_tokenpos_start + 1;
                    return;

                case '*':  case ':':  case '=':  case ';':  case '/':  case '<': case '>':  case '!':  case '-':  
                case '^':  case '|':  case '%':  case '&':  case '$':  case '@': case '~':  case '?':  case '+': 
                    m_token_type = TkType.Punctuation;
                    m_tokenpos_start_next = m_tokenpos_start + 1;
                    return;

                case ' ':
                    m_token_type = TkType.Space;
                    m_tokenpos_start_next = m_tokenpos_start;
                    while (m_tokenpos_start_next + 1 < m_source.Length && m_source[m_tokenpos_start_next + 1] == ' ')
                        m_tokenpos_start_next++;
                    m_tokenpos_start_next++;
                    return;

                case '0': case '1': case '2': case '3': case '4': case '5': case '6': case '7': case '8': case '9':
                    m_token_type = TkType.NumberInt;
                    m_tokenpos_start_next = m_tokenpos_start;
                    while (m_tokenpos_start_next + 1 < m_source.Length) {
                        if (NSScanner.token_nums.IndexOf(m_source[m_tokenpos_start_next + 1]) != -1)
                            m_tokenpos_start_next++;
                        break;
                    }
                    m_tokenpos_start_next++;
                    return;

                case '[':
                    m_token_type = TkType.BracketLeft;
                    m_tokenpos_start_next = m_tokenpos_start + 1;
                    return;

                case ']':
                    m_token_type = TkType.BracketRight;
                    m_tokenpos_start_next = m_tokenpos_start + 1;
                    return;

            }

            m_token_type = TkType.Other;
            m_tokenpos_start_next = m_source.IndexOfAny(NSScanner.token_delims, m_tokenpos_start);
            //if (m_tokenpos_start_next!=-1)
            //    m_token_delim_prev=m_source[m_tokenpos_start_next];
            //else
            //    m_token_delim_prev='\0';

            string token_str;
            if (m_tokenpos_start_next == -1) {
                token_str = m_source.Substring(m_tokenpos_start);
                m_tokenpos_start_next = m_source.Length;
            } else {
                token_str = m_source.Substring(m_tokenpos_start, (m_tokenpos_start_next - m_tokenpos_start));
            }

            if (LangConst.keywords.Contains(token_str)) {
                m_token_type = TkType.Keyword;
                m_token_keyword_prev = token_str.ToLower();
                return;
            }

            if (LangConst.datatypes.Contains(token_str)) {
                m_token_type = TkType.DataType;
                return;
            }

            if (LangConst.proctypes.Contains(token_str)) {
                m_token_type = TkType.Procedure;
                return;
            }

            int delim_idx=0;
            while (delim_idx < m_tokens_delims.Count && m_tokens_delims[delim_idx]<m_tokenpos_start)
                delim_idx++;

            while (delim_idx<m_tokens_delims.Count){           
                if (m_source[m_tokens_delims[delim_idx]]==' ') { delim_idx++; continue; };
                if (m_source[m_tokens_delims[delim_idx]]=='*') { delim_idx++; continue; };
                if (m_source[m_tokens_delims[delim_idx]]=='(') { m_token_type = TkType.Procedure; break; };
                break;
            }

            //int delim_next_pos = m_source.IndexOfAny(NSScanner.token_delims, m_tokenpos_start_next);
            //char delim_next_char = '\0';
            //if (delim_next_pos != -1) {
            //    delim_next_char = m_source[delim_next_pos];
            //}

            //if (delim_next_char=='(' || m_token_keyword_prev=="proc") {
            //    m_token_type = TkType.Procedure;
            //    return;
            //}

        }

        // populates token_info using m_ vars
        public bool ScanTokenAndProvideInfoAboutIt(TokenInfo token_info, ref int state) {
            NSScanState flags = (NSScanState)state;
            //Debug.Print("ScanTokenAndProvideInfoAboutIt " + DateTime.Now.Second.ToString());
            //Debug.Print("ScanTokenAndProvideInfoAboutIt " + DateTime.Now.Millisecond.ToString());
            m_token_type_prev = m_token_type;
            switch (m_token_type) {
                case TkType.Eof:
                    return false; // end of line
                case TkType.None:
                    token_info.Type = TokenType.Unknown;
                    token_info.Color = (VSTkColor)TokenColor.Text;
                    break;
                case TkType.Space:
                    token_info.Type = TokenType.WhiteSpace;
                    token_info.Color = (VSTkColor)TokenColor.Text;
                    break;
                case TkType.NumberInt:
                case TkType.NumberBin:
                case TkType.NumberHex:
                case TkType.NumberOct:
                case TkType.NumberFloat:
                    token_info.Type = TokenType.Literal;
                    token_info.Color = (VSTkColor)TokenColor.Number;
                    break;
                case TkType.Identifier:
                    token_info.Type = TokenType.Identifier;
                    token_info.Color = (VSTkColor)TokenColor.Identifier;
                    break;
                case TkType.Keyword:
                    token_info.Type = TokenType.Keyword;
                    token_info.Color = (VSTkColor)TokenColor.Keyword;
                    break;
                case TkType.DataType:
                    token_info.Type = TokenType.Unknown;
                    token_info.Color = (VSTkColor)TokenColor.DataType;
                    break;
                case TkType.Procedure:
                    token_info.Type = TokenType.Unknown;
                    token_info.Color = (VSTkColor)TokenColor.Procedure;
                    break;
                case TkType.StringLit:
                    token_info.Type = TokenType.String;
                    token_info.Color = (VSTkColor)TokenColor.String;
                    if (!flags.HasFlag(NSScanState.StringLitRaw)) {
                        flags ^= NSScanState.StringLit;
                    }
                    break;
                case TkType.StringLitLong:
                    token_info.Type = TokenType.String;
                    token_info.Color = (VSTkColor)TokenColor.String;
                    flags ^= NSScanState.StringLitRaw;
                    break;
                case TkType.CharLit:
                    token_info.Type = TokenType.String;
                    token_info.Color = (VSTkColor)TokenColor.String;
                    break;
                case TkType.Escape:
                    token_info.Type = TokenType.Unknown;
                    token_info.Color = (VSTkColor)TokenColor.Text;
                    break;
                case TkType.Operator:
                    token_info.Type = TokenType.Operator;
                    token_info.Color = (VSTkColor)TokenColor.Punctuation;
                    break;
                case TkType.Punctuation:
                    token_info.Type = TokenType.Text;
                    token_info.Color = (VSTkColor)TokenColor.Punctuation;
                    break;
                case TkType.Comment:
                    token_info.Type = TokenType.Comment;
                    token_info.Color = (VSTkColor)TokenColor.Comment;
                    break;
                case TkType.CommentLong:
                    token_info.Type = TokenType.LineComment;
                    token_info.Color = (VSTkColor)TokenColor.Comment;
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
                    token_info.Type = TokenType.Unknown;
                    token_info.Color = (VSTkColor)TokenColor.Text;
                    break;
                case TkType.CurlyDotLeft:
                case TkType.CurlyDotRight:
                case TkType.BracketLeft:
                case TkType.BracketRight:
                    token_info.Type = TokenType.Delimiter;
                    token_info.Color = (VSTkColor)TokenColor.Punctuation;
                    break;
                case TkType.Dot:
                    token_info.Type = TokenType.Operator;
                    token_info.Color = (VSTkColor)TokenColor.Punctuation;
                    token_info.Trigger = TokenTriggers.MemberSelect;
                    break;
                case TkType.ParenLeft:
                    token_info.Type = TokenType.Delimiter;
                    token_info.Color = (VSTkColor)TokenColor.Punctuation;
                    token_info.Trigger = TokenTriggers.ParameterStart;
                    break;
                case TkType.ParenRight:
                    token_info.Type = TokenType.Delimiter;
                    token_info.Color = (VSTkColor)TokenColor.Punctuation;
                    token_info.Trigger = TokenTriggers.ParameterEnd;
                    break;
                case TkType.Comma:
                    token_info.Type = TokenType.Text;
                    token_info.Color = (VSTkColor)TokenColor.Punctuation;
                    token_info.Trigger = TokenTriggers.ParameterNext;
                    break;
            }
            if (flags.HasFlag(NSScanState.StringLit) || flags.HasFlag(NSScanState.StringLitRaw)) {
                token_info.Color = (VSTkColor)TokenColor.String;
                token_info.Type = TokenType.String;

            }
            state = (int)flags;
            token_info.StartIndex = m_tokenpos_start;
            token_info.EndIndex = m_tokenpos_start_next-1;
            //Debug.WriteLine("$" + m_source.Substring( m_tokenpos_start,m_tokenpos_start_next - 
                //m_tokenpos_start) + "$" + m_token_type);
            TokenNextGet(flags);
            m_tokens_type.Add(m_token_type);
            return true;
        }
    }

    public enum TokenColor {
        Text = 0,
        Keyword = 1,
        Comment = 2,
        Identifier = 3,
        String = 4,
        Number = 5,
        Procedure = 6,
        DataType = 7,
        Punctuation = 8,
    }
    //public static int NSTokenColor_Procedure = 1;


    [Flags]
    enum NSScanState: int {
        None = 0,
        StringLitRaw = 1,
        StringLit = 2,
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
        DataType,
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
        Procedure,
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
