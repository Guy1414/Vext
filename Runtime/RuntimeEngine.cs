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
        /// <param name="instructions">The list of bytecode instructions to execute.</param>
        /// <param name="usedModules">A set of modules to load if they were detected as used.</param>
        /// <param name="writer">Optional TextWriter for streaming output.</param>
        /// <param name="reader">Optional TextReader for interactive input.</param>
        /// <returns></returns>
        public static (double Time, VextValue[] FinalState, string Stdout) Run(List<Instruction> instructions, HashSet<string> usedModules, TextWriter? writer = null, TextReader? reader = null)
        {
            Stopwatch sw = Stopwatch.StartNew();

            RuntimeOutput output = new RuntimeOutput(writer, reader);

            List<Module> activeModules = [];
            if (usedModules.Contains("Math"))
                activeModules.Add(new MathModule { Name = "Math" }.Initialize());

            if (usedModules.Contains("IO"))
                activeModules.Add(new IOModule { Name = "IO" }.Initialize(output));

            DefaultFunctions defaults = new DefaultFunctions();
            defaults.Initialize();

            VextVM vm = new VextVM(
                modulesList: activeModules,
                defaults: defaults
            );

            int sp = 0;
            vm.Run(instructions, ref sp);

            string stdout = output.Flush();

            sw.Stop();
            return (sw.Elapsed.TotalMilliseconds, vm.GetVariables(), stdout);
        }
    }
}
