using Vext.Shared.AST;

namespace Vext.Shared.Modules
{
    public class UserFunction
    {
        public required string Name;
        public List<FunctionParameterNode>? Arguments = [];
        public List<Instruction> Body = [];
        public required int LocalCount;
        public List<int> ArgumentSlots { get; set; } = [];
    }
}
