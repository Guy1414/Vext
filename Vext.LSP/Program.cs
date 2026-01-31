using System.Text.Json;
using System.Text.Json.Serialization;
using Vext.Compiler;
using Vext.Compiler.VM;
using Vext.LSP;
using static Vext.Compiler.Diagnostics.Diagnostic;

[JsonSerializable(typeof(Program.Result))]
[JsonSerializable(typeof(Program.ErrorInfo))]
[JsonSerializable(typeof(Program.RunOutput))]
[JsonSerializable(typeof(VextValue))]
[JsonSerializable(typeof(VextValue[]))]
[JsonSourceGenerationOptions(Converters = [typeof(VextValueConverter)])]
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

        var result = new Result();

        try
        {
            var compileResult = VextEngine.Compile(code);

            result.Tokens = compileResult.Tokens.ConvertAll(t => new TokenInfo
            {
                Line = t.Line - 1,
                StartColumn = t.StartColumn - 1,
                EndColumn = t.EndColumn - 1,
                Type = t.TokenType.ToString().ToLower(),
            });

            if (compileResult.Errors.Count > 0)
            {
                foreach (ErrorDescriptor ed in compileResult.Errors)
                {
                    result.Errors.Add(new ErrorInfo
                    {
                        Message = ed.Message,
                        Line = ed.LspLine,
                        StartColumn = ed.LspStartCol,
                        EndColumn = ed.LspEndCol
                    });
                }
                result.Success = false;
            } else
            {
                result.Success = true;

                if (run)
                {
                    var (time, finalState) = VextEngine.Run(compileResult.Instructions);
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
                EndColumn = 1
            });
        }

        // Serialize result to JSON
        Console.WriteLine(JsonSerializer.Serialize(result, VextJsonContext.Default.Result));
        return 0;
    }
}
