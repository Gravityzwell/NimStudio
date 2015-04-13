using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Diagnostics;

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
        public static char[] token_delims = new char[] { ' ', '"', '(', ')', '*', ':', '.', '[', ']', ',' };
        public static char[] token_para_star = new char[] { '(', '*' };
        public static string token_nums = "0123456789";

        public NSScanner(IVsTextBuffer buffer) {
            m_buffer = buffer;
            Debug.Print("NSScanner");
        }

        // Called first. Calls NSTokenizer, which sets m_ vars. Then ScanTokenAndProvideInfoAboutIt is called, which populates token_info
        public void SetSource(string source, int offset) {
            // source is a line
            //Debug.Print("SetSource:" + source + ":" + DateTime.Now.Millisecond.ToString());
            m_source_line_str = source.Substring(offset);
            m_tokenizer = new NSTokenizer(m_source_line_str);
        }

        // populates token_info using m_ vars set in NSTokenizer
        public bool ScanTokenAndProvideInfoAboutIt(TokenInfo token_info, ref int state) {
            NSScanState flags = (NSScanState)state;
            //Debug.Print("ScanTokenAndProvideInfoAboutIt " + DateTime.Now.Second.ToString());
            //Debug.Print("ScanTokenAndProvideInfoAboutIt " + DateTime.Now.Millisecond.ToString());
            var lastToken = m_tokenizer.m_token_type;
            switch (m_tokenizer.m_token_type) {
                case TkType.Eof:
                    return false;
                case TkType.None:
                    token_info.Type = TokenType.Unknown;
                    token_info.Color = TokenColor.Text;
                    break;
                case TkType.Space:
                    token_info.Type = TokenType.WhiteSpace;
                    token_info.Color = TokenColor.Text;
                    break;
                case TkType.NumberInt:
                case TkType.NumberBin:
                case TkType.NumberHex:
                case TkType.NumberOct:
                case TkType.NumberFloat:
                    token_info.Type = TokenType.Literal;
                    token_info.Color = TokenColor.Number;
                    break;
                case TkType.Identifier:
                    token_info.Type = TokenType.Identifier;
                    token_info.Color = TokenColor.Identifier;

                    break;
                case TkType.Keyword:
                    token_info.Type = TokenType.Keyword;
                    token_info.Color = TokenColor.Keyword;
                    break;
                case TkType.DataType:
                    token_info.Type = TokenType.Unknown;
                    token_info.Color = TokenColor.Number+2;
                    break;
                case TkType.StringLit:
                    token_info.Type = TokenType.String;
                    token_info.Color = TokenColor.String;
                    if (!flags.HasFlag(NSScanState.StringLitRaw)) {
                        flags ^= NSScanState.StringLit;
                    }
                    break;
                case TkType.StringLitLong:
                    token_info.Type = TokenType.String;
                    token_info.Color = TokenColor.String;
                    flags ^= NSScanState.StringLitRaw;
                    break;
                case TkType.CharLit:
                    token_info.Type = TokenType.String;
                    token_info.Color = TokenColor.String;
                    break;
                case TkType.Escape:
                    token_info.Type = TokenType.Unknown;
                    token_info.Color = TokenColor.Text;
                    break;
                case TkType.Operator:
                    token_info.Type = TokenType.Operator;
                    token_info.Color = TokenColor.Text;
                    break;
                case TkType.Punctuation:
                    token_info.Type = TokenType.Text;
                    token_info.Color = TokenColor.Text;
                    break;
                case TkType.Comment:
                    token_info.Type = TokenType.Comment;
                    token_info.Color = TokenColor.Comment;
                    break;
                case TkType.CommentLong:
                    token_info.Type = TokenType.LineComment;
                    token_info.Color = TokenColor.Comment;
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
                    token_info.Color = TokenColor.Text;
                    break;
                case TkType.CurlyDotLeft:
                    token_info.Type = TokenType.Delimiter;
                    token_info.Color = TokenColor.Text;
                    break;
                case TkType.CurlyDotRight:
                    token_info.Type = TokenType.Delimiter;
                    token_info.Color = TokenColor.Text;
                    break;
                case TkType.Dot:
                    token_info.Type = TokenType.Operator;
                    token_info.Color = TokenColor.Text;
                    token_info.Trigger = TokenTriggers.MemberSelect;
                    break;
                case TkType.ParenLeft:
                    token_info.Type = TokenType.Delimiter;
                    token_info.Color = TokenColor.Text;
                    token_info.Trigger = TokenTriggers.ParameterStart;
                    break;
                case TkType.ParenRight:
                    token_info.Type = TokenType.Delimiter;
                    token_info.Color = TokenColor.Text;
                    token_info.Trigger = TokenTriggers.ParameterEnd;
                    break;
                case TkType.Comma:
                    token_info.Type = TokenType.Text;
                    token_info.Color = TokenColor.Text;
                    token_info.Trigger = TokenTriggers.ParameterNext;
                    break;
            }
            if (flags.HasFlag(NSScanState.StringLit) || flags.HasFlag(NSScanState.StringLitRaw)) {
                token_info.Color = TokenColor.String;
                token_info.Type = TokenType.String;

            }
            state = (int)flags;
            token_info.StartIndex = m_tokenizer.m_tokenpos_start;
            token_info.EndIndex = m_tokenizer.m_tokenpos_start_next-1;
            Debug.WriteLine("$" + m_source_line_str.Substring( token_info.StartIndex,token_info.EndIndex - token_info.StartIndex+1) + "$" + m_tokenizer.m_token_type);
            m_tokenizer.TokenNext(flags);
            return true;
        }
    }

    class NSTokenizer {
        public TkType m_token_type;
        private string m_source;
        public int m_tokenpos_start;
        public int m_tokenpos_start_next;
        private int m_tokenpos_end;
        private string m_token_next;
        public NSTokenizer(string source) {
            //inString = TStringTypes.stNone;
            m_source = source;
            //Debug.Print("NSTokenizer:" + source + ":" + DateTime.Now.Millisecond.ToString());
            m_tokenpos_start = 0;
            m_tokenpos_start_next = 0;
            TokenNext(NSScanState.None);
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

        private char PeekChar(int pos) {
            if (m_tokenpos_start + pos > m_source.Length)
                return '\0';
            return m_source[m_tokenpos_start + pos];
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

        public void TokenNext(NSScanState flags) {
            m_tokenpos_start = m_tokenpos_start_next;
            if (m_tokenpos_start_next >= m_source.Length) {
                m_token_type = TkType.Eof;
                return;
            }
            if (m_tokenpos_start >= m_source.Length) {
                m_token_type = TkType.Eof;
                return;
            }
            char char_curr = m_source[m_tokenpos_start];
            if (char_curr == '#' && flags == NSScanState.None) {
                m_token_type = TkType.Comment;
                m_tokenpos_start_next = m_source.Length;
                m_tokenpos_end = m_source.Length;
            } else if (char_curr == '\'') {
                m_token_type = TkType.CharLit;
                if (m_tokenpos_start + 2 < m_source.Length && m_source[m_tokenpos_start + 2] == '\'') {
                    m_tokenpos_start_next = m_tokenpos_start + 3;
                    m_tokenpos_end = m_tokenpos_start + 3;
                } else {
                    m_tokenpos_start_next = m_tokenpos_start + 1;
                    m_tokenpos_end = m_tokenpos_start + 1;
                }
            } else if (char_curr == '"') {
                if (m_tokenpos_start + 2 < m_source.Length && m_source.Substring(m_tokenpos_start, 3) == "\"\"\"") {
                    m_tokenpos_start_next = m_tokenpos_start + 3;
                    m_tokenpos_end = m_tokenpos_start + 3;
                    m_token_type = TkType.StringLitLong;
                    return;
                } else {
                    m_tokenpos_start_next = m_tokenpos_start + 1;
                    m_tokenpos_end = m_tokenpos_start + 1;
                    m_token_type = TkType.StringLit;
                    return;
                }
            } else if (char_curr == '{') {
                if (CharNext('.') && !CharNext('.',2)) {
                    m_tokenpos_start_next = m_tokenpos_start + 2;
                    m_tokenpos_end = m_tokenpos_start + 2;
                    m_token_type = TkType.CurlyDotLeft;
                } else {
                    m_tokenpos_start_next = m_tokenpos_start + 1;
                    m_tokenpos_end = m_tokenpos_start + 1;
                    m_token_type = TkType.Punctuation;
                }
            } else if (char_curr == '.') {
                if (CharNext('}')) {
                    m_tokenpos_start_next = m_tokenpos_start + 2;
                    m_tokenpos_end = m_tokenpos_start + 2;
                    m_token_type = TkType.CurlyDotRight;
                //} else if (CharNext() >= '0' &&  CharNext() <= '9') {
                } else {
                    m_tokenpos_start_next = m_tokenpos_start + 1;
                    m_tokenpos_end = m_tokenpos_start + 1;
                    m_token_type = TkType.Dot;
                }
            } else if (char_curr == '(') {
                m_token_type = TkType.ParenLeft;
                m_tokenpos_start_next = m_tokenpos_start + 1;
                m_tokenpos_end = m_tokenpos_start + 1;
            } else if (char_curr == ')') {
                m_token_type = TkType.ParenRight;
                m_tokenpos_start_next = m_tokenpos_start + 1;
                m_tokenpos_end = m_tokenpos_start + 1;
            } else if (char_curr == ',') {
                m_token_type = TkType.Comma;
                m_tokenpos_start_next = m_tokenpos_start + 1;
                m_tokenpos_end = m_tokenpos_start + 1;
            } else if (char_curr == '*') {
                m_token_type = TkType.Punctuation;
                m_tokenpos_start_next = m_tokenpos_start + 1;
                m_tokenpos_end = m_tokenpos_start + 1;
            } else if (char_curr == ':') {
                m_token_type = TkType.Punctuation;
                m_tokenpos_start_next = m_tokenpos_start + 1;
                m_tokenpos_end = m_tokenpos_start + 1;
            } else if (char_curr == ' ') {
                m_token_type = TkType.Space;
                m_tokenpos_start_next = m_tokenpos_start + 1;
                m_tokenpos_end = m_tokenpos_start + 1;
            } else if (char_curr == '[') {
                m_token_type = TkType.BracketLeft;
                m_tokenpos_start_next = m_tokenpos_start + 1;
                m_tokenpos_end = m_tokenpos_start + 1;
            } else if (char_curr == ']') {
                m_token_type = TkType.BracketRight;
                m_tokenpos_start_next = m_tokenpos_start + 1;
                m_tokenpos_end = m_tokenpos_start + 1;
            } else {

                if (m_tokenpos_start >= m_source.Length) {
                    m_token_type = TkType.Eof;
                    return;
                }
                m_token_type = TkType.Other;
                m_tokenpos_end = m_source.IndexOf(' ', m_tokenpos_start); // idx of next space
                m_tokenpos_start_next = m_tokenpos_end;
                if (m_tokenpos_start_next != -1) { 
                    while (m_tokenpos_start_next + 1 < m_source.Length && m_source[m_tokenpos_start_next + 1] == ' ')
                        m_tokenpos_start_next++;
                }
                // check if delimeter before m_tokenpos_start_next
                var delim_idx = m_source.IndexOfAny(NSScanner.token_delims, m_tokenpos_start);

                if (m_tokenpos_start_next == -1) {
                    m_token_next = m_source.Substring(m_tokenpos_start);
                    m_tokenpos_start_next = m_source.Length;
                    m_tokenpos_end = m_source.Length;
                }
                if (delim_idx != -1 && delim_idx < m_tokenpos_start_next) {
                    m_tokenpos_start_next = delim_idx;
                    m_tokenpos_end = delim_idx;
                    if (m_source[delim_idx] == '(' || m_source[delim_idx] == '*') {
                        m_token_type = TkType.Identifier;
                    }
                }

                m_token_next = m_source.Substring(m_tokenpos_start, (m_tokenpos_start_next - m_tokenpos_start));

                if (LangConst.keywords.Contains(m_token_next)) {
                    m_token_type = TkType.Keyword;
                } else if (LangConst.datatypes.Contains(m_token_next))
                    m_token_type = TkType.DataType;
            }
        }
    }
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
