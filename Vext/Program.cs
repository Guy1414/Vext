using System.Diagnostics;
using Vext.Bytecode_Generator;
using Vext.Lexer;
using Vext.Modules;
using Vext.Parser;
using Vext.SemanticPass;
using Vext.Shared;
using Vext.VM;

class Program
{
    static string DefaultCode()
    {
        return """
        // --- 1. Basic Types & Declarations ---
        int i = 42;
        float f = 3.14159;
        bool flag = true;
        string text = "Hello, World!";
        auto inferredInt = 100;
        auto inferredFloat = 0.25;
        auto inferredBool = false;
        auto inferredString = "auto text";

        // --- 2. Arithmetic & Type Coercion ---
        int sum = i + 10;
        float result = f * 2 - 1.5;
        string concat = text + " " + inferredString + " " + sum + " " + result;
        bool complexBool = (i > 10 && f < 10.0) || !flag;

        // --- 3. Unary & Compound Operators ---
        i++;
        --f;
        sum += 5;
        result *= 2.0;
        concat += "!";
        bool testNegation = !complexBool;

        // --- 4. Strings & Escapes ---
        string escaped = "Line1\\nLine2\\tTabbed\\\"Quote\\'Single";
        print(escaped);

        // --- 5. Comments ---
        print("Comments ignored");

        // --- 6. Conditionals ---
        if (i > 40) {
            print("i > 40");
        } else if (i == 42) {
            print("i == 42");
        } else {
            print("i < 40");
        }

        // --- 7. Loops ---
        int total = 0;
        for (int j = 0; j < 5; j++) {
            total += j;
            if (j % 2 == 0) print("Even: " + j);
        }
        int k = 0;
        while (k < 3) {
            print("While: " + k);
            k++;
        }

        // --- 8. Functions & Nested Calls ---
        int square(int n) { return n * n; }
        float multiply(float a, float b) { return a * b; }
        string greet(string name) { return "Hello, " + name + "!"; }
        int addThree(auto a, auto b, auto c) { return a + b + c; } // auto allows int/float mix

        int sq = square(3);
        int val = addThree(1, 2, sq);
        float calc = multiply(2.5, square(4));
        string message = greet("Vext");

        // --- 9. Nested Expressions & Constant Folding ---
        float complexCalc = ((2 + 3) * (5 - 1) / 2) + Math.pow(2, 3) - 4;
        int nestedFold = square(addThree(1, 2, 3)) + square(2);

        // --- 10. Booleans & Logic ---
        bool logicTest = (true && false) || (false || true) && !false;

        // --- 11. Advanced Operators ---
        int a = 10;
        int b = 3;
        int mod = a % b;
        float exp = Math.pow(a, b); // 10^3

        // --- 14. Math & Trigonometry ---
        float angle = 0.5;
        float trigTest = Math.sin(angle) * Math.cos(angle) + Math.pow(Math.tan(angle), 2);
        float hypot = Math.sqrt(Math.pow(3, 2) + Math.pow(4, 2));

        // --- 15. Deep Function Chains ---
        int s1 = square(1);
        float m = multiply(2.0, 3.0);
        int val1 = addThree(s1, m, 4);
        int deepChain = square(val1);
        print("Deep chain: " + deepChain);

        // --- 16. Full Expression Mix ---
        float finalCalc = ((3 + 5) * (2 - 7) / 2 + Math.pow(2, 3) - 4) / 2 + Math.sqrt(16) - 1;
        string mixed = "Result: " + finalCalc + ", Bool: " + logicTest + ", Msg: " + greet("Tester");

        // --- 17. Edge Cases ---
        string empty = "";
        float zero = 0.0;
        int negative = -42;
        float negativeFloat = -3.14;
        bool falseVal = false;
        bool trueVal = true;
        string specialChars = "!@#$%^&*()_+-=[]{}|;:'\\\",.<>/?";

        // --- 18. A BIG While Loop ---
        int x = 0;
        while (x < 100000) {
            x++;
        }

        // --- 19. printing everything ---
        print("sum: " + sum + ", result: " + result + ", concat: " + concat);
        print("complexBool: " + complexBool + ", testNegation: " + testNegation);
        print("val: " + val + ", calc: " + calc + ", message: " + message);
        print("complexCalc: " + complexCalc + ", nestedFold: " + nestedFold);
        print("logicTest: " + logicTest + ", mod: " + mod + ", exp: " + exp);
        print("angle: " + angle + ", trigTest: " + trigTest + ", hypot: " + hypot);
        print("finalCalc: " + finalCalc + ", mixed: " + mixed);
        print("empty: '" + empty + "', zero: " + zero + ", negative: " + negative + ", negativeFloat: " + negativeFloat);
        print("falseVal: " + falseVal + ", trueVal: " + trueVal + ", specialChars: " + specialChars);
        print("Big While Loop: " + x);
        """;
    }
    static void Main()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), "VextUserCode.vext");
        File.WriteAllText(tempPath, DefaultCode());

        Console.WriteLine("A notepad window will open with default code. Edit it as you like, save it, and close notepad.");
        Console.WriteLine("Press Enter to open the editor...");
        Console.ReadLine();

        Process editor = Process.Start("notepad.exe", tempPath);
        editor.WaitForExit();

        string userCode = File.ReadAllText(tempPath);

        Console.WriteLine("\n--- USER CODE START ---");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(userCode);
        Console.ResetColor();
        Console.WriteLine("--- USER CODE END ---\n");

        Console.WriteLine("Press Enter to start the compilation proccess...");
        Console.ReadLine();

        RunVext(userCode);

        Console.WriteLine("\nPress Enter to exit...");
        Console.ReadLine();
    }

    static void RunVext(string code)
    {
        string phase = "";
        List<Instruction> instructions = [];

        double compileTime;
        double executionTime = 0;

        double lexTime;
        int lexTokenCount;
        double parsTime;
        int parsStmtCount;
        double semTime;
        int semErrCount;
        double bytGenTime;
        int byteGenInstrCount;

        // --- COMPILATION PHASE ---
        Stopwatch compileSw = Stopwatch.StartNew();

        SemanticPass semanticPass;

        try
        {
            PrintHeader("COMPILATION PHASE");

            // Lexing
            phase = "Lexer";
            var sw = Stopwatch.StartNew();
            Lexer lexer = new Lexer(code);
            List<Token> tokens = lexer.Tokenize();
            lexTokenCount = tokens.Count;
            lexTime = sw.Elapsed.TotalMilliseconds;
            PrintStat("Lexing", lexTokenCount, "tokens", lexTime);

            // Parsing
            phase = "Parser";
            sw.Restart();
            Parser parser = new Parser(tokens);
            List<StatementNode> statements = parser.Parse();
            parsStmtCount = statements.Count;
            parsTime = sw.ElapsedMilliseconds;
            PrintStat("Parsing", parsStmtCount, "nodes", parsTime);

            // Semantic Analysis
            phase = "Semantic";
            sw.Restart();
            semanticPass = new SemanticPass(statements);

            var mathModule = new MathFunctions { Name = "Math" }.Initialize();
            foreach (var funcList in mathModule.Functions.Values)
                semanticPass.RegisterBuiltInFunctions(funcList);

            var defaults = new DefaultFunctions();
            defaults.Initialize();
            foreach (var funcList in defaults.Functions.Values)
                semanticPass.RegisterBuiltInFunctions(funcList);

            List<string> errors = semanticPass.Pass();
            semErrCount = errors.Count;
            semTime = sw.Elapsed.TotalMilliseconds;
            PrintStat("Semantics", semErrCount, "errors", semTime);

            if (errors.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var e in errors)
                    Console.WriteLine($" [!] {e}");
                Console.ResetColor();
                return;
            }

            // Bytecode Generation
            phase = "Bytecode Gen";
            sw.Restart();
            foreach (var stmt in statements)
                BytecodeGenerator.EmitStatement(stmt, instructions);

            byteGenInstrCount = instructions.Count;
            bytGenTime = sw.Elapsed.TotalMilliseconds;
            PrintStat("Bytecode Gen", byteGenInstrCount, "ops", bytGenTime);

            compileSw.Stop();
            compileTime = compileSw.Elapsed.TotalMilliseconds;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[✓] Compilation finished in {compileTime:F4} ms\n");
            Console.ResetColor();

            //foreach (var instr in instructions)
            //    Console.WriteLine($"\u001b[32mOP:\u001b[0m {instr.Op,-15} | \u001b[36mARG:\u001b[0m {instr.Arg}");

            // --- Print Instructions ---
            PrintHeader("BYTECODE INSTRUCTIONS");
            Console.WriteLine($" {"OP",-20} | {"ARG",-50}");
            Console.WriteLine(new string('─', 45));
            foreach (var instr in instructions)
            {
                // Column 1: Operation
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{instr.Op,-20}");
                Console.ResetColor();

                // Separator
                Console.Write(" │ ");

                // Column 2: Argument
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{instr.Arg,-50}");
                Console.ResetColor();

                // Divider
                Console.WriteLine(new string('─', 53));
            }

        } catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nCRITICAL ERROR during {phase}: {ex.Message}");
            Console.ResetColor();
            return; // Stop if compilation fails
        }

        // --- EXECUTION PHASE ---
        Console.WriteLine("Press Enter to start the execution proccess...");
        Console.ReadLine();

        Stopwatch execSw = Stopwatch.StartNew();
        try
        {
            phase = "Execution";
            PrintHeader("EXECUTION PHASE");

            // Re-initialize modules for the VM
            var mathModule = new MathFunctions { Name = "Math" }.Initialize();
            var defaults = new DefaultFunctions();
            defaults.Initialize();

            var vm = new VextVM(modulesList: [mathModule], defaults: defaults);
            int sp = 0;
            vm.Run(instructions, ref sp);

            execSw.Stop();
            executionTime = execSw.Elapsed.TotalMilliseconds;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[✓] Execution finished in {executionTime:F4} ms\n");
            Console.ResetColor();

            // --- Final VM State ---
            PrintHeader("FINAL VM STATE");
            var varMap = semanticPass.GetVariableMap();
            var values = vm.GetVariables();

            Console.WriteLine($" {"Variable",-20} │ {"Type",-10} │ {"Value",-30}");
            Console.WriteLine(new string('─', 65));

            for (int i = 0; i < values.Length; i++)
            {
                VextValue val = values[i]!;
                if (!varMap.ContainsKey(i) || val.Type == VextType.Null)
                    continue;
                string name = varMap[i];
                string displayValue = val.Type switch
                {
                    VextType.Number => val.AsNumber.ToString(),
                    VextType.Bool => val.AsBool ? "true" : "false",

                    VextType.String => val.AsString ?? "",
                    VextType.Null => "null",
                    _ => "unknown"
                };
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{name,-20}");
                Console.ResetColor();
                Console.Write(" │ ");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write($"{val.Type,-10}");
                Console.ResetColor();
                Console.Write(" │ ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{displayValue,-30}");
                Console.ResetColor();
                Console.WriteLine(new string('─', 65));
            }
        } catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nCRITICAL ERROR during {phase}: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine(new string('=', 65));
        Console.WriteLine($"\nTotal Run Time: {(executionTime + compileTime):F4} ms\n");
        Console.WriteLine(new string('=', 65));

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n\nSo, to recap:");
        Console.ResetColor();

        PrintHeader("COMPILATION PHASE");

        PrintStat("Lexing", lexTokenCount, "tokens", lexTime);
        PrintStat("Parsing", parsStmtCount, "nodes", parsTime);
        PrintStat("Semantics", semErrCount, "errors", semTime);
        PrintStat("Bytecode Gen", byteGenInstrCount, "ops", bytGenTime);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n[✓] Compilation finished in {compileTime:F4} ms\n");
        Console.ResetColor();

        PrintHeader("EXECUTION PHASE");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n[✓] Execution finished in {executionTime:F4} ms\n");
        Console.ResetColor();

        Console.WriteLine(new string('=', 65));
        Console.WriteLine($"\nTotal Run Time: {(executionTime + compileTime):F4} ms\n");
        Console.WriteLine(new string('=', 65));
    }

    static void PrintHeader(string title)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n--- {title} ---");
        Console.ResetColor();
    }

    static void PrintStat(string phase, int count, string label, double ms)
    {
        Console.WriteLine($" {phase,-15} | {count,-5} {label,-8} | {ms,8:F4} ms");
        Console.WriteLine(new string('─', 50));
    }
}
