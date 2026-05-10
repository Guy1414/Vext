using Vext.Shared.Modules.Base;
using Vext.Shared.Runtime;

namespace Vext.Shared.Modules.Library
{
    public class IOModule : Module
    {
        public Module Initialize(RuntimeOutput output)
        {
            Name = "IO";

            // --- Input ---

            Add("ReadLine", 0, "string", args => output.ReadLine());
            Add("ReadInt", 0, "int", args => output.ReadInt());
            Add("ReadFloat", 0, "float", args => output.ReadFloat());

            // --- Output ---

            Add("Print", 1, "void", args =>
            {
                output.Write(ToVextString(args[0]));
                return null!;
            }, ("value", "auto"));

            Add("Print", 0, "void", args =>
            {
                output.Write("");
                return null!;
            });

            Add("Println", 1, "void", args =>
            {
                output.WriteLine(ToVextString(args[0]));
                return null!;
            }, ("value", "auto"));

            Add("Println", 0, "void", args =>
            {
                output.WriteLine("");
                return null!;
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
