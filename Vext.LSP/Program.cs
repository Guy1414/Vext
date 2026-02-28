using System.Text.Json;
using System.Text.Json.Serialization;

using Vext.Compiler;
using Vext.Compiler.Lexing;
using Vext.Compiler.Semantic;
using Vext.Compiler.VM;
using Vext.LSP;

using static Program;
using static Vext.Compiler.Diagnostics.Diagnostic;

[JsonSerializable(typeof(Result))]
[JsonSerializable(typeof(ErrorInfo))]
[JsonSerializable(typeof(List<ErrorInfo>))]
[JsonSerializable(typeof(RunOutput))]
[JsonSerializable(typeof(TokenInfo))]
[JsonSerializable(typeof(List<TokenInfo>))]
[JsonSerializable(typeof(KeywordInfo))]
[JsonSerializable(typeof(KeywordInfo[]))]
[JsonSerializable(typeof(VextValue))]
[JsonSerializable(typeof(VextValue[]))]
[JsonSerializable(typeof(Response))]
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
        public string Stdout { get; set; } = "";
    }

    public class Response
    {
        public int Id { get; set; }
        public Result Result { get; set; } = null!;
    }

    public class KeywordInfo
    {
        public required string Label { get; init; }
        public required string InsertText { get; init; }

        public static readonly KeywordInfo[] AllKeywords =
        [
            new() { Label = "int", InsertText = "int" },
            new() { Label = "float", InsertText = "float" },
            new() { Label = "bool", InsertText = "bool" },
            new() { Label = "string", InsertText = "string" },
            new() { Label = "auto", InsertText = "auto" },
            new() { Label = "if", InsertText = "if (${1:condition}) {\n\t$0\n}" },
            new() { Label = "else", InsertText = "else {\n\t$0\n}" },
            new() { Label = "while", InsertText = "while (${1:condition}) {\n\t$0\n}" },
            new() { Label = "for", InsertText = "for (${1:int i = 0}; ${2:i < n}; ${3:i++}) {\n\t$0\n}" },
            new() { Label = "return", InsertText = "return ${1:expression};" },
            new() { Label = "is", InsertText = "is" },
            new() { Label = "as", InsertText = "as" },
        ];
    }

    // Full result object
    public class Result
    {
        public bool Success { get; set; }
        public List<ErrorInfo> Errors { get; set; } = [];
        public RunOutput? Output { get; set; } = null;
        public List<TokenInfo> Tokens { get; set; } = [];
        public KeywordInfo[] Keywords { get; set; } = KeywordInfo.AllKeywords;
    }

    static int Main()
    {
        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            using JsonDocument doc = JsonDocument.Parse(line);
            JsonElement root = doc.RootElement;
            int id = root.GetProperty("id").GetInt32();
            string code = root.GetProperty("code").GetString() ?? "";

            bool run = root.TryGetProperty("run", out var runProp) && runProp.GetBoolean();

            Result result = CompileAndRun(code, run);

            Response response = new Response { Id = id, Result = result };

            // Serialize result to JSON
            Console.WriteLine(JsonSerializer.Serialize(response, VextJsonContext.Default.Response));
            Console.Out.Flush();
        }
        return 0;
    }

    public static Result CompileAndRun(string code, bool run)
    {
        Result result = new Result { };
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

                    bool overlaps = processedTokens.Any(pt =>
                        pt.Line == t.Line - 1 &&
                        !(t.EndColumn <= pt.StartColumn || t.StartColumn - 1 >= pt.EndColumn)
                    );

                    if (!overlaps)
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
                .GroupBy(t => new { t.Line, t.StartColumn, t.EndColumn, t.Type })
                .Select(g => g.First())
                .OrderBy(t => t.Line)
                .ThenBy(t => t.StartColumn)];


            if (compileResult.Errors.Count > 0)
            {
                foreach (ErrorDescriptor ed in compileResult.Errors)
                {
                    result.Errors.Add(new ErrorInfo
                    {
                        Message = ed.Message,
                        Line = Math.Max(0, ed.LspLine),
                        StartColumn = Math.Max(0, ed.LspStartCol),
                        EndColumn = Math.Max(0, ed.LspEndCol + 1),
                        Severity = ed.LspSeverity.ToString().ToLower()
                    });
                }
                result.Success = false;
            } else
            {
                result.Success = true;

                if (run)
                {
                    (double time, VextValue[] finalState, string stdout) = VextEngine.Run(compileResult.Instructions);

                    result.Output = new RunOutput
                    {
                        Time = time,
                        FinalState = finalState,
                        Stdout = stdout
                    };
                }
            }
            return result;
        } catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add(new ErrorInfo
            {
                Message = ex.ToString(),
                Line = 0,
                StartColumn = 0,
                EndColumn = 1,
                Severity = "error"
            });
        }
        return result;
    }
}
