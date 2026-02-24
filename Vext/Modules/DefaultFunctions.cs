using Vext.Compiler.Parsing;

namespace Vext.Compiler.Modules
{
    internal class DefaultFunctions
    {
        public Dictionary<string, List<Function>> Functions { get; private set; } = [];

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
            Add("len", new Function("len", 1, args =>
            {
                if (args[0] is string s)
                    return s.Length;
                throw new Exception("len() only works on strings");
            })
            {
                Parameters =
                [
                    new FunctionParameterNode { Name = "s", Type = "string" }
                ],
                ReturnType = "int"
            });

            Add("print", new Function("print", 1, args =>
            {
                RuntimeOutput.WriteLine(args[0]?.ToString() ?? "null");
                return null!;
            })
            {
                Parameters = [new FunctionParameterNode { Name = "value", Type = "auto" }],
                ReturnType = "void"
            });

            // --- Functions for Member Access ---

            Add("__v_gettype", new Function("__v_gettype", 1, args =>
            {
                return args[0] switch
                {
                    double or int or float => "int",
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
        }
    }
}
