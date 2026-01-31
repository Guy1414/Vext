namespace Vext.Compiler.Diagnostics
{
    /// <summary>
    /// Represents a diagnostic message, such as an error or warning, produced during
    /// </summary>
    public static class Diagnostic
    {
        /// <summary>
        /// Represents a list of errors encountered during compilation.
        /// </summary>
        private static readonly List<ErrorDescriptor> _errors = [];


        /// <summary>
        /// Represents an error descriptor with message and position information.
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="StartLine"></param>
        /// <param name="StartCol"></param>
        /// <param name="EndLine"></param>
        /// <param name="EndCol"></param>
        public record ErrorDescriptor(string Message, int StartLine, int StartCol, int EndLine, int EndCol)
        {
            /// <summary>
            /// Returns a formatted display string for the error descriptor.
            /// </summary>
            public string Display => StartLine != EndLine
                ? $"Lines {StartLine}-{EndLine}: {Message}"
                : $"Line {StartLine}, Col {StartCol}: {Message}";

            /// <summary>
            /// Returns the zero-based line number for LSP (Language Server Protocol).
            /// </summary>
            public int LspLine => StartLine - 1;
            /// <summary>
            /// Returns the zero-based column number for LSP (Language Server Protocol).
            /// </summary>
            public int LspStartCol => StartCol - 1;
            /// <summary>
            /// Returns the zero-based end column number for LSP (Language Server Protocol).
            /// </summary>
            public int LspEndCol => EndCol - 1;
        }

        /// <summary>
        /// Allows reporting of errors found during compilation.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="startLine"></param>
        /// <param name="startCol"></param>
        /// <param name="endLine"></param>
        /// <param name="endCol"></param>
        public static void ReportError(string message, int startLine, int startCol, int endLine, int endCol)
        {
            _errors.Add(new ErrorDescriptor(message, startLine, startCol, endLine, endCol));
        }

        /// <summary>
        /// Gets the list of reported errors.
        /// </summary>
        /// <returns></returns>
        public static List<ErrorDescriptor> GetErrors()
        {
            return _errors;
        }
    }
}
