namespace Vext.Modules
{
    internal class Module
    {
        public required string Name { get; set; }
        public Dictionary<string, List<Function>> Functions { get; } = [];
    }
}
