using System.Text;

namespace Vext.Compiler
{
    /// <summary>
    /// RuntimeOutput is a utility class that provides a simple way to capture output generated during the execution of Vext code.
    /// </summary>
    public class RuntimeOutput
    {
        private readonly StringBuilder _buffer = new();

        /// <summary>
        /// Writes text to the runtime output buffer.
        /// </summary>
        /// <param name="text"></param>
        public void Write(string text)
        {
            _buffer.Append(text);
        }

        /// <summary>
        /// Writes a line of text to the runtime output buffer, followed by a newline character.
        /// </summary>
        /// <param name="text"></param>
        public void WriteLine(string text)
        {
            _buffer.AppendLine(text);
        }

        /// <summary>
        /// Flushes the current contents of the runtime output buffer and returns it as a string. After flushing, the buffer is cleared.
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
