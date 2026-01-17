using Vext.Parser;

namespace Vext.Modules
{
    /// <summary>
    /// Represents a function within a module or as a defaultFunction.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="arity"></param>
    /// <param name="native"></param>
    public class Function(string name, int arity, Func<List<object>, object> native)
    {
        /// <summary>
        /// Represents the name of the function.
        /// </summary>
        public string Name { get; set; } = name;
        /// <summary>
        /// Represents the number of parameters the function takes.
        /// </summary>
        public int Arity { get; set; } = arity;
        /// <summary>
        /// Represents the native implementation of the function in C#.
        /// </summary>
        public Func<List<object>, object> Native { get; set; } = native;

        /// <summary>
        /// Represents the list of parameters for the function.
        /// </summary>
        public List<FunctionParameterNode>? Parameters { get; set; }
        /// <summary>
        /// Represents the return type of the function.
        /// </summary>
        public string? ReturnType { get; set; }
    }
}
