using System.Globalization;

using Vext.Compiler.Diagnostics;
using Vext.Compiler.Lexing;
using Vext.Compiler.Shared;

namespace Vext.Compiler.Parsing
{
    internal class Parser(List<Token> tokens)
    {
        private readonly List<Token> tokens = tokens;
        private int currentToken = 0;

        private static void ReportError(string message, int startLine, int startCol, int endLine, int endCol) => Diagnostic.ReportError(message, startLine, startCol, endLine, endCol);
        private static void ReportWarning(string message, int startLine, int startCol, int endLine, int endCol) => Diagnostic.ReportWarning(message, startLine, startCol, endLine, endCol);
        private static void ReportInfo(string message, int startLine, int startCol, int endLine, int endCol) => Diagnostic.ReportInfo(message, startLine, startCol, endLine, endCol);
        private static void ReportHint(string message, int startLine, int startCol, int endLine, int endCol) => Diagnostic.ReportHint(message, startLine, startCol, endLine, endCol);

        /// <summary>
        /// Recovers from a parsing error by skipping to a logical statement boundary.
        /// Stops at semicolons, closing braces, or statement-starting keywords.
        /// </summary>
        private void RecoverToStatementBoundary()
        {
            while (currentToken < tokens.Count)
            {
                Token tok = tokens[currentToken];

                // Stop at semicolons or closing braces
                if ((tok.TokenType == TokenType.Punctuation && (tok.Value == ";" || tok.Value == "}")))
                {
                    // Skip the semicolon but keep the closing brace for the parent context
                    if (tok.Value == ";")
                        Advance();
                    break;
                }

                // Stop at statement-starting keywords (but not in the middle of an expression)
                if (tok.TokenType == TokenType.Keyword &&
                    (LanguageSpecs.ReturnTypes.Contains(tok.Value) ||
                     tok.Value == "if" || tok.Value == "while" || tok.Value == "for" || tok.Value == "return"))
                {
                    break;
                }

                Advance();
            }
        }

        /// <summary>
        /// Parses the input token stream and returns a list of statement nodes representing the parsed statements.
        /// </summary>
        /// <remarks>If a parsing error occurs, the method attempts to recover and continue parsing
        /// subsequent statements. Errors encountered during parsing are reported and do not cause the method to throw
        /// exceptions under normal circumstances.</remarks>
        /// <returns>A list of <see cref="StatementNode"/> objects corresponding to the successfully parsed statements. The list
        /// may be empty if no valid statements are found.</returns>
        public List<StatementNode> Parse()
        {
            List<StatementNode> statements = [];
            while (currentToken < tokens.Count && tokens[currentToken].TokenType != TokenType.EOF)
            {
                StatementNode? stmt = null;
                try
                {
                    stmt = ParseStatement();
                } catch (Exception ex)
                {
                    // Shouldn't normally happen because Parse methods report errors instead of throwing,
                    // but catch any unexpected exception to convert into an error and attempt to continue.
                    Token tok = currentToken < tokens.Count ? tokens[currentToken] : new Token(TokenType.EOF, "", 0, 0, 0);
                    ReportError(ex.Message, tok.Line, tok.StartColumn, tok.Line, tok.EndColumn);
                    RecoverToStatementBoundary();
                }

                if (stmt != null)
                {
                    statements.Add(stmt);
                } else
                {
                    // Only report error if one wasn't already reported one in the parsing attempt
                    Token tok = CurrentToken();
                    if (tok.TokenType != TokenType.EOF)
                    {
                        ReportError($"Unexpected token '{tok.Value}'", tok.Line, tok.StartColumn, tok.Line, tok.EndColumn);
                        RecoverToStatementBoundary();
                    }
                }
            }
            return (statements);
        }

        /// <summary>
        /// Parses the next statement from the current position in the token stream.
        /// </summary>
        /// <remarks>This method advances the parser's position as it consumes tokens for a statement. If
        /// the input is invalid or incomplete, the method attempts to recover by skipping to the next logical statement
        /// boundary. The returned node may represent various statement types, such as variable declarations, function
        /// declarations, control flow statements, assignments, or expression statements, depending on the current token
        /// context.</remarks>
        /// <returns>A <see cref="StatementNode"/> representing the parsed statement, or <see langword="null"/> if no valid
        /// statement could be parsed.</returns>
        private StatementNode? ParseStatement()
        {
            Token token = CurrentToken();
            if (token.TokenType == TokenType.Unknown)
            {
                ReportError($"Unknown token '{token.Value}'", token.Line, token.StartColumn, token.Line, token.EndColumn);
                Advance();
                return null;
            } else if (token.TokenType == TokenType.Comment)
            {
                Advance(); // skip comments
                return ParseStatement();
            } else if (token.TokenType == TokenType.Keyword)
            {
                if (LanguageSpecs.ReturnTypes.Contains(token.Value))
                {
                    if (Peek().TokenType == TokenType.Identifier && Peek(2).TokenType == TokenType.Punctuation && Peek(2).Value == "(")
                    {
                        FunctionDefinitionNode? func = ParseFunctionDeclaration();
                        if (func != null)
                            return func;
                    } else if (Peek().TokenType == TokenType.Identifier)
                    {
                        if ((Peek(2).TokenType == TokenType.Operator && Peek(2).Value == "=") ||
                            (Peek(2).TokenType == TokenType.Punctuation && Peek(2).Value == ";"))

                        {
                            VariableDeclarationNode? decl = ParseVariableDeclaration();
                            Match(TokenType.Punctuation, ";"); // consume semicolon if present
                            if (decl != null)
                                return decl;
                        } else
                        {
                            Token startToken = CurrentToken();
                            ReportError("Invalid variable declaration", startToken.Line, startToken.StartColumn, startToken.Line, startToken.EndColumn);
                            RecoverToStatementBoundary();
                            return null;
                        }
                    } else
                    {
                        ReportError("Invalid statement", token.Line, token.StartColumn, CurrentToken().Line, CurrentToken().EndColumn);
                        Advance();
                        return null;
                    }
                } else if (token.Value == "if")
                {
                    IfStatementNode? stmt = ParseIfStatement();
                    if (stmt != null)
                        return stmt;
                } else if (token.Value == "while")
                {
                    WhileStatementNode? stmt = ParseWhileStatement();
                    if (stmt != null)
                        return stmt;
                } else if (token.Value == "for")
                {
                    ForStatementNode? stmt = ParseForStatement();
                    if (stmt != null)
                        return stmt;
                } else if (token.Value == "return")
                {
                    ReturnStatementNode? rtrn = ParseReturn();
                    Match(TokenType.Punctuation, ";"); // consume semicolon if present
                    if (rtrn != null)
                        return rtrn;

                } else
                {
                    ReportError($"Unexpected keyword '{token.Value}'", token.Line, token.StartColumn, CurrentToken().Line, CurrentToken().EndColumn);
                    Advance();
                    return null;
                }
            } else if (token.TokenType == TokenType.Identifier)
            {
                if (Peek().TokenType == TokenType.Operator &&
                   (Peek().Value == "++" || Peek().Value == "--"))
                {
                    Token name = Expect(TokenType.Identifier);
                    Token op = Expect(TokenType.Operator);
                    Match(TokenType.Punctuation, ";"); // consume semicolon if present

                    return new IncrementStatementNode
                    {
                        VariableName = name.Value,
                        VariableStartColumn = name.StartColumn,
                        VariableEndColumn = name.EndColumn,
                        IsIncrement = op.Value == "++",
                        Line = name.Line,
                        StartColumn = name.StartColumn,
                        EndColumn = name.EndColumn,
                        OperatorLine = op.Line,
                        OperatorStartColumn = op.StartColumn,
                        OperatorEndColumn = op.EndColumn
                    };
                }

                if (Peek().TokenType == TokenType.Operator &&
                   (Peek().Value == "=" ||
                    Peek().Value == "+=" ||
                    Peek().Value == "-=" ||
                    Peek().Value == "*="))
                {
                    Token name = Expect(TokenType.Identifier);
                    Token op = Expect(TokenType.Operator);
                    ExpressionNode value = ParseExpression();
                    Match(TokenType.Punctuation, ";"); // consume semicolon if present

                    return new AssignmentStatementNode
                    {
                        VariableName = name.Value,
                        VariableStartColumn = name.StartColumn,
                        VariableEndColumn = name.EndColumn,
                        Operator = op.Value,
                        Value = value,
                        Line = name.Line,
                        StartColumn = name.StartColumn,
                        EndColumn = tokens[currentToken - 1].EndColumn,
                        OperatorLine = op.Line,
                        OperatorStartColumn = op.StartColumn,
                        OperatorEndColumn = op.EndColumn
                    };
                }

                // Otherwise normal expression statement
                ExpressionNode expr = ParseExpression();

                if (expr is not FunctionCallNode)
                {
                    Token startToken = CurrentToken();
                    ReportError($"Unexpected identifier", startToken.Line, startToken.StartColumn, startToken.Line, startToken.EndColumn);
                    Match(TokenType.Punctuation, ";"); // consume semicolon if present
                    return null;
                }
                Match(TokenType.Punctuation, ";"); // consume semicolon if present

                return new ExpressionStatementNode
                {
                    Expression = expr,
                    Line = token.Line,
                    StartColumn = token.StartColumn,
                    EndColumn = tokens[currentToken - 1].EndColumn
                };
            }

            return null;
        }

        private FunctionDefinitionNode? ParseFunctionDeclaration()
        {
            Token returnType = Expect(TokenType.Keyword);
            Token name = Expect(TokenType.Identifier);

            Expect(TokenType.Punctuation, "("); // consume '('
            List<FunctionParameterNode> arguments = [];

            // handle empty calls like func()
            Token tok = CurrentToken();
            if (!(tok.TokenType == TokenType.Punctuation && tok.Value == ")"))
            {
                while (true)
                {
                    FunctionParameterNode? param = ParseFunctionParameter();
                    if (param == null)
                    {
                        Token startToken = CurrentToken();
                        // attempt to skip to closing ')' - stop at ) or }
                        while (currentToken < tokens.Count &&
                               !(tokens[currentToken].TokenType == TokenType.Punctuation &&
                               (tokens[currentToken].Value == ")" || tokens[currentToken].Value == "}")))
                            Advance();

                        Token endToken = CurrentToken();
                        ReportError("Invalid parameter declaration", startToken.Line, startToken.StartColumn, endToken.Line, endToken.EndColumn);
                        break;
                    }
                    arguments.Add(param);
                    if (!Match(TokenType.Punctuation, ","))
                        break; // continue if there's a comma
                }
            }
            Expect(TokenType.Punctuation, ")"); // consume ')'

            Expect(TokenType.Punctuation, "{"); // consume opening brace
            List<StatementNode> body = [];
            while (!(CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == "}"))
            {
                if (CurrentToken().TokenType == TokenType.EOF)
                    break;

                StatementNode? statement = ParseStatement();
                if (statement == null)
                {
                    // try to recover: skip until '}' or next statement keyword
                    if (currentToken < tokens.Count && tokens[currentToken].TokenType == TokenType.Punctuation && tokens[currentToken].Value == "}")
                        break;

                    while (currentToken < tokens.Count)
                    {
                        Token t = tokens[currentToken];
                        // Stop at closing brace or statement keywords
                        if ((t.TokenType == TokenType.Punctuation && t.Value == "}") ||
                            (t.TokenType == TokenType.Keyword &&
                             (LanguageSpecs.ReturnTypes.Contains(t.Value) ||
                              t.Value == "if" || t.Value == "while" || t.Value == "for" || t.Value == "return")))
                        {
                            break;
                        }
                        Advance();
                    }
                    continue;
                }
                body.Add(statement);
            }
            Expect(TokenType.Punctuation, "}"); // consume closing brace

            return new FunctionDefinitionNode
            {
                ReturnType = returnType.Value,
                ReturnTypeStartColumn = returnType.StartColumn,
                ReturnTypeEndColumn = returnType.EndColumn,
                FunctionName = name.Value,
                Arguments = arguments,
                Body = body,
                Line = returnType.Line,
                StartColumn = returnType.StartColumn,
                EndColumn = tokens[currentToken - 1].EndColumn,
                NameLine = name.Line,
                NameStartColumn = name.StartColumn,
                NameEndColumn = name.EndColumn
            };
        }

        private FunctionParameterNode? ParseFunctionParameter()
        {
            Token type = Expect(TokenType.Keyword);
            Token name = Expect(TokenType.Identifier);

            if (type.TokenType != TokenType.Keyword || name.TokenType != TokenType.Identifier)
                return null;

            ExpressionNode? initializer = null;
            if (Match(TokenType.Operator, "="))
            {
                initializer = ParseExpression();
            }
            return new FunctionParameterNode
            {
                Type = type.Value,
                TypeStartColumn = type.StartColumn,
                TypeEndColumn = type.EndColumn,
                Name = name.Value,
                Initializer = initializer,
                Line = type.Line,
                StartColumn = type.StartColumn,
                EndColumn = initializer != null ? initializer.EndColumn : name.EndColumn,
                NameLine = name.Line,
                NameStartColumn = name.StartColumn,
                NameEndColumn = name.EndColumn
            };
        }

        private FunctionCallNode ParseFunctionCall(string name, int line, int column, int eColumn)
        {
            // current token should be '(' when this is called (caller ensures that)
            Expect(TokenType.Punctuation, "("); // consume '('
            List<ExpressionNode> arguments = [];

            // handle empty calls like func()
            Token tok = CurrentToken();
            if (!(tok.TokenType == TokenType.Punctuation && tok.Value == ")"))
            {
                while (true)
                {
                    arguments.Add(ParseExpression());
                    if (!Match(TokenType.Punctuation, ","))
                        break; // continue if there's a comma
                }
            }

            Expect(TokenType.Punctuation, ")"); // consume ')'
            return new FunctionCallNode { FunctionName = name, FunctionNameStartColumn = column, FunctionNameEndColumn = eColumn, Arguments = arguments, Line = line, StartColumn = column, EndColumn = tokens[currentToken - 1].EndColumn };
        }

        private ReturnStatementNode? ParseReturn()
        {
            Token rtrnToken = Expect(TokenType.Keyword, "return");
            if (CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == ";")
            {
                return new ReturnStatementNode { KeywordColumnStart = rtrnToken.StartColumn, KeywordColumnEnd = rtrnToken.EndColumn, Expression = null, Line = rtrnToken.Line, StartColumn = rtrnToken.StartColumn, EndColumn = tokens[currentToken - 1].EndColumn };
            } else if (IsStatementBoundary())
            {
                // Return with no expression at statement boundary
                return new ReturnStatementNode { KeywordColumnStart = rtrnToken.StartColumn, KeywordColumnEnd = rtrnToken.EndColumn, Expression = null, Line = rtrnToken.Line, StartColumn = rtrnToken.StartColumn, EndColumn = rtrnToken.EndColumn };
            } else
            {
                ExpressionNode expr = ParseExpression();
                return new ReturnStatementNode { KeywordColumnStart = rtrnToken.StartColumn, KeywordColumnEnd = rtrnToken.EndColumn, Expression = expr, Line = rtrnToken.Line, StartColumn = rtrnToken.StartColumn, EndColumn = tokens[currentToken - 1].EndColumn };
            }
        }

        /// <summary>
        /// Checks if the current token is at a statement boundary (closing brace, EOF, or keyword starting a new statement).
        /// </summary>
        private bool IsStatementBoundary()
        {
            Token tok = CurrentToken();
            if (tok.TokenType == TokenType.EOF)
                return true;
            if (tok.TokenType == TokenType.Punctuation && (tok.Value == "}" || tok.Value == ";"))
                return true;
            if (tok.TokenType == TokenType.Keyword &&
                (LanguageSpecs.ReturnTypes.Contains(tok.Value) ||
                 tok.Value == "if" || tok.Value == "while" || tok.Value == "for" || tok.Value == "return" || tok.Value == "else"))
                return true;
            return false;
        }

        private IfStatementNode? ParseIfStatement()
        {
            Token ifToken = Expect(TokenType.Keyword, "if");
            Expect(TokenType.Punctuation, "(");
            ExpressionNode expr = ParseExpression(0);
            Expect(TokenType.Punctuation, ")");

            List<StatementNode> body = [];

            if (Match(TokenType.Punctuation, "{"))
            {
                while (!(CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == "}"))
                {
                    if (CurrentToken().TokenType == TokenType.EOF)
                        break;

                    StatementNode? statement = ParseStatement();
                    if (statement == null)
                    {
                        // recover - skip to closing brace or next statement
                        while (currentToken < tokens.Count)
                        {
                            Token t = tokens[currentToken];
                            if ((t.TokenType == TokenType.Punctuation && (t.Value == "}" || t.Value == ";")) ||
                                (t.TokenType == TokenType.Keyword &&
                                 (LanguageSpecs.ReturnTypes.Contains(t.Value) ||
                                  t.Value == "if" || t.Value == "while" || t.Value == "for" || t.Value == "return")))
                            {
                                if (t.Value == ";")
                                    Advance();
                                break;
                            }
                            Advance();
                        }
                        continue;
                    }
                    body.Add(statement);
                }
                Expect(TokenType.Punctuation, "}");
            } else
            {
                // single statement if
                StatementNode? stmt = ParseStatement();
                if (stmt == null)
                {

                } else
                    body.Add(stmt);
            }

            List<StatementNode>? elseBody = null;
            Token elseToken = new Token(TokenType.Unknown, "", 0, 0, 0); // Placeholder
            if (Match(TokenType.Keyword, "else"))
            {
                elseToken = tokens[currentToken - 1];
                elseBody = [];
                if (Match(TokenType.Punctuation, "{"))
                {
                    while (!(CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == "}"))
                    {
                        if (CurrentToken().TokenType == TokenType.EOF)
                            break;

                        StatementNode? statement = ParseStatement();
                        if (statement == null)
                        {
                            // recover
                            while (currentToken < tokens.Count)
                            {
                                Token t = tokens[currentToken];
                                if ((t.TokenType == TokenType.Punctuation && (t.Value == "}" || t.Value == ";")) ||
                                    (t.TokenType == TokenType.Keyword &&
                                     (LanguageSpecs.ReturnTypes.Contains(t.Value) ||
                                      t.Value == "if" || t.Value == "while" || t.Value == "for" || t.Value == "return")))
                                {
                                    if (t.Value == ";")
                                        Advance();
                                    break;
                                }
                                Advance();
                            }
                            continue;
                        }
                        elseBody.Add(statement);
                    }
                    Expect(TokenType.Punctuation, "}");
                } else
                {
                    // single statement else
                    StatementNode? stmt = ParseStatement();
                    if (stmt == null)
                        ReportError("Invalid single-line else body", ifToken.Line, ifToken.StartColumn, CurrentToken().Line, CurrentToken().EndColumn);
                    else
                        elseBody.Add(stmt);
                }
            }

            return new IfStatementNode
            {
                Condition = expr,
                Body = body,
                ElseBody = elseBody,
                Line = ifToken.Line,
                StartColumn = ifToken.StartColumn,
                EndColumn = ifToken.EndColumn,
                ElseLine = elseToken.Line,
                ElseStartColumn = elseToken.StartColumn
            };
        }

        private ForStatementNode? ParseForStatement()
        {
            Token forToken = Expect(TokenType.Keyword, "for");
            Expect(TokenType.Punctuation, "(");
            StatementNode? initialization = null;
            if (!(CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == ";"))
            {
                if (LanguageSpecs.ReturnTypes.Contains(CurrentToken().Value))
                {
                    initialization = ParseVariableDeclaration();
                } else
                {
                    ExpressionNode? expr = ParseExpression();
                    initialization = new ExpressionStatementNode
                    {
                        Expression = expr,
                        Line = expr.Line,
                        StartColumn = expr.StartColumn,
                        EndColumn = tokens[currentToken - 1].EndColumn
                    };
                }
                if (!(initialization is VariableDeclarationNode || initialization is ExpressionStatementNode))
                {
                    ReportError("For-loop initialization must be a variable declaration or expression statement", forToken.Line, forToken.StartColumn, CurrentToken().Line, CurrentToken().EndColumn);
                }
                if (initialization == null)
                {
                    ReportError("Invalid for-loop initialization", forToken.Line, forToken.StartColumn, CurrentToken().Line, CurrentToken().EndColumn);
                    // attempt to recover to semicolon
                    while (currentToken < tokens.Count && !(tokens[currentToken].TokenType == TokenType.Punctuation && tokens[currentToken].Value == ";"))
                        Advance();
                }
            }
            Expect(TokenType.Punctuation, ";");
            ExpressionNode? condition = null;
            if (!(CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == ";"))
            {
                condition = ParseExpression(0);
            }
            Expect(TokenType.Punctuation, ";");
            StatementNode? increment = null;
            if (!(CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == ")"))
            {
                ExpressionNode? expr = ParseExpression();

                increment = new ExpressionStatementNode
                {
                    Expression = expr,
                    Line = expr.Line,
                    StartColumn = expr.StartColumn,
                    EndColumn = tokens[currentToken - 1].EndColumn
                };
            }

            Expect(TokenType.Punctuation, ")");

            List<StatementNode> body = [];
            if (Match(TokenType.Punctuation, "{"))
            {
                while (!(CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == "}"))
                {
                    if (CurrentToken().TokenType == TokenType.EOF)
                        break;

                    StatementNode? stmt = ParseStatement();
                    if (stmt == null)
                    {
                        // recover - skip to closing brace or next statement
                        while (currentToken < tokens.Count)
                        {
                            Token t = tokens[currentToken];
                            if ((t.TokenType == TokenType.Punctuation && (t.Value == "}" || t.Value == ";")) ||
                                (t.TokenType == TokenType.Keyword &&
                                 (LanguageSpecs.ReturnTypes.Contains(t.Value) ||
                                  t.Value == "if" || t.Value == "while" || t.Value == "for" || t.Value == "return")))
                            {
                                if (t.Value == ";")
                                    Advance();
                                break;
                            }
                            Advance();
                        }
                        continue;
                    }
                    body.Add(stmt);
                }
                Expect(TokenType.Punctuation, "}");
            } else
            {
                // single statement body
                StatementNode? stmt = ParseStatement();
                if (stmt == null)
                {
                    ReportError("Invalid single-line for body", forToken.Line, forToken.StartColumn, CurrentToken().Line, CurrentToken().EndColumn);
                } else
                {
                    body.Add(stmt);
                }
            }

            return new ForStatementNode
            {
                KeywordColumnStart = forToken.StartColumn,
                KeywordColumnEnd = forToken.EndColumn,
                Initialization = initialization,
                Condition = condition,
                Increment = increment,
                Body = body,
                Line = forToken.Line,
                StartColumn = forToken.StartColumn,
                EndColumn = tokens[currentToken - 1].EndColumn
            };
        }

        private WhileStatementNode? ParseWhileStatement()
        {
            Token whileToken = Expect(TokenType.Keyword, "while");
            Expect(TokenType.Punctuation, "(");
            ExpressionNode expr = ParseExpression(0);
            Expect(TokenType.Punctuation, ")");

            List<StatementNode> body = [];

            if (Match(TokenType.Punctuation, "{"))
            {
                while (!(CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == "}"))
                {
                    if (CurrentToken().TokenType == TokenType.EOF)
                        break;

                    StatementNode? statement = ParseStatement();
                    if (statement == null)
                    {
                        // recover - skip to closing brace or next statement
                        while (currentToken < tokens.Count)
                        {
                            Token t = tokens[currentToken];
                            if ((t.TokenType == TokenType.Punctuation && (t.Value == "}" || t.Value == ";")) ||
                                (t.TokenType == TokenType.Keyword &&
                                 (LanguageSpecs.ReturnTypes.Contains(t.Value) ||
                                  t.Value == "if" || t.Value == "while" || t.Value == "for" || t.Value == "return")))
                            {
                                if (t.Value == ";")
                                    Advance();
                                break;
                            }
                            Advance();
                        }
                        continue;
                    }
                    body.Add(statement);
                }
                Expect(TokenType.Punctuation, "}");
            } else
            {
                // single statement while
                StatementNode? stmt = ParseStatement();
                if (stmt == null)
                {
                    ReportError("Invalid single-line while body", whileToken.Line, whileToken.StartColumn, CurrentToken().Line, CurrentToken().EndColumn);
                } else
                    body.Add(stmt);
            }

            return new WhileStatementNode
            {
                KeywordColumnStart = whileToken.StartColumn,
                KeywordColumnEnd = whileToken.EndColumn,
                Condition = expr,
                Body = body,
                Line = whileToken.Line,
                StartColumn = whileToken.StartColumn,
                EndColumn = tokens[currentToken - 1].EndColumn
            };
        }

        private VariableDeclarationNode? ParseVariableDeclaration()
        {
            Token type = Expect(TokenType.Keyword);
            Token name = Expect(TokenType.Identifier);

            if (!LanguageSpecs.VariableTypes.Contains(type.Value))
            {
                ReportError($"Variable cannot be of type '{type.Value}'", name.Line, type.StartColumn, CurrentToken().Line, CurrentToken().EndColumn);
                return null;
            }

            ExpressionNode? initializer = null;
            if (Match(TokenType.Operator, "="))
            {
                initializer = ParseExpression();
            }
            return new VariableDeclarationNode
            {
                VariableType = type.Value,
                TypeStartColumn = type.StartColumn,
                TypeEndColumn = type.EndColumn,
                DeclaredType = type.Value,
                Name = name.Value,
                Initializer = initializer,
                Line = type.Line,
                StartColumn = type.StartColumn,
                EndColumn = tokens[currentToken - 1].EndColumn,
                NameLine = name.Line,
                NameStartColumn = name.StartColumn,
                NameEndColumn = name.EndColumn
            };
        }

        /// <summary>
        /// Parses a primary expression from the specified token and returns the corresponding expression node.
        /// </summary>
        /// <remarks>Primary expressions include literals (numeric, string, boolean), variable references,
        /// function calls, module function calls, parenthesized expressions, and unary operations (such as negation or
        /// logical not). If the token does not represent a valid primary expression, a dummy literal node is returned
        /// and an error is reported.</remarks>
        /// <param name="token">The token representing the start of the primary expression to parse. Must not be null.</param>
        /// <returns>An ExpressionNode representing the parsed primary expression, such as a literal, variable, function call, or
        /// unary operation.</returns>
        private ExpressionNode ParsePrimary(Token token)
        {
            ExpressionNode left;

            if (token.TokenType == TokenType.Operator && (token.Value == "-" || token.Value == "!"))
            {
                Advance(); // consume '-' or '!'
                ExpressionNode? right = ParsePrimary(CurrentToken());
                return new UnaryExpressionNode
                {
                    Operator = token.Value,
                    OperatorColumnStart = token.StartColumn,
                    OperatorColumnEnd = token.EndColumn,
                    Right = right,
                    Line = token.Line,
                    StartColumn = token.StartColumn,
                    EndColumn = tokens[currentToken - 1].EndColumn
                };
            }

            if (token.TokenType == TokenType.Punctuation && token.Value == "(")
            {
                Advance(); // consume '('
                ExpressionNode expr = ParseExpression();
                Expect(TokenType.Punctuation, ")");
                left = expr;
            } else if (token.TokenType == TokenType.Numeric)
            {
                Advance();
                // support both integer and floating point (user writes float, we make it a double for accuracy)
                if (token.Value.IndexOfAny(['.', 'e', 'E']) >= 0)
                {
                    left = new LiteralNode { Value = double.Parse(token.Value, CultureInfo.InvariantCulture), Line = token.Line, StartColumn = token.StartColumn, EndColumn = tokens[currentToken - 1].EndColumn };
                } else
                {
                    left = new LiteralNode { Value = int.Parse(token.Value, CultureInfo.InvariantCulture), Line = token.Line, StartColumn = token.StartColumn, EndColumn = tokens[currentToken - 1].EndColumn };
                }
            } else if (token.TokenType == TokenType.String)
            {
                Advance();
                left = new LiteralNode { Value = token.Value, Line = token.Line, StartColumn = token.StartColumn, EndColumn = tokens[currentToken - 1].EndColumn };
            } else if (token.TokenType == TokenType.Identifier)
            {
                Token next = Peek(1);

                // Regular function call: func()
                if (next.TokenType == TokenType.Punctuation && next.Value == "(")
                {
                    Advance(); // consume identifier
                    left = ParseFunctionCall(token.Value, token.Line, token.StartColumn, token.EndColumn);
                } else
                {
                    // Otherwise, just a variable
                    Advance();
                    left = new VariableNode { Name = token.Value, Line = token.Line, StartColumn = token.StartColumn, EndColumn = tokens[currentToken - 1].EndColumn };
                }
            } else if (token.TokenType == TokenType.Boolean)
            {
                Advance();
                left = new LiteralNode { Value = bool.Parse(token.Value), Line = token.Line, StartColumn = token.StartColumn, EndColumn = tokens[currentToken - 1].EndColumn };
            } else
            {
                ReportError($"Unexpected token '{token.Value}'", token.Line, token.StartColumn, CurrentToken().Line, CurrentToken().EndColumn);
                // Return a dummy literal with error flag instead of skipping tokens
                left = new LiteralNode { Value = 0, IsError = true, Line = token.Line, StartColumn = token.StartColumn, EndColumn = token.EndColumn };
            }

            // --- Member Access Suffix (.member or .method()) ---
            while (currentToken < tokens.Count && CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == ".")
            {
                Advance(); // consume '.'
                Token memberToken = Expect(TokenType.Identifier);

                if (currentToken < tokens.Count && CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == "(")
                {
                    // Method call: receiver.method(...)
                    FunctionCallNode call = ParseFunctionCall(memberToken.Value, memberToken.Line, memberToken.StartColumn, memberToken.EndColumn);
                    left = new MemberAccessNode
                    {
                        Receiver = left,
                        MemberName = memberToken.Value,
                        MemberNameStartColumn = memberToken.StartColumn,
                        MemberNameEndColumn = memberToken.EndColumn,
                        Arguments = call.Arguments,
                        Line = left.Line,
                        StartColumn = left.StartColumn,
                        EndColumn = call.EndColumn
                    };
                } else
                {
                    // Property access: receiver.property
                    left = new MemberAccessNode
                    {
                        Receiver = left,
                        MemberName = memberToken.Value,
                        MemberNameStartColumn = memberToken.StartColumn,
                        MemberNameEndColumn = memberToken.EndColumn,
                        Arguments = null,
                        Line = left.Line,
                        StartColumn = left.StartColumn,
                        EndColumn = memberToken.EndColumn
                    };
                }
            }

            return left;
        }

        /// <summary>
        /// Parses an expression from the current token position, respecting operator precedence rules.
        /// </summary>
        /// <remarks>This method advances the current token position as it parses the expression.
        /// Assignment operators ('=') are not allowed and will result in an error and a fallback literal node. The
        /// method is intended for use within the parser's expression parsing logic and assumes that the token list and
        /// current position are valid.</remarks>
        /// <param name="minPrecedence">The minimum operator precedence to consider when parsing the expression. Operators with lower precedence are
        /// not parsed at this level. Defaults to 0.</param>
        /// <returns>An ExpressionNode representing the parsed expression subtree. Returns a LiteralNode with a default value if
        /// parsing fails due to invalid syntax or unexpected end of input.</returns>
        private ExpressionNode ParseExpression(int minPrecedence = 0)
        {
            if (currentToken < tokens.Count &&
                    tokens[currentToken].TokenType == TokenType.Operator &&
                    tokens[currentToken].Value == "=")
            {
                Token tok = CurrentToken();
                ReportError("Assignment is not allowed in this context", tok.Line, tok.StartColumn, tok.Line, tok.StartColumn);
                Advance(); // Skip the assignment operator
                return new LiteralNode { Value = 0, IsError = true, Line = tok.Line, StartColumn = tok.StartColumn, EndColumn = tok.EndColumn };
            }

            if (currentToken >= tokens.Count)
            {
                ReportError("Unexpected end of file while parsing expression", tokens.Count > 0 ? tokens.Last().Line : 0, tokens.Count > 0 ? tokens.Last().StartColumn : 0, tokens.Count > 0 ? tokens.Last().Line : 0, tokens.Count > 0 ? tokens.Last().EndColumn : 0);
                return new LiteralNode { Value = 0, IsError = true, Line = tokens.Count > 0 ? tokens.Last().Line : 0, StartColumn = tokens.Count > 0 ? tokens.Last().StartColumn : 0, EndColumn = tokens.Count > 0 ? tokens.Last().StartColumn : 0 };
            }

            // Parse the left-hand side primary expression
            ExpressionNode left = ParsePrimary(CurrentToken());

            if (currentToken < tokens.Count &&
                tokens[currentToken].TokenType == TokenType.Operator &&
                (tokens[currentToken].Value == "++" || tokens[currentToken].Value == "--"))
            {
                Token op = tokens[currentToken];
                Advance();

                left = new UnaryExpressionNode
                {
                    Operator = op.Value,
                    OperatorColumnStart = op.StartColumn,
                    OperatorColumnEnd = op.EndColumn,
                    Right = left,
                    Line = left.Line,
                    StartColumn = left.StartColumn,
                    EndColumn = left.EndColumn
                };
            }

            while (currentToken < tokens.Count && tokens[currentToken].TokenType == TokenType.Operator)
            {
                Token opToken = tokens[currentToken];

                if (opToken.Value == "=")
                    break;

                if (!LanguageSpecs.Precedence.TryGetValue(opToken.Value, out int prec))
                    break;

                if (prec < minPrecedence)
                    break;

                Advance(); // consume operator token

                int nextMin = prec + 1;

                // parse the right-hand side with the adjusted precedence
                ExpressionNode right = ParseExpression(nextMin);

                // new left-hand side
                left = new BinaryExpressionNode
                {
                    Left = left,
                    Operator = opToken.Value,
                    OperatorColumnStart = opToken.StartColumn,
                    OperatorColumnEnd = opToken.EndColumn,
                    Right = right,
                    Line = left.Line,
                    StartColumn = left.StartColumn,
                    EndColumn = right.EndColumn
                };
            }

            return left;
        }

        private void Advance()
        {
            currentToken++;
        }

        private Token Peek(int offset = 1)
        {

            if (currentToken + offset >= tokens.Count)
            {
                // return EOF token instead of throwing
                return new Token(TokenType.EOF, "", tokens.Count > 0 ? tokens.Last().Line : 0, 0, 0);
            }

            return tokens[currentToken + offset];
        }

        private Token CurrentToken()
        {
            if (currentToken >= tokens.Count)
            {
                // return EOF token instead of throwing
                return new Token(TokenType.EOF, "", tokens.Count > 0 ? tokens.Last().Line : 0, 0, 0);
            }
            return tokens[currentToken];
        }

        /// <summary>
        /// Attempts to match the current token against the specified token type and optional value, advancing to the
        /// next token if successful.
        /// </summary>
        /// <param name="type">The expected type of the current token to match.</param>
        /// <param name="value">The expected value of the current token to match, or null to match any value.</param>
        /// <returns>true if the current token matches the specified type and value; otherwise, false.</returns>
        private bool Match(TokenType type, string? value = null)
        {
            if (currentToken >= tokens.Count)
                return false;
            Token t = tokens[currentToken];
            if (t.TokenType != type)
                return false;
            if (value != null && t.Value != value)
                return false;
            Advance();
            return true;
        }

        /// <summary>
        /// Retrieves the next token if it matches the specified type and, optionally, value; otherwise, reports an
        /// error and returns a fallback token to allow parsing to continue.
        /// </summary>
        /// <remarks>If the next token does not match the specified criteria, an error is reported and a
        /// fallback token is returned to enable error recovery during parsing. This method allows the parser to
        /// continue processing input after encountering unexpected tokens.</remarks>
        /// <param name="type">The expected type of the token to match.</param>
        /// <param name="value">The expected value of the token to match, or null to match any value.</param>
        /// <returns>The matched token if the next token matches the specified type and value; otherwise, a fallback token
        /// representing the current position.</returns>
        private Token Expect(TokenType type, string? value = null)
        {
            if (!Match(type, value))
            {
                Token tok = currentToken < tokens.Count ? tokens[currentToken] : new Token(TokenType.EOF, "", 0, 0, 0);
                ReportError($"Expected {type}{(value != null ? $" '{value}'" : "")} at token {currentToken}, {CurrentToken().Value} {CurrentToken().TokenType}", tok.Line, tok.StartColumn, tok.Line, tok.EndColumn);

                // return a best-effort token so parsing can continue
                return tok;
            }
            return tokens[currentToken - 1];
        }
    }

    /// <summary>
    /// Represents a node in an expression tree, such as a value or an operation.
    /// </summary>
    /// <remarks>ExpressionNode instances are typically used to model the structure of parsed expressions,
    /// including literals, variables, and operators. This class serves as a base for more specific expression node
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

    class BinaryExpressionNode : ExpressionNode // for binary operations (+, -, *, /)
    {
        public required ExpressionNode Left { get; set; }
        public required string Operator { get; set; }
        public int OperatorColumnStart { get; set; }
        public int OperatorColumnEnd { get; set; }
        public required ExpressionNode Right { get; set; }
    }

    class LiteralNode : ExpressionNode // numbers, strings, booleans
    {
        public required object Value { get; set; }
        public bool IsError { get; set; } = false;
    }

    class VariableNode : ExpressionNode // identifiers
    {
        public int SlotIndex;
        public required string Name { get; set; }
    }

    /// <summary>
    /// Represents a statement in the abstract syntax tree (AST) of the programming language.
    /// </summary>
    public abstract class StatementNode // base class for statements
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

    class ExpressionStatementNode : StatementNode // e.g., x + 1;
    {
        public required ExpressionNode Expression { get; set; }
    }

    class VariableDeclarationNode : StatementNode
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

    class IfStatementNode : StatementNode
    {
        public required ExpressionNode Condition { get; set; }
        public required List<StatementNode> Body { get; set; }
        public List<StatementNode>? ElseBody { get; set; }
        public int ElseLine { get; set; }
        public int ElseStartColumn { get; set; }
    }

    class WhileStatementNode : StatementNode
    {
        public required int KeywordColumnStart { get; set; }
        public required int KeywordColumnEnd { get; set; }
        public required ExpressionNode Condition { get; set; }
        public required List<StatementNode> Body { get; set; }
    }

    class ForStatementNode : StatementNode
    {
        public required int KeywordColumnStart { get; set; }
        public required int KeywordColumnEnd { get; set; }
        public StatementNode? Initialization { get; set; }
        public ExpressionNode? Condition { get; set; }
        public StatementNode? Increment { get; set; }
        public required List<StatementNode> Body { get; set; }
    }

    class ReturnStatementNode : StatementNode
    {
        public required int KeywordColumnStart { get; set; }
        public required int KeywordColumnEnd { get; set; }
        public required ExpressionNode? Expression { get; set; } = null;
    }

    class FunctionCallNode : ExpressionNode
    {
        public required string FunctionName { get; set; }
        public required int FunctionNameStartColumn { get; set; }
        public required int FunctionNameEndColumn { get; set; }
        public required List<ExpressionNode> Arguments { get; set; } = [];
        public string? ReturnType { get; set; } = "unknown";
    }

    class MemberAccessNode : ExpressionNode
    {
        public required ExpressionNode Receiver { get; set; }
        public required string MemberName { get; set; }
        public int MemberNameStartColumn { get; set; }
        public int MemberNameEndColumn { get; set; }
        public List<ExpressionNode>? Arguments { get; set; } = null;
        public bool IsModuleCall { get; set; } = false;
        public string ReturnType { get; set; } = "auto";
    }

    class FunctionDefinitionNode : StatementNode
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

    class UnaryExpressionNode : ExpressionNode
    {
        public required string Operator { get; set; }
        public required int OperatorColumnStart { get; set; }
        public required int OperatorColumnEnd { get; set; }
        public required ExpressionNode Right { get; set; }
    }

    class AssignmentStatementNode : StatementNode
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

    class IncrementStatementNode : StatementNode
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
