using Vext.Compiler.VM;

namespace Vext.Shared
{
    /// <summary>
    /// Represents a single bytecode instruction for the Vext virtual machine.
    /// </summary>
    public class Instruction
    {
        /// <summary>
        /// Represents the operation code (opcode) of the instruction.
        /// </summary>
        public VextVMBytecode Op { get; set; }
        /// <summary>
        /// Represents an optional argument for the instruction (eg. a constant or an index of a variable).
        /// </summary>
        public object? Arg { get; set; }
        /// <summary>
        /// Reprsent Arg in integer form
        /// </summary>
        public int ArgInt => Arg is null ? 0 : (int)Arg;

        /// <summary>
        /// Gets the argument value as a <see cref="VextValue"/> instance.
        /// </summary>
        public VextValue ArgVal { get; set; } = VextValue.FromString("");
        /// <summary>
        /// Represents the line number in the source code where this instruction was generated.
        /// </summary>
        public int LineNumber { get; set; }
        /// <summary>
        /// Represents the column number in the source code where this instruction was generated.
        /// </summary>
        public int ColumnNumber { get; set; }
    }

    /// <summary>
    /// Defines the set of bytecode instructions supported by the Vext virtual machine.
    /// </summary>
    public enum VextVMBytecode : byte
    {
        /// <summary>Defines a function.</summary>
        DEF_FUNC,

        /// <summary>Loads a constant value onto the stack.</summary>
        LOAD_CONST,

        /// <summary>Loads a variable value onto the stack.</summary>
        LOAD_VAR,

        /// <summary>Stores the top stack value into a variable.</summary>
        STORE_VAR,

        /// <summary>Adds the top two stack values.</summary>
        ADD,

        /// <summary>Subtracts the top stack value from the previous one.</summary>
        SUB,

        /// <summary>Multiplies the top two stack values.</summary>
        MUL,

        /// <summary>Divides the previous stack value by the top value.</summary>
        DIV,

        /// <summary>Raises the previous stack value to the power of the top value.</summary>
        POW,

        /// <summary>Computes the remainder of division.</summary>
        MOD,

        /// <summary>Checks equality of the top two stack values.</summary>
        EQ,

        /// <summary>Checks inequality of the top two stack values.</summary>
        NEQ,

        /// <summary>Checks if the previous stack value is less than the top value.</summary>
        LT,

        /// <summary>Checks if the previous stack value is greater than the top value.</summary>
        GT,

        /// <summary>Checks if the previous stack value is less than or equal to the top value.</summary>
        LTE,

        /// <summary>Checks if the previous stack value is greater than or equal to the top value.</summary>
        GTE,

        /// <summary>Logical NOT of the top stack value.</summary>
        NOT,

        /// <summary>Calls a function and pushes the return value.</summary>
        CALL,

        /// <summary>Calls a function and discards the return value.</summary>
        CALL_VOID,

        /// <summary>Returns from the current function.</summary>
        RET,

        /// <summary>Unconditional jump.</summary>
        JMP,

        /// <summary>Jumps if the top stack value is false.</summary>
        JMP_IF_FALSE,

        /// <summary>Jumps if the top stack value is true.</summary>
        JMP_IF_TRUE,

        /// <summary>Removes the top value from the stack.</summary>
        POP,
        /// <summary>Increments variable by 1.</summary>
        INC_VAR,
        /// <summary>Decrements the variable by 1.</summary>
        DEC_VAR,
        /// <summary>Jump if var {op} constant</summary>
        JMP_IF_VAR_OP_CONST,
    }
}
