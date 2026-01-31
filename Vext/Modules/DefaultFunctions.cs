using Vext.Compiler.Parsing;
using Vext.Modules;

namespace Vext.Compiler.Modules
{
    internal class DefaultFunctions
    {
        public Dictionary<string, List<Function>> Functions { get; private set; } = [];

        private void Add(string name, Function fn)
        {
            if (!Functions.TryGetValue(name, out var list))
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
                //Console.WriteLine(args[0]?.ToString());
                return string.Empty;
            })
            {
                Parameters = [new FunctionParameterNode { Name = "value", Type = "auto" }],
                ReturnType = "void"
            });
        }
    }
}
