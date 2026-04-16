using System.Runtime.InteropServices;

using Vext.Shared.AST;
using Vext.Shared.Modules;
using Vext.Shared.Rules;
using Vext.Shared.Runtime;

namespace Vext.Runtime.VM
{
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
                            VextValue val = (VextValue)(instr.ArgVal ?? VextValue.Null());
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

                    case VextVMBytecode.ADD_INT:
                        {
                            if (sp < 2)
                                throw new Exception("Not enough operands for ADD_INT.");

                            VextValue right = Pop(ref sp);
                            VextValue left = Pop(ref sp);
                            Push(ref sp, VextValue.FromInt(left.AsInt + right.AsInt));
                            break;
                        }

                    case VextVMBytecode.ADD_FLOAT:
                        {
                            if (sp < 2)
                                throw new Exception("Not enough operands for ADD_FLOAT.");

                            VextValue right = Pop(ref sp);
                            VextValue left = Pop(ref sp);
                            Push(ref sp, VextValue.FromFloat(left.ToDouble() + right.ToDouble()));
                            break;
                        }

                    case VextVMBytecode.CONCAT_STRING:
                        {
                            if (sp < 2)
                                throw new Exception("Not enough operands for CONCAT_STRING.");

                            VextValue right = Pop(ref sp);
                            VextValue left = Pop(ref sp);
                            string lStr = left.Type == VextType.String ? left.AsString : left.ToString();
                            string rStr = right.Type == VextType.String ? right.AsString : right.ToString();
                            Push(ref sp, VextValue.FromString(lStr + rStr));
                            break;
                        }

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

                            // Handle Numeric Operations (Int and/or Float)
                            if (left.IsNumeric && right.IsNumeric)
                            {
                                bool bothInt = left.Type == VextType.Int && right.Type == VextType.Int;

                                if (bothInt)
                                {
                                    long lInt = left.AsInt;
                                    long rInt = right.AsInt;
                                    VextValue res = instr.Op switch
                                    {
                                        VextVMBytecode.SUB => VextValue.FromInt(lInt - rInt),
                                        VextVMBytecode.MUL => VextValue.FromInt(lInt * rInt),
                                        VextVMBytecode.DIV => VextValue.FromInt(lInt / rInt),
                                        VextVMBytecode.MOD => VextValue.FromInt(lInt % rInt),
                                        VextVMBytecode.POW => VextValue.FromFloat(Math.Pow(lInt, rInt)),
                                        VextVMBytecode.EQ => VextValue.FromBool(lInt == rInt),
                                        VextVMBytecode.NEQ => VextValue.FromBool(lInt != rInt),
                                        VextVMBytecode.LT => VextValue.FromBool(lInt < rInt),
                                        VextVMBytecode.LTE => VextValue.FromBool(lInt <= rInt),
                                        VextVMBytecode.GT => VextValue.FromBool(lInt > rInt),
                                        VextVMBytecode.GTE => VextValue.FromBool(lInt >= rInt),
                                        _ => throw new Exception($"Unhandled numeric op {instr.Op}"),
                                    };
                                    Push(ref sp, res);
                                } else
                                {
                                    double lNum = left.ToDouble();
                                    double rNum = right.ToDouble();
                                    VextValue res = instr.Op switch
                                    {
                                        VextVMBytecode.SUB => VextValue.FromFloat(lNum - rNum),
                                        VextVMBytecode.MUL => VextValue.FromFloat(lNum * rNum),
                                        VextVMBytecode.DIV => VextValue.FromFloat(lNum / rNum),
                                        VextVMBytecode.MOD => VextValue.FromFloat(lNum % rNum),
                                        VextVMBytecode.POW => VextValue.FromFloat(Math.Pow(lNum, rNum)),
                                        VextVMBytecode.EQ => VextValue.FromBool(lNum == rNum),
                                        VextVMBytecode.NEQ => VextValue.FromBool(lNum != rNum),
                                        VextVMBytecode.LT => VextValue.FromBool(lNum < rNum),
                                        VextVMBytecode.LTE => VextValue.FromBool(lNum <= rNum),
                                        VextVMBytecode.GT => VextValue.FromBool(lNum > rNum),
                                        VextVMBytecode.GTE => VextValue.FromBool(lNum >= rNum),
                                        _ => throw new Exception($"Unhandled numeric op {instr.Op}"),
                                    };
                                    Push(ref sp, res);
                                }
                            }
                            // Handle Boolean Equality
                            else if (left.Type == VextType.Bool && right.Type == VextType.Bool)
                            {
                                VextValue res = instr.Op switch
                                {
                                    VextVMBytecode.EQ => VextValue.FromBool(left.AsBool == right.AsBool),
                                    VextVMBytecode.NEQ => VextValue.FromBool(left.AsBool != right.AsBool),
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
                        if (!variables[slot].IsNumeric)
                            throw new Exception($"JMP_IF_VAR_OP_CONST used on non-numeric variable at slot {slot}");

                        double value = variables[slot].ToDouble();
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
                        if (variables[instr.ArgInt].Type == VextType.Int)
                            variables[instr.ArgInt].AsInt++;
                        else if (variables[instr.ArgInt].Type == VextType.Float)
                            variables[instr.ArgInt].AsFloat++;
                        else
                            throw new Exception($"INC_VAR applied to non-numeric variable at index {instr.ArgInt}");
                        break;

                    case VextVMBytecode.DEC_VAR:
                        if (instr.ArgInt < 0 || instr.ArgInt >= variables.Length)
                            throw new Exception($"DEC_VAR index out of bounds: {instr.ArgInt}");
                        if (variables[instr.ArgInt].Type == VextType.Int)
                            variables[instr.ArgInt].AsInt--;
                        else if (variables[instr.ArgInt].Type == VextType.Float)
                            variables[instr.ArgInt].AsFloat--;
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

                    // Pop runtime arguments first to can inspect their runtime types
                    object[] args = new object[argCount];
                    for (int i = argCount - 1; i >= 0; i--)
                    {
                        VextValue v = Pop(ref sp);
                        args[i] = v.Type switch
                        {
                            VextType.Int => (object)v.AsInt,
                            VextType.Float => v.AsFloat,
                            VextType.Bool => v.AsBool,
                            VextType.String => v.AsString,
                            _ => null!
                        };
                    }

                    // Filter by arity first
                    List<Function> viable = [.. candidates.Where(fn => (fn.Parameters?.Count ?? 0) == argCount)];
                    if (viable.Count == 0)
                        throw new Exception($"Module '{moduleName}' has no overload for '{functionName}' taking {argCount} args.");

                    // Then filter by runtime argument types
                    Function? matched = null;
                    foreach (Function fn in viable)
                    {
                        List<FunctionParameterNode> ps = fn.Parameters ?? [];
                        bool ok = true;
                        for (int i = 0; i < ps.Count; i++)
                        {
                            string target = ps[i].Type;
                            string source = GetRuntimeArgType(args[i]);
                            if (!AreTypesCompatibleRuntime(target, source))
                            {
                                ok = false;
                                break;
                            }
                        }
                        if (ok)
                        { matched = fn; break; }
                    }

                    if (matched == null)
                        throw new Exception($"Module '{moduleName}' has no overload for '{functionName}' matching runtime argument types.");

                    return MapToVextValue(matched.Native([.. args]));
                }

                // GLOBAL / USER FUNCTION
                if (!functions.TryGetValue(funcName, out object? funcObj))
                    throw new Exception($"Function '{funcName}' not defined.");

                // If multiple overloads stored as List<Function>, resolve by arity then by runtime types
                if (funcObj is List<Function> candidatesList)
                {
                    // Pop arguments first
                    object[] args = new object[argCount];
                    for (int i = argCount - 1; i >= 0; i--)
                    {
                        VextValue v = Pop(ref sp);
                        args[i] = v.Type switch
                        {
                            VextType.Int => (object)v.AsInt,
                            VextType.Float => v.AsFloat,
                            VextType.Bool => v.AsBool,
                            VextType.String => v.AsString,
                            _ => null!
                        };
                    }

                    List<Function> viable = [.. candidatesList.Where(fn => (fn.Parameters?.Count ?? 0) == argCount)];
                    if (viable.Count == 0)
                        throw new Exception($"Function '{funcName}' has no overload taking {argCount} args.");

                    Function? matched = null;
                    foreach (Function fn in viable)
                    {
                        List<FunctionParameterNode> ps = fn.Parameters ?? [];
                        bool ok = true;
                        for (int i = 0; i < ps.Count; i++)
                        {
                            string target = ps[i].Type;
                            string source = GetRuntimeArgType(args[i]);
                            if (!AreTypesCompatibleRuntime(target, source))
                            { ok = false; break; }
                        }
                        if (ok)
                        { matched = fn; break; }
                    }

                    if (matched == null)
                        throw new Exception($"Function '{funcName}' has no overload matching runtime argument types.");

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
                            VextType.Int => (object)v.AsInt,
                            VextType.Float => v.AsFloat,
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
            long l => VextValue.FromInt(l),
            int i => VextValue.FromInt(i),
            double d => VextValue.FromFloat(d),
            float f => VextValue.FromFloat(f),
            bool b => VextValue.FromBool(b),
            string s => VextValue.FromString(s),
            _ => VextValue.Null()
        };

        private static string GetRuntimeArgType(object? obj)
        {
            if (obj is int || obj is long)
                return "int";
            if (obj is double || obj is float)
                return "float";
            if (obj is bool)
                return "bool";
            if (obj is string)
                return "string";
            return "auto";
        }

        private static bool AreTypesCompatibleRuntime(string target, string source)
        {
            if (target == "auto" || source == "auto")
                return true;
            if (target == "numeral")
                return source == "int" || source == "float";
            if (target == "error" || source == "error")
                return true;
            if (target == source)
                return true;
            if (target == "float" && source == "int")
                return true;
            if (target == "string")
                return source == "string";
            return false;
        }

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
