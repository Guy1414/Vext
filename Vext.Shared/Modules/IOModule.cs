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

            Add("ReadLine", new Function("ReadLine", 0, args =>
            {
                return output.ReadLine();
            })
            {
                Parameters = [],
                ReturnType = "string"
            });

            Add("ReadInt", new Function("ReadInt", 0, args =>
            {
                return output.ReadInt();
            })
            {
                Parameters = [],
                ReturnType = "int"
            });

            Add("ReadFloat", new Function("ReadFloat", 0, args =>
            {
                return output.ReadFloat();
            })
            {
                Parameters = [],
                ReturnType = "float"
            });

            // --- Output ---

            Add("Print", new Function("Print", 1, args =>
            {
                output.Write(ToVextString(args[0]));
                return null!;
            })
            {
                Parameters = [new FunctionParameterNode { Name = "value", Type = "auto" }],
                ReturnType = "void"
            });

            Add("Print", new Function("Print", 0, args =>
            {
                output.Write("");
                return null!;
            })
            {
                Parameters = [],
                ReturnType = "void"
            });

            Add("Println", new Function("Println", 1, args =>
            {
                output.WriteLine(ToVextString(args[0]));
                return null!;
            })
            {
                Parameters = [new FunctionParameterNode { Name = "value", Type = "auto" }],
                ReturnType = "void"
            });

            Add("Println", new Function("Println", 0, args =>
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
