using System.Collections;
using Vext.Compiler.Diagnostics;
using Vext.Compiler.Parsing;
using Vext.Compiler.Shared;
using Vext.Modules;

namespace Vext.Compiler.Semantic
{
    internal class SemanticPass(List<StatementNode> statements)
    {
        private readonly List<StatementNode> statements = statements;

        private readonly Dictionary<string, List<FunctionDefinitionNode>> builtInFunctions = [];
        private readonly List<FunctionDefinitionNode> functions = [];
        private readonly Dictionary<string, List<FunctionDefinitionNode>> functionLookup = [];

        private Scope? currentScope;
        private readonly Stack<BitArray> assignedSlots = new();
        private readonly Dictionary<int, VariableDeclarationNode> visibleVariables = [];
        private readonly Dictionary<int, string> slotToNameMap = [];
        private int variableSlotIndex = 0;

        public List<FunctionDefinitionNode> GetDiscoveredFunctions() => functions;
        public Dictionary<int, string> GetVariableMap() => slotToNameMap;

        public List<SemanticToken> SemanticTokens { get; } = [];

        private void AddToken(int line, int startCol, int endCol, string type, params string[] modifiers)
        {
            if (line == 0)
                return;

            SemanticTokens.Add(new SemanticToken
            {
                Line = line,
                StartColumn = startCol,
                EndColumn = endCol,
                Type = type,
                Modifiers = [.. modifiers]
            });
        }

        private static void ReportError(string message, int startLine, int startCol, int endCol) => Diagnostic.ReportError(message, startLine, startCol, startLine, endCol);
        private static void ReportWarning(string message, int startLine, int startCol, int endCol) => Diagnostic.ReportWarning(message, startLine, startCol, startLine, endCol);
        private static void ReportInfo(string message, int startLine, int startCol, int endCol) => Diagnostic.ReportInfo(message, startLine, startCol, startLine, endCol);
        private static void ReportHint(string message, int startLine, int startCol, int endCol) => Diagnostic.ReportHint(message, startLine, startCol, startLine, endCol);
        public void Pass()
        {
            functions.Clear();
            functionLookup.Clear();
            PushScope(); // Global

            // 1. Function discovery
            CollectFunctions();

            // 2. Sequential top-level analysis
            foreach (StatementNode stmt in statements)
            {
                if (stmt is FunctionDefinitionNode)
                    continue;

                AnalyzeStatement(stmt, null);
            }

            // 3. Analyze function bodies
            AnalyzeFunctionBodies();

            PopScope(); // Global
        }

        /// <summary>
        /// Registers a collection of built-in functions for use within the current context.
        /// </summary>
        /// <remarks>If a function with the same name already exists among the built-ins, the new function
        /// is added alongside existing definitions. This allows for function overloading by name.</remarks>
        /// <param name="builtIns">The collection of built-in functions to register. Each function in the collection must have a unique name
        /// within the set of built-ins.</param>
        public void RegisterBuiltInFunctions(IEnumerable<Function> builtIns)
        {
            foreach (Function f in builtIns)
            {
                if (!builtInFunctions.TryGetValue(f.Name, out List<FunctionDefinitionNode>? list))
                {
                    list = [];
                    builtInFunctions[f.Name] = list;
                }
                int slot = 0;
                list.Add(new FunctionDefinitionNode
                {
                    FunctionName = f.Name,
                    Arguments = f.Parameters?.Select(p => new FunctionParameterNode
                    {
                        Name = p.Name,
                        Type = p.Type,
                        SlotIndex = slot++,
                    }).ToList() ?? [],
                    ReturnType = f.ReturnType ?? "void",
                    Body = []
                });
            }
        }

        /// <summary>
        /// Analyzes the collection of statements to identify and register all function definitions, validating their
        /// signatures and reporting any errors found.
        /// </summary>
        /// <remarks>This method checks for duplicate function definitions, unknown return or parameter
        /// types, and duplicate parameter names within each function. Any issues encountered during validation are
        /// reported as errors. Only valid function definitions are added to the internal function registry.</remarks>
        private void CollectFunctions()
        {
            foreach (FunctionDefinitionNode stmt in statements.OfType<FunctionDefinitionNode>())
            {
                if (functions.Any(f => f.FunctionName == stmt.FunctionName && ParametersMatch(f.Arguments, stmt.Arguments)))
                    ReportError($"Function '{stmt.FunctionName}' with these parameters is already defined.", stmt.Line, stmt.StartColumn, stmt.EndColumn);

                if (!IsValidType(stmt.ReturnType))
                    ReportError($"Unknown return type '{stmt.ReturnType}'.", stmt.Line, stmt.StartColumn, stmt.EndColumn);

                HashSet<string> paramNames = [];
                foreach (FunctionParameterNode p in stmt.Arguments ?? [])
                {
                    if (!paramNames.Add(p.Name))
                        ReportError($"Duplicate parameter name '{p.Name}' in function '{stmt.FunctionName}'.", stmt.Line, stmt.StartColumn, stmt.EndColumn);
                    if (!IsValidType(p.Type))
                        ReportError($"Unknown parameter type '{p.Type}'.", stmt.Line, stmt.StartColumn, stmt.EndColumn);
                }

                functions.Add(stmt);

                if (!functionLookup.TryGetValue(stmt.FunctionName, out List<FunctionDefinitionNode>? list))
                    functionLookup[stmt.FunctionName] = list = [];

                list.Add(stmt);

                // Token for Return Type
                AddToken(stmt.Line, stmt.ReturnTypeStartColumn, stmt.ReturnTypeEndColumn, "type");

                // Token for Function Name
                AddToken(stmt.NameLine, stmt.NameStartColumn, stmt.NameEndColumn, "function", "declaration");

                if (stmt.Arguments != null)
                    foreach (FunctionParameterNode p in stmt.Arguments)
                    {
                        AddToken(p.Line, p.TypeStartColumn, p.TypeEndColumn, "type");
                        AddToken(p.NameLine, p.NameStartColumn, p.NameEndColumn, "variable", "parameter", "declaration");
                    }
            }
        }

        private void AnalyzeFunctionBodies()
        {
            foreach (FunctionDefinitionNode? func in functions)
            {
                PushScope();

                foreach (FunctionParameterNode param in func.Arguments)
                {
                    VariableDeclarationNode decl = new VariableDeclarationNode
                    {
                        Name = param.Name,
                        VariableType = param.Type,
                        TypeStartColumn = param.StartColumn,
                        TypeEndColumn = param.EndColumn,
                        DeclaredType = param.Type,
                        Line = func.Line,
                        StartColumn = func.StartColumn,
                        EndColumn = func.EndColumn,
                        SlotIndex = variableSlotIndex++
                    };

                    param.SlotIndex = decl.SlotIndex;
                    currentScope!.Variables[param.Name] = decl;
                    slotToNameMap[decl.SlotIndex] = param.Name;

                    assignedSlots.Peek().Set(decl.SlotIndex, true);
                }

                RebuildVisibleVariables();

                foreach (StatementNode? stmt in func.Body)
                    AnalyzeStatement(stmt, func);

                PopScope();

                if (func.ReturnType != "void" && !CheckReturnPath(func.Body, func.ReturnType))
                    ReportError($"Function '{func.FunctionName}' does not return a value on all code paths.", func.Line, func.StartColumn, func.EndColumn);
            }
        }

        private void AnalyzeStatementBlock(List<StatementNode> body, FunctionDefinitionNode? func, bool isNested = false)
        {
            if (isNested)
                PushScope();

            bool terminated = false;

            foreach (StatementNode? stmt in body)
            {
                if (terminated)
                {
                    ReportWarning(
                        "Unreachable code detected.",
                        stmt.Line,
                        stmt.StartColumn,
                        stmt.EndColumn
                    );
                    continue;
                }

                AnalyzeStatement(stmt, func);

                if (func != null && AlwaysExits(stmt, func.ReturnType))
                    terminated = true;
            }

            if (isNested)
                PopScope();
        }

        /// <summary>
        /// Analyzes a single statement node for semantic correctness within the context of the specified function.
        /// </summary>
        /// <remarks>This method performs semantic checks such as type compatibility, variable declaration
        /// and assignment validation, and ensures that control flow constructs are analyzed correctly. Errors
        /// encountered during analysis are reported through the error reporting mechanism. The analysis may update the
        /// state of variable assignments and perform constant folding on expressions where applicable.</remarks>
        /// <param name="stmt">The statement node to analyze. Must not be null.</param>
        /// <param name="func">The function definition node that provides the context for analysis, or null if the statement is not within
        /// a function.</param>
        private void AnalyzeStatement(StatementNode stmt, FunctionDefinitionNode? func)
        {
            switch (stmt)
            {
                case VariableDeclarationNode v:
                    DeclareVariable(v);

                    if (v.Initializer != null)
                    {
                        CheckExpression(v.Initializer);
                        string initType = GetExpressionType(v.Initializer);
                        if (v.VariableType == "auto")
                        {
                            if (initType == "error")
                                ReportError("Cannot infer type from invalid initializer.", v.Line, v.StartColumn, v.EndColumn);
                            else
                                v.VariableType = initType;
                        }

                        if (!AreTypesCompatible(v.VariableType, initType))
                            ReportError($"Type mismatch...", v.Line, v.StartColumn, v.EndColumn);

                        v.Initializer = Fold(v.Initializer);
                    }

                    assignedSlots.Peek().Set(v.SlotIndex, true);

                    if (!IsValidType(v.VariableType))
                        ReportError($"Unknown type {v.VariableType}", v.Line, v.StartColumn, v.EndColumn);

                    AddToken(v.Line, v.TypeStartColumn, v.TypeEndColumn, "type");
                    AddToken(v.NameLine, v.NameStartColumn, v.NameEndColumn, "variable", "declaration");
                    break;

                case AssignmentStatementNode a:
                    {
                        VariableDeclarationNode? decl = ResolveVariable(a.VariableName);

                        if (decl == null)
                        {
                            ReportError($"Variable '{a.VariableName}' used before declaration.", a.Line, a.StartColumn, a.EndColumn);
                        } else
                        {
                            a.SlotIndex = decl.SlotIndex;

                            CheckExpression(a.Value);

                            assignedSlots.Peek().Set(decl.SlotIndex, true);

                            string rhsType = GetExpressionType(a.Value);
                            if (!AreTypesCompatible(decl.VariableType, rhsType))
                                ReportError($"Cannot assign '{rhsType}' to '{decl.VariableType}'.", a.Line, a.StartColumn, a.EndColumn);

                            // Variable token
                            AddToken(a.Line, a.VariableStartColumn, a.VariableEndColumn, "variable");

                            // Operator token
                            if (a.OperatorLine > 0)
                                AddToken(a.OperatorLine, a.OperatorStartColumn, a.OperatorEndColumn, "operator");
                        }
                        break;
                    }

                case IncrementStatementNode inc:
                    {
                        VariableDeclarationNode? decl = ResolveVariable(inc.VariableName);

                        if (decl == null)
                        {
                            ReportError($"Variable '{inc.VariableName}' used before declaration.", inc.Line, inc.StartColumn, inc.EndColumn);
                        } else
                        {
                            inc.SlotIndex = decl.SlotIndex;
                            assignedSlots.Peek().Set(decl.SlotIndex, true);
                            if (decl.VariableType != "int" && decl.VariableType != "float")
                                ReportError($"Cannot apply increment operator to type '{decl.VariableType}'.", inc.Line, inc.StartColumn, inc.EndColumn);

                            // Add token for increment variable
                            AddToken(inc.Line, inc.VariableStartColumn, inc.VariableEndColumn, "variable");

                            // Operator token
                            if (inc.OperatorLine > 0)
                                AddToken(inc.OperatorLine, inc.OperatorStartColumn, inc.OperatorEndColumn, "operator");
                        }
                        break;
                    }

                case ExpressionStatementNode e:
                    CheckExpression(e.Expression);
                    e.Expression = Fold(e.Expression);
                    break;

                case ReturnStatementNode r:
                    AddToken(r.Line, r.KeywordColumnStart, r.KeywordColumnEnd, "keyword", "control"); // "return"
                    if (r.Expression != null)
                    {
                        CheckExpression(r.Expression);
                        string retType = GetExpressionType(r.Expression);
                        if (!AreTypesCompatible(func!.ReturnType, retType))
                            ReportError($"Function '{func.FunctionName}' expects to return '{func.ReturnType}' but got '{retType}'.", r.Line, r.StartColumn, r.EndColumn);
                        r.Expression = Fold(r.Expression);
                    } else
                    {
                        if (func != null && func.ReturnType != "void")
                            ReportError($"Function declared to return '{func.ReturnType}' but returned nothing.", r.Line, r.StartColumn, r.EndColumn);
                    }
                    break;

                case IfStatementNode i:
                    {
                        AddToken(i.Line, i.StartColumn, i.EndColumn, "keyword", "control"); // "if"
                        CheckExpression(i.Condition);
                        BitArray beforeIf = new BitArray(assignedSlots.Peek());

                        AnalyzeStatementBlock(i.Body, func, true);
                        BitArray afterIf = new BitArray(assignedSlots.Peek());

                        if (i.ElseBody != null)
                        {
                            if (i.ElseLine > 0)
                                AddToken(i.ElseLine, i.ElseStartColumn, i.ElseStartColumn + 4, "keyword", "control"); // "else"

                            assignedSlots.Pop();
                            assignedSlots.Push(new BitArray(beforeIf));

                            AnalyzeStatementBlock(i.ElseBody, func, true);
                            BitArray afterElse = new BitArray(assignedSlots.Peek());

                            afterIf.And(afterElse);
                            assignedSlots.Pop();
                            assignedSlots.Push(afterIf);
                        } else
                        {
                            afterIf.And(beforeIf);
                            assignedSlots.Pop();
                            assignedSlots.Push(afterIf);
                        }
                        break;
                    }

                case WhileStatementNode w:
                    AddToken(w.Line, w.KeywordColumnStart, w.KeywordColumnEnd, "keyword", "control"); // "while"
                    CheckExpression(w.Condition);

                    BitArray beforeW = new BitArray(assignedSlots.Peek());

                    string whileType = GetExpressionType(w.Condition);
                    if (whileType != "bool" && whileType != "error")
                        ReportError($"While condition must be boolean, got '{whileType}'.", w.Line, w.StartColumn, w.EndColumn);

                    AnalyzeStatementBlock(w.Body, func, true);

                    BitArray afterW = new BitArray(assignedSlots.Peek());

                    beforeW.Or(afterW);
                    assignedSlots.Pop();
                    assignedSlots.Push(beforeW);

                    break;

                case ForStatementNode fo:
                    AddToken(fo.Line, fo.StartColumn, fo.EndColumn, "keyword", "control"); // "for"
                    if (fo.Initialization != null)
                    {
                        switch (fo.Initialization)
                        {
                            case VariableDeclarationNode vd:
                                if (vd.Initializer != null)
                                {
                                    CheckExpression(vd.Initializer);
                                    string initType = GetExpressionType(vd.Initializer);
                                    if (initType != "int" && initType != "float" && initType != "error")
                                        ReportError($"For loop initialization must be numeral, got '{initType}'.", vd.Line, vd.StartColumn, vd.EndColumn);
                                }
                                DeclareVariable(vd);
                                assignedSlots.Peek().Set(vd.SlotIndex, true);
                                break;

                            case ExpressionStatementNode es:
                                CheckExpression(es.Expression);
                                string esType = GetExpressionType(es.Expression);
                                if (esType != "int" && esType != "float" && esType != "error")
                                    ReportError($"For loop initialization must be numeral, got '{esType}'.", es.Line, es.StartColumn, es.EndColumn);
                                break;

                            default:
                                ReportError("For loop initialization must be a variable declaration or expression statement.", fo.Line, fo.Initialization.StartColumn, fo.Initialization.EndColumn);
                                break;
                        }
                    }

                    if (fo.Condition != null)
                    {
                        CheckExpression(fo.Condition);

                        string forType = GetExpressionType(fo.Condition);
                        if (forType != "bool" && forType != "error")
                            ReportError($"For condition must be boolean, got '{forType}'.", fo.Line, fo.StartColumn, fo.EndColumn);
                    }

                    if (fo.Increment != null)
                    {
                        switch (fo.Increment)
                        {
                            case ExpressionStatementNode ies:
                                CheckExpression(ies.Expression);
                                string incrType = GetExpressionType(ies.Expression);
                                if (incrType != "int" && incrType != "float" && incrType != "error")
                                    ReportError($"For loop increment must be numeric, got '{incrType}'.", ies.Line, ies.StartColumn, ies.EndColumn);
                                break;

                            case IncrementStatementNode inc:
                                VariableDeclarationNode? decl = ResolveVariable(inc.VariableName);
                                if (decl == null)
                                {
                                    ReportError($"Variable '{inc.VariableName}' used before declaration.", inc.Line, inc.StartColumn, inc.EndColumn);
                                } else
                                {
                                    inc.SlotIndex = decl.SlotIndex;
                                    assignedSlots.Peek().Set(decl.SlotIndex, true);
                                    if (decl.VariableType != "int" && decl.VariableType != "float")
                                        ReportError($"Cannot apply increment operator to type '{decl.VariableType}'.", inc.Line, inc.StartColumn, inc.EndColumn);

                                    AddToken(inc.Line, inc.StartColumn, inc.VariableEndColumn, "variable");

                                    if (inc.OperatorLine > 0)
                                        AddToken(inc.OperatorLine, inc.OperatorStartColumn, inc.OperatorEndColumn, "operator");
                                }
                                break;

                            default:
                                ReportError("For loop increment must be an expression or increment statement.", fo.Increment.Line, fo.Increment.StartColumn, fo.Increment.EndColumn);
                                break;
                        }
                    }

                    BitArray beforeFo = new BitArray(assignedSlots.Peek());

                    AnalyzeStatementBlock(fo.Body, func, true);

                    BitArray afterFo = new BitArray(assignedSlots.Peek());

                    beforeFo.Or(afterFo);
                    assignedSlots.Pop();
                    assignedSlots.Push(beforeFo);

                    break;
            }
        }

        /// <summary>
        /// Performs semantic checks on the specified expression node, validating variable usage and assignment within
        /// the current function context.
        /// </summary>
        /// <remarks>This method reports errors for variables that are used before declaration or may be
        /// unassigned at the point of use. It recursively checks sub-expressions as needed.</remarks>
        /// <param name="expr">The expression node to be checked for semantic correctness.</param>
        private void CheckExpression(ExpressionNode expr)
        {
            switch (expr)
            {
                case ModuleAccessNode m:
                    AddToken(m.Line, m.ModuleNameStartColumn, m.ModuleNameEndColumn, "variable", "readonly", "static"); // Module as variable-ish
                    AddToken(m.Line, m.FunctionNameStartColumn, m.FunctionNameEndColumn, "function", "call");
                    foreach (ExpressionNode? arg in m.Arguments)
                        CheckExpression(arg);
                    break;
                case VariableNode v:
                    {
                        VariableDeclarationNode? decl = ResolveVariable(v.Name);

                        if (decl == null)
                        {
                            ReportError(
                                $"Variable '{v.Name}' used before declaration.",
                                v.Line,
                                v.StartColumn,
                                v.EndColumn
                            );
                        } else
                        {
                            v.SlotIndex = decl.SlotIndex;

                            if (!assignedSlots.Peek().Get(decl.SlotIndex))
                            {
                                ReportWarning(
                                    $"Variable '{v.Name}' may be unassigned when used.",
                                    v.Line,
                                    v.StartColumn,
                                    v.EndColumn
                                );
                            }

                            AddToken(v.Line, v.StartColumn, v.EndColumn, "variable");
                        }
                        break;
                    }

                case UnaryExpressionNode u:
                    CheckExpression(u.Right);
                    break;
                case BinaryExpressionNode b:
                    CheckExpression(b.Left);
                    CheckExpression(b.Right);
                    break;
                case FunctionCallNode c:
                    foreach (ExpressionNode arg in c.Arguments)
                        CheckExpression(arg);
                    break;
            }

            GetExpressionType(expr);
        }

        /// <summary>
        /// Performs constant folding on the specified expression tree, simplifying expressions by evaluating constant
        /// sub-expressions where possible.
        /// </summary>
        /// <remarks>Constant folding reduces expressions such as arithmetic or logical operations on
        /// literals to a single literal node. Expressions that cannot be evaluated at compile time are left unchanged.
        /// Division by zero is detected and reported, but the original expression is returned in such cases.</remarks>
        /// <param name="expr">The root node of the expression tree to be folded. Must not be null.</param>
        /// <returns>An expression tree equivalent to the input, with constant sub-expressions replaced by their evaluated
        /// results where possible. Returns the original node if no folding can be performed.</returns>
        private static ExpressionNode Fold(ExpressionNode expr)
        {
            switch (expr)
            {
                case LiteralNode:
                    return expr;

                case UnaryExpressionNode u:
                    {
                        u.Right = Fold(u.Right);
                        if (u.Right is LiteralNode r)
                        {
                            return u.Operator switch
                            {
                                "-" => r.Value switch
                                {
                                    int i => new LiteralNode { Value = -i, Line = u.Line, StartColumn = u.StartColumn, EndColumn = u.EndColumn },
                                    double d => new LiteralNode { Value = -d, Line = u.Line, StartColumn = u.StartColumn, EndColumn = u.EndColumn },
                                    _ => u
                                },
                                "!" => r.Value is bool b ? new LiteralNode { Value = !b, Line = u.Line, StartColumn = u.StartColumn, EndColumn = u.EndColumn } : u,
                                _ => u
                            };
                        }
                        return u;
                    }

                case BinaryExpressionNode b:
                    {
                        b.Left = Fold(b.Left);
                        b.Right = Fold(b.Right);

                        if (b.Left is LiteralNode left && b.Right is LiteralNode right)
                        {
                            object leftValue = left.Value;
                            object rightValue = right.Value;

                            switch (b.Operator)
                            {
                                case "+":
                                    if (leftValue is string || rightValue is string)
                                    {
                                        return new LiteralNode
                                        {
                                            Value = leftValue?.ToString() + rightValue?.ToString(),
                                            Line = b.Line,
                                            StartColumn = b.StartColumn,
                                            EndColumn = b.EndColumn
                                        };
                                    }

                                    if (leftValue is int li && rightValue is int ri)
                                        return new LiteralNode { Value = li + ri, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };
                                    if (leftValue is double ld && rightValue is double rd)
                                        return new LiteralNode { Value = ld + rd, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };
                                    if (leftValue is int li2 && rightValue is double rd2)
                                        return new LiteralNode { Value = li2 + rd2, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };
                                    if (leftValue is double ld2 && rightValue is int ri2)
                                        return new LiteralNode { Value = ld2 + ri2, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };

                                    return b;

                                case "-":
                                    if (leftValue is int li3 && rightValue is int ri3)
                                        return new LiteralNode { Value = li3 - ri3, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };
                                    if (leftValue is double ld3 && rightValue is double rd3)
                                        return new LiteralNode { Value = ld3 - rd3, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };
                                    if (leftValue is int li4 && rightValue is double rd4)
                                        return new LiteralNode { Value = li4 - rd4, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };
                                    if (leftValue is double ld4 && rightValue is int ri4)
                                        return new LiteralNode { Value = ld4 - ri4, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };
                                    break;

                                case "*":
                                    if (leftValue is int li5 && rightValue is int ri5)
                                        return new LiteralNode { Value = li5 * ri5, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };
                                    if (leftValue is double ld5 && rightValue is double rd5)
                                        return new LiteralNode { Value = ld5 * rd5, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };
                                    if (leftValue is int li6 && rightValue is double rd6)
                                        return new LiteralNode { Value = li6 * rd6, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };
                                    if (leftValue is double ld6 && rightValue is int ri6)
                                        return new LiteralNode { Value = ld6 * ri6, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };
                                    break;

                                case "/":
                                    if ((rightValue is int ri7 && ri7 == 0) || (rightValue is double rd7 && rd7 == 0))
                                    {
                                        ReportError("Division by zero is not allowed.", b.Line, b.StartColumn, b.EndColumn);
                                        return b;
                                    } else if (leftValue is int li7 && rightValue is int ri8)
                                        return new LiteralNode { Value = (double)li7 / ri8, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };
                                    else if (leftValue is double ld7 && rightValue is double rd8)
                                        return new LiteralNode { Value = ld7 / rd8, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };
                                    else if (leftValue is int li8 && rightValue is double rd9)
                                        return new LiteralNode { Value = li8 / rd9, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };
                                    else if (leftValue is double ld8 && rightValue is int ri9)
                                        return new LiteralNode { Value = ld8 / ri9, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };
                                    break;

                                case "**":
                                    if ((leftValue is int || leftValue is double) && (rightValue is int || rightValue is double) && Convert.ToDouble(rightValue) != 0)
                                    {
                                        double pow = Math.Pow(Convert.ToDouble(leftValue), Convert.ToDouble(rightValue));
                                        return new LiteralNode { Value = pow, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };
                                    }
                                    break;

                                case "&&":
                                    if (leftValue is bool lb && !lb)
                                        return left; // short-circuit
                                    if (leftValue is bool lb2 && rightValue is bool rb2)
                                        return new LiteralNode { Value = lb2 && rb2, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };
                                    break;

                                case "||":
                                    if (leftValue is bool lb3 && lb3)
                                        return left; // short-circuit
                                    if (leftValue is bool lb4 && rightValue is bool rb4)
                                        return new LiteralNode { Value = lb4 || rb4, Line = b.Line, StartColumn = b.StartColumn, EndColumn = b.EndColumn };
                                    break;
                            }
                        }

                        return b;
                    }

                default:
                    return expr;
            }
        }

        /// <summary>
        /// Determines the type of the specified expression node as a string representation.
        /// </summary>
        /// <remarks>The returned type string corresponds to the language's supported types. If the
        /// expression is a function or module call, the method attempts to resolve overloads based on argument types.
        /// If no matching overload is found or the expression is not recognized, the method returns "error" and may
        /// report an error for diagnostic purposes.</remarks>
        /// <param name="expr">The expression node to analyze. Must not be null.</param>
        /// <returns>A string representing the type of the expression, such as "int", "float", "bool", or "string". Returns
        /// "error" if the type cannot be determined or if the expression is invalid.</returns>
        private string GetExpressionType(ExpressionNode expr)
        {
            if (expr is LiteralNode l)
            {
                string type = l.Value switch
                {
                    int => "int",
                    double => "float",
                    bool => "bool",
                    string => "string",
                    _ => "error"
                };

                string tokenType = type switch
                {
                    "int" => "number",
                    "float" => "number",
                    "bool" => "boolean",
                    "string" => "string",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(tokenType))
                    AddToken(l.Line, l.StartColumn, l.EndColumn, tokenType);

                return type;
            }
            if (expr is VariableNode v)
            {
                VariableDeclarationNode? varDecl = FindVisibleVariable(v.SlotIndex);
                if (varDecl != null)
                    return varDecl.VariableType;

                return "error";
            }
            if (expr is UnaryExpressionNode u)
            {
                string rightType = GetExpressionType(u.Right);

                // Add token for operator
                AddToken(u.Line, u.OperatorColumnStart, u.OperatorColumnEnd, "operator");

                if (rightType == "error")
                    return "error";

                switch (u.Operator)
                {
                    case "-":
                        if (rightType != "int" && rightType != "float")
                        {
                            ReportError($"Operator '-' cannot be applied to type '{rightType}'.", u.Line, u.StartColumn, u.EndColumn);
                            return "error";
                        }
                        return rightType;

                    case "!":
                        if (rightType != "bool")
                        {
                            ReportError($"Operator '!' cannot be applied to type '{rightType}'.", u.Line, u.StartColumn, u.EndColumn);
                            return "error";
                        }
                        return "bool";

                    default:
                        return "error";
                }
            }
            if (expr is BinaryExpressionNode b)
            {
                string leftType = GetExpressionType(b.Left);
                string rightType = GetExpressionType(b.Right);
                string op = b.Operator;

                // Add token for operator
                AddToken(b.Line, b.OperatorColumnStart, b.OperatorColumnEnd, "operator");

                if (leftType == "error" || rightType == "error")
                    return "error";
                if (op is "+" or "-" or "*" or "/" or "**")
                {
                    return GetBinaryResultType(leftType, rightType, op, b.Line, b.StartColumn, b.EndColumn);
                }
                if (op is "==" or "!=" or "<" or ">" or "<=" or ">=")
                {
                    if (!AreTypesCompatible(leftType, rightType) && !AreTypesCompatible(rightType, leftType))
                    {
                        ReportError($"Cannot compare '{leftType}' and '{rightType}'.", b.Line, b.StartColumn, b.EndColumn);
                        return "error";
                    }
                    return "bool";
                }
                if (op is "&&" or "||")
                {
                    if (leftType != "bool" || rightType != "bool")
                    {
                        ReportError("Logical operators require boolean operands.", b.Line, b.StartColumn, b.EndColumn);
                        return "error";
                    }
                    return "bool";
                }
                return "error";
            }
            if (expr is FunctionCallNode f)
            {
                List<FunctionDefinitionNode> candidates = [];

                if (functionLookup.TryGetValue(f.FunctionName, out List<FunctionDefinitionNode>? userFns))
                    candidates.AddRange(userFns);

                if (builtInFunctions.TryGetValue(f.FunctionName, out List<FunctionDefinitionNode>? builtIns))
                    candidates.AddRange(builtIns);

                foreach (FunctionDefinitionNode fn in candidates)
                {
                    List<FunctionParameterNode> ps = fn.Arguments ?? [];
                    if (ps.Count != f.Arguments.Count)
                        continue;

                    bool match = true;
                    for (int i = 0; i < ps.Count; i++)
                    {
                        string argType = GetExpressionType(f.Arguments[i]);
                        if (!AreTypesCompatible(ps[i].Type, argType))
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        f.ReturnType = fn.ReturnType;
                        AddToken(f.Line, f.FunctionNameStartColumn, f.FunctionNameEndColumn, "function", "call");
                        return fn.ReturnType;
                    }
                }

                ReportError($"No matching overload for function '{f.FunctionName}'.", f.Line, f.StartColumn, f.EndColumn);
                AddToken(f.Line, f.StartColumn, f.EndColumn, "function", "call");
                return "error";
            }
            if (expr is ModuleAccessNode mod)
            {
                string fullName = mod.ModuleName + "." + mod.FunctionName;

                if (!builtInFunctions.TryGetValue(fullName, out List<FunctionDefinitionNode>? builtIns))
                {
                    ReportError($"No matching overload for function '{fullName}'.", mod.Line, mod.StartColumn, mod.EndColumn);
                    return "error";
                }

                foreach (FunctionDefinitionNode fn in builtIns)
                {
                    List<FunctionParameterNode> ps = fn.Arguments ?? [];
                    if (ps.Count != mod.Arguments.Count)
                        continue;

                    bool match = true;
                    for (int i = 0; i < ps.Count; i++)
                    {
                        string argType = GetExpressionType(mod.Arguments[i]);
                        if (!AreTypesCompatible(ps[i].Type, argType))
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        mod.ReturnType = fn.ReturnType;
                        return fn.ReturnType;
                    }
                }

                ReportError($"No matching overload for function '{fullName}'.", mod.Line, mod.StartColumn, mod.EndColumn);
                return "error";
            }

            ReportError("Unknown expression type.", expr.Line, expr.StartColumn, expr.EndColumn);
            return "error";
        }

        private void PushScope()
        {
            currentScope = new Scope(currentScope);
            assignedSlots.Push(assignedSlots.Count > 0
                ? new BitArray(assignedSlots.Peek())
                : new BitArray(1024));
        }

        private void PopScope()
        {
            assignedSlots.Pop();
            currentScope = currentScope?.Parent;
            RebuildVisibleVariables();
        }

        private void RebuildVisibleVariables()
        {
            visibleVariables.Clear();
            for (Scope? scope = currentScope; scope != null; scope = scope.Parent)
            {
                foreach (KeyValuePair<string, VariableDeclarationNode> kvp in scope.Variables)
                    visibleVariables[kvp.Value.SlotIndex] = kvp.Value;
            }
        }

        private void DeclareVariable(VariableDeclarationNode v)
        {
            if (currentScope!.Variables.ContainsKey(v.Name))
            {
                ReportError($"Variable '{v.Name}' already defined.", v.Line, v.StartColumn, v.EndColumn);
                return;
            }

            v.SlotIndex = variableSlotIndex++;
            currentScope.Variables[v.Name] = v;
            slotToNameMap[v.SlotIndex] = v.Name;

            BitArray? currentBits = assignedSlots.Peek();
            if (v.SlotIndex >= currentBits.Length)
                currentBits.Length = Math.Max(currentBits.Length * 2, v.SlotIndex + 1);

            RebuildVisibleVariables();
        }

        private VariableDeclarationNode? FindVisibleVariable(int slotIndex) => visibleVariables.TryGetValue(slotIndex, out VariableDeclarationNode? v) ? v : null;

        private static string GetBinaryResultType(string left, string right, string op, int line, int startColumn, int endColumn)
        {
            if (op == "+")
            {
                if (left == "string" || right == "string")
                    return "string";

                if (left == right)
                    return left;

                if ((left == "int" && right == "float") || (left == "float" && right == "int"))
                    return "float";

                ReportError($"Operator '+' cannot be applied to '{left}' and '{right}'.", line, startColumn, endColumn);
                return "error";
            } else
            {
                if ((left == "int" || left == "float") && (right == "int" || right == "float"))
                {
                    return left == "float" || right == "float" ? "float" : "int";
                }
            }

            ReportError($"Operator '{op}' cannot be applied to '{left}' and '{right}'.", line, startColumn, endColumn);
            return "error";
        }

        private static bool AreTypesCompatible(string target, string source)
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

        private static bool ParametersMatch(List<FunctionParameterNode>? a, List<FunctionParameterNode>? b)
        {
            List<FunctionParameterNode> l1 = a ?? [];
            List<FunctionParameterNode> l2 = b ?? [];
            if (l1.Count != l2.Count)
                return false;
            for (int i = 0; i < l1.Count; i++)
                if (l1[i].Type != l2[i].Type)
                    return false;
            return true;
        }

        private static bool IsValidType(string typeName) => LanguageSpecs.ReturnTypes.Contains(typeName);

        private static bool CheckReturnPath(List<StatementNode> body, string returnType)
        {
            foreach (StatementNode stmt in body)
            {
                if (stmt is ReturnStatementNode r)
                {
                    if (returnType == "void" && r.Expression != null)
                        ReportError("Void functions cannot return a value.", r.Line, r.StartColumn, r.EndColumn);
                    if (returnType != "void" && r.Expression == null)
                        ReportError($"Function must return a value of type '{returnType}'.", r.Line, r.StartColumn, r.EndColumn);
                    return true;
                }
                if (stmt is IfStatementNode i)
                {
                    bool ifReturns = CheckReturnPath(i.Body, returnType);
                    bool elseReturns = i.ElseBody != null && CheckReturnPath(i.ElseBody, returnType);
                    if (ifReturns && elseReturns)
                        return true;
                }
                if (stmt is WhileStatementNode w && w.Condition is LiteralNode l && l.Value is bool b && b)
                {
                    if (CheckReturnPath(w.Body, returnType))
                        return true;
                }
                if (stmt is ForStatementNode f && f.Condition is LiteralNode fl && fl.Value is bool fb && fb)
                {
                    if (CheckReturnPath(f.Body, returnType))
                        return true;
                }
            }
            return false;
        }

        private bool AlwaysExits(StatementNode stmt, string returnType)
        {
            switch (stmt)
            {
                case ReturnStatementNode:
                    return true;

                case IfStatementNode i:
                    if (i.ElseBody == null)
                        return false;

                    return
                        BlockAlwaysExits(i.Body, returnType) &&
                        BlockAlwaysExits(i.ElseBody, returnType);

                case WhileStatementNode w:
                    if (w.Condition is LiteralNode l && l.Value is bool b && b)
                        return BlockAlwaysExits(w.Body, returnType);
                    return false;

                case ForStatementNode f:
                    if (f.Condition is LiteralNode ln && ln.Value is bool b2 && b2)
                        return BlockAlwaysExits(f.Body, returnType);
                    return false;

                default:
                    return false;
            }
        }

        private bool BlockAlwaysExits(List<StatementNode> body, string returnType)
        {
            foreach (StatementNode stmt in body)
            {
                if (AlwaysExits(stmt, returnType))
                    return true;

                // statement does not guarantee exit, execution continues
                return false;
            }
            return false;
        }

        private VariableDeclarationNode? ResolveVariable(string name)
        {
            Scope? scope = currentScope;
            while (scope != null)
            {
                if (scope.Variables.TryGetValue(name, out VariableDeclarationNode? decl))
                    return decl;
                scope = scope.Parent;
            }
            return null;
        }
    }

    internal class Scope(Scope? parent)
    {
        public Dictionary<string, VariableDeclarationNode> Variables { get; } = [];
        public Scope? Parent { get; } = parent;
    }
}
