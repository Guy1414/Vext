namespace Vext.Tests
{
    [Collection("VextTests")]
    public class ControlFlowTests : VextTestBase
    {
        [Fact]
        public void IfStatement_Branching_ExecutesCorrectBranch()
        {
            string code = @"
                int x = 10;
                int result = 0;
                if (x > 5) {
                    result = 1;
                } else {
                    result = 2;
                }

                int y = 3;
                int result2 = 0;
                if (y > 5) {
                    result2 = 1;
                } else if (y == 3) {
                    result2 = 2;
                } else {
                    result2 = 3;
                }
            ";

            (Compiler.CompilationResult? result, double _, Shared.Runtime.VextValue[]? state, string _) = RunCode(code);

            AssertVariable(state, result.VariableMap, "result", 1);
            AssertVariable(state, result.VariableMap, "result2", 2);
        }

        [Fact]
        public void ForLoop_Iteration_RunsCorrectNumberOfTimes()
        {
            string code = @"
                int sum = 0;
                for (int i = 0; i < 5; i++) {
                    sum = sum + i;
                }
            ";

            (Compiler.CompilationResult? result, double _, Shared.Runtime.VextValue[]? state, string _) = RunCode(code);

            AssertVariable(state, result.VariableMap, "sum", 10); // 0+1+2+3+4 = 10
        }

        [Fact]
        public void WhileLoop_Condition_RunsUntilFalse()
        {
            string code = @"
                int sum = 0;
                int i = 0;
                while (i < 5) {
                    sum = sum + i;
                    i++;
                }
            ";

            (Compiler.CompilationResult? result, double _, Shared.Runtime.VextValue[]? state, string _) = RunCode(code);

            AssertVariable(state, result.VariableMap, "sum", 10);
            AssertVariable(state, result.VariableMap, "i", 5);
        }
    }
}
