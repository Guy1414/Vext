namespace Vext.Shared.AST
{
    /// <summary>
    /// Represents a node in an expression tree, such as a value or an operation.
    /// </summary>
    /// <remarks>ExpressionNode instances are typically used to model the structure of parsed expressions,
    /// including literals, variables, and operators. This public class serves as a base for more specific expression node
    /// types.</remarks>
    public class ExpressionNode // represents values or operations (like 1 + 2, x, "hello")
    {
        /// <summary>
        /// The line number in the source code where this expression appears
        /// </summary>
        public int Line { get; set; }
        /// <summary>
        /// The column number in the source code where this expression appears
        /// </summary>
        public int StartColumn { get; set; }
        /// <summary>
        /// The ending column number in the source code where this expression ends
        /// </summary>
        public int EndColumn { get; set; }
    }

    public class BinaryExpressionNode : ExpressionNode // for binary operations (+, -, *, /)
    {
        public required ExpressionNode Left { get; set; }
        public required string Operator { get; set; }
        public int OperatorColumnStart { get; set; }
        public int OperatorColumnEnd { get; set; }
        public required ExpressionNode Right { get; set; }
    }

    public class LiteralNode : ExpressionNode // numbers, strings, booleans
    {
        public required object Value { get; set; }
        public bool IsError { get; set; } = false;
    }

    public class VariableNode : ExpressionNode // identifiers
    {
        public int SlotIndex;
        public required string Name { get; set; }
    }

    /// <summary>
    /// Represents a statement in the abstract syntax tree (AST) of the programming language.
    /// </summary>
    public abstract class StatementNode // base public class for statements
    {
        /// <summary>
        /// Represents the line number in the source code where this statement appears.
        /// </summary>
        public int Line { get; set; }
        /// <summary>
        /// Represents the column number in the source code where this statement appears.
        /// </summary>
        public int StartColumn { get; set; }
        /// <summary>
        /// Represents the ending column number in the source code where this statement ends.
        /// </summary>
        public int EndColumn { get; set; }
    }

    public class ExpressionStatementNode : StatementNode // e.g., x + 1;
    {
        public required ExpressionNode Expression { get; set; }
    }

    public class VariableDeclarationNode : StatementNode
    {
        public int SlotIndex;
        public required string VariableType { get; set; }
        public int TypeStartColumn;
        public int TypeEndColumn;
        public required string DeclaredType;
        public required string Name { get; set; }
        public int NameLine { get; set; }
        public int NameStartColumn { get; set; }
        public int NameEndColumn { get; set; }
        public ExpressionNode? Initializer { get; set; } // optional
    }

    public class IfStatementNode : StatementNode
    {
        public required ExpressionNode Condition { get; set; }
        public required List<StatementNode> Body { get; set; }
        public List<StatementNode>? ElseBody { get; set; }
        public int ElseLine { get; set; }
        public int ElseStartColumn { get; set; }
    }

    public class WhileStatementNode : StatementNode
    {
        public required int KeywordColumnStart { get; set; }
        public required int KeywordColumnEnd { get; set; }
        public required ExpressionNode Condition { get; set; }
        public required List<StatementNode> Body { get; set; }
    }

    public class ForStatementNode : StatementNode
    {
        public required int KeywordColumnStart { get; set; }
        public required int KeywordColumnEnd { get; set; }
        public StatementNode? Initialization { get; set; }
        public ExpressionNode? Condition { get; set; }
        public StatementNode? Increment { get; set; }
        public required List<StatementNode> Body { get; set; }
    }

    public class ReturnStatementNode : StatementNode
    {
        public required int KeywordColumnStart { get; set; }
        public required int KeywordColumnEnd { get; set; }
        public required ExpressionNode? Expression { get; set; } = null;
    }

    public class FunctionCallNode : ExpressionNode
    {
        public required string FunctionName { get; set; }
        public required int FunctionNameStartColumn { get; set; }
        public required int FunctionNameEndColumn { get; set; }
        public required List<ExpressionNode> Arguments { get; set; } = [];
        public string? ReturnType { get; set; } = "unknown";
    }

    public class MemberAccessNode : ExpressionNode
    {
        public required ExpressionNode Receiver { get; set; }
        public required string MemberName { get; set; }
        public int MemberNameStartColumn { get; set; }
        public int MemberNameEndColumn { get; set; }
        public List<ExpressionNode>? Arguments { get; set; } = null;
        public bool IsModuleCall { get; set; } = false;
        public string ReturnType { get; set; } = "auto";
    }

    public class FunctionDefinitionNode : StatementNode
    {
        public required string ReturnType { get; set; }
        public int ReturnTypeStartColumn { get; set; }
        public int ReturnTypeEndColumn { get; set; }
        public required string FunctionName { get; set; }
        public int NameLine { get; set; }
        public int NameStartColumn { get; set; }
        public int NameEndColumn { get; set; }
        public List<FunctionParameterNode> Arguments { get; set; } = [];
        public required List<StatementNode> Body { get; set; }
    }

    /// <summary>
    /// Represents a function parameter in a function definition.
    /// </summary>
    public class FunctionParameterNode
    {
        /// <summary>
        /// The slot index assigned to this parameter for variable storage.
        /// </summary>
        public int SlotIndex { get; set; }
        /// <summary>
        /// The data type of the parameter (e.g., "int", "float", "string").
        /// </summary>
        public required string Type { get; set; }
        /// <summary>
        /// Represents the starting column number in the source code where the parameter's type is defined.
        /// </summary>
        public int TypeStartColumn { get; set; }
        /// <summary>
        /// Represents the ending column number in the source code where the parameter's type definition ends.
        /// </summary>
        public int TypeEndColumn { get; set; }
        /// <summary>
        /// The name of the parameter.
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// An optional initializer expression for the parameter.
        /// </summary>
        public ExpressionNode? Initializer { get; set; } // optional
        /// <summary>
        /// The line number where the parameter is defined.
        /// </summary>
        public int Line { get; set; }
        /// <summary>
        /// The start column where the parameter is defined.
        /// </summary>
        public int StartColumn { get; set; }
        /// <summary>
        /// The end column where the parameter definition ends.
        /// </summary>
        public int EndColumn { get; set; }
        /// <summary>
        /// The line number where the parameter name appears.
        /// </summary>
        public int NameLine { get; set; }
        /// <summary>
        /// The start column where the parameter name appears.
        /// </summary>
        public int NameStartColumn { get; set; }
        /// <summary>
        /// The end column where the parameter name appears.
        /// </summary>
        public int NameEndColumn { get; set; }
    }

    public class UnaryExpressionNode : ExpressionNode
    {
        public required string Operator { get; set; }
        public required int OperatorColumnStart { get; set; }
        public required int OperatorColumnEnd { get; set; }
        public required ExpressionNode Right { get; set; }
    }

    public class AssignmentStatementNode : StatementNode
    {
        public int SlotIndex;
        public required string VariableName { get; set; }
        public int VariableStartColumn { get; set; }
        public int VariableEndColumn { get; set; }
        public required string Operator { get; set; }
        public int OperatorLine { get; set; }
        public int OperatorStartColumn { get; set; }
        public int OperatorEndColumn { get; set; }
        public required ExpressionNode Value { get; set; }
    }

    public class IncrementStatementNode : StatementNode
    {
        public int SlotIndex;
        public required string VariableName { get; set; }
        public int VariableStartColumn { get; set; }
        public int VariableEndColumn { get; set; }
        public bool IsIncrement { get; set; }
        public int OperatorLine { get; set; }
        public int OperatorStartColumn { get; set; }
        public int OperatorEndColumn { get; set; }
    }
}
