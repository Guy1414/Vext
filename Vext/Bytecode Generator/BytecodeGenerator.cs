using Vext.Compiler.Parsing;
using Vext.Compiler.VM;
using Vext.Shared;

namespace Vext.Compiler.Bytecode_Generator
{
    internal class BytecodeGenerator
    {
        public static void EmitExpression(ExpressionNode expr, List<Instruction> instructions)
        {
            if (expr is LiteralNode l)
            {
                VextValue vl = l.Value switch
                {
                    int i => VextValue.FromNumber(i),
                    double d => VextValue.FromNumber(d),
                    bool b => VextValue.FromBool(b),
                    string s => VextValue.FromString(s),
                    null => VextValue.Null(),
                    _ => throw new Exception($"Unsupported literal type {l.Value.GetType()}")
                };
                instructions.Add(new Instruction
                {
                    Op = VextVMBytecode.LOAD_CONST,
                    ArgVal = vl,
                    LineNumber = l.Line,
                    ColumnNumber = l.Column
                });
            } else if (expr is VariableNode v)
            {
                instructions.Add(new Instruction
                {
                    Op = VextVMBytecode.LOAD_VAR,
                    Arg = v.SlotIndex,
                    LineNumber = v.Line,
                    ColumnNumber = v.Column
                });
            } else if (expr is BinaryExpressionNode b)
            {
                if (b.Operator == "&&")
                {
                    EmitExpression(b.Left, instructions);
                    var jumpFalse = instructions.Count;
                    instructions.Add(new Instruction { Op = VextVMBytecode.JMP_IF_FALSE, Arg = -1, LineNumber = b.Line, ColumnNumber = b.Column });

                    // Left was true (and popped). Now evaluate Right.
                    EmitExpression(b.Right, instructions);
                    var jumpEnd = instructions.Count;
                    instructions.Add(new Instruction { Op = VextVMBytecode.JMP, Arg = -1, LineNumber = b.Line, ColumnNumber = b.Column });

                    // Target for jumpFalse
                    instructions[jumpFalse].Arg = instructions.Count;
                    instructions.Add(new Instruction { Op = VextVMBytecode.LOAD_CONST, ArgVal = new VextValue { Type = VextType.Bool, AsBool = false }, LineNumber = b.Line, ColumnNumber = b.Column });

                    instructions[jumpEnd].Arg = instructions.Count;
                } else if (b.Operator == "||")
                {
                    EmitExpression(b.Left, instructions);
                    var jumpTrue = instructions.Count;
                    instructions.Add(new Instruction { Op = VextVMBytecode.JMP_IF_TRUE, Arg = -1, LineNumber = b.Line, ColumnNumber = b.Column });

                    // Left was false (and popped). Evaluate Right.
                    EmitExpression(b.Right, instructions);
                    var jumpEnd = instructions.Count;
                    instructions.Add(new Instruction { Op = VextVMBytecode.JMP, Arg = -1, LineNumber = b.Line, ColumnNumber = b.Column });

                    // Target for jumpTrue
                    instructions[jumpTrue].Arg = instructions.Count;
                    instructions.Add(new Instruction { Op = VextVMBytecode.LOAD_CONST, ArgVal = new VextValue { Type = VextType.Bool, AsBool = true }, LineNumber = b.Line, ColumnNumber = b.Column });

                    instructions[jumpEnd].Arg = instructions.Count;
                } else
                {
                    // All other binary ops
                    EmitExpression(b.Left, instructions);
                    EmitExpression(b.Right, instructions);
                    VextVMBytecode op = b.Operator switch
                    {
                        "+" => VextVMBytecode.ADD,
                        "-" => VextVMBytecode.SUB,
                        "*" => VextVMBytecode.MUL,
                        "/" => VextVMBytecode.DIV,
                        "**" => VextVMBytecode.POW,
                        "%" => VextVMBytecode.MOD,
                        "==" => VextVMBytecode.EQ,
                        "!=" => VextVMBytecode.NEQ,
                        "<" => VextVMBytecode.LT,
                        ">" => VextVMBytecode.GT,
                        "<=" => VextVMBytecode.LTE,
                        ">=" => VextVMBytecode.GTE,
                        _ => throw new Exception($"Unknown operator {b.Operator}")
                    };

                    instructions.Add(new Instruction { Op = op, LineNumber = b.Line, ColumnNumber = b.Column });
                }
            } else if (expr is FunctionCallNode f)
            {
                foreach (var arg in f.Arguments)
                {
                    EmitExpression(arg, instructions);
                }

                string targetName = f is ModuleAccessNode modCall
                    ? $"{modCall.ModuleName}.{modCall.FunctionName}"
                    : f.FunctionName;

                if (f.ReturnType == "void")
                {
                    instructions.Add(new Instruction
                    {
                        Op = VextVMBytecode.CALL_VOID,
                        Arg = (targetName as object, f.Arguments.Count),
                        LineNumber = f.Line,
                        ColumnNumber = f.Column
                    });
                } else
                {
                    instructions.Add(new Instruction
                    {
                        Op = VextVMBytecode.CALL,
                        Arg = (targetName as object, f.Arguments.Count),
                        LineNumber = f.Line,
                        ColumnNumber = f.Column
                    });
                }
            } else if (expr is UnaryExpressionNode u)
            {
                EmitExpression(u.Right, instructions);

                if (u.Operator == "-")
                {
                    instructions.Add(new Instruction { Op = VextVMBytecode.LOAD_CONST, ArgVal = new VextValue { Type = VextType.Number, AsNumber = -1 }, LineNumber = u.Line, ColumnNumber = u.Column });
                    instructions.Add(new Instruction { Op = VextVMBytecode.MUL, LineNumber = u.Line, ColumnNumber = u.Column });
                } else if (u.Operator == "!")
                {
                    instructions.Add(new Instruction { Op = VextVMBytecode.NOT, LineNumber = u.Line, ColumnNumber = u.Column });
                } else
                {
                    throw new Exception($"Unknown unary operator {u.Operator}");
                }
            }
        }

        public static void EmitStatement(StatementNode stmt, List<Instruction> instructions)
        {
            if (stmt is IfStatementNode ifStmt)
            {
                EmitExpression(ifStmt.Condition, instructions);
                var jumpIndex = instructions.Count;
                instructions.Add(new Instruction { Op = VextVMBytecode.JMP_IF_FALSE, Arg = -1, LineNumber = ifStmt.Line, ColumnNumber = ifStmt.Column });

                foreach (var s in ifStmt.Body)
                    EmitStatement(s, instructions);

                if (ifStmt.ElseBody != null)
                {
                    var jumpEnd = instructions.Count;
                    instructions.Add(new Instruction { Op = VextVMBytecode.JMP, Arg = -1, LineNumber = ifStmt.Line, ColumnNumber = ifStmt.Column });
                    instructions[jumpIndex].Arg = instructions.Count;
                    foreach (var s in ifStmt.ElseBody)
                        EmitStatement(s, instructions);
                    instructions[jumpEnd].Arg = instructions.Count;
                } else
                {
                    instructions[jumpIndex].Arg = instructions.Count;
                }
            } else if (stmt is WhileStatementNode whileStmt)
            {
                var loopStart = instructions.Count;

                // Specialized optimization for simple "while i < constant" loops
                if (whileStmt.Condition is BinaryExpressionNode cond &&
                    cond.Left is VariableNode varNode &&
                    cond.Right is LiteralNode litNode &&
                    (cond.Operator == "<" || cond.Operator == "<=" || cond.Operator == ">" || cond.Operator == ">="))
                {
                    // Ensure the limit is a double to match the VM's expected cast
                    double limitValue = Convert.ToDouble(litNode.Value);

                    instructions.Add(new Instruction
                    {
                        Op = VextVMBytecode.JMP_IF_VAR_OP_CONST,
                        Arg = (varNode.SlotIndex, cond.Operator, limitValue, -1),
                        LineNumber = whileStmt.Line,
                        ColumnNumber = whileStmt.Column
                    });

                    int jumpIndexW = instructions.Count - 1;

                    foreach (var s in whileStmt.Body)
                        EmitStatement(s, instructions);

                    // Loop back
                    instructions.Add(new Instruction { Op = VextVMBytecode.JMP, Arg = loopStart, LineNumber = whileStmt.Line, ColumnNumber = whileStmt.Column });

                    // Patch the jump target
                    instructions[jumpIndexW].Arg = (varNode.SlotIndex, cond.Operator, limitValue, instructions.Count);
                    return;
                }

                EmitExpression(whileStmt.Condition, instructions);
                var jumpIndex = instructions.Count;
                instructions.Add(new Instruction { Op = VextVMBytecode.JMP_IF_FALSE, Arg = -1, LineNumber = whileStmt.Line, ColumnNumber = whileStmt.Column });

                foreach (var s in whileStmt.Body)
                    EmitStatement(s, instructions);

                instructions.Add(new Instruction { Op = VextVMBytecode.JMP, Arg = loopStart, LineNumber = whileStmt.Line, ColumnNumber = whileStmt.Column });
                instructions[jumpIndex].Arg = instructions.Count;
            } else if (stmt is ForStatementNode forStmt)
            {
                EmitStatement(forStmt.Initialization, instructions);

                var loopStart = instructions.Count;

                if (forStmt.Condition is BinaryExpressionNode cond &&
                    cond.Left is VariableNode varNode &&
                    cond.Right is LiteralNode litNode &&
                    (cond.Operator == "<" || cond.Operator == "<=" || cond.Operator == ">" || cond.Operator == ">="))
                {
                    // Ensure the limit is a double to match the VM's expected cast
                    double limitValue = Convert.ToDouble(litNode.Value);

                    instructions.Add(new Instruction
                    {
                        Op = VextVMBytecode.JMP_IF_VAR_OP_CONST,
                        Arg = (varNode.SlotIndex, cond.Operator, limitValue, -1),
                        LineNumber = forStmt.Line,
                        ColumnNumber = forStmt.Column
                    });

                    int jumpIndexW = instructions.Count - 1;

                    foreach (var s in forStmt.Body)
                        EmitStatement(s, instructions);

                    // Increment
                    EmitStatement(forStmt.Increment, instructions);

                    // Loop back
                    instructions.Add(new Instruction { Op = VextVMBytecode.JMP, Arg = loopStart, LineNumber = forStmt.Line, ColumnNumber = forStmt.Column });

                    // Patch the jump target
                    instructions[jumpIndexW].Arg = (varNode.SlotIndex, cond.Operator, limitValue, instructions.Count);
                    return;
                }

                EmitExpression(forStmt.Condition, instructions);
                var jumpIndex = instructions.Count;
                instructions.Add(new Instruction { Op = VextVMBytecode.JMP_IF_FALSE, Arg = -1, LineNumber = forStmt.Line, ColumnNumber = forStmt.Column });

                foreach (var s in forStmt.Body)
                    EmitStatement(s, instructions);

                EmitStatement(forStmt.Increment, instructions);

                instructions.Add(new Instruction { Op = VextVMBytecode.JMP, Arg = loopStart, LineNumber = forStmt.Line, ColumnNumber = forStmt.Column });
                instructions[jumpIndex].Arg = instructions.Count;
            } else if (stmt is ExpressionStatementNode exprStmt)
            {
                EmitExpression(exprStmt.Expression, instructions);

                if (ExpressionNeedsPop(exprStmt.Expression, exprStmt))
                    instructions.Add(new Instruction { Op = VextVMBytecode.POP, LineNumber = exprStmt.Line, ColumnNumber = exprStmt.Column });
            } else if (stmt is VariableDeclarationNode varDecl)
            {
                if (varDecl.Initializer != null)
                {
                    EmitExpression(varDecl.Initializer, instructions);
                } else
                {
                    instructions.Add(new Instruction
                    {
                        Op = VextVMBytecode.LOAD_CONST,
                        ArgVal = VextValue.Null(),
                        LineNumber = varDecl.Line,
                        ColumnNumber = varDecl.Column
                    });
                }

                instructions.Add(new Instruction
                {
                    Op = VextVMBytecode.STORE_VAR,
                    Arg = varDecl.SlotIndex,
                    LineNumber = varDecl.Line,
                    ColumnNumber = varDecl.Column
                });
            } else if (stmt is AssignmentStatementNode assign)
            {
                if (assign.Operator == "=")
                {
                    EmitExpression(assign.Value, instructions);
                } else
                {
                    // x += y  →  x = x + y
                    instructions.Add(new Instruction
                    {
                        Op = VextVMBytecode.LOAD_VAR,
                        Arg = assign.SlotIndex,
                        LineNumber = assign.Line,
                        ColumnNumber = assign.Column
                    });

                    EmitExpression(assign.Value, instructions);

                    instructions.Add(assign.Operator switch
                    {
                        "+=" => new Instruction { Op = VextVMBytecode.ADD, LineNumber = assign.Line, ColumnNumber = assign.Column },
                        "-=" => new Instruction { Op = VextVMBytecode.SUB, LineNumber = assign.Line, ColumnNumber = assign.Column },
                        "*=" => new Instruction { Op = VextVMBytecode.MUL, LineNumber = assign.Line, ColumnNumber = assign.Column },
                        _ => throw new Exception($"Unsupported operator {assign.Operator}")
                    });
                }

                instructions.Add(new Instruction
                {
                    Op = VextVMBytecode.STORE_VAR,
                    Arg = assign.SlotIndex,
                    LineNumber = assign.Line,
                    ColumnNumber = assign.Column
                });
            } else if (stmt is IncrementStatementNode incrmtStmt)
            {
                if (incrmtStmt.IsIncrement)
                {
                    instructions.Add(new Instruction
                    {
                        Op = VextVMBytecode.INC_VAR,
                        Arg = incrmtStmt.SlotIndex,
                        LineNumber = incrmtStmt.Line,
                        ColumnNumber = incrmtStmt.Column
                    });
                } else
                {
                    instructions.Add(new Instruction
                    {
                        Op = VextVMBytecode.DEC_VAR,
                        Arg = incrmtStmt.SlotIndex,
                        LineNumber = incrmtStmt.Line,
                        ColumnNumber = incrmtStmt.Column
                    });
                }
            } else if (stmt is ReturnStatementNode rtrnStmt)
            {
                if (rtrnStmt.Expression != null)
                    EmitExpression(rtrnStmt.Expression, instructions);

                instructions.Add(new Instruction { Op = VextVMBytecode.RET, LineNumber = rtrnStmt.Line, ColumnNumber = rtrnStmt.Column });
                return;
            } else if (stmt is FunctionDefinitionNode func)
            {
                // 1. Create a new instruction list for the function body
                var funcInstructions = new List<Instruction>();

                for (int i = func.Arguments.Count - 1; i >= 0; i--)
                {
                    var arg = func.Arguments[i];
                    funcInstructions.Add(new Instruction
                    {
                        Op = VextVMBytecode.STORE_VAR,
                        Arg = arg.SlotIndex,
                        LineNumber = func.Line,
                        ColumnNumber = func.Column
                    });
                }

                foreach (var s in func.Body)
                    EmitStatement(s, funcInstructions);

                if (funcInstructions.Count == 0 || funcInstructions[^1].Op != VextVMBytecode.RET)
                {
                    funcInstructions.Add(new Instruction
                    {
                        Op = VextVMBytecode.LOAD_CONST,
                        ArgVal = VextValue.Null(),
                        LineNumber = func.Line,
                        ColumnNumber = func.Column
                    });
                    funcInstructions.Add(new Instruction { Op = VextVMBytecode.RET, LineNumber = func.Line, ColumnNumber = func.Column });
                }

                // 2. Create a UserFunction object
                var userFunc = new UserFunction
                {
                    Name = func.FunctionName,
                    Arguments = func.Arguments,
                    Body = funcInstructions,
                    LocalCount = func.Arguments.Count + CountLocals(func.Body)
                };

                // 3. Register the function in VM functions dictionary
                instructions.Add(new Instruction
                {
                    Op = VextVMBytecode.DEF_FUNC,
                    Arg = userFunc,
                    LineNumber = func.Line,
                    ColumnNumber = func.Column
                });
            }
        }

        private static int CountLocals(List<StatementNode> body)
        {
            int maxSlot = -1;

            void Walk(StatementNode stmt)
            {
                switch (stmt)
                {
                    case VariableDeclarationNode v:
                        maxSlot = Math.Max(maxSlot, v.SlotIndex);
                        break;

                    case IfStatementNode i:
                        foreach (var s in i.Body)
                            Walk(s);
                        if (i.ElseBody != null)
                            foreach (var s in i.ElseBody)
                                Walk(s);
                        break;

                    case WhileStatementNode w:
                        foreach (var s in w.Body)
                            Walk(s);
                        break;
                    case ForStatementNode fo:
                        foreach (var s in fo.Body)
                            Walk(s);
                        break;
                }
            }

            foreach (var stmt in body)
                Walk(stmt);

            return maxSlot + 1;
        }

        private static bool ExpressionNeedsPop(ExpressionNode expr, StatementNode parent)
        {
            if (parent is not ExpressionStatementNode)
                return false;

            return expr switch
            {
                FunctionCallNode call => call.ReturnType != "void",
                _ => true
            };
        }
    }
}

internal class UserFunction
{
    public required string Name;
    public List<FunctionParameterNode>? Arguments = [];
    public List<Instruction> Body = [];
    public required int LocalCount;
    public List<int> ArgumentSlots { get; set; } = [];
}
