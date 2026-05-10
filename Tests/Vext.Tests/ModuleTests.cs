namespace Vext.Tests
{
    [Collection("VextTests")]
    public class ModuleTests : VextTestBase
    {
        [Fact]
        public void MathModule_BuiltinFunctions_ExecuteCorrectly()
        {
            string code = @"
                float s = Math.Sqrt(16);
                float p = Math.Pow(2, 3);
                float r = Math.Round(3.6);
            ";

            (Compiler.CompilationResult? result, double _, Shared.Runtime.VextValue[]? state, string _) = RunCode(code);

            AssertVariable(state, result.VariableMap, "s", 4.0);
            AssertVariable(state, result.VariableMap, "p", 8.0);
            AssertVariable(state, result.VariableMap, "r", 4.0);
        }

        [Fact]
        public void CoreBuiltins_IntrinsicMembers_WorkOnTypes()
        {
            string code = @"
                string s = ""hello"";
                int len = s.Length;
                string typeS = s.Type;
                
                int i = 42;
                string typeI = i.Type;
                string strI = i.ToString();
            ";

            (Compiler.CompilationResult? result, double _, Shared.Runtime.VextValue[]? state, string _) = RunCode(code);

            AssertVariable(state, result.VariableMap, "len", 5);
            AssertVariable(state, result.VariableMap, "typeS", "string");
            AssertVariable(state, result.VariableMap, "typeI", "int");
            AssertVariable(state, result.VariableMap, "strI", "42");
        }
    }
}
