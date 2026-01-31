using System.Text;
using Vext.Compiler.Diagnostics;
using Vext.Compiler.Shared;

namespace Vext.Compiler.Lexing
{
    internal class Lexer(string vextCode)
    {
        private readonly string vextCode = vextCode;
        private int currentIndex = 0;
        private int currentLine = 1;
        private int currentColumn = 1;

        private static void ReportError(string message, int startLine, int startCol, int endLine, int endCol) => Diagnostic.ReportError(message, startLine, startCol, endLine, endCol);

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (currentIndex < vextCode.Length)
            {
                Token? tk = SkipTrivia();
                if (tk != null)
                {
                    tokens.Add(tk);
                    continue;
                }
                if (currentIndex >= vextCode.Length)
                    break;

                char current = vextCode[currentIndex];

                if (char.IsDigit(current))
                    tokens.Add(ReadNumber());
                else if (char.IsLetter(current) || current == '_')
                    tokens.Add(ReadIdentifierOrKeyword());
                else if (current == '"' || current == '\'')
                    tokens.Add(ReadString(current));
                else if (IsOperator(current))
                    tokens.Add(ReadOperator());
                else if (LanguageSpecs.Punctuation.Contains(current))
                {
                    tokens.Add(new Token(TokenType.Punctuation, current.ToString(), currentLine, currentColumn));
                    Advance();
                } else
                {
                    tokens.Add(new Token(TokenType.Unknown, current.ToString(), currentLine, currentColumn));
                    Advance();
                }
            }

            tokens.Add(new Token(TokenType.EOF, string.Empty, currentLine, currentColumn));
            return (tokens);
        }

        private Token? SkipTrivia()
        {
            bool skipped;
            do
            {
                skipped = false;

                if (currentIndex >= vextCode.Length)
                    return null;

                char c = vextCode[currentIndex];

                if (c == '/' && Peek() == '/')
                    return ReadSingleLineComment();

                if (char.IsWhiteSpace(c))
                {
                    Advance();
                    skipped = true;
                }

            } while (skipped);

            return null;
        }

        private Token ReadSingleLineComment()
        {
            int startLine = currentLine;
            int startCol = currentColumn;

            int start = currentIndex;

            Advance(2); // Skip the //

            while (currentIndex < vextCode.Length &&
                   vextCode[currentIndex] != '\n' &&
                   vextCode[currentIndex] != '\r')
            {
                Advance();
            }

            string value = vextCode[start..currentIndex];
            return new Token(TokenType.Comment, value, startLine, startCol);
        }

        private void Advance()
        {
            if (currentIndex >= vextCode.Length)
                return;

            char c = vextCode[currentIndex++];
            if (c == '\n')
            {
                currentLine++;
                currentColumn = 1;
            } else
            {
                currentColumn++;
            }
        }

        private void Advance(int count)
        {
            for (int i = 0; i < count; i++)
                Advance();
        }

        private char Peek(int offset = 1)
        {
            return currentIndex + offset < vextCode.Length ? vextCode[currentIndex + offset] : '\0';
        }

        private Token ReadNumber()
        {
            int startCol = currentColumn;
            int start = currentIndex;
            bool hasDecimal = false;

            while (currentIndex < vextCode.Length)
            {
                char c = vextCode[currentIndex];

                if (char.IsDigit(c))
                {
                    Advance();
                } else if (c == '.' && !hasDecimal && char.IsDigit(Peek()))
                {
                    hasDecimal = true;
                    Advance();
                } else
                    break;
            }

            string value = vextCode[start..currentIndex];
            return new Token(TokenType.Numeric, value, currentLine, startCol);
        }

        private Token ReadIdentifierOrKeyword()
        {
            int startCol = currentColumn;
            int start = currentIndex;

            while (currentIndex < vextCode.Length && (char.IsLetterOrDigit(vextCode[currentIndex]) || vextCode[currentIndex] == '_'))
                Advance();

            string value = vextCode[start..currentIndex];

            TokenType type = LanguageSpecs.Keywords.Contains(value) ? TokenType.Keyword :
                             LanguageSpecs.Booleans.Contains(value) ? TokenType.Boolean :
                             TokenType.Identifier;

            return new Token(type, value, currentLine, startCol);
        }

        private Token ReadString(char quoteType)
        {
            int startLine = currentLine;
            int startCol = currentColumn;
            Advance(); // skip opening quote
            StringBuilder sb = new();

            while (currentIndex < vextCode.Length)
            {
                char c = vextCode[currentIndex];

                // End of string
                if (c == quoteType)
                {
                    int endClol = currentColumn;
                    Advance();
                    return new Token(TokenType.String, sb.ToString(), startLine, startCol, endClol);
                }

                // Handle Escape Sequences
                if (c == '\\')
                {
                    HandleEscapeSequence(sb);
                    continue; // HandleEscapeSequence calls Advance() internally
                }

                // Newlines in strings (usually not allowed in non-verbatim strings)
                if (c == '\n' || c == '\r')
                {
                    ReportError("Unterminated string literal", startLine, startCol, currentLine, currentColumn);
                    break;
                }

                sb.Append(c);
                Advance();
            }

            if (currentIndex >= vextCode.Length)
            {
                ReportError("Unterminated string literal at EOF", startLine, startCol, currentLine, currentColumn);
            }

            return new Token(TokenType.String, sb.ToString(), startLine, startCol);
        }

        private void HandleEscapeSequence(StringBuilder sb)
        {
            Advance(); // skip '\'

            if (currentIndex >= vextCode.Length)
                return;

            char escaped = vextCode[currentIndex];
            switch (escaped)
            {
                case 'n':
                    sb.Append('\n');
                    break;
                case 'r':
                    sb.Append('\r');
                    break;
                case 't':
                    sb.Append('\t');
                    break;
                case '\\':
                    sb.Append('\\');
                    break;
                case '"':
                    sb.Append('"');
                    break;
                case '\'':
                    sb.Append('\'');
                    break;
                default:
                    ReportError($"Invalid escape sequence '\\{escaped}'", currentLine, currentColumn, currentLine, currentColumn);
                    sb.Append(escaped); // Recovery
                    break;
            }
            Advance();
        }

        private static bool IsOperator(char c) => LanguageSpecs.OperatorChars.Contains(c);

        private Token ReadOperator()
        {
            int startCol = currentColumn;
            char c0 = vextCode[currentIndex];
            char c1 = Peek();
            char c2 = Peek(2);

            string? op;
            if (c2 != '\0' && LanguageSpecs.MultiCharOperators.Contains($"{c0}{c1}{c2}"))
                op = $"{c0}{c1}{c2}";
            else if (c1 != '\0' && LanguageSpecs.MultiCharOperators.Contains($"{c0}{c1}"))
                op = $"{c0}{c1}";
            else
                op = c0.ToString();

            Advance(op.Length);
            return new Token(TokenType.Operator, op, currentLine, startCol);
        }
    }
}
