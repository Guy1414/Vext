using System.Text;
using Vext.Shared;

namespace Vext.Lexer
{
    internal class Lexer(string vextCode)
    {
        private readonly string vextCode = vextCode;
        private int currentIndex = 0;
        private int currentLine = 1;
        private int currentColumn = 1;

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (currentIndex < vextCode.Length)
            {
                SkipTrivia();
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
                }
                else
                {
                    tokens.Add(new Token(TokenType.Unknown, current.ToString(), currentLine, currentColumn));
                    Advance();
                }
            }

            tokens.Add(new Token(TokenType.EOF, string.Empty, currentLine, currentColumn));
            return tokens;
        }

        private void SkipTrivia()
        {
            while (currentIndex < vextCode.Length)
            {
                char c = vextCode[currentIndex];

                if (c == '/' && Peek() == '/')
                {
                    SkipSingleLineComment();
                }
                else if (char.IsWhiteSpace(c))
                {
                    HandleWhitespace(c);
                }
                else
                    break;
            }
        }

        private void SkipSingleLineComment()
        {
            Advance(2); // Skip the //

            int start = currentIndex;

            while (currentIndex < vextCode.Length && vextCode[currentIndex] != '\n' && vextCode[currentIndex] != '\r')
                currentIndex++;

            currentColumn += currentIndex - start;

            if (currentIndex < vextCode.Length && vextCode[currentIndex] == '\n')
            {
                currentIndex++;
                currentLine++;
                currentColumn = 1;
            }
        }

        private void Advance(int count = 1)
        {
            currentIndex += count;
            currentColumn += count;
        }

        private char Peek(int offset = 1)
        {
            return currentIndex + offset < vextCode.Length ? vextCode[currentIndex + offset] : '\0';
        }

        private void HandleWhitespace(char current)
        {
            if (current == '\r' && Peek() == '\n')
                Advance(2);
            else
                Advance();

            if (current == '\r' || current == '\n')
            {
                currentLine++;
                currentColumn = 1;
            }
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
                }
                else if (c == '.' && !hasDecimal && char.IsDigit(Peek()))
                {
                    hasDecimal = true;
                    Advance();
                }
                else
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
            int startCol = currentColumn;
            Advance(); // skip opening quote
            StringBuilder sb = new();

            while (currentIndex < vextCode.Length)
            {
                char c = vextCode[currentIndex];

                if (c == quoteType)
                {
                    Advance();
                    return new Token(TokenType.String, sb.ToString(), currentLine, startCol);
                }

                if (c == '\\')
                {
                    Advance();
                    if (currentIndex >= vextCode.Length)
                        throw new Exception($"Unterminated string escape at line {currentLine}, column {currentColumn}");

                    char escaped = vextCode[currentIndex];
                    sb.Append(escaped switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        '\\' => '\\',
                        '"' => '"',
                        '\'' => '\'',
                        _ => escaped
                    });
                    Advance();
                }
                else if (c == '\n' || c == '\r')
                    throw new Exception($"Unterminated string literal at line {currentLine}, column {currentColumn}");
                else
                {
                    sb.Append(c);
                    Advance();
                }
            }

            throw new Exception($"Unterminated string literal at line {currentLine}, column {startCol}");
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
