namespace Vext.Tests
{
    [Collection("VextTests")]
    public class ErrorTests : VextTestBase
    {
        [Fact]
        public void UndefinedVariable_ThrowsCompilationError()
        {
            string code = @"
                int x = y + 1;
            ";

            Assert.Throws<Exception>(() => RunCode(code));
        }

        [Fact]
        public void TypeMismatch_Assignment_ThrowsCompilationError()
        {
            string code = @"
                int x = ""hello"";
            ";

            Assert.Throws<Exception>(() => RunCode(code));
        }

        [Fact]
        public void FunctionArityMismatch_ThrowsCompilationError()
        {
            string code = @"
                int square(int n) { return n * n; }
                int x = square(1, 2);
            ";

            Assert.Throws<Exception>(() => RunCode(code));
        }

        [Fact]
        public void MissingSemicolon_DifferentLine_OmitsButFound()
        {
            string code = @"
                IO.Println(""Sqrt of 16: "" + Math.Sqrt(16))
                int x = 5;
            ";

            Exception ex = Assert.Throws<Exception>(() => RunCode(code));
            Assert.Contains("Expected Punctuation ';'", ex.Message);
            Assert.DoesNotContain("but found", ex.Message);
        }

        [Fact]
        public void MissingSemicolon_SameLine_IncludesButFound()
        {
            string code = @"IO.Println(""Sqrt of 16: "" + Math.Sqrt(16)) int x = 5;";

            Exception ex = Assert.Throws<Exception>(() => RunCode(code));
            Assert.Contains("Expected Punctuation ';' but found 'int' (Keyword)", ex.Message);
        }
    }
}
