using Vext.Shared.AST;

namespace Vext.Shared.Modules
{
    public class MathModule : Module
    {
        private static readonly Random rng = new();

        public Module Initialize()
        {
            Name = "Math";

            Add("Sqrt", new Function("Sqrt", 1, args => Math.Sqrt(Convert.ToDouble(args[0])))
            {
                Parameters =
                [
                    new FunctionParameterNode { Name = "num", Type = "numeral" }
                ],
                ReturnType = "float"
            });

            Add("Pow", new Function("Pow", 2, args => Math.Pow(Convert.ToDouble(args[0]), Convert.ToDouble(args[1])))
            {
                Parameters =
                [
                    new FunctionParameterNode { Name = "num", Type = "numeral" },
                    new FunctionParameterNode { Name = "power", Type = "numeral" }
                ],
                ReturnType = "float"
            });

            Add("Sin", new Function("Sin", 1, args => Math.Sin(Convert.ToDouble(args[0])))
            {
                Parameters = [new FunctionParameterNode { Name = "num", Type = "numeral" }],
                ReturnType = "float"
            });

            Add("Cos", new Function("Cos", 1, args => Math.Cos(Convert.ToDouble(args[0])))
            {
                Parameters = [new FunctionParameterNode { Name = "num", Type = "numeral" }],
                ReturnType = "float"
            });

            Add("Tan", new Function("Tan", 1, args => Math.Tan(Convert.ToDouble(args[0])))
            {
                Parameters = [new FunctionParameterNode { Name = "num", Type = "numeral" }],
                ReturnType = "float"
            });

            Add("Log", new Function("Log", 1, args => Math.Log(Convert.ToDouble(args[0])))
            {
                Parameters = [new FunctionParameterNode { Name = "num", Type = "numeral" }],
                ReturnType = "float"
            });

            Add("Exp", new Function("Exp", 1, args => Math.Exp(Convert.ToDouble(args[0])))
            {
                Parameters = [new FunctionParameterNode { Name = "num", Type = "numeral" }],
                ReturnType = "float"
            });

            Add("Random", new Function("Random", 0, args => rng.NextDouble())
            {
                Parameters = [],
                ReturnType = "float"
            });

            Add("Random", new Function("Random", 2, args => Convert.ToDouble(args[0]) + rng.NextDouble() * (Convert.ToDouble(args[1]) - Convert.ToDouble(args[0])))
            {
                Parameters = [
                    new FunctionParameterNode { Name = "min", Type = "numeral" },
                    new FunctionParameterNode { Name = "max", Type = "numeral" }
                ],
                ReturnType = "float"
            });

            Add("Abs", new Function("Abs", 1, args => Math.Abs(Convert.ToDouble(args[0])))
            {
                Parameters = [new FunctionParameterNode { Name = "num", Type = "numeral" }],
                ReturnType = "float"
            });

            Add("Round", new Function("Round", 1, args => Math.Round(Convert.ToDouble(args[0])))
            {
                Parameters = [new FunctionParameterNode { Name = "num", Type = "numeral" }],
                ReturnType = "float"
            });

            Add("Floor", new Function("Floor", 1, args => Math.Floor(Convert.ToDouble(args[0])))
            {
                Parameters = [new FunctionParameterNode { Name = "num", Type = "numeral" }],
                ReturnType = "float"
            });

            Add("Ceil", new Function("Ceil", 1, args => Math.Ceiling(Convert.ToDouble(args[0])))
            {
                Parameters = [new FunctionParameterNode { Name = "num", Type = "numeral" }],
                ReturnType = "float"
            });

            Add("Min", new Function("Min", 2, args => Math.Min(Convert.ToDouble(args[0]), Convert.ToDouble(args[1])))
            {
                Parameters =
                [
                    new FunctionParameterNode { Name = "a", Type = "numeral" },
                    new FunctionParameterNode { Name = "b", Type = "numeral" }
                ],
                ReturnType = "float"
            });

            Add("Max", new Function("Max", 2, args => Math.Max(Convert.ToDouble(args[0]), Convert.ToDouble(args[1])))
            {
                Parameters =
                [
                    new FunctionParameterNode { Name = "a", Type = "numeral" },
                    new FunctionParameterNode { Name = "b", Type = "numeral" }
                ],
                ReturnType = "float"
            });

            return this;
        }
    }
}
