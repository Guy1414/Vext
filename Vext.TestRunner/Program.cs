using System.Diagnostics;

using Vext.Compiler;
using Vext.Runtime;
using Vext.Shared.Rules;
using Vext.Shared.Runtime;

using static Vext.Compiler.Diagnostics.Diagnostic;

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

        // --- Intrinsic Members ---
        IO.Println("Type of i: " + i.Type);            // "int"
        IO.Println("String of f: " + f.ToString());    // "3.14159"
        IO.Println("Length: " + text.Length);        // 13

        // --- 3. Functions & Overloading ---
        int square(int n) { return n * n; }
        string greet(string name = "Guy") { return "Hello, " + name + "!"; }

        IO.Println(greet("Vext")); // "Hello, Vext!"
        IO.Println(greet());        // "Hello, User!"

        // --- 3. Unary & Compound Operators ---
        i++;
        sum += 5;
        result *= 2.0;
        concat += "!";
        bool testNegation = !complexBool;

        // --- 4. Strings & Escapes ---
        string escaped = "Line1\\nLine2\\tTabbed\\\"Quote\\'Single";
        IO.Println(escaped);

        // --- 5. Comments ---
        IO.Println("Comments ignored");

        // --- 6. Conditionals ---
        if (i > 40) {
            IO.Println("i > 40");
        } else if (i == 42) {
            IO.Println("i == 42");
        } else {
            IO.Println("i < 40");
        }

        // --- 7. Loops ---
        int total = 0;
        for (int j = 0; j < 5; j++) {
            total += j;
            if (j % 2 == 0) IO.Println("Even: " + j);
        }
        int k = 0;
        while (k < 3) {
            IO.Println("While: " + k);
            k++;
        }

        // --- 8. Functions & Nested Calls ---
        float multiply(float a, float b) { return a * b; }
        int addThree(auto a, auto b, auto c) { return a + b + c; } // auto allows int/float mix

        int sq = square(3);
        int val = addThree(1, 2, sq);
        float calc = multiply(2.5, square(4));
        string message = greet("Vext");

        // --- 9. Nested Expressions & ConsTant Folding ---
        float complexCalc = ((2 + 3) * (5 - 1) / 2) + Math.Pow(2, 3) - 4;
        int nestedFold = square(addThree(1, 2, 3)) + square(2);

        // --- 10. Booleans & Logic ---
        bool logicTest = (true && false) || (false || true) && !false;

        // --- 11. Advanced Operators ---
        int a = 10;
        int b = 3;
        int mod = a % b;
        float exp = Math.Pow(a, b); // 10^3

        for (int j = 0; j < 3; j++) {
            IO.Println("Loop: " + j);
        }

        // --- 14. Math & Trigonometry ---
        float angle = 0.5;
        float trigTest = Math.Sin(angle) * Math.Cos(angle) + Math.Pow(Math.Tan(angle), 2);
        float hypot = Math.Sqrt(Math.Pow(3, 2) + Math.Pow(4, 2));

        // --- 15. Deep Function Chains ---
        int s1 = square(1);
        float m = multiply(2.0, 3.0);
        int val1 = addThree(s1, m, 4);
        int deepChain = square(val1);
        IO.Println("Deep chain: " + deepChain);

        // --- 16. Full Expression Mix ---
        float finalCalc = ((3 + 5) * (2 - 7) / 2 + Math.Pow(2, 3) - 4) / 2 + Math.Sqrt(16) - 1;
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

        // --- 19. Printing everything ---
        IO.Println("sum: " + sum + ", result: " + result + ", concat: " + concat);
        IO.Println("complexBool: " + complexBool + ", testNegation: " + testNegation);
        IO.Println("val: " + val + ", calc: " + calc + ", message: " + message);
        IO.Println("complexCalc: " + complexCalc + ", nestedFold: " + nestedFold);
        IO.Println("logicTest: " + logicTest + ", mod: " + mod + ", exp: " + exp);
        IO.Println("angle: " + angle + ", trigTest: " + trigTest + ", hypot: " + hypot);
        IO.Println("finalCalc: " + finalCalc + ", mixed: " + mixed);
        IO.Println("empty: '" + empty + "', zero: " + zero + ", negative: " + negative + ", negativeFloat: " + negativeFloat);
        IO.Println("falseVal: " + falseVal + ", trueVal: " + trueVal + ", specialChars: " + specialChars);
        IO.Println("Big While Loop: " + x);
        IO.Println("Hypotenuse: " + hypot);
        int x1 = 10;
        IO.Println("Type of x: " + x1.Type);
        IO.Println("String of x: " + x1.ToString());
        float f1 = 3.14;
        IO.Println("Type of f: " + f1.Type);
        IO.Println("String of f: " + f1.ToString());
        // Chaining
        IO.Println("Chained type: " + x1.ToString().Type);
        // Module access
        IO.Println("Sqrt of 16: " + Math.Sqrt(16));
        """;
    }

    static void Main()
    {
        Console.WriteLine("A notepad window will open with default code. Edit it as you like, save it, and close notepad.");
        Console.WriteLine("Press Enter to open the editor...");
        Console.ReadLine();
        string tempPath = Path.Combine(Path.GetTempPath(), "VextUserCode.vext");
        File.WriteAllText(tempPath, DefaultCode());

        Process.Start("notepad.exe", tempPath).WaitForExit();

        string userCode = File.ReadAllText(tempPath);

        Console.WriteLine("\n--- USER CODE START ---");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(userCode);
        Console.ResetColor();
        Console.WriteLine("--- USER CODE END ---\n");

        RunVext(userCode);
    }

    static void RunVext(string code)
    {
        string phase = "";

        // Variables for summary
        double compileTime;
        double executionTime = 0;

        // --- COMPILATION PHASE ---
        Console.WriteLine("Press Enter to start the compilation process...");
        Console.ReadLine();

        CompilationResult result;
        Stopwatch compileSw = Stopwatch.StartNew();
        try
        {
            phase = "Compilation";
            PrintHeader("COMPILATION PHASE");

            result = VextEngine.Compile(code);

            PrintStat("Lexing", result.TokenCount, "tokens", result.LexTime);
            PrintStat("Parsing", result.NodeCount, "nodes", result.ParseTime);
            PrintStat("Semantics", result.Errors.Count, "errors", result.SemanticTime);

            if (result.Errors.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (ErrorDescriptor e in result.Errors)
                    Console.WriteLine($" [!] {e.Message}. Line: {e.StartLine}, Col: {e.StartCol}.");
                Console.ResetColor();
                return;
            }

            PrintStat("Bytecode Gen", result.Instructions.Count, "ops", result.BytecodeTime);

            compileSw.Stop();
            compileTime = compileSw.Elapsed.TotalMilliseconds;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[✓] Compilation finished in {compileTime:F4} ms\n");
            Console.ResetColor();

            // --- Print Instructions ---
            PrintHeader("BYTECODE INSTRUCTIONS");
            Console.WriteLine($" {"OP",-20} | {"ARG",-50}");
            Console.WriteLine(new string('─', 53));
            foreach (Instruction instr in result.Instructions)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{instr.Op,-20}");
                Console.ResetColor();
                Console.Write(" │ ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{instr.ArgVal,-50}");
                Console.ResetColor();
                Console.WriteLine(new string('─', 53));
            }
        } catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nCRITICAL ERROR during {phase}: {ex.Message}");
            Console.ResetColor();
            return;
        }

        // --- EXECUTION PHASE ---
        Console.WriteLine("Press Enter to start the execution process...");
        Console.ReadLine();

        VextValue[] finalState = null!;
        string output = "";
        Stopwatch execSw = Stopwatch.StartNew();
        try
        {
            phase = "Execution";
            PrintHeader("EXECUTION PHASE");

            (double execTime, VextValue[] state, string stdout) = RuntimeEngine.Run(result.Instructions, result.UsedModules, Console.Out, Console.In);
            finalState = state;
            execSw.Stop();
            executionTime = execSw.Elapsed.TotalMilliseconds;

            output = stdout ?? "";

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[✓] Execution finished in {executionTime:F4} ms\n");
            Console.ResetColor();
        } catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nCRITICAL ERROR during {phase}: {ex.Message}");
            Console.ResetColor();
        }

        PrintHeader("Output");
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine(output);

        // --- Final VM State ---
        PrintHeader("FINAL VM STATE");
        DisplayState(result.VariableMap, finalState);

        // --- Total Run Time ---
        Console.WriteLine(new string('=', 65));
        Console.WriteLine($"\nTotal Run Time: {(executionTime + compileTime):F4} ms\n");
        Console.WriteLine(new string('=', 65));

        // --- Pause before recap ---
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();

        // --- Recap ---
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n\nSo, to recap:");
        Console.ResetColor();

        PrintHeader("COMPILATION PHASE");
        PrintStat("Lexing", result.TokenCount, "tokens", result.LexTime);
        PrintStat("Parsing", result.NodeCount, "nodes", result.ParseTime);
        PrintStat("Semantics", result.Errors.Count, "errors", result.SemanticTime);
        PrintStat("Bytecode Gen", result.Instructions.Count, "ops", result.BytecodeTime);

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

        PrintHeader("Output");
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine(output);
        Console.ResetColor();
    }

    static void DisplayState(Dictionary<int, string> varMap, VextValue[] values)
    {
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
                VextType.Int => val.AsInt.ToString(),
                VextType.Float => val.AsFloat.ToString(),
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
