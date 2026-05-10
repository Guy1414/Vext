using Vext.Shared.Modules.Base;

namespace Vext.Shared.Modules.Library
{
    public class MathModule : Module
    {
        private static readonly Random rng = new();

        public Module Initialize()
        {
            Name = "Math";

            Add("Sqrt", 1, "float", args => Math.Sqrt(Convert.ToDouble(args[0])), ("num", "numeral"));
            
            Add("Pow", 2, "float", args => Math.Pow(Convert.ToDouble(args[0]), Convert.ToDouble(args[1])), ("num", "numeral"), ("power", "numeral"));
            
            Add("Sin", 1, "float", args => Math.Sin(Convert.ToDouble(args[0])), ("num", "numeral"));
            Add("Cos", 1, "float", args => Math.Cos(Convert.ToDouble(args[0])), ("num", "numeral"));
            Add("Tan", 1, "float", args => Math.Tan(Convert.ToDouble(args[0])), ("num", "numeral"));
            Add("Log", 1, "float", args => Math.Log(Convert.ToDouble(args[0])), ("num", "numeral"));
            Add("Exp", 1, "float", args => Math.Exp(Convert.ToDouble(args[0])), ("num", "numeral"));
            
            Add("Random", 0, "float", args => rng.NextDouble());
            Add("Random", 2, "float", args => Convert.ToDouble(args[0]) + rng.NextDouble() * (Convert.ToDouble(args[1]) - Convert.ToDouble(args[0])), ("min", "numeral"), ("max", "numeral"));
            
            Add("Abs", 1, "int", args => (long)Math.Abs(Convert.ToDouble(args[0])), ("num", "int"));
            Add("Abs", 1, "float", args => Math.Abs(Convert.ToDouble(args[0])), ("num", "float"));
            
            Add("Round", 1, "float", args => Math.Round(Convert.ToDouble(args[0])), ("num", "numeral"));
            Add("Floor", 1, "float", args => Math.Floor(Convert.ToDouble(args[0])), ("num", "numeral"));
            Add("Ceil", 1, "float", args => Math.Ceiling(Convert.ToDouble(args[0])), ("num", "numeral"));
            
            Add("Min", 2, "int", args => Math.Min(Convert.ToInt64(args[0]), Convert.ToInt64(args[1])), ("a", "int"), ("b", "int"));
            Add("Min", 2, "float", args => Math.Min(Convert.ToDouble(args[0]), Convert.ToDouble(args[1])), ("a", "float"), ("b", "float"));
            
            Add("Max", 2, "int", args => Math.Max(Convert.ToInt64(args[0]), Convert.ToInt64(args[1])), ("a", "int"), ("b", "int"));
            Add("Max", 2, "float", args => Math.Max(Convert.ToDouble(args[0]), Convert.ToDouble(args[1])), ("a", "float"), ("b", "float"));

            return this;
        }
    }
}
