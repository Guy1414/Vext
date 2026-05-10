using Vext.Shared.Modules.Base;

namespace Vext.Shared.Modules.Builtins
{
    public class CoreBuiltins : Module
    {
        public CoreBuiltins Initialize()
        {
            Name = "Core";
            IsGlobal = true;

            // --- Functions for Member Access ---

            Add("__v_gettype", 1, "string", args =>
            {
                return args[0] switch
                {
                    int or long => "int",
                    double or float => "float",
                    bool => "bool",
                    string => "string",
                    null => "null",
                    _ => "unknown"
                };
            }, ("value", "auto"));

            Add("__v_tostring", 1, "string", args =>
            {
                return args[0]?.ToString() ?? "null";
            }, ("value", "auto"));

            Add("__v_len", 1, "int", args =>
            {
                if (args[0] is string s)
                    return (long)s.Length;
                throw new Exception("Length only works on strings");
            }, ("value", "string"));

            return this;
        }
    }
}
