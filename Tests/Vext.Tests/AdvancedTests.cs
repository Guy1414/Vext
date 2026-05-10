namespace Vext.Tests
{
    [Collection("VextTests")]
    public class AdvancedTests : VextTestBase
    {
        [Fact]
        public void UnaryOperators_IncrementDecrement_UpdateVariable()
        {
            string code = @"
                int i = 0;
                i++;
                int j = 10;
                j--;
                int k = -5;
                bool l = !true;
            ";


            (Compiler.CompilationResult? result, double _, Shared.Runtime.VextValue[]? state, string _) = RunCode(code);

            AssertVariable(state, result.VariableMap, "i", 1);
            AssertVariable(state, result.VariableMap, "j", 9);
            AssertVariable(state, result.VariableMap, "k", -5);
        }

        [Fact]
        public void Casting_NumericTypes_ConvertsCorrectly()
        {
            string code = @"
                float f = 3.9;
                int i = (int)f;
                int j = (int)10.5;
                float k = (float)5;
            ";

            (Compiler.CompilationResult? result, double _, Shared.Runtime.VextValue[]? state, string _) = RunCode(code);

            AssertVariable(state, result.VariableMap, "i", 3); // truncation
            AssertVariable(state, result.VariableMap, "j", 10);
            AssertVariable(state, result.VariableMap, "k", 5.0);
        }

        [Fact]
        public void StringEscapes_LineBreaksAndTabs_FormatCorrectly()
        {
            string code = @"
                string s = ""Line1\nLine2\tTabbed"";
            ";

            (Compiler.CompilationResult? result, double _, Shared.Runtime.VextValue[]? state, string _) = RunCode(code);

            AssertVariable(state, result.VariableMap, "s", "Line1\nLine2\tTabbed");
        }

        [Fact]
        public void CompoundAssignment_UpdateVariable_CalculatesCorrectly()
        {
            string code = @"
                int x = 10;
                x += 5;
                int y = 20;
                y -= 4;
                float z = 2.5;
                z *= 2.0;
            ";

            (Compiler.CompilationResult? result, double _, Shared.Runtime.VextValue[]? state, string _) = RunCode(code);

            AssertVariable(state, result.VariableMap, "x", 15);
            AssertVariable(state, result.VariableMap, "y", 16);
            AssertVariable(state, result.VariableMap, "z", 5.0);
        }
    }
}
