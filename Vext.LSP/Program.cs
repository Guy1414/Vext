using System.Text.Json;
using Vext.Compiler;
using Vext.Compiler.VM;

class Program
{
    // Error representation
    public class ErrorInfo
    {
        public string Message { get; set; } = "";
        public int Line { get; set; }
        public int Column { get; set; }
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

            if (compileResult.Errors.Count > 0)
            {
                // Map tuple errors to ErrorInfo objects
                foreach (var (msg, line, col) in compileResult.Errors)
                {
                    result.Errors.Add(new ErrorInfo
                    {
                        Message = msg,
                        Line = line,
                        Column = col
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
                Column = 0
            });
        }

        // Serialize result to JSON
        Console.WriteLine(JsonSerializer.Serialize(result));
        return 0;
    }
}
