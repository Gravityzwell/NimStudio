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
        }

        public void SetSource(string source, int offset) {
            m_source = source.Substring(offset);
            m_tokenizer = new NSTokenizer(m_source);
        }

        public bool ScanTokenAndProvideInfoAboutIt(TokenInfo tokenInfo, ref int state) {
            NSScannerFlags flags = (NSScannerFlags)state;
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
            tokenInfo.StartIndex = m_tokenizer.Start;
            tokenInfo.EndIndex = m_tokenizer.End;
            m_tokenizer.advanceOne(flags);
            return true;
        }
    }

    class NSTokenizer {
        private TTokenClass kind;
        private string m_source;
        private int start;
        private int end;
        private int tokenEnd;
        private TStringTypes inString;
        private string nextToken;
        public int Start {
            get {
                return start;
            }
        }
        public int End {
            get {
                return tokenEnd - 1;
            }
        }
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
            inString = TStringTypes.stNone;
            m_source = source;
            start = 0;
            end = 0;
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
            start = end;
            if (end >= m_source.Length) {
                kind = TTokenClass.gtEof;
                return;
            }
            if (start >= m_source.Length) {
                kind = TTokenClass.gtEof;
                return;
            }
            if (m_source[start] == '#' && flags == NSScannerFlags.None) {
                kind = TTokenClass.gtComment;
                end = m_source.Length;
                tokenEnd = m_source.Length;
            } else if (m_source[start] == '\'') {
                kind = TTokenClass.gtCharLit;
                if (start + 2 < m_source.Length && m_source[start + 2] == '\'') {
                    end = start + 3;
                    tokenEnd = start + 3;
                } else {
                    end = start + 1;
                    tokenEnd = start + 1;
                }
            } else if (m_source[start] == '"') {
                if (start + 2 < m_source.Length && m_source.Substring(start, 3) == "\"\"\"") {
                    end = start + 3;
                    tokenEnd = start + 3;
                    kind = TTokenClass.gtLongStringLit;
                    return;
                } else {
                    end = start + 1;
                    tokenEnd = start + 1;
                    kind = TTokenClass.gtStringLit;
                    return;
                }
            } else if (m_source[start] == '{') {
                if (checkEqual(start + 1, '.') && checkNotEqual(start + 2, '.')) {
                    end = start + 2;
                    tokenEnd = start + 2;
                    kind = TTokenClass.tkCurlyDorLe;
                } else {
                    end = start + 1;
                    tokenEnd = start + 1;
                    kind = TTokenClass.gtPunctation;

                }

            } else if (m_source[start] == '.') {
                if (checkEqual(start + 1, '}')) {
                    end = start + 2;
                    tokenEnd = start + 2;
                    kind = TTokenClass.tkCurlyDotRi;
                } else {
                    end = start + 1;
                    tokenEnd = start + 1;
                    kind = TTokenClass.tkDot;
                }
            } else if (m_source[start] == '(') {
                kind = TTokenClass.tkParLe;
                end = start + 1;
                tokenEnd = start + 1;
            } else if (m_source[start] == ')') {
                kind = TTokenClass.tkParRi;
                end = start + 1;
                tokenEnd = start + 1;
            } else if (m_source[start] == ',') {
                kind = TTokenClass.tkComma;
                end = start + 1;
                tokenEnd = start + 1;
            } else if (m_source[start] == '*') {
                kind = TTokenClass.gtPunctation;
                end = start + 1;
                tokenEnd = start + 1;
            } else if (m_source[start] == ':') {
                kind = TTokenClass.gtPunctation;
                end = start + 1;
                tokenEnd = start + 1;
            } else if (m_source[start] == ' ') {
                kind = TTokenClass.gtWhitespace;
                end = start + 1;
                tokenEnd = start + 1;
            } else if (m_source[start] == '[') {
                kind = TTokenClass.tkBracketLe;
                end = start + 1;
                tokenEnd = start + 1;
            } else if (m_source[start] == ']') {
                kind = TTokenClass.tkBracketRe;
                end = start + 1;
                tokenEnd = start + 1;
            } else {

                if (start >= m_source.Length) {
                    kind = TTokenClass.gtEof;
                    return;
                }
                kind = TTokenClass.gtOther;
                var spaceIdx = m_source.IndexOf(' ', start);
                tokenEnd = spaceIdx;
                spaceIdx = skipChar(m_source, ' ', spaceIdx);
                var searchStart = start;
                var quoteIdx = m_source.IndexOf('"', searchStart);
                var parenIdx = m_source.IndexOf('(', searchStart);
                var closeParenIdx = m_source.IndexOf(')', searchStart);
                var starIdx = m_source.IndexOf('*', searchStart);
                var colonIdx = m_source.IndexOf(':', searchStart);
                var dotidx = m_source.IndexOf('.', searchStart);
                var squareIdx = m_source.IndexOf('[', searchStart);
                var closeSquareIdx = m_source.IndexOf(']', searchStart);
                end = spaceIdx;
                if (end == -1) {
                    nextToken = m_source.Substring(start);
                    end = m_source.Length;
                    tokenEnd = m_source.Length;
                }
                if (squareIdx != -1 && squareIdx < end) {
                    end = squareIdx;
                    tokenEnd = squareIdx;
                }
                if (closeSquareIdx != -1 && closeSquareIdx < end) {
                    end = closeSquareIdx;
                    tokenEnd = closeSquareIdx;
                }
                if (parenIdx != -1 && parenIdx < end) {
                    kind = TTokenClass.gtIdentifier;
                    end = parenIdx;
                    tokenEnd = parenIdx;
                }
                if (closeParenIdx != -1 && closeParenIdx < end) {
                    end = closeParenIdx;
                    tokenEnd = closeParenIdx;
                }
                if (starIdx != -1 && starIdx < end) {
                    end = starIdx;
                    tokenEnd = starIdx;
                    kind = TTokenClass.gtIdentifier;
                }
                if (quoteIdx != -1 && quoteIdx < end) {
                    end = quoteIdx;
                    tokenEnd = quoteIdx;
                }
                if (colonIdx != -1 && colonIdx < end) {
                    end = colonIdx;
                    tokenEnd = colonIdx;
                }
                if (dotidx != -1 && dotidx < end) {
                    end = dotidx;
                    tokenEnd = dotidx;
                }

                try {
                    nextToken = m_source.Substring(start, (end - start));
                } catch (Exception e) {
                    Debugger.Break();
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
