namespace Vext.Shared.Rules
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
        /// Gets the pattern matching keywords supported by the Vext programming language.
        /// </summary>
        public static readonly HashSet<string> IsMatchKeywords = ["is", "as"];

        /// <summary>
        /// Gets the boolean literals supported by the Vext programming language.
        /// </summary>
        public static readonly HashSet<string> Booleans = ["true", "false"];

        /// <summary>
        /// Gets the variable types supported by the Vext programming language.
        /// </summary>
        public enum Types
        {
            Int,
            Float,
            Bool,
            String,
            Unknown
        }

        /// <summary>
        /// Gets the variable types supported by the Vext programming language wtih auto.
        /// </summary>
        public static readonly HashSet<string> VariableTypes =
        [
            ..Enum.GetNames<Types>().Select(t => t.ToLowerInvariant()),
            "auto"
        ];

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
        public static readonly HashSet<string> Keywords = [.. ReturnTypes, .. ControlKeywords, .. IsMatchKeywords];

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
        public const string Punctuation = ";,.(){}|";

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
        /// <summary>
        /// Converts a string representation of a type to its corresponding <see cref="Types"/> enum value.
        /// </summary>
        /// <param name="typeName">The name of the type to convert.</param>
        /// <returns>The corresponding <see cref="Types"/> value, or the default value if the type is unknown.</returns>
        public static Types TypeFromString(string typeName)
        {
            return typeName.ToLowerInvariant() switch
            {
                "int" => Types.Int,
                "float" => Types.Float,
                "bool" => Types.Bool,
                "string" => Types.String,
                _ => Types.Unknown // Default to Unknown
            };
        }
    }
}
