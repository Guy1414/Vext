namespace Vext.Compiler.Shared
{
    /// <summary>
    /// Defines the language specifications for the Vext programming language, including
    /// keywords, variable types, operator precedence, and punctuation.
    /// </summary>
    public static class LanguageSpecs
    {
        /// <summary>
        /// Gets the control flow keywords supported by the Vext programming language.
        /// </summary>
        public static readonly HashSet<string> ControlKeywords = ["if", "else", "while", "for", "return"];

        /// <summary>
        /// Gets the boolean literals supported by the Vext programming language.
        /// </summary>
        public static readonly HashSet<string> Booleans = ["true", "false"];

        /// <summary>
        /// Gets the variable types supported by the Vext programming language.
        /// </summary>
        public static readonly HashSet<string> VariableTypes = ["int", "float", "bool", "string", "auto"];

        /// <summary>
        /// Gets the allowed parameter types for functions in the Vext programming language.
        /// </summary>
        public static readonly HashSet<string> AllowedFunctionParameterTypes = [.. VariableTypes, "numeral"];

        /// <summary>
        /// Gets the return types supported by the Vext programming language.
        /// </summary>
        public static readonly HashSet<string> ReturnTypes = [.. VariableTypes, "void"];

        /// <summary>
        /// Gets all keywords supported by the Vext programming language, including
        /// control flow keywords and return types.
        /// </summary>
        public static readonly HashSet<string> Keywords = [.. ReturnTypes, .. ControlKeywords];

        /// <summary>
        /// Defines the operator precedence levels used by the Vext programming language.
        /// A higher value indicates a higher precedence.
        /// </summary>
        public static readonly Dictionary<string, int> Precedence = new()
        {
            { "=", 1 },
            { "||", 2 },
            { "&&", 3 },
            { "==", 4 }, { "!=", 4 },
            { "<", 5 }, { ">", 5 }, { "<=", 5 }, { ">=", 5 },
            { "+", 6 }, { "-", 6 },
            { "*", 7 }, { "/", 7 }, { "%", 7 },
            { "**", 8 },
        };

        /// <summary>
        /// Gets the punctuation characters used by the Vext programming language.
        /// </summary>
        public const string Punctuation = ";,.(){}";

        /// <summary>
        /// Gets the operator characters used by the Vext programming language.
        /// </summary>
        public const string OperatorChars = "+-*/=&|!<>%";

        /// <summary>
        /// Gets the multi-character operators used by the Vext programming language.
        /// </summary>
        public static readonly HashSet<string> MultiCharOperators =
        [
            "==", "!=", "<=", ">=", "+=", "-=", "*=", "&&", "||", "++", "--", "**"
        ];
    }
}
