using Vext.Shared.AST;
using Vext.Shared.Runtime;

namespace Vext.Shared.Modules
{
    public class IOModule : Module
    {
        public Module Initialize(RuntimeOutput output)
        {
            Name = "IO";

            // --- Input ---

            Add("readLine", new Function("readLine", 0, args =>
            {
                string? input = Console.ReadLine();
                return input ?? "";
            })
            {
                Parameters = [],
                ReturnType = "string"
            });

            Add("readInt", new Function("readInt", 0, args =>
            {
                string? input = Console.ReadLine();
                return long.TryParse(input, out var result) ? result : 0;
            })
            {
                Parameters = [],
                ReturnType = "int"
            });

            Add("readFloat", new Function("readFloat", 0, args =>
            {
                string? input = Console.ReadLine();
                return double.TryParse(input, out var result) ? result : 0.0;
            })
            {
                Parameters = [],
                ReturnType = "float"
            });

            // --- Output ---

            Add("print", new Function("print", 1, args =>
            {
                output.Write(ToVextString(args[0]));
                return null!;
            })
            {
                Parameters = [new FunctionParameterNode { Name = "value", Type = "auto" }],
                ReturnType = "void"
            });

            Add("print", new Function("print", 0, args =>
            {
                output.Write("");
                return null!;
            })
            {
                Parameters = [],
                ReturnType = "void"
            });

            Add("println", new Function("println", 1, args =>
            {
                output.WriteLine(ToVextString(args[0]));
                return null!;
            })
            {
                Parameters = [new FunctionParameterNode { Name = "value", Type = "auto" }],
                ReturnType = "void"
            });

            Add("println", new Function("println", 0, args =>
            {
                output.WriteLine("");
                return null!;
            })
            {
                Parameters = [],
                ReturnType = "void"
            });

            return this;
        }

        private static string ToVextString(object? value)
        {
            return value switch
            {
                bool b => b.ToString().ToLower(),
                string s => s,
                long l => l.ToString(),
                double d => d.ToString(),
                _ => value?.ToString()?.ToLower() ?? "null"
            };
        }
    }
}
