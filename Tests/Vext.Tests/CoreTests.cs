namespace Vext.Tests
{
    [Collection("VextTests")]
    public class CoreTests : VextTestBase
    {
        [Fact]
        public void Arithmetic_BasicOperations_CalculatesCorrectly()
        {
            string code = @"
                int a = 10 + 5;
                int b = 20 - 4;
                int c = 6 * 7;
                int d = 40 / 8;
                int e = 10 % 3;
                float f = 2.5 * 4.0;
            ";

            (Compiler.CompilationResult? result, double _, Shared.Runtime.VextValue[]? state, string _) = RunCode(code);

            AssertVariable(state, result.VariableMap, "a", 15);
            AssertVariable(state, result.VariableMap, "b", 16);
            AssertVariable(state, result.VariableMap, "c", 42);
            AssertVariable(state, result.VariableMap, "d", 5);
            AssertVariable(state, result.VariableMap, "e", 1);
            AssertVariable(state, result.VariableMap, "f", 10.0);
        }

        [Fact]
        public void Logic_ComplexConditions_EvaluatesCorrectly()
        {
            string code = @"
                bool b1 = true && false;
                bool b2 = true || false;
                bool b3 = !true;
                bool b4 = (10 > 5) && (3 <= 3);
                bool b5 = (1 == 2) || (5 != 4);
            ";

            (Compiler.CompilationResult? result, double _, Shared.Runtime.VextValue[]? state, string _) = RunCode(code);

            AssertVariable(state, result.VariableMap, "b1", false);
            AssertVariable(state, result.VariableMap, "b2", true);
            AssertVariable(state, result.VariableMap, "b3", false);
            AssertVariable(state, result.VariableMap, "b4", true);
            AssertVariable(state, result.VariableMap, "b5", true);
        }

        [Fact]
        public void Variables_AutoInference_InfereCorrectTypes()
        {
            string code = @"
                auto i = 10;
                auto f = 3.14;
                auto b = true;
                auto s = ""hello"";
            ";

            (Compiler.CompilationResult? result, double _, Shared.Runtime.VextValue[]? state, string _) = RunCode(code);

            AssertVariable(state, result.VariableMap, "i", 10);
            AssertVariable(state, result.VariableMap, "f", 3.14);
            AssertVariable(state, result.VariableMap, "b", true);
            AssertVariable(state, result.VariableMap, "s", "hello");
        }
    }
}
