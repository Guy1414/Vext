using Vext.Compiler.Parsing;

namespace Vext.Compiler.Modules
{
    internal class DefaultFunctions(RuntimeOutput output)
    {
        public Dictionary<string, List<Function>> Functions { get; } = [];

        private void Add(string name, Function fn)
        {
            if (!Functions.TryGetValue(name, out List<Function>? list))
            {
                list = [];
                Functions[name] = list;
            }
            list.Add(fn);
        }

        public void Initialize()
        {
            Add("print", new Function("print", 1, args =>
            {
                string text = args[0] switch
                {
                    bool b => b.ToString().ToLower(),
                    string s => s,
                    _ => args[0]?.ToString()?.ToLower() ?? "null"
                };

                output.Write(text);
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
                string text = args[0] switch
                {
                    bool b => b.ToString().ToLower(),
                    string s => s,
                    _ => args[0]?.ToString()?.ToLower() ?? "null"
                };

                output.WriteLine(text);
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

            // --- Functions for Member Access ---

            Add("__v_gettype", new Function("__v_gettype", 1, args =>
            {
                return args[0] switch
                {
                    int => "int",
                    double or float => "float",
                    bool => "bool",
                    string => "string",
                    null => "null",
                    _ => "unknown"
                };
            })
            {
                Parameters = [new FunctionParameterNode { Name = "value", Type = "auto" }],
                ReturnType = "string"
            });

            Add("__v_tostring", new Function("__v_tostring", 1, args =>
            {
                return args[0]?.ToString() ?? "null";
            })
            {
                Parameters = [new FunctionParameterNode { Name = "value", Type = "auto" }],
                ReturnType = "string"
            });

            Add("__v_len", new Function("__v_len", 1, args =>
            {
                if (args[0] is string s)
                    return s.Length;
                throw new Exception("Length only works on strings");
            })
            {
                Parameters =
                [
                    new FunctionParameterNode { Name = "value", Type = "string" }
                ],
                ReturnType = "int"
            });
        }
    }
}
