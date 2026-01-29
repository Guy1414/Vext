using System.Globalization;
using Vext.Compiler.Shared;
using Vext.Lexer;

namespace Vext.Compiler.Parsing
{
    internal class Parser(List<Token> tokens)
    {
        private readonly List<Token> tokens = tokens;
        private int currentToken = 0;
        private readonly List<(string, int, int)> _errors = [];

        private void ReportError(string message, int line, int column)
        {
            _errors.Add(($"Line {line}, Column {column}: {message}", line, column));
        }

        /// <summary>
        /// Parses the input token stream and returns a list of statement nodes representing the parsed statements.
        /// </summary>
        /// <remarks>If a parsing error occurs, the method attempts to recover and continue parsing
        /// subsequent statements. Errors encountered during parsing are reported and do not cause the method to throw
        /// exceptions under normal circumstances.</remarks>
        /// <returns>A list of <see cref="StatementNode"/> objects corresponding to the successfully parsed statements. The list
        /// may be empty if no valid statements are found.</returns>
        public (List<StatementNode>, List<(string, int, int)>) Parse()
        {
            _errors.Clear();
            var statements = new List<StatementNode>();
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
                    var tok = currentToken < tokens.Count ? tokens[currentToken] : new Token(TokenType.EOF, "", 0, 0);
                    ReportError(ex.Message, tok.Line, tok.Column);
                }

                if (stmt != null)
                {
                    statements.Add(stmt);
                } else
                {
                    var tok = CurrentToken();
                    ReportError($"Unexpected token '{tok.Value}' at position {currentToken}", tok.Line, tok.Column);
                    // attempt to recover by skipping one token
                    if (currentToken < tokens.Count)
                        Advance();
                }
            }
            return (statements, _errors);
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
        private StatementNode? ParseStatement(bool expect = true)
        {
            Token token = CurrentToken();
            if (token.TokenType == TokenType.Unknown)
            {
                ReportError($"Unknown token '{token.Value}'", token.Line, token.Column);
                Advance();
                return null;
            } else if (token.TokenType == TokenType.Keyword)
            {
                if (LanguageSpecs.ReturnTypes.Contains(token.Value))
                {
                    if (Peek().TokenType == TokenType.Identifier && Peek(2).TokenType == TokenType.Punctuation && Peek(2).Value == "(")
                    {
                        var func = ParseFunctionDeclaration();
                        if (func != null)
                            return func;
                    } else if (Peek().TokenType == TokenType.Identifier)
                    {
                        if ((Peek(2).TokenType == TokenType.Operator && Peek(2).Value == "=") ||
                            (Peek(2).TokenType == TokenType.Punctuation && Peek(2).Value == ";"))

                        {
                            var decl = ParseVariableDeclaration();
                            Expect(TokenType.Punctuation, ";");
                            if (decl != null)
                                return decl;
                        } else
                        {
                            ReportError("Invalid variable declaration", token.Line, token.Column);
                            // attempt to recover by skipping to next semicolon or newline
                            while (currentToken < tokens.Count && !(tokens[currentToken].TokenType == TokenType.Punctuation && tokens[currentToken].Value == ";"))
                                Advance();

                            return null;
                        }
                    } else
                    {
                        ReportError("Invalid statement", token.Line, token.Column);
                        Advance();
                        return null;
                    }
                } else if (token.Value == "if")
                {
                    var stmt = ParseIfStatement();
                    if (stmt != null)
                        return stmt;
                } else if (token.Value == "while")
                {
                    var stmt = ParseWhileStatement();
                    if (stmt != null)
                        return stmt;
                } else if (token.Value == "for")
                {
                    var stmt = ParseForStatement();
                    if (stmt != null)
                        return stmt;
                } else if (token.Value == "return")
                {
                    var rtrn = ParseReturn();
                    Expect(TokenType.Punctuation, ";");
                    if (rtrn != null)
                        return rtrn;

                } else
                {
                    ReportError($"Unexpected keyword '{token.Value}'", token.Line, token.Column);
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
                    if (expect)
                        Expect(TokenType.Punctuation, ";");

                    return new IncrementStatementNode
                    {
                        VariableName = name.Value,
                        IsIncrement = op.Value == "++",
                        Line = name.Line,
                        Column = name.Column
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
                    if (expect)
                        Expect(TokenType.Punctuation, ";");

                    return new AssignmentStatementNode
                    {
                        VariableName = name.Value,
                        Operator = op.Value,
                        Value = value,
                        Line = name.Line,
                        Column = name.Column
                    };
                }

                // Otherwise normal expression statement
                ExpressionNode expr = ParseExpression();

                if (expr is not FunctionCallNode)
                {
                    ReportError("Only function calls can be used as expression statements", token.Line, token.Column);
                    // try to recover to semicolon
                    while (currentToken < tokens.Count && !(tokens[currentToken].TokenType == TokenType.Punctuation && tokens[currentToken].Value == ";"))
                        Advance();
                    Expect(TokenType.Punctuation, ";");
                    return null;
                }
                if (expect)
                    Expect(TokenType.Punctuation, ";");

                return new ExpressionStatementNode
                {
                    Expression = expr,
                    Line = token.Line,
                    Column = token.Column
                };
            }

            return null;
        }

        private FunctionDefinitionNode? ParseFunctionDeclaration()
        {
            Token returnType = Expect(TokenType.Keyword);
            Token name = Expect(TokenType.Identifier);

            Expect(TokenType.Punctuation, "("); // consume '('
            var arguments = new List<FunctionParameterNode>();

            // handle empty calls like func()
            var tok = CurrentToken();
            if (!(tok.TokenType == TokenType.Punctuation && tok.Value == ")"))
            {
                while (true)
                {
                    var param = ParseFunctionParameter();
                    if (param == null)
                    {
                        ReportError("Invalid parameter declaration", name.Line, name.Column);
                        // attempt to skip to closing ')'
                        while (currentToken < tokens.Count && !(tokens[currentToken].TokenType == TokenType.Punctuation && tokens[currentToken].Value == ")"))
                            Advance();
                        break;
                    }
                    arguments.Add(param);
                    if (!Match(TokenType.Punctuation, ","))
                        break; // continue if there's a comma
                }
            }
            Expect(TokenType.Punctuation, ")"); // consume ')'

            Expect(TokenType.Punctuation, "{"); // consume opening brace
            var body = new List<StatementNode>();
            while (!(CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == "}"))
            {
                var statement = ParseStatement();
                if (statement == null)
                {
                    // try to recover: skip until '}' or ';'
                    if (currentToken < tokens.Count && tokens[currentToken].TokenType == TokenType.Punctuation && tokens[currentToken].Value == "}")
                        break;
                    while (currentToken < tokens.Count && !(tokens[currentToken].TokenType == TokenType.Punctuation && (tokens[currentToken].Value == ";" || tokens[currentToken].Value == "}")))
                        Advance();
                    if (currentToken < tokens.Count && tokens[currentToken].TokenType == TokenType.Punctuation && tokens[currentToken].Value == ";")
                        Advance();
                    continue;
                }
                body.Add(statement);
            }
            Expect(TokenType.Punctuation, "}"); // consume closing brace

            return new FunctionDefinitionNode { ReturnType = returnType.Value, FunctionName = name.Value, Arguments = arguments, Body = body, Line = returnType.Line, Column = returnType.Column };
        }

        private FunctionParameterNode? ParseFunctionParameter()
        {
            Token type = Expect(TokenType.Keyword);
            Token name = Expect(TokenType.Identifier);

            ExpressionNode? initializer = null;
            if (Match(TokenType.Operator, "="))
            {
                initializer = ParseExpression();
            }
            return new FunctionParameterNode
            {
                Type = type.Value,
                Name = name.Value,
                Initializer = initializer
            };
        }

        private FunctionCallNode ParseFunctionCall(string name, int line, int column)
        {
            // current token should be '(' when this is called (caller ensures that)
            Expect(TokenType.Punctuation, "("); // consume '('
            var arguments = new List<ExpressionNode>();

            // handle empty calls like func()
            var tok = CurrentToken();
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
            return new FunctionCallNode { FunctionName = name, Arguments = arguments, Line = line, Column = column };
        }

        private ReturnStatementNode? ParseReturn()
        {
            Token rtrnToken = Expect(TokenType.Keyword, "return");
            if (CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == ";")
            {
                return new ReturnStatementNode { Expression = null, Line = rtrnToken.Line, Column = rtrnToken.Column };
            } else
            {
                ExpressionNode expr = ParseExpression();
                return new ReturnStatementNode { Expression = expr, Line = rtrnToken.Line, Column = rtrnToken.Column };
            }
        }

        private IfStatementNode? ParseIfStatement()
        {
            Token ifToken = Expect(TokenType.Keyword, "if");
            Expect(TokenType.Punctuation, "(");
            var expr = ParseExpression(0);
            Expect(TokenType.Punctuation, ")");

            var body = new List<StatementNode>();

            if (Match(TokenType.Punctuation, "{"))
            {
                while (!(CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == "}"))
                {
                    var statement = ParseStatement();
                    if (statement == null)
                    {
                        // recover
                        while (currentToken < tokens.Count && !(tokens[currentToken].TokenType == TokenType.Punctuation && (tokens[currentToken].Value == ";" || tokens[currentToken].Value == "}")))
                            Advance();
                        if (currentToken < tokens.Count && tokens[currentToken].TokenType == TokenType.Punctuation && tokens[currentToken].Value == ";")
                            Advance();
                        continue;
                    }
                    body.Add(statement);
                }
                Expect(TokenType.Punctuation, "}");
            } else
            {
                // single statement if
                var stmt = ParseStatement();
                if (stmt == null)
                {
                    ReportError("Invalid single-line if body", ifToken.Line, ifToken.Column);
                } else
                    body.Add(stmt);
            }

            List<StatementNode>? elseBody = null;
            if (Match(TokenType.Keyword, "else"))
            {
                elseBody = [];
                if (Match(TokenType.Punctuation, "{"))
                {
                    while (!(CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == "}"))
                    {
                        var statement = ParseStatement();
                        if (statement == null)
                        {
                            // recover
                            while (currentToken < tokens.Count && !(tokens[currentToken].TokenType == TokenType.Punctuation && (tokens[currentToken].Value == ";" || tokens[currentToken].Value == "}")))
                                Advance();
                            if (currentToken < tokens.Count && tokens[currentToken].TokenType == TokenType.Punctuation && tokens[currentToken].Value == ";")
                                Advance();
                            continue;
                        }
                        elseBody.Add(statement);
                    }
                    Expect(TokenType.Punctuation, "}");
                } else
                {
                    // single statement else
                    var stmt = ParseStatement();
                    if (stmt == null)
                    {
                        ReportError("Invalid single-line else body", ifToken.Line, ifToken.Column);
                    } else
                        elseBody.Add(stmt);
                }
            }

            return new IfStatementNode
            {
                Condition = expr,
                Body = body,
                ElseBody = elseBody,
                Line = ifToken.Line,
                Column = ifToken.Column
            };
        }

        private ForStatementNode? ParseForStatement()
        {
            Token forToken = Expect(TokenType.Keyword, "for");
            Expect(TokenType.Punctuation, "(");
            StatementNode? initialization = null;
            if (!(CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == ";"))
            {
                //initialization = ParseStatement();
                if (LanguageSpecs.ReturnTypes.Contains(CurrentToken().Value))
                {
                    initialization = ParseVariableDeclaration();
                } else
                {
                    var expr = ParseExpression();
                    initialization = new ExpressionStatementNode
                    {
                        Expression = expr,
                        Line = expr.Line,
                        Column = expr.Column
                    };

                }
                if (!(initialization is VariableDeclarationNode || initialization is ExpressionStatementNode))
                {
                    ReportError("For-loop initialization must be a variable declaration or expression statement", forToken.Line, forToken.Column);
                }
                if (initialization == null)
                {
                    ReportError("Invalid for-loop initialization", forToken.Line, forToken.Column);
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
                var expr = ParseExpression();

                increment = new ExpressionStatementNode
                {
                    Expression = expr,
                    Line = expr.Line,
                    Column = expr.Column
                };
            }

            Expect(TokenType.Punctuation, ")");

            var body = new List<StatementNode>();
            if (Match(TokenType.Punctuation, "{"))
            {
                while (!(CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == "}"))
                {
                    var stmt = ParseStatement();
                    if (stmt == null)
                    {
                        // recover
                        while (currentToken < tokens.Count && !(tokens[currentToken].TokenType == TokenType.Punctuation && (tokens[currentToken].Value == ";" || tokens[currentToken].Value == "}")))
                            Advance();
                        if (currentToken < tokens.Count && tokens[currentToken].TokenType == TokenType.Punctuation && tokens[currentToken].Value == ";")
                            Advance();
                        continue;
                    }
                    body.Add(stmt);
                }
                Expect(TokenType.Punctuation, "}");
            } else
            {
                // single statement body
                var stmt = ParseStatement();
                if (stmt == null)
                {
                    ReportError("Invalid single-line for body", forToken.Line, forToken.Column);
                } else
                {
                    body.Add(stmt);
                }
            }

            return new ForStatementNode
            {
                Initialization = initialization!,
                Condition = condition!,
                Increment = increment!,
                Body = body,
                Line = forToken.Line,
                Column = forToken.Column
            };
        }

        private WhileStatementNode? ParseWhileStatement()
        {
            Token whileToken = Expect(TokenType.Keyword, "while");
            Expect(TokenType.Punctuation, "(");
            var expr = ParseExpression(0);
            Expect(TokenType.Punctuation, ")");

            var body = new List<StatementNode>();

            if (Match(TokenType.Punctuation, "{"))
            {
                while (!(CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == "}"))
                {
                    var statement = ParseStatement();
                    if (statement == null)
                    {
                        // recover
                        while (currentToken < tokens.Count && !(tokens[currentToken].TokenType == TokenType.Punctuation && (tokens[currentToken].Value == ";" || tokens[currentToken].Value == "}")))
                            Advance();
                        if (currentToken < tokens.Count && tokens[currentToken].TokenType == TokenType.Punctuation && tokens[currentToken].Value == ";")
                            Advance();
                        continue;
                    }
                    body.Add(statement);
                }
                Expect(TokenType.Punctuation, "}");
            } else
            {
                // single statement else
                var stmt = ParseStatement();
                if (stmt == null)
                {
                    ReportError("Invalid single-line while body", whileToken.Line, whileToken.Column);
                } else
                    body.Add(stmt);
            }

            return new WhileStatementNode
            {
                Condition = expr,
                Body = body,
                Line = whileToken.Line,
                Column = whileToken.Column
            };
        }

        private VariableDeclarationNode? ParseVariableDeclaration()
        {
            Token type = Expect(TokenType.Keyword);
            Token name = Expect(TokenType.Identifier);

            if (name.Value == "void")
            {
                ReportError("Variable cannot be of type 'void'", type.Line, type.Column);
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
                Name = name.Value,
                Initializer = initializer,
                Line = type.Line,
                Column = type.Column
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
            if (token.TokenType == TokenType.Operator && (token.Value == "-" || token.Value == "!"))
            {
                Advance(); // consume '-' or '!'
                var right = ParsePrimary(CurrentToken());
                return new UnaryExpressionNode { Operator = token.Value, Right = right, Line = token.Line, Column = token.Column };
            }

            if (token.TokenType == TokenType.Punctuation && token.Value == "(")
            {
                Advance(); // consume '('
                ExpressionNode expr = ParseExpression();
                Expect(TokenType.Punctuation, ")");
                return expr;
            } else if (token.TokenType == TokenType.Numeric)
            {
                Advance();
                // support both integer and floating point (user writes float, we make it a double for accuracy)
                if (token.Value.IndexOfAny(['.', 'e', 'E']) >= 0)
                {
                    return new LiteralNode { Value = double.Parse(token.Value, CultureInfo.InvariantCulture), Line = token.Line, Column = token.Column };
                } else
                {
                    return new LiteralNode { Value = int.Parse(token.Value, CultureInfo.InvariantCulture), Line = token.Line, Column = token.Column };
                }
            } else if (token.TokenType == TokenType.String)
            {
                Advance();
                return new LiteralNode { Value = token.Value, Line = token.Line, Column = token.Column };
            } else if (token.TokenType == TokenType.Identifier)
            {
                Token next = Peek(1);

                // Module function call: moduleName.func()
                if (next.TokenType == TokenType.Punctuation && next.Value == ".")
                {
                    string moduleName = token.Value;
                    Advance(); // consume module name
                    Expect(TokenType.Punctuation, ".");
                    Token funcToken = Expect(TokenType.Identifier);

                    // Expect '(' after function name
                    Expect(TokenType.Punctuation, "(");
                    var arguments = new List<ExpressionNode>();
                    if (!(CurrentToken().TokenType == TokenType.Punctuation && CurrentToken().Value == ")"))
                    {
                        while (true)
                        {
                            arguments.Add(ParseExpression());
                            if (!Match(TokenType.Punctuation, ","))
                                break;
                        }
                    }
                    Expect(TokenType.Punctuation, ")");

                    return new ModuleAccessNode
                    {
                        ModuleName = moduleName,
                        FunctionName = funcToken.Value,
                        Arguments = arguments,
                        Line = token.Line,
                        Column = token.Column
                    };
                }
                // Regular function call: func()
                else if (next.TokenType == TokenType.Punctuation && next.Value == "(")
                {
                    Advance(); // consume identifier
                    return ParseFunctionCall(token.Value, token.Line, token.Column);
                }

                // Otherwise, just a variable
                Advance();
                return new VariableNode { Name = token.Value, Line = token.Line, Column = token.Column };
            } else if (token.TokenType == TokenType.Boolean)
            {
                Advance();
                return new LiteralNode { Value = bool.Parse(token.Value), Line = token.Line, Column = token.Column };
            } else
            {
                ReportError($"Unexpected token '{token.Value}'", token.Line, token.Column);
                // attempt to recover by advancing once and returning a dummy literal
                if (currentToken < tokens.Count)
                    Advance();
                return new LiteralNode { Value = 0, Line = token.Line, Column = token.Column };
            }
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
                ReportError("Assignment is not allowed in this context", CurrentToken().Line, CurrentToken().Column);
                var badToken = CurrentToken();
                // consume the problematic token to avoid infinite loop
                if (currentToken < tokens.Count)
                    Advance();
                return new LiteralNode { Value = 0, Line = badToken.Line, Column = badToken.Column };
            }

            if (currentToken >= tokens.Count)
            {
                ReportError("Unexpected end of file while parsing expression", tokens.Count > 0 ? tokens.Last().Line : 0, tokens.Count > 0 ? tokens.Last().Column : 0);
                return new LiteralNode { Value = 0, Line = tokens.Count > 0 ? tokens.Last().Line : 0, Column = tokens.Count > 0 ? tokens.Last().Column : 0 };
            }

            // Parse the left-hand side primary expression
            ExpressionNode left = ParsePrimary(CurrentToken());

            if (currentToken < tokens.Count &&
                tokens[currentToken].TokenType == TokenType.Operator &&
                (tokens[currentToken].Value == "++" || tokens[currentToken].Value == "--"))
            {
                var op = tokens[currentToken];
                Advance();

                left = new UnaryExpressionNode
                {
                    Operator = op.Value,
                    Right = left,
                    Line = op.Line,
                    Column = op.Column
                };
            }

            while (currentToken < tokens.Count && tokens[currentToken].TokenType == TokenType.Operator)
            {
                var opToken = tokens[currentToken];

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
                    Right = right,
                    Line = opToken.Line,
                    Column = opToken.Column
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
                return new Token(TokenType.EOF, "", tokens.Count > 0 ? tokens.Last().Line : 0, 0);
            }

            return tokens[currentToken + offset];
        }

        private Token CurrentToken()
        {
            if (currentToken >= tokens.Count)
            {
                // return EOF token instead of throwing
                return new Token(TokenType.EOF, "", tokens.Count > 0 ? tokens.Last().Line : 0, 0);
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
            var t = tokens[currentToken];
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
                var tok = currentToken < tokens.Count ? tokens[currentToken] : new Token(TokenType.EOF, "", 0, 0);
                ReportError($"Expected {type}{(value != null ? $" '{value}'" : "")} at token {currentToken}, {CurrentToken().Value} {CurrentToken().TokenType}", tok.Line, tok.Column);
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
        public int Column { get; set; }
    }

    class BinaryExpressionNode : ExpressionNode // for binary operations (+, -, *, /)
    {
        public required ExpressionNode Left { get; set; }
        public required string Operator { get; set; }
        public required ExpressionNode Right { get; set; }
    }

    class LiteralNode : ExpressionNode // numbers, strings, booleans
    {
        public required object Value { get; set; }
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
        public int Column { get; set; }
    }

    class ExpressionStatementNode : StatementNode // e.g., x + 1;
    {
        public required ExpressionNode Expression { get; set; }
    }

    class VariableDeclarationNode : StatementNode
    {
        public int SlotIndex;
        public required string VariableType { get; set; }
        public required string Name { get; set; }
        public ExpressionNode? Initializer { get; set; } // optional
    }

    class IfStatementNode : StatementNode
    {
        public required ExpressionNode Condition { get; set; }
        public required List<StatementNode> Body { get; set; }
        public List<StatementNode>? ElseBody { get; set; }
    }

    class WhileStatementNode : StatementNode
    {
        public required ExpressionNode Condition { get; set; }
        public required List<StatementNode> Body { get; set; }
    }

    class ForStatementNode : StatementNode
    {
        public StatementNode? Initialization { get; set; }
        public ExpressionNode? Condition { get; set; }
        public StatementNode? Increment { get; set; }
        public required List<StatementNode> Body { get; set; }
    }

    class ReturnStatementNode : StatementNode
    {
        public required ExpressionNode? Expression { get; set; } = null;
    }

    class FunctionCallNode : ExpressionNode
    {
        public required string FunctionName { get; set; }
        public required List<ExpressionNode> Arguments { get; set; } = [];
        public string? ReturnType { get; set; } = "unknown";
    }

    class ModuleAccessNode : FunctionCallNode
    {
        public required string ModuleName { get; set; }
    }

    class FunctionDefinitionNode : StatementNode
    {
        public required string ReturnType { get; set; }
        public required string FunctionName { get; set; }
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
        /// The name of the parameter.
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// An optional initializer expression for the parameter.
        /// </summary>
        public ExpressionNode? Initializer { get; set; } // optional
    }

    class UnaryExpressionNode : ExpressionNode
    {
        public required string Operator { get; set; }
        public required ExpressionNode Right { get; set; }
    }

    class AssignmentStatementNode : StatementNode
    {
        public int SlotIndex;
        public required string VariableName { get; set; }
        public required string Operator { get; set; }
        public required ExpressionNode Value { get; set; }
    }

    class IncrementStatementNode : StatementNode
    {
        public int SlotIndex;
        public required string VariableName { get; set; }
        public bool IsIncrement { get; set; }
    }
}
