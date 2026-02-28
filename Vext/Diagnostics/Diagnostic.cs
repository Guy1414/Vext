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
        /// <param name="Severity"></param>
        public record ErrorDescriptor(string Message, int StartLine, int StartCol, int EndLine, int EndCol, DiagnosticSeverity Severity)
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
            /// <summary>
            /// Represents the severity of the diagnostic message (e.g., error, warning, info).
            /// </summary>
            public DiagnosticSeverity LspSeverity => Severity;
        }

        /// <summary>
        /// Represents the severity levels for diagnostic messages.
        /// </summary>
        public enum DiagnosticSeverity
        {
            /// <summary>
            /// Represents an error severity level, indicating a critical issue that prevents successful compilation or execution.
            /// </summary>
            Error,
            /// <summary>
            /// Represents a warning severity level, indicating a potential issue that does not prevent compilation but may lead to unexpected behavior or performance issues.
            /// </summary>
            Warning,
            /// <summary>
            /// Represents an informational severity level, indicating a message that provides additional context or information about the code but does not indicate a problem.
            /// </summary>
            Information,
            /// <summary>
            /// Represents a hint severity level, indicating a suggestion for improving code quality or readability without indicating an actual problem.
            /// </summary>
            Hint
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
            _errors.Add(new ErrorDescriptor(message, startLine, startCol, endLine, endCol, DiagnosticSeverity.Error));
        }

        /// <summary>
        /// Allows reporting of warnings found during compilation.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="startLine"></param>
        /// <param name="startCol"></param>
        /// <param name="endLine"></param>
        /// <param name="endCol"></param>
        public static void ReportWarning(string message, int startLine, int startCol, int endLine, int endCol)
        {
            _errors.Add(new ErrorDescriptor(message, startLine, startCol, endLine, endCol, DiagnosticSeverity.Warning));
        }

        /// <summary>
        /// Allows reporting of informational messages found during compilation.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="startLine"></param>
        /// <param name="startCol"></param>
        /// <param name="endLine"></param>
        /// <param name="endCol"></param>
        public static void ReportInfo(string message, int startLine, int startCol, int endLine, int endCol)
        {
            _errors.Add(new ErrorDescriptor(message, startLine, startCol, endLine, endCol, DiagnosticSeverity.Information));
        }

        /// <summary>
        /// Allows reporting of hint messages found during compilation.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="startLine"></param>
        /// <param name="startCol"></param>
        /// <param name="endLine"></param>
        /// <param name="endCol"></param>
        public static void ReportHint(string message, int startLine, int startCol, int endLine, int endCol)
        {
            _errors.Add(new ErrorDescriptor(message, startLine, startCol, endLine, endCol, DiagnosticSeverity.Hint));
        }

        /// <summary>
        /// Gets the list of reported errors.
        /// </summary>
        /// <returns></returns>
        public static List<ErrorDescriptor> GetErrors()
        {
            return _errors;
        }

        /// <summary>
        /// Clears all recorded diagnostics. Call at the start of a new compilation to avoid stale errors.
        /// </summary>
        public static void Clear()
        {
            _errors.Clear();
        }
    }
}
