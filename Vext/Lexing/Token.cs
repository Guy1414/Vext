namespace Vext.Lexer
{
    internal class Token(TokenType type, string value, int line, int column)
    {
        public TokenType TokenType { get; set; } = type;
        public string Value { get; set; } = value;
        public int Line { get; set; } = line;
        public int Column { get; set; } = column;
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
