using System.Diagnostics;
using Vext.Compiler.Bytecode_Generator;
using Vext.Compiler.Modules;
using Vext.Compiler.Parsing;
using Vext.Compiler.Semantic;
using Vext.Compiler.VM;
using Vext.Lexer;
using Vext.Shared;

namespace Vext.Compiler
{
    /// <summary>
    /// Results of a compilation process
    /// </summary>
    /// <param name="Code"></param>
    /// <param name="ParsedStatements"></param>
    /// <param name="Instructions"></param>
    /// <param name="Errors"></param>
    /// <param name="VariableMap"></param>
    /// <param name="LexTime"></param>
    /// <param name="ParseTime"></param>
    /// <param name="SemanticTime"></param>
    /// <param name="BytecodeTime"></param>
    /// <param name="TokenCount"></param>
    /// <param name="NodeCount"></param>
    public record CompilationResult(
        string Code,
        List<StatementNode> ParsedStatements,
        List<Instruction> Instructions,
        List<(string, int, int)> Errors,
        Dictionary<int, string> VariableMap,
        double LexTime, double ParseTime, double SemanticTime, double BytecodeTime,
        int TokenCount, int NodeCount
    );

    /// <summary>
    /// Represents the Vext Engine, responsible for compiling and running Vext code.
    /// </summary>
    public class VextEngine
    {
        /// <summary>
        /// Runs the compilation process on the provided Vext code.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static CompilationResult Compile(string code)
        {
            List<Instruction> instructions = [];
            var sw = new Stopwatch();

            // 1. Lexing
            sw.Restart();
            var lexer = new Lexing.Lexer(code);
            (List<Token>, List<(string, int, int)>) tokens = lexer.Tokenize(); // (tokens, errors)
            double lexTime = sw.Elapsed.TotalMilliseconds;

            // 2. Parsing
            sw.Restart();
            var parser = new Parser(tokens.Item1);
            (List<StatementNode>, List<(string, int, int)>) statements = parser.Parse(); // (statements, errors)
            double parseTime = sw.Elapsed.TotalMilliseconds;

            // 3. Semantic Analysis
            sw.Restart();
            var semanticPass = new SemanticPass(statements.Item1);
            RegisterBuiltIns(semanticPass);
            List<(string, int, int)> errors = semanticPass.Pass(); // (message, line, column)
            double semTime = sw.Elapsed.TotalMilliseconds;
            Dictionary<int, string> varMap = semanticPass.GetVariableMap();

            errors.AddRange(tokens.Item2);
            errors.AddRange(statements.Item2);

            if (errors.Count > 0)
                return new CompilationResult(code, statements.Item1, [], errors, varMap, lexTime, parseTime, semTime, 0, tokens.Item1.Count, statements.Item1.Count);

            // 4. Bytecode Generation
            sw.Restart();
            foreach (StatementNode stmt in statements.Item1)
                BytecodeGenerator.EmitStatement(stmt, instructions);
            double bcTime = sw.Elapsed.TotalMilliseconds;

            return new CompilationResult(code, statements.Item1, instructions, errors, varMap, lexTime, parseTime, semTime, bcTime, tokens.Item1.Count, statements.Item1.Count);
        }

        private static void RegisterBuiltIns(SemanticPass pass)
        {
            var math = new MathFunctions { Name = "Math" }.Initialize();
            foreach (var func in math.Functions.Values)
                pass.RegisterBuiltInFunctions(func);

            var defaults = new DefaultFunctions();
            defaults.Initialize();
            foreach (var func in defaults.Functions.Values)
                pass.RegisterBuiltInFunctions(func);
        }

        /// <summary>
        /// Runs the provided bytecode instructions in the Vext VM.
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        public static (double Time, VextValue[] FinalState) Run(List<Instruction> instructions)
        {
            var sw = Stopwatch.StartNew();
            var mathModule = new MathFunctions { Name = "Math" }.Initialize();
            var defaults = new DefaultFunctions();
            defaults.Initialize();

            var vm = new VextVM(modulesList: [mathModule], defaults: defaults);
            int sp = 0;
            vm.Run(instructions, ref sp);
            sw.Stop();
            return (sw.Elapsed.TotalMilliseconds, vm.GetVariables());
        }
    }
}
