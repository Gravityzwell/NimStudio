using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Diagnostics;

namespace NimStudio.NimStudio {

    public enum TTokenClass: int {
        gtEof, gtNone, gtWhitespace, gtDecNumber, gtBinNumber, gtHexNumber,
        gtOctNumber, gtFloatNumber, gtIdentifier, gtKeyword, gtStringLit,
        gtLongStringLit, gtCharLit, gtEscapeSequence,
        gtOperator, gtPunctation, gtComment, gtLongComment, gtRegularExpression,
        gtTagStart, gtTagEnd, gtKey, gtValue, gtRawData, gtAssembler,
        gtPreprocessor, gtDirective, gtCommand, gtRule, gtHyperlink, gtLabel,
        gtReference, gtOther, tkCurlyDorLe, tkCurlyDotRi, tkDot, tkParLe, tkParRi, tkComma,
        tkBracketLe, tkBracketRe
    }
    static class LanguageConstants {
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

    class NSScanner: IScanner {
        private IVsTextBuffer m_buffer;
        private string m_source;
        private NSTokenizer m_tokenizer;

        public NSScanner(IVsTextBuffer buffer) {
            m_buffer = buffer;
            Debug.Print("NSScanner");
        }

        public void SetSource(string source, int offset) {
            //Debug.Print("SetSource:" + source + ":" + DateTime.Now.Millisecond.ToString());
            m_source = source.Substring(offset);
            m_tokenizer = new NSTokenizer(m_source);
        }

        public bool ScanTokenAndProvideInfoAboutIt(TokenInfo tokenInfo, ref int state) {
            NSScannerFlags flags = (NSScannerFlags)state;
            //Debug.Print("ScanTokenAndProvideInfoAboutIt " + DateTime.Now.Second.ToString());
            //Debug.Print("ScanTokenAndProvideInfoAboutIt " + DateTime.Now.Millisecond.ToString());
            var lastToken = m_tokenizer.Kind;
            switch (m_tokenizer.Kind) {
                case TTokenClass.gtEof:
                    return false;
                case TTokenClass.gtNone:
                    tokenInfo.Type = TokenType.Unknown;
                    tokenInfo.Color = TokenColor.Text;
                    break;
                case TTokenClass.gtWhitespace:
                    tokenInfo.Type = TokenType.WhiteSpace;
                    tokenInfo.Color = TokenColor.Text;
                    break;
                case TTokenClass.gtDecNumber:
                case TTokenClass.gtBinNumber:
                case TTokenClass.gtHexNumber:
                case TTokenClass.gtOctNumber:
                case TTokenClass.gtFloatNumber:
                    tokenInfo.Type = TokenType.Literal;
                    tokenInfo.Color = TokenColor.Number;
                    break;
                case TTokenClass.gtIdentifier:
                    tokenInfo.Type = TokenType.Identifier;
                    tokenInfo.Color = TokenColor.Identifier;

                    break;
                case TTokenClass.gtKeyword:
                    tokenInfo.Type = TokenType.Keyword;
                    tokenInfo.Color = TokenColor.Keyword;
                    break;
                case TTokenClass.gtStringLit:
                    tokenInfo.Type = TokenType.String;
                    tokenInfo.Color = TokenColor.String;
                    if (!flags.HasFlag(NSScannerFlags.RawStringLit)) {
                        flags ^= NSScannerFlags.NormalStringLit;
                    }
                    break;
                case TTokenClass.gtLongStringLit:
                    tokenInfo.Type = TokenType.String;
                    tokenInfo.Color = TokenColor.String;
                    flags ^= NSScannerFlags.RawStringLit;
                    break;
                case TTokenClass.gtCharLit:
                    tokenInfo.Type = TokenType.String;
                    tokenInfo.Color = TokenColor.String;
                    break;
                case TTokenClass.gtEscapeSequence:
                    tokenInfo.Type = TokenType.Unknown;
                    tokenInfo.Color = TokenColor.Text;
                    break;
                case TTokenClass.gtOperator:
                    tokenInfo.Type = TokenType.Operator;
                    tokenInfo.Color = TokenColor.Text;
                    break;
                case TTokenClass.gtPunctation:
                    tokenInfo.Type = TokenType.Text;
                    tokenInfo.Color = TokenColor.Text;
                    break;
                case TTokenClass.gtComment:
                    tokenInfo.Type = TokenType.Comment;
                    tokenInfo.Color = TokenColor.Comment;
                    break;
                case TTokenClass.gtLongComment:
                    tokenInfo.Type = TokenType.LineComment;
                    tokenInfo.Color = TokenColor.Comment;
                    break;
                case TTokenClass.gtRegularExpression:
                case TTokenClass.gtTagStart:
                case TTokenClass.gtTagEnd:
                case TTokenClass.gtKey:
                case TTokenClass.gtValue:
                case TTokenClass.gtRawData:
                case TTokenClass.gtAssembler:
                case TTokenClass.gtPreprocessor:
                case TTokenClass.gtDirective:
                case TTokenClass.gtCommand:
                case TTokenClass.gtRule:
                case TTokenClass.gtHyperlink:
                case TTokenClass.gtLabel:
                case TTokenClass.gtReference:
                case TTokenClass.gtOther:
                    tokenInfo.Type = TokenType.Unknown;
                    tokenInfo.Color = TokenColor.Text;
                    break;
                case TTokenClass.tkCurlyDorLe:
                    tokenInfo.Type = TokenType.Delimiter;
                    tokenInfo.Color = TokenColor.Text;
                    break;
                case TTokenClass.tkCurlyDotRi:
                    tokenInfo.Type = TokenType.Delimiter;
                    tokenInfo.Color = TokenColor.Text;
                    break;
                case TTokenClass.tkDot:
                    tokenInfo.Type = TokenType.Operator;
                    tokenInfo.Color = TokenColor.Text;
                    tokenInfo.Trigger = TokenTriggers.MemberSelect;

                    break;
                case TTokenClass.tkParLe:
                    tokenInfo.Type = TokenType.Delimiter;
                    tokenInfo.Color = TokenColor.Text;
                    tokenInfo.Trigger = TokenTriggers.ParameterStart;
                    break;
                case TTokenClass.tkParRi:
                    tokenInfo.Type = TokenType.Delimiter;
                    tokenInfo.Color = TokenColor.Text;
                    tokenInfo.Trigger = TokenTriggers.ParameterEnd;
                    break;
                case TTokenClass.tkComma:
                    tokenInfo.Type = TokenType.Text;
                    tokenInfo.Color = TokenColor.Text;
                    tokenInfo.Trigger = TokenTriggers.ParameterNext;
                    break;
            }
            if (flags.HasFlag(NSScannerFlags.NormalStringLit) || flags.HasFlag(NSScannerFlags.RawStringLit)) {
                tokenInfo.Color = TokenColor.String;
                tokenInfo.Type = TokenType.String;

            }
            state = (int)flags;
            tokenInfo.StartIndex = m_tokenizer.m_start;
            tokenInfo.EndIndex = m_tokenizer.m_end;
            m_tokenizer.advanceOne(flags);
            return true;
        }
    }

    class NSTokenizer {
        private TTokenClass kind;
        private string m_source;
        public int m_start;
        public int m_end;
        private int tokenEnd;
        //public TStringTypes inString = TStringTypes.stNone;
        private string nextToken;
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
                return nextToken;
            }
        }
        public TTokenClass Kind {
            get {
                return kind;
            }
        }
        public NSTokenizer(string source) {
            //inString = TStringTypes.stNone;
            m_source = source;
            //Debug.Print("NSTokenizer:" + source + ":" + DateTime.Now.Millisecond.ToString());
            m_start = 0;
            m_end = 0;
            advanceOne(NSScannerFlags.None);
        }
        private static int skipChar(string str, char chr, int idx) {
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

        private bool checkEqual(int position, char chr) {
            if (position >= m_source.Length) {
                return false;
            } else {
                return m_source[position] == chr;
            }
        }

        private bool checkNotEqual(int position, char chr) {
            if (position >= m_source.Length) {
                return true;
            } else {
                return m_source[position] != chr;
            }
        }

        public void advanceOne(NSScannerFlags flags) {
            /*  if (m_source.Contains("else:"))
                {
                Debugger.Break();
                }*/
            m_start = m_end;
            if (m_end >= m_source.Length) {
                kind = TTokenClass.gtEof;
                return;
            }
            if (m_start >= m_source.Length) {
                kind = TTokenClass.gtEof;
                return;
            }
            if (m_source[m_start] == '#' && flags == NSScannerFlags.None) {
                kind = TTokenClass.gtComment;
                m_end = m_source.Length;
                tokenEnd = m_source.Length;
            } else if (m_source[m_start] == '\'') {
                kind = TTokenClass.gtCharLit;
                if (m_start + 2 < m_source.Length && m_source[m_start + 2] == '\'') {
                    m_end = m_start + 3;
                    tokenEnd = m_start + 3;
                } else {
                    m_end = m_start + 1;
                    tokenEnd = m_start + 1;
                }
            } else if (m_source[m_start] == '"') {
                if (m_start + 2 < m_source.Length && m_source.Substring(m_start, 3) == "\"\"\"") {
                    m_end = m_start + 3;
                    tokenEnd = m_start + 3;
                    kind = TTokenClass.gtLongStringLit;
                    return;
                } else {
                    m_end = m_start + 1;
                    tokenEnd = m_start + 1;
                    kind = TTokenClass.gtStringLit;
                    return;
                }
            } else if (m_source[m_start] == '{') {
                if (checkEqual(m_start + 1, '.') && checkNotEqual(m_start + 2, '.')) {
                    m_end = m_start + 2;
                    tokenEnd = m_start + 2;
                    kind = TTokenClass.tkCurlyDorLe;
                } else {
                    m_end = m_start + 1;
                    tokenEnd = m_start + 1;
                    kind = TTokenClass.gtPunctation;

                }

            } else if (m_source[m_start] == '.') {
                if (checkEqual(m_start + 1, '}')) {
                    m_end = m_start + 2;
                    tokenEnd = m_start + 2;
                    kind = TTokenClass.tkCurlyDotRi;
                } else {
                    m_end = m_start + 1;
                    tokenEnd = m_start + 1;
                    kind = TTokenClass.tkDot;
                }
            } else if (m_source[m_start] == '(') {
                kind = TTokenClass.tkParLe;
                m_end = m_start + 1;
                tokenEnd = m_start + 1;
            } else if (m_source[m_start] == ')') {
                kind = TTokenClass.tkParRi;
                m_end = m_start + 1;
                tokenEnd = m_start + 1;
            } else if (m_source[m_start] == ',') {
                kind = TTokenClass.tkComma;
                m_end = m_start + 1;
                tokenEnd = m_start + 1;
            } else if (m_source[m_start] == '*') {
                kind = TTokenClass.gtPunctation;
                m_end = m_start + 1;
                tokenEnd = m_start + 1;
            } else if (m_source[m_start] == ':') {
                kind = TTokenClass.gtPunctation;
                m_end = m_start + 1;
                tokenEnd = m_start + 1;
            } else if (m_source[m_start] == ' ') {
                kind = TTokenClass.gtWhitespace;
                m_end = m_start + 1;
                tokenEnd = m_start + 1;
            } else if (m_source[m_start] == '[') {
                kind = TTokenClass.tkBracketLe;
                m_end = m_start + 1;
                tokenEnd = m_start + 1;
            } else if (m_source[m_start] == ']') {
                kind = TTokenClass.tkBracketRe;
                m_end = m_start + 1;
                tokenEnd = m_start + 1;
            } else {

                if (m_start >= m_source.Length) {
                    kind = TTokenClass.gtEof;
                    return;
                }
                kind = TTokenClass.gtOther;
                var spaceIdx = m_source.IndexOf(' ', m_start);
                tokenEnd = spaceIdx;
                spaceIdx = skipChar(m_source, ' ', spaceIdx);
                var searchStart = m_start;
                var quoteIdx = m_source.IndexOf('"', searchStart);
                var parenIdx = m_source.IndexOf('(', searchStart);
                var closeParenIdx = m_source.IndexOf(')', searchStart);
                var starIdx = m_source.IndexOf('*', searchStart);
                var colonIdx = m_source.IndexOf(':', searchStart);
                var dotidx = m_source.IndexOf('.', searchStart);
                var squareIdx = m_source.IndexOf('[', searchStart);
                var closeSquareIdx = m_source.IndexOf(']', searchStart);
                m_end = spaceIdx;
                if (m_end == -1) {
                    nextToken = m_source.Substring(m_start);
                    m_end = m_source.Length;
                    tokenEnd = m_source.Length;
                }
                if (squareIdx != -1 && squareIdx < m_end) {
                    m_end = squareIdx;
                    tokenEnd = squareIdx;
                }
                if (closeSquareIdx != -1 && closeSquareIdx < m_end) {
                    m_end = closeSquareIdx;
                    tokenEnd = closeSquareIdx;
                }
                if (parenIdx != -1 && parenIdx < m_end) {
                    kind = TTokenClass.gtIdentifier;
                    m_end = parenIdx;
                    tokenEnd = parenIdx;
                }
                if (closeParenIdx != -1 && closeParenIdx < m_end) {
                    m_end = closeParenIdx;
                    tokenEnd = closeParenIdx;
                }
                if (starIdx != -1 && starIdx < m_end) {
                    m_end = starIdx;
                    tokenEnd = starIdx;
                    kind = TTokenClass.gtIdentifier;
                }
                if (quoteIdx != -1 && quoteIdx < m_end) {
                    m_end = quoteIdx;
                    tokenEnd = quoteIdx;
                }
                if (colonIdx != -1 && colonIdx < m_end) {
                    m_end = colonIdx;
                    tokenEnd = colonIdx;
                }
                if (dotidx != -1 && dotidx < m_end) {
                    m_end = dotidx;
                    tokenEnd = dotidx;
                }

                try {
                    nextToken = m_source.Substring(m_start, (m_end - m_start));
                } catch (Exception e) {
                    Debug.Print(e.Message);
                }

                if (LanguageConstants.keywords.Contains(nextToken)) {
                    kind = TTokenClass.gtKeyword;
                }
            }
        }

    }
    [Flags]
    enum NSScannerFlags: int {
        None = 0,
        RawStringLit = 1,
        NormalStringLit = 2,
        Pragma = 4
    }

}
