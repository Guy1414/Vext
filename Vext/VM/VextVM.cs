using System.Runtime.InteropServices;

using Vext.Compiler.Modules;
using Vext.Compiler.Shared;

namespace Vext.Compiler.VM
{
    /// <summary>
    /// Represents the type of a Vext value.
    /// </summary>
    public enum VextType : byte
    {
        /// <summary>
        /// Represents a numeric type.
        /// </summary>
        Number,
        /// <summary>
        /// Represents a boolean type.
        /// </summary>
        Bool,
        /// <summary>
        /// Represents a string type.
        /// </summary>
        String,
        /// <summary>
        /// Represents a null type.
        /// </summary>
        Null
    }

    /// <summary>
    /// Represents a value in the Vext virtual machine.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct VextValue
    {
        /// <summary>
        /// Represents the type of the value.
        /// </summary>
        [FieldOffset(0)] public VextType Type;
        /// <summary>
        /// Represents the boolean value.
        /// </summary>
        [FieldOffset(8)] public double AsNumber;
        /// <summary>
        /// Represents the numeric value.
        /// </summary>
        [FieldOffset(8)] public bool AsBool;
        /// <summary>
        /// Represents the string value.
        /// </summary>
        [FieldOffset(16)] public string AsString;

        /// <summary>
        /// Takes a number and returns a VextValue of type Number.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static VextValue FromNumber(double n) => new VextValue { Type = VextType.Number, AsNumber = n };
        /// <summary>
        /// Takes a boolean and returns a VextValue of type Bool.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static VextValue FromBool(bool b) => new VextValue { Type = VextType.Bool, AsBool = b };
        /// <summary>
        /// Takes a string and returns a VextValue of type String.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static VextValue FromString(string s) => new VextValue { Type = VextType.String, AsString = s };
        /// <summary>
        /// Takes no arguments and returns a VextValue of type Null.
        /// </summary>
        /// <returns></returns>
        public static VextValue Null() => new VextValue { Type = VextType.Null };

        /// <summary>
        /// Returns a string representation of the VextValue.
        /// </summary>
        /// <returns></returns>
        public override readonly string ToString()
        {
            return Type switch
            {
                VextType.Number => AsNumber.ToString(),
                VextType.Bool => AsBool.ToString(),
                VextType.String => AsString ?? "null",
                VextType.Null => "null",
                _ => "unknown"
            };
        }
    }

    /// <summary>
    /// Represents a virtual machine for executing Vext bytecode instructions, supporting user-defined and built-in
    /// functions, variables, and modules.
    /// </summary>
    /// <remarks>The VextVM class provides an execution environment for Vext scripts, managing the runtime
    /// stack, variable storage, and function/module resolution. It supports both native and user-defined functions, as
    /// well as module-based function organization. Instances of this class are not thread-safe and should not be shared
    /// across threads without external synchronization.</remarks>
    internal class VextVM
    {
        private VextValue[] stack = new VextValue[256];
        private VextValue[] variables = new VextValue[64];
        private readonly Dictionary<string, object> functions = [];
        private readonly Dictionary<string, Module> modules = [];

        /// <summary>
        /// Initializes a new instance of the VextVM class with the specified modules and default functions.
        /// </summary>
        /// <param name="modulesList">A list of modules to load into the virtual machine. If null, no modules are loaded.</param>
        /// <param name="defaults">An object containing default functions to load into the virtual machine. If null, no default functions are
        /// loaded.</param>
        public VextVM(List<Module>? modulesList = null, DefaultFunctions? defaults = null)
        {
            // Load modules
            if (modulesList != null)
            {
                foreach (Module module in modulesList)
                {
                    modules[module.Name] = module;

                    foreach (List<Function> funcList in module.Functions.Values)
                        foreach (Function fn in funcList)
                            functions[fn.Name] = fn;
                }
            }

            // Load default functions
            if (defaults != null)
            {
                foreach (KeyValuePair<string, List<Function>> kvp in defaults.Functions)
                {
                    // store the list of overloads to resolve by arity at call time
                    functions[kvp.Key] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Executes a sequence of virtual machine instructions and returns the result of the computation.
        /// </summary>
        /// <remarks>The method processes instructions sequentially, updating the stack and variable state
        /// as required. The stack pointer parameter is modified to reflect the final stack state after execution. The
        /// method supports a variety of operations, including arithmetic, logical, control flow, and function calls. If
        /// a RET instruction is encountered, the value at the top of the stack is returned immediately.</remarks>
        /// <param name="instructions">The list of instructions to execute. Each instruction represents an operation to be performed by the virtual
        /// machine. Cannot be null.</param>
        /// <param name="sp">A reference to the current stack pointer. This value is updated to reflect the stack state as instructions
        /// are executed.</param>
        /// <returns>The result value produced by the executed instructions. If the instruction sequence does not explicitly
        /// return a value, a value of type Null is returned.</returns>
        /// <exception cref="Exception">Thrown if an invalid operation is encountered during execution, such as stack underflow, invalid variable
        /// access, type mismatch, or an invalid jump target.</exception>
        public VextValue Run(List<Instruction> instructions, ref int sp)
        {
            int ip = 0; // Instruction Pointer

            ReadOnlySpan<Instruction> code = CollectionsMarshal.AsSpan(instructions);

            while (ip < code.Length)
            {
                ref readonly Instruction instr = ref code[ip];

                //Console.WriteLine($"IP: {ip} | OP: {instr.Op} | SP: {sp}");

                switch (instr.Op)
                {
                    case VextVMBytecode.LOAD_CONST:
                        {
                            VextValue val = instr.ArgVal;
                            Push(ref sp, val);
                            break;
                        }

                    case VextVMBytecode.LOAD_VAR:
                        if (instr.ArgInt < 0 || instr.ArgInt >= variables.Length)
                            throw new Exception($"Variable at index '{instr.ArgInt}' not defined/out of bounds.");

                        Push(ref sp, variables[instr.ArgInt]);
                        break;

                    case VextVMBytecode.STORE_VAR:
                        if (sp == 0)
                        {
                            Console.Error.WriteLine($"ERROR: Tried to STORE_VAR {instr.Arg} but stack is empty!");
                            break;
                        }
                        EnsureCapacity(instr.ArgInt);
                        variables[instr.ArgInt] = Pop(ref sp);
                        break;

                    case VextVMBytecode.ADD:
                    case VextVMBytecode.SUB:
                    case VextVMBytecode.MUL:
                    case VextVMBytecode.POW:
                    case VextVMBytecode.DIV:
                    case VextVMBytecode.MOD:
                    case VextVMBytecode.EQ:
                    case VextVMBytecode.NEQ:
                    case VextVMBytecode.LT:
                    case VextVMBytecode.LTE:
                    case VextVMBytecode.GT:
                    case VextVMBytecode.GTE:
                        {
                            if (sp < 2)
                                throw new Exception($"Not enough operands for {instr.Op}.");

                            VextValue right = Pop(ref sp);
                            VextValue left = Pop(ref sp);

                            // 1. Handle String Concatenation (Only for ADD)
                            if (instr.Op == VextVMBytecode.ADD &&
                               (left.Type == VextType.String || right.Type == VextType.String))
                            {
                                string lStr = left.Type switch
                                {
                                    VextType.String => left.AsString,
                                    _ => left.ToString()
                                };

                                string rStr = right.Type switch
                                {
                                    VextType.String => right.AsString,
                                    _ => right.ToString()
                                };

                                Push(ref sp, VextValue.FromString(lStr + rStr));
                            }
                            // 2. Handle Numeric Operations
                            else if (left.Type == VextType.Number && right.Type == VextType.Number)
                            {
                                double lNum = left.AsNumber;
                                double rNum = right.AsNumber;
                                VextValue res = instr.Op switch
                                {
                                    VextVMBytecode.ADD => new VextValue { Type = VextType.Number, AsNumber = lNum + rNum },
                                    VextVMBytecode.SUB => new VextValue { Type = VextType.Number, AsNumber = lNum - rNum },
                                    VextVMBytecode.MUL => new VextValue { Type = VextType.Number, AsNumber = lNum * rNum },
                                    VextVMBytecode.DIV => new VextValue { Type = VextType.Number, AsNumber = lNum / rNum },
                                    VextVMBytecode.MOD => new VextValue { Type = VextType.Number, AsNumber = lNum % rNum },
                                    VextVMBytecode.POW => new VextValue { Type = VextType.Number, AsNumber = Math.Pow(lNum, rNum) },
                                    VextVMBytecode.EQ => new VextValue { Type = VextType.Bool, AsBool = lNum == rNum },
                                    VextVMBytecode.NEQ => new VextValue { Type = VextType.Bool, AsBool = lNum != rNum },
                                    VextVMBytecode.LT => new VextValue { Type = VextType.Bool, AsBool = lNum < rNum },
                                    VextVMBytecode.LTE => new VextValue { Type = VextType.Bool, AsBool = lNum <= rNum },
                                    VextVMBytecode.GT => new VextValue { Type = VextType.Bool, AsBool = lNum > rNum },
                                    VextVMBytecode.GTE => new VextValue { Type = VextType.Bool, AsBool = lNum >= rNum },
                                    _ => throw new Exception($"Unhandled numeric op {instr.Op}"),
                                };
                                Push(ref sp, res);

                            }
                            // 3. Handle Boolean Equality
                            else if (left.Type == VextType.Bool && right.Type == VextType.Bool)
                            {
                                VextValue res = instr.Op switch
                                {
                                    VextVMBytecode.EQ => new VextValue { Type = VextType.Bool, AsBool = left.AsBool == right.AsBool },
                                    VextVMBytecode.NEQ => new VextValue { Type = VextType.Bool, AsBool = left.AsBool != right.AsBool },
                                    _ => throw new Exception($"Operator {instr.Op} not supported for Booleans.")
                                };
                                Push(ref sp, res);
                            } else
                            {
                                throw new Exception($"Type mismatch for {instr.Op}: {left.Type} and {right.Type}.");
                            }
                            break;
                        }

                    case VextVMBytecode.JMP:
                        int target = instr.ArgInt;
                        if (target < 0 || target >= instructions.Count)
                            throw new Exception($"Invalid jump target {target}.");

                        ip = instr.ArgInt;
                        continue;

                    case VextVMBytecode.JMP_IF_FALSE:
                        {
                            if (sp < 1)
                                throw new Exception("Stack empty: cannot evaluate JMP_IF_FALSE.");

                            bool cond = Pop(ref sp).AsBool;
                            if (!cond)
                            { ip = instr.ArgInt; continue; }
                            break;
                        }

                    case VextVMBytecode.JMP_IF_TRUE:
                        {
                            if (sp < 1)
                                throw new Exception("Stack empty: cannot evaluate JMP_IF_TRUE.");

                            bool cond = Pop(ref sp).AsBool;

                            if (cond)
                            {
                                ip = instr.ArgInt;
                                continue;
                            }
                            break;
                        }

                    case VextVMBytecode.JMP_IF_VAR_OP_CONST:
                        (int slot, string op, double limit, int targetJMPIF) = ((int, string, double, int))instr.Arg!;
                        if (variables[slot].Type != VextType.Number)
                            throw new Exception($"JMP_IF_VAR_OP_CONST used on non-numeric variable at slot {slot}");

                        double value = variables[slot].AsNumber;
                        bool jump = op switch
                        {
                            "<" => value >= limit,
                            "<=" => value > limit,
                            ">" => value <= limit,
                            ">=" => value < limit,
                            _ => throw new Exception($"Unknown operator '{op}' in JMP_IF_VAR_OP_CONST")
                        };

                        if (jump)
                        {
                            ip = targetJMPIF;
                            continue;
                        }
                        break;

                    case VextVMBytecode.RET:
                        if (sp < 1)
                            throw new Exception("Stack empty: cannot RET value.");

                        return Pop(ref sp);

                    case VextVMBytecode.CALL:
                        (object funcInfo, int argCount) = ((object, int))instr.Arg!;
                        VextValue callResult = ExecuteCall(funcInfo, argCount, ref sp);
                        if (callResult.Type != VextType.Null)
                            Push(ref sp, callResult);
                        break;

                    case VextVMBytecode.CALL_VOID:
                        (object funcInfoV, int argCountV) = ((object, int))instr.Arg!;
                        ExecuteCall(funcInfoV, argCountV, ref sp);
                        break;


                    case VextVMBytecode.DEF_FUNC:
                        UserFunction userFunc = (UserFunction)instr.Arg!;
                        functions[userFunc.Name] = userFunc;
                        break;

                    case VextVMBytecode.POP:
                        if (sp == 0)
                            throw new Exception("Stack empty: cannot POP value.");
                        sp--;
                        break;

                    case VextVMBytecode.NOT:
                        if (sp == 0)
                            throw new Exception("Stack empty: cannot NOT");
                        VextValue top = Pop(ref sp);
                        top.AsBool = !top.AsBool;
                        Push(ref sp, top);
                        break;

                    case VextVMBytecode.INC_VAR:
                        if (instr.ArgInt < 0 || instr.ArgInt >= variables.Length)
                            throw new Exception($"INC_VAR index out of bounds: {instr.ArgInt}");
                        if (variables[instr.ArgInt].Type == VextType.Number)
                            variables[instr.ArgInt].AsNumber++;
                        else
                            throw new Exception($"INC_VAR applied to non-numeric variable at index {instr.ArgInt}");
                        break;

                    case VextVMBytecode.DEC_VAR:
                        if (instr.ArgInt < 0 || instr.ArgInt >= variables.Length)
                            throw new Exception($"DEC_VAR index out of bounds: {instr.ArgInt}");
                        if (variables[instr.ArgInt].Type == VextType.Number)
                            variables[instr.ArgInt].AsNumber--;
                        else
                            throw new Exception($"DEC_VAR applied to non-numeric variable at index {instr.ArgInt}");
                        break;

                }
                ip++;
            }

            return new VextValue { Type = VextType.Null };
        }

        private VextValue ExecuteCall(object funcNode, int argCount, ref int sp)
        {
            if (sp < argCount)
                throw new Exception($"Execution Error: function expected {argCount} args but stack has {sp}");

            if (funcNode is string funcName)
            {
                // MODULE CALL
                if (funcName.Contains('.'))
                {
                    string[] parts = funcName.Split('.');
                    string moduleName = parts[0];
                    string functionName = parts[1];

                    if (!modules.TryGetValue(moduleName, out Module? module))
                        throw new Exception($"Module '{moduleName}' not loaded.");

                    if (!module.Functions.TryGetValue(functionName, out List<Function>? candidates))
                        throw new Exception($"Function '{functionName}' not found in module '{moduleName}'.");

                    Function matched = candidates.FirstOrDefault(fn => (fn.Parameters?.Count ?? 0) == argCount)
                        ?? throw new Exception($"Module '{moduleName}' has no overload for '{functionName}' taking {argCount} args.");

                    // Native functions consume arguments as objects
                    object[] args = new object[argCount];
                    for (int i = argCount - 1; i >= 0; i--)
                    {
                        VextValue v = Pop(ref sp);
                        args[i] = v.Type switch
                        {
                            VextType.Number => v.AsNumber,
                            VextType.Bool => v.AsBool,
                            VextType.String => v.AsString,
                            _ => null!
                        };
                    }

                    return MapToVextValue(matched.Native([.. args]));
                }

                // GLOBAL / USER FUNCTION
                if (!functions.TryGetValue(funcName, out object? funcObj))
                    throw new Exception($"Function '{funcName}' not defined.");

                // If multiple overloads stored as List<Function>, resolve by arity
                if (funcObj is List<Function> candidatesList)
                {
                    Function matched = candidatesList.FirstOrDefault(fn => (fn.Parameters?.Count ?? 0) == argCount)
                        ?? throw new Exception($"Function '{funcName}' has no overload taking {argCount} args.");

                    object[] args = new object[argCount];
                    for (int i = argCount - 1; i >= 0; i--)
                    {
                        VextValue v = Pop(ref sp);
                        args[i] = v.Type switch
                        {
                            VextType.Number => v.AsNumber,
                            VextType.Bool => v.AsBool,
                            VextType.String => v.AsString,
                            _ => null!
                        };
                    }

                    return MapToVextValue(matched.Native([.. args]));
                }

                // Native global
                if (funcObj is Function nativeFunc)
                {
                    object[] args = new object[argCount];
                    for (int i = argCount - 1; i >= 0; i--)
                    {
                        VextValue v = Pop(ref sp);
                        args[i] = v.Type switch
                        {
                            VextType.Number => v.AsNumber,
                            VextType.Bool => v.AsBool,
                            VextType.String => v.AsString,
                            _ => null!
                        };
                    }

                    return MapToVextValue(nativeFunc.Native([.. args]));
                }

                // USER FUNCTION
                if (funcObj is UserFunction userFunc)
                {
                    //Console.WriteLine($"[CALL] Entering {userFunc.Name} with SP={sp}");

                    // Snapshot locals
                    VextValue[] snapshot = (VextValue[])variables.Clone();

                    // Run function body
                    VextValue ret = Run(userFunc.Body, ref sp);

                    // Restore locals
                    variables = snapshot;

                    return ret;
                }
            }

            return VextValue.Null();
        }

        private static VextValue MapToVextValue(object? value) => value switch
        {
            double d => new VextValue { Type = VextType.Number, AsNumber = d },
            float f => new VextValue { Type = VextType.Number, AsNumber = f },
            int i => new VextValue { Type = VextType.Number, AsNumber = i },
            bool b => new VextValue { Type = VextType.Bool, AsBool = b },
            string s => new VextValue { Type = VextType.String, AsString = s },
            _ => new VextValue { Type = VextType.Null }
        };

        private void EnsureCapacity(int index)
        {
            if (index >= variables.Length)
            {
                // Double the size until it fits the index
                int newSize = variables.Length * 2;
                while (index >= newSize)
                    newSize *= 2;

                Array.Resize(ref variables, newSize);
            }
        }

        private void Push(ref int sp, VextValue val)
        {
            if (sp >= stack.Length)
                Array.Resize(ref stack, stack.Length * 2);

            stack[sp++] = val;
        }

        private VextValue Pop(ref int sp)
        {
            if (sp == 0)
                throw new Exception("Stack empty: cannot POP value.");
            return stack[--sp];
        }

        /// <summary>
        /// Gets the collection of variables associated with this instance.
        /// </summary>
        /// <returns>An array of <see cref="VextValue"/> objects representing the variables. The array may be empty if no
        /// variables are defined.</returns>
        public VextValue[] GetVariables() => variables;
    }
}
