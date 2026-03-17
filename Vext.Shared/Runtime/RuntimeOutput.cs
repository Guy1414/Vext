using System.Text;

namespace Vext.Shared.Runtime
{
    /// <summary>
    /// RuntimeOutput is a utility class that provides a simple way to capture output generated during the execution of Vext code.
    /// </summary>
    public class RuntimeOutput(TextWriter? writer = null, TextReader? reader = null)
    {
        private readonly StringBuilder _buffer = new();
        private readonly TextWriter _writer = writer ?? TextWriter.Null;
        private readonly TextReader _reader = reader ?? TextReader.Null;

        /// <summary>
        /// Writes text to the runtime output buffer and the writer.
        /// </summary>
        /// <param name="text"></param>
        public void Write(string text)
        {
            _buffer.Append(text);
            _writer.Write(text);
        }

        /// <summary>
        /// Writes a line of text to the runtime output buffer and the writer, followed by a newline character.
        /// </summary>
        /// <param name="text"></param>
        public void WriteLine(string text)
        {
            _buffer.AppendLine(text);
            _writer.WriteLine(text);
        }

        /// <summary>
        /// Reads a line from the input stream.
        /// </summary>
        public string ReadLine()
        {
            return _reader.ReadLine() ?? "";
        }

        /// <summary>
        /// Reads an integer from the input stream.
        /// </summary>
        public long ReadInt()
        {
            string input = ReadLine();
            return long.TryParse(input, out var result) ? result : 0;
        }

        /// <summary>
        /// Reads a float from the input stream.
        /// </summary>
        public double ReadFloat()
        {
            string input = ReadLine();
            return double.TryParse(input, out var result) ? result : 0.0;
        }

        /// <summary>
        /// Flushes the captured content buffer and returns it as a string.
        /// </summary>
        /// <returns></returns>
        public string Flush()
        {
            string result = _buffer.ToString();
            _buffer.Clear();
            return result;
        }
    }
}
