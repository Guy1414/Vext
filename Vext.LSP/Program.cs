using System.Text.Json;
using System.Text.Json.Serialization;
using Vext.Compiler;
using Vext.Compiler.Lexing;
using Vext.Compiler.Semantic;
using Vext.Compiler.VM;
using Vext.LSP;
using static Vext.Compiler.Diagnostics.Diagnostic;

[JsonSerializable(typeof(Program.Result))]
[JsonSerializable(typeof(Program.ErrorInfo))]
[JsonSerializable(typeof(Program.RunOutput))]
[JsonSerializable(typeof(VextValue))]
[JsonSerializable(typeof(VextValue[]))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, Converters = [typeof(VextValueConverter)])]
internal partial class VextJsonContext : JsonSerializerContext
{
}

class Program
{
    // Error representation
    public class ErrorInfo
    {
        public string Message { get; set; } = "";
        public int Line { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
        public required string Severity { get; set; }
    }

    public class TokenInfo
    {
        public int Line { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
        public required string Type { get; set; } // e.g., "keyword", "identifier", "function", etc.
        public bool IsDeclaration { get; set; } = false;
    }

    // Output representation
    public class RunOutput
    {
        public double Time { get; set; }
        public VextValue[] FinalState { get; set; } = [];
    }

    // Full result object
    public class Result
    {
        public bool Success { get; set; }
        public List<ErrorInfo> Errors { get; set; } = [];
        public RunOutput? Output { get; set; } = null;
        public List<TokenInfo> Tokens { get; set; } = [];
    }

    static int Main(string[] args)
    {
        string code;
        bool run;
        // Accept file path or stdin
        if (args.Length >= 1 && File.Exists(args[0]))
        {
            code = File.ReadAllText(args[0]);
            run = args.Length > 1 && args[1] == "--run";
        } else if (args.Length == 1 && args[0] == "--stdin")
        {
            code = Console.In.ReadToEnd();
            run = true;
        } else
        {
            Console.Error.WriteLine("No valid source file provided.");
            return 1;
        }

        Result result = new Result();

        try
        {
            CompilationResult compileResult = VextEngine.Compile(code);

            List<TokenInfo> processedTokens = [];

            // 1. Add Semantic Tokens from SemanticPass
            foreach (SemanticToken? st in compileResult.SemanticTokens)
            {
                processedTokens.Add(new TokenInfo
                {
                    Line = st.Line - 1,
                    StartColumn = st.StartColumn - 1,
                    EndColumn = st.EndColumn,
                    Type = st.Type,
                    IsDeclaration = st.Modifiers.Contains("declaration")
                });
            }

            // 2. Add Lexer tokens that are usually not in AST (Comments, Strings, Numbers, Keywords that might be missed)
            foreach (Token t in compileResult.Tokens)
            {
                string type = t.TokenType switch
                {
                    TokenType.Comment => "comment",
                    TokenType.String => "string",
                    TokenType.Numeric => "number",
                    TokenType.Boolean => "boolean",
                    TokenType.Keyword => "keyword",
                    TokenType.Operator => "operator",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(type))
                {
                    int start = t.StartColumn - 1; // convert to 0-based
                    int end = t.EndColumn;

                    // Only add if no token already exists at the exact same line/start/end
                    bool alreadyExists = processedTokens.Any(pt =>
                        pt.Line == t.Line - 1 &&
                        pt.StartColumn - 1 == start &&
                        pt.EndColumn == end
                    );

                    if (!alreadyExists)
                    {
                        processedTokens.Add(new TokenInfo
                        {
                            Line = t.Line - 1,
                            StartColumn = start,
                            EndColumn = end,
                            Type = type
                        });
                    }
                }
            }

            result.Tokens = [.. processedTokens
                .OrderBy(t => t.Line)
                .ThenBy(t => t.StartColumn)];

            if (compileResult.Errors.Count > 0)
            {
                foreach (ErrorDescriptor ed in compileResult.Errors)
                {
                    result.Errors.Add(new ErrorInfo
                    {
                        Message = ed.Message,
                        Line = Math.Max(0, ed.LspLine - 1),
                        StartColumn = Math.Max(0, ed.LspStartCol - 1),
                        EndColumn = Math.Max(0, ed.LspEndCol),
                        Severity = ed.LspSeverity.ToString().ToLower()
                    });
                }
                result.Success = false;
            } else
            {
                result.Success = true;

                if (run)
                {
                    (double time, VextValue[] finalState) = VextEngine.Run(compileResult.Instructions);
                    result.Output = new RunOutput
                    {
                        Time = time,
                        FinalState = finalState
                    };
                }
            }
        } catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add(new ErrorInfo
            {
                Message = ex.Message,
                Line = 0,
                StartColumn = 0,
                EndColumn = 1,
                Severity = "error"
            });
        }

        // Serialize result to JSON
        Console.WriteLine(JsonSerializer.Serialize(result, VextJsonContext.Default.Result));
        return 0;
    }
}
