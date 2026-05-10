namespace Vext.Tests
{
    [Collection("VextTests")]
    public class FunctionTests : VextTestBase
    {
        [Fact]
        public void FunctionDefinition_SimpleCall_ReturnsCorrectValue()
        {
            string code = @"
                int square(int n) {
                    return n * n;
                }
                int result = square(4);
            ";

            (Compiler.CompilationResult result, double _, Shared.Runtime.VextValue[] state, string _) = RunCode(code);

            AssertVariable(state, result.VariableMap, "result", 16);
        }

        [Fact]
        public void FunctionDefinition_DefaultParameters_UsesDefaultValue()
        {
            string code = @"
                int add(int a, int b = 10) {
                    return a + b;
                }
                int r1 = add(5, 5);
                int r2 = add(5);
            ";

            (Compiler.CompilationResult result, double _, Shared.Runtime.VextValue[] state, string _) = RunCode(code);

            AssertVariable(state, result.VariableMap, "r1", 10);
            AssertVariable(state, result.VariableMap, "r2", 15);
        }

        [Fact]
        public void FunctionDefinition_Recursion_CalculatesFactorial()
        {
            string code = @"
                int fact(int n) {
                    if (n <= 1) return 1;
                    return n * fact(n - 1);
                }
                int result = fact(5);
            ";

            (Compiler.CompilationResult? result, double _, Shared.Runtime.VextValue[]? state, string _) = RunCode(code);

            AssertVariable(state, result.VariableMap, "result", 120);
        }
    }
}
