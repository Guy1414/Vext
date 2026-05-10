using Vext.Shared.AST;

namespace Vext.Shared.Modules.Base
{
    public class Module
    {
        public string Name { get; set; } = string.Empty;
        public bool IsGlobal { get; set; } = false;
        public Dictionary<string, List<Function>> Functions { get; } = [];

        public void Add(string name, Function fn)
        {
            if (!Functions.TryGetValue(name, out List<Function>? list))
            {
                list = [];
                Functions[name] = list;
            }
            fn.Name = name;
            list.Add(fn);
        }

        public void Add(string name, int arity, string returnType, Func<List<object>, object> native, params (string Name, string Type)[] parameters)
        {
            var fn = new Function(name, arity, native)
            {
                ReturnType = returnType,
                Parameters = parameters.Select(p => new FunctionParameterNode { Name = p.Name, Type = p.Type }).ToList()
            };
            Add(name, fn);
        }
    }
}

