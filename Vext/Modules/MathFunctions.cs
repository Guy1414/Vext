using Vext.Compiler.Parsing;

namespace Vext.Compiler.Modules
{
    internal class MathFunctions : Module
    {
        private static readonly Random rng = new();

        private void Add(string name, Function fn)
        {
            //name = "Math." + name;
            if (!Functions.TryGetValue(name, out List<Function>? list))
            {
                list = [];
                Functions[name] = list;
            }
            fn.Name = name;
            list.Add(fn);
        }

        public Module Initialize()
        {
            Name = "Math";

            Add("sqrt", new Function("sqrt", 1, args => Math.Sqrt(Convert.ToDouble(args[0])))
            {
                Parameters =
                [
                    new FunctionParameterNode { Name = "num", Type = "numeral" }
                ],
                ReturnType = "float"
            });

            Add("pow", new Function("pow", 2, args => Math.Pow(Convert.ToDouble(args[0]), Convert.ToDouble(args[1])))
            {
                Parameters =
                [
                    new FunctionParameterNode { Name = "num", Type = "numeral" },
                    new FunctionParameterNode { Name = "power", Type = "numeral" }
                ],
                ReturnType = "float"
            });

            Add("sin", new Function("sin", 1, args => Math.Sin(Convert.ToDouble(args[0])))
            {
                Parameters = [new FunctionParameterNode { Name = "num", Type = "numeral" }],
                ReturnType = "float"
            });

            Add("cos", new Function("cos", 1, args => Math.Cos(Convert.ToDouble(args[0])))
            {
                Parameters = [new FunctionParameterNode { Name = "num", Type = "numeral" }],
                ReturnType = "float"
            });

            Add("tan", new Function("tan", 1, args => Math.Tan(Convert.ToDouble(args[0])))
            {
                Parameters = [new FunctionParameterNode { Name = "num", Type = "numeral" }],
                ReturnType = "float"
            });

            Add("log", new Function("log", 1, args => Math.Log(Convert.ToDouble(args[0])))
            {
                Parameters = [new FunctionParameterNode { Name = "num", Type = "numeral" }],
                ReturnType = "float"
            });

            Add("exp", new Function("exp", 1, args => Math.Exp(Convert.ToDouble(args[0])))
            {
                Parameters = [new FunctionParameterNode { Name = "num", Type = "numeral" }],
                ReturnType = "float"
            });

            Add("random", new Function("random", 0, args => rng.NextDouble())
            {
                Parameters = [],
                ReturnType = "float"
            });

            Add("random", new Function("random", 2, args => Convert.ToDouble(args[0]) + rng.NextDouble() * (Convert.ToDouble(args[1]) - Convert.ToDouble(args[0])))
            {
                Parameters = [
                    new FunctionParameterNode { Name = "min", Type = "numeral" },
                    new FunctionParameterNode { Name = "max", Type = "numeral" }
                ],
                ReturnType = "float"
            });

            Add("abs", new Function("abs", 1, args => Math.Abs(Convert.ToDouble(args[0])))
            {
                Parameters = [new FunctionParameterNode { Name = "num", Type = "numeral" }],
                ReturnType = "float"
            });

            Add("round", new Function("round", 1, args => Math.Round(Convert.ToDouble(args[0])))
            {
                Parameters = [new FunctionParameterNode { Name = "num", Type = "numeral" }],
                ReturnType = "float"
            });

            Add("floor", new Function("floor", 1, args => Math.Floor(Convert.ToDouble(args[0])))
            {
                Parameters = [new FunctionParameterNode { Name = "num", Type = "numeral" }],
                ReturnType = "float"
            });

            Add("ceil", new Function("ceil", 1, args => Math.Ceiling(Convert.ToDouble(args[0])))
            {
                Parameters = [new FunctionParameterNode { Name = "num", Type = "numeral" }],
                ReturnType = "float"
            });

            Add("min", new Function("min", 2, args => Math.Min(Convert.ToDouble(args[0]), Convert.ToDouble(args[1])))
            {
                Parameters =
                [
                    new FunctionParameterNode { Name = "a", Type = "numeral" },
                    new FunctionParameterNode { Name = "b", Type = "numeral" }
                ],
                ReturnType = "float"
            });

            Add("max", new Function("max", 2, args => Math.Max(Convert.ToDouble(args[0]), Convert.ToDouble(args[1])))
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
