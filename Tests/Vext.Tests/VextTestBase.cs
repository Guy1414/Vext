using Vext.Compiler;
using Vext.Runtime;
using Vext.Shared.Runtime;



namespace Vext.Tests
{
    [CollectionDefinition("VextTests")]
    public class VextTestCollection : ICollectionFixture<object> { }

    public abstract class VextTestBase
    {

        protected static (CompilationResult Result, double Time, VextValue[] FinalState, string Stdout) RunCode(string code)
        {
            CompilationResult result = VextEngine.Compile(code);
            if (result.Errors.Count > 0)
            {
                string errors = string.Join("\n", result.Errors.Select(e => $"[!] {e.Message}. Line: {e.StartLine}, Col: {e.StartCol}."));
                throw new Exception($"Compilation failed:\n{errors}");
            }

            (double Time, VextValue[] FinalState, string Stdout) runtimeResult = RuntimeEngine.Run(result.Instructions, result.UsedModules);
            return (result, runtimeResult.Time, runtimeResult.FinalState, runtimeResult.Stdout);
        }


        protected static void AssertVariable(VextValue[] state, Dictionary<int, string> varMap, string name, object expectedValue)
        {
            KeyValuePair<int, string> entry = varMap.FirstOrDefault(kvp => kvp.Value == name);
            if (entry.Equals(default(KeyValuePair<int, string>)))
            {
                throw new Exception($"Variable '{name}' not found in variable map.");
            }

            VextValue actualValue = state[entry.Key];

            if (expectedValue is int i)
            {
                Assert.Equal(VextType.Int, actualValue.Type);
                Assert.Equal(i, actualValue.AsInt);
            } else if (expectedValue is long l)
            {
                Assert.Equal(VextType.Int, actualValue.Type);
                Assert.Equal(l, actualValue.AsInt);
            } else if (expectedValue is double d)
            {
                Assert.Equal(VextType.Float, actualValue.Type);
                Assert.Equal(d, actualValue.AsFloat, 5);
            } else if (expectedValue is float f)
            {
                Assert.Equal(VextType.Float, actualValue.Type);
                Assert.Equal(f, actualValue.AsFloat, 5);
            } else if (expectedValue is bool b)
            {
                Assert.Equal(VextType.Bool, actualValue.Type);
                Assert.Equal(b, actualValue.AsBool);
            } else if (expectedValue is string s)
            {
                Assert.Equal(VextType.String, actualValue.Type);
                Assert.Equal(s, actualValue.AsString);
            } else if (expectedValue == null)
            {
                Assert.Equal(VextType.Null, actualValue.Type);
            }
        }
    }
}
