using Vext.Compiler.Parsing;
using Vext.Compiler.Shared;
using Vext.Compiler.VM;

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
                    ColumnNumber = l.StartColumn
                });
            } else if (expr is VariableNode v)
            {
                instructions.Add(new Instruction
                {
                    Op = VextVMBytecode.LOAD_VAR,
                    Arg = v.SlotIndex,
                    LineNumber = v.Line,
                    ColumnNumber = v.StartColumn
                });
            } else if (expr is BinaryExpressionNode b)
            {
                if (b.Operator == "&&")
                {
                    EmitExpression(b.Left, instructions);
                    int jumpFalse = instructions.Count;
                    instructions.Add(new Instruction { Op = VextVMBytecode.JMP_IF_FALSE, Arg = -1, LineNumber = b.Line, ColumnNumber = b.StartColumn });

                    // Left was true (and popped). Now evaluate Right.
                    EmitExpression(b.Right, instructions);
                    int jumpEnd = instructions.Count;
                    instructions.Add(new Instruction { Op = VextVMBytecode.JMP, Arg = -1, LineNumber = b.Line, ColumnNumber = b.StartColumn });

                    // Target for jumpFalse
                    instructions[jumpFalse].Arg = instructions.Count;
                    instructions.Add(new Instruction { Op = VextVMBytecode.LOAD_CONST, ArgVal = new VextValue { Type = VextType.Bool, AsBool = false }, LineNumber = b.Line, ColumnNumber = b.StartColumn });

                    instructions[jumpEnd].Arg = instructions.Count;
                } else if (b.Operator == "||")
                {
                    EmitExpression(b.Left, instructions);
                    int jumpTrue = instructions.Count;
                    instructions.Add(new Instruction { Op = VextVMBytecode.JMP_IF_TRUE, Arg = -1, LineNumber = b.Line, ColumnNumber = b.StartColumn });

                    // Left was false (and popped). Evaluate Right.
                    EmitExpression(b.Right, instructions);
                    int jumpEnd = instructions.Count;
                    instructions.Add(new Instruction { Op = VextVMBytecode.JMP, Arg = -1, LineNumber = b.Line, ColumnNumber = b.StartColumn });

                    // Target for jumpTrue
                    instructions[jumpTrue].Arg = instructions.Count;
                    instructions.Add(new Instruction { Op = VextVMBytecode.LOAD_CONST, ArgVal = new VextValue { Type = VextType.Bool, AsBool = true }, LineNumber = b.Line, ColumnNumber = b.StartColumn });

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

                    instructions.Add(new Instruction { Op = op, LineNumber = b.Line, ColumnNumber = b.StartColumn });
                }
            } else if (expr is FunctionCallNode f)
            {
                foreach (ExpressionNode arg in f.Arguments)
                {
                    EmitExpression(arg, instructions);
                }

                VextVMBytecode op = f.ReturnType != "void" ? VextVMBytecode.CALL : VextVMBytecode.CALL_VOID;

                instructions.Add(new Instruction
                {
                    Op = op,
                    Arg = (f.FunctionName as object, f.Arguments?.Count ?? 0),
                    LineNumber = f.Line,
                    ColumnNumber = f.StartColumn
                });
            } else if (expr is MemberAccessNode m)
            {
                if (m.IsModuleCall && m.Receiver is VariableNode vMod)
                {
                    // Static module call: Module.Func(...)
                    if (m.Arguments != null)
                        foreach (ExpressionNode arg in m.Arguments)
                            EmitExpression(arg, instructions);

                    string fullName = $"{vMod.Name}.{m.MemberName}";
                    VextVMBytecode op = m.ReturnType == "void" ? VextVMBytecode.CALL_VOID : VextVMBytecode.CALL;
                    instructions.Add(new Instruction
                    {
                        Op = op,
                        Arg = (fullName as object, m.Arguments?.Count ?? 0),
                        LineNumber = m.Line,
                        ColumnNumber = m.StartColumn
                    });
                } else
                {
                    // Instance member (Intrinsic)
                    EmitExpression(m.Receiver, instructions);
                    if (m.Arguments != null)
                        foreach (ExpressionNode arg in m.Arguments)
                            EmitExpression(arg, instructions);

                    string targetName;
                    if (m.MemberName == "type" && m.Arguments == null)
                        targetName = "__v_gettype";
                    else if (m.MemberName == "ToString" && m.Arguments != null && m.Arguments.Count == 0)
                        targetName = "__v_tostring";
                    else
                        throw new Exception($"Unsupported member access: {m.MemberName}");

                    VextVMBytecode op = m.ReturnType == "void" ? VextVMBytecode.CALL_VOID : VextVMBytecode.CALL;
                    instructions.Add(new Instruction
                    {
                        Op = op,
                        Arg = (targetName as object, (m.Arguments?.Count ?? 0) + 1), // +1 for the receiver
                        LineNumber = m.Line,
                        ColumnNumber = m.StartColumn
                    });
                }
            } else if (expr is UnaryExpressionNode u)
            {
                if (u.Operator == "++" || u.Operator == "--")
                {
                    if (u.Right is VariableNode varNode)
                    {
                        instructions.Add(new Instruction
                        {
                            Op = u.Operator == "++" ? VextVMBytecode.INC_VAR : VextVMBytecode.DEC_VAR,
                            Arg = varNode.SlotIndex,
                            LineNumber = u.Line,
                            ColumnNumber = u.StartColumn
                        });
                        return;
                    } else
                    {
                        throw new Exception("Postfix ++/-- can only be applied to variables");
                    }
                }

                EmitExpression(u.Right, instructions);

                if (u.Operator == "-")
                {
                    instructions.Add(new Instruction { Op = VextVMBytecode.LOAD_CONST, ArgVal = new VextValue { Type = VextType.Number, AsNumber = -1 }, LineNumber = u.Line, ColumnNumber = u.StartColumn });
                    instructions.Add(new Instruction { Op = VextVMBytecode.MUL, LineNumber = u.Line, ColumnNumber = u.StartColumn });
                } else if (u.Operator == "!")
                {
                    instructions.Add(new Instruction { Op = VextVMBytecode.NOT, LineNumber = u.Line, ColumnNumber = u.StartColumn });
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
                int jumpIndex = instructions.Count;
                instructions.Add(new Instruction { Op = VextVMBytecode.JMP_IF_FALSE, Arg = -1, LineNumber = ifStmt.Line, ColumnNumber = ifStmt.StartColumn });

                foreach (StatementNode s in ifStmt.Body)
                    EmitStatement(s, instructions);

                if (ifStmt.ElseBody != null)
                {
                    int jumpEnd = instructions.Count;
                    instructions.Add(new Instruction { Op = VextVMBytecode.JMP, Arg = -1, LineNumber = ifStmt.Line, ColumnNumber = ifStmt.StartColumn });
                    instructions[jumpIndex].Arg = instructions.Count;
                    foreach (StatementNode s in ifStmt.ElseBody)
                        EmitStatement(s, instructions);
                    instructions[jumpEnd].Arg = instructions.Count;
                } else
                {
                    instructions[jumpIndex].Arg = instructions.Count;
                }
            } else if (stmt is WhileStatementNode whileStmt)
            {
                int loopStart = instructions.Count;

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
                        ColumnNumber = whileStmt.StartColumn
                    });

                    int jumpIndexW = instructions.Count - 1;

                    foreach (StatementNode s in whileStmt.Body)
                        EmitStatement(s, instructions);

                    // Loop back
                    instructions.Add(new Instruction { Op = VextVMBytecode.JMP, Arg = loopStart, LineNumber = whileStmt.Line, ColumnNumber = whileStmt.StartColumn });

                    // Patch the jump target
                    instructions[jumpIndexW].Arg = (varNode.SlotIndex, cond.Operator, limitValue, instructions.Count);
                    return;
                }

                EmitExpression(whileStmt.Condition, instructions);
                int jumpIndex = instructions.Count;
                instructions.Add(new Instruction { Op = VextVMBytecode.JMP_IF_FALSE, Arg = -1, LineNumber = whileStmt.Line, ColumnNumber = whileStmt.StartColumn });

                foreach (StatementNode s in whileStmt.Body)
                    EmitStatement(s, instructions);

                instructions.Add(new Instruction { Op = VextVMBytecode.JMP, Arg = loopStart, LineNumber = whileStmt.Line, ColumnNumber = whileStmt.StartColumn });
                instructions[jumpIndex].Arg = instructions.Count;
            } else if (stmt is ForStatementNode forStmt)
            {
                forStmt.Initialization ??= new VariableDeclarationNode
                {
                    Name = "i",
                    VariableType = "int",
                    DeclaredType = "int",
                    Initializer = new LiteralNode { Value = 0, Line = forStmt.Line, StartColumn = forStmt.StartColumn },
                    Line = forStmt.Line,
                    StartColumn = forStmt.StartColumn
                };
                EmitStatement(forStmt.Initialization, instructions);

                int loopStart = instructions.Count;

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
                        ColumnNumber = forStmt.StartColumn
                    });

                    int jumpIndexW = instructions.Count - 1;

                    foreach (StatementNode s in forStmt.Body)
                        EmitStatement(s, instructions);

                    // Increment
                    forStmt.Increment ??= new IncrementStatementNode
                    {
                        VariableName = "i",
                        IsIncrement = true,
                        Line = forStmt.Line,
                        StartColumn = forStmt.StartColumn
                    };
                    EmitStatement(forStmt.Increment, instructions);

                    // Loop back
                    instructions.Add(new Instruction { Op = VextVMBytecode.JMP, Arg = loopStart, LineNumber = forStmt.Line, ColumnNumber = forStmt.StartColumn });

                    // Patch the jump target
                    instructions[jumpIndexW].Arg = (varNode.SlotIndex, cond.Operator, limitValue, instructions.Count);
                    return;
                }

                forStmt.Condition ??= new BinaryExpressionNode { Left = new VariableNode { Name = "i" }, Operator = "<", Right = new LiteralNode { Value = 10 }, Line = forStmt.Line, StartColumn = forStmt.StartColumn };

                EmitExpression(forStmt.Condition, instructions);
                int jumpIndex = instructions.Count;
                instructions.Add(new Instruction { Op = VextVMBytecode.JMP_IF_FALSE, Arg = -1, LineNumber = forStmt.Line, ColumnNumber = forStmt.StartColumn });

                foreach (StatementNode s in forStmt.Body)
                    EmitStatement(s, instructions);

                forStmt.Increment ??= new IncrementStatementNode
                {
                    VariableName = "i",
                    IsIncrement = true,
                    Line = forStmt.Line,
                    StartColumn = forStmt.StartColumn
                };
                EmitStatement(forStmt.Increment, instructions);

                instructions.Add(new Instruction { Op = VextVMBytecode.JMP, Arg = loopStart, LineNumber = forStmt.Line, ColumnNumber = forStmt.StartColumn });
                instructions[jumpIndex].Arg = instructions.Count;
            } else if (stmt is ExpressionStatementNode exprStmt)
            {
                EmitExpression(exprStmt.Expression, instructions);

                if (ExpressionNeedsPop(exprStmt.Expression, exprStmt))
                    instructions.Add(new Instruction { Op = VextVMBytecode.POP, LineNumber = exprStmt.Line, ColumnNumber = exprStmt.StartColumn });
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
                        ColumnNumber = varDecl.StartColumn
                    });
                }

                instructions.Add(new Instruction
                {
                    Op = VextVMBytecode.STORE_VAR,
                    Arg = varDecl.SlotIndex,
                    LineNumber = varDecl.Line,
                    ColumnNumber = varDecl.StartColumn
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
                        ColumnNumber = assign.StartColumn
                    });

                    EmitExpression(assign.Value, instructions);

                    instructions.Add(assign.Operator switch
                    {
                        "+=" => new Instruction { Op = VextVMBytecode.ADD, LineNumber = assign.Line, ColumnNumber = assign.StartColumn },
                        "-=" => new Instruction { Op = VextVMBytecode.SUB, LineNumber = assign.Line, ColumnNumber = assign.StartColumn },
                        "*=" => new Instruction { Op = VextVMBytecode.MUL, LineNumber = assign.Line, ColumnNumber = assign.StartColumn },
                        _ => throw new Exception($"Unsupported operator {assign.Operator}")
                    });
                }

                instructions.Add(new Instruction
                {
                    Op = VextVMBytecode.STORE_VAR,
                    Arg = assign.SlotIndex,
                    LineNumber = assign.Line,
                    ColumnNumber = assign.StartColumn
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
                        ColumnNumber = incrmtStmt.StartColumn
                    });
                } else
                {
                    instructions.Add(new Instruction
                    {
                        Op = VextVMBytecode.DEC_VAR,
                        Arg = incrmtStmt.SlotIndex,
                        LineNumber = incrmtStmt.Line,
                        ColumnNumber = incrmtStmt.StartColumn
                    });
                }
            } else if (stmt is ReturnStatementNode rtrnStmt)
            {
                if (rtrnStmt.Expression != null)
                    EmitExpression(rtrnStmt.Expression, instructions);

                instructions.Add(new Instruction { Op = VextVMBytecode.RET, LineNumber = rtrnStmt.Line, ColumnNumber = rtrnStmt.StartColumn });
                return;
            } else if (stmt is FunctionDefinitionNode func)
            {
                // 1. Create a new instruction list for the function body
                List<Instruction> funcInstructions = [];

                for (int i = func.Arguments.Count - 1; i >= 0; i--)
                {
                    FunctionParameterNode arg = func.Arguments[i];
                    funcInstructions.Add(new Instruction
                    {
                        Op = VextVMBytecode.STORE_VAR,
                        Arg = arg.SlotIndex,
                        LineNumber = func.Line,
                        ColumnNumber = func.StartColumn
                    });
                }

                foreach (StatementNode s in func.Body)
                    EmitStatement(s, funcInstructions);

                if (funcInstructions.Count == 0 || funcInstructions[^1].Op != VextVMBytecode.RET)
                {
                    funcInstructions.Add(new Instruction
                    {
                        Op = VextVMBytecode.LOAD_CONST,
                        ArgVal = VextValue.Null(),
                        LineNumber = func.Line,
                        ColumnNumber = func.StartColumn
                    });
                    funcInstructions.Add(new Instruction { Op = VextVMBytecode.RET, LineNumber = func.Line, ColumnNumber = func.StartColumn });
                }

                // 2. Create a UserFunction object
                UserFunction userFunc = new UserFunction
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
                    ColumnNumber = func.StartColumn
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
                        foreach (StatementNode s in i.Body)
                            Walk(s);
                        if (i.ElseBody != null)
                            foreach (StatementNode s in i.ElseBody)
                                Walk(s);
                        break;

                    case WhileStatementNode w:
                        foreach (StatementNode s in w.Body)
                            Walk(s);
                        break;
                    case ForStatementNode fo:
                        foreach (StatementNode s in fo.Body)
                            Walk(s);
                        break;
                }
            }

            foreach (StatementNode stmt in body)
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
                MemberAccessNode m => m.ReturnType != "void",
                UnaryExpressionNode u when u.Operator == "++" || u.Operator == "--" => false,
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
