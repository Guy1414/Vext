namespace Vext.Compiler.Semantic
{
    /// <summary>
    /// Represents a single semantic token, providing type and modifier information for a span of code.
    /// </summary>
    public class SemanticToken
    {
        /// <summary>
        /// The line number where the token appears (1-based).
        /// </summary>
        public int Line { get; set; }
        /// <summary>
        /// The starting column of the token (1-based).
        /// </summary>
        public int StartColumn { get; set; }
        /// <summary>
        /// The ending column of the token (1-based).
        /// </summary>
        public int EndColumn { get; set; }
        /// <summary>
        /// The semantic type of the token (e.g., "variable", "function", "type").
        /// </summary>
        public required string Type { get; set; }
        /// <summary>
        /// A list of semantic modifiers for the token (e.g., "declaration", "readonly").
        /// </summary>
        public List<string> Modifiers { get; set; } = [];
    }
}
