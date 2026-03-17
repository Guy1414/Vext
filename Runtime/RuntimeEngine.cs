using System.Diagnostics;

using Vext.Runtime.VM;
using Vext.Shared.Modules;
using Vext.Shared.Rules;
using Vext.Shared.Runtime;

namespace Vext.Runtime
{
    /// <summary>
    /// Represents the Vext Runtime Engine, responsible for running Vext code.
    /// </summary>
    public class RuntimeEngine
    {
        /// <summary>
        /// Runs the provided bytecode instructions in the Vext VM.
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        public static (double Time, VextValue[] FinalState, string Stdout) Run(List<Instruction> instructions)
        {
            Stopwatch sw = Stopwatch.StartNew();

            // TODO: check if IO is needed, output
            // RuntimeOutput output = new RuntimeOutput();

            Module mathModule = new MathModule { Name = "Math" }.Initialize();
            DefaultFunctions defaults = new DefaultFunctions();
            defaults.Initialize();

            VextVM vm = new VextVM(
                modulesList: [mathModule],
                defaults: defaults
            );

            int sp = 0;
            vm.Run(instructions, ref sp);

            // string stdout = output.Flush();
            string stdout = "";

            sw.Stop();
            return (sw.Elapsed.TotalMilliseconds, vm.GetVariables(), stdout);
        }
    }
}
