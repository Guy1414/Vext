namespace Vext.Shared.Modules
{
    public class Module
    {
        public required string Name { get; set; }
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
    }
}
