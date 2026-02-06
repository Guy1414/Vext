namespace Vext.Compiler.Lexing
{
    /// <summary>
    /// Represents a lexical token identified by the lexer.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="value"></param>
    /// <param name="line"></param>
    /// <param name="column"></param>
    /// <param name="eColumn"></param>
    public class Token(TokenType type, string value, int line, int column, int? eColumn = null)
    {
        /// <summary>
        /// Represents the type of the token.
        /// </summary>
        public TokenType TokenType { get; set; } = type;
        /// <summary>
        /// Represents the value of the token.
        /// </summary>
        public string Value { get; set; } = value;
        /// <summary>
        /// Represents the line number where the token is located.
        /// </summary>
        public int Line { get; set; } = line;
        /// <summary>
        /// Represents the starting column number of the token.
        /// </summary>
        public int StartColumn { get; set; } = column;
        /// <summary>
        /// Represents the ending column number of the token.
        /// </summary>
        public int EndColumn => eColumn ?? StartColumn + Value.Length - 1;
    }

    /// <summary>
    /// Defines the different types of tokens that can be identified by the lexer.
    /// </summary>
    public enum TokenType
    {
        /// <summary>
        /// Represents an identifier token such as names for variables, functions, etc.
        /// </summary>
        Identifier,

        /// <summary>
        /// Represents a keyword token such as reserved words in the language
        /// (int, float, bool, void, if, else, etc.).
        /// </summary>
        Keyword,

        /// <summary>
        /// Represents a string literal token.
        /// </summary>
        String,

        /// <summary>
        /// Represents a numeric literal token.
        /// </summary>
        Numeric,

        /// <summary>
        /// Represents an operator token such as +, -, *, /, %, ==, !=, &lt;, &lt;=, &gt;, &gt;=, &amp;&amp;, ||, !, =, etc.
        /// </summary>
        Operator,

        /// <summary>
        /// Represents a punctuation token such as ;, ,, (, ), {, }, etc.
        /// </summary>
        Punctuation,
        /// <summary>
        /// Represents a comment token.
        /// </summary>
        Comment,

        /// <summary>
        /// Represents a boolean literal token (true, false).
        /// </summary>
        Boolean,

        /// <summary>
        /// Represents the end-of-file token.
        /// </summary>
        EOF,

        /// <summary>
        /// Represents an unknown or unrecognized token.
        /// </summary>
        Unknown
    }
}
