// S-expression parser

namespace BaD;

//language does not support lists, so all expressions are s-expressions

public class Parser 
{
    
    public class ParserException : Exception
    {
        public ParserException(string message, StreamPosition position) : base(message + " at " + position.ToString()) { }
    }


    private readonly Lexer lexer;
    public Parser(Lexer lexer)
    {
        this.lexer = lexer;
    }

    public bool IsEof { get; set;} = false;

    public SExpression? Parse()
    {
        // peek next token for eof
        if(lexer.Peek().Type == TokenType.Eof)
        {
            IsEof = true;
            return null;
        }
        return ParseSExpression();
    }

    private SExpression? ParseSExpression()
    {
        // all non single token expressions start with a left paren, we need to check for that

        Token token = lexer.Peek();
        // test out the token type

        switch (token.Type)
        {
            // these types CANNOT be the start of an s-expression
            case TokenType.Literal:
                return ParseLiteral();
            case TokenType.Identifier:
                return ParseIdentifier();
            case TokenType.Eof:
                return null;
            case TokenType.LeftParen:
                // consume left paren
                lexer.NextToken();

                // should this be peek or next? it for some reason is next but I think it should be peek, yet it works
                token = lexer.NextToken();
                switch (token.Type)
                {
                    case TokenType.AssignPtr:
                        return ParseAssignPtr(token);
                    case TokenType.Ampersand:
                        return ParseAddressOf(token);
                    case TokenType.Block:
                        return ParseBlock(token);
                    case TokenType.Return:
                        return ParseReturn(token);
                    case TokenType.If:
                        return ParseIf(token);
                    case TokenType.Extern:
                        return ParseExtern(token);
                    case TokenType.Func:
                        return ParseFunction(token);
                    case TokenType.Call:
                        return ParseFunctionCall(token);
                    case TokenType.Not:
                        return ParseNot(token);
                    case TokenType.Plus:
                    case TokenType.Minus:
                    case TokenType.Multiply:
                    case TokenType.Divide:
                        return ParseArithmetic(token);
                    case TokenType.LessThan:
                    case TokenType.GreaterThan:
                    case TokenType.LessThanOrEqual:
                    case TokenType.GreaterThanOrEqual:
                    case TokenType.Equal:
                    case TokenType.NotEqual:
                        return ParseComparison(token);
                    case TokenType.And:
                    case TokenType.Or:
                        return ParseLogical(token);
                    case TokenType.VarDec:
                        return ParseVarDec(token);
                    case TokenType.Assign:
                        return ParseAssign(token);
                    case TokenType.AtSymbol:
                        return ParseDereference(token);
                    case TokenType.While:
                        return ParseWhile(token);
                    case TokenType.SizeOf:
                        return ParseSizeOf(token);
                    case TokenType.Index:
                        return ParseIndex(token);
                    case TokenType.Array:
                        return ParseList(token);
                    case TokenType.Cast:
                        return ParseCast(token);
                    default:
                        throw new ParserException("Unexpected token: " + token + " inside statement", token.Position);
                }
            default:
                throw new ParserException("Unexpected token: " + token.Type.ToString() + " outside of statement ", token.Position);
        }
    }

    private SExpression ParseCast(Token startToken)
    {
        Type type = ParseType();
        SExpression? expression = ParseSExpression();
        CheckNextToken(TokenType.RightParen);
        return new CastExpression(type, expression!, startToken.Position);
    }

    private SExpression ParseAssignPtr(Token startToken)
    {
        // (assignptr ptr expr) assign the value of expr to the memory pointed to by ptr
        SExpression? ptr = ParseSExpression();
        SExpression? expr = ParseSExpression();
        CheckNextToken(TokenType.RightParen);
        return new AssignPtrExpression(ptr!, expr!, startToken.Position);
    }

    private SExpression ParseList(Token startToken)
    {
        List<SExpression> expressions = new();
        while (lexer.Peek().Type != TokenType.RightParen)
        {
            expressions.Add(ParseSExpression()!);
        }
        CheckNextToken(TokenType.RightParen);
        return new ListExpression(expressions.ToArray(), startToken.Position);
    }

    private SExpression ParseWhile(Token startToken)
    {
        SExpression? condition = ParseSExpression();
        SExpression? body = ParseSExpression();
        CheckNextToken(TokenType.RightParen);
        return new WhileExpression(condition!, body!, startToken.Position);
    }

    // expressions with a single token
    private SExpression ParseLiteral()
    {
        Token token = lexer.NextToken();
        return token switch
        {
            IntLiteral intLiteral => new IntLiteralExpression(intLiteral.IntValue, intLiteral.Position),
            CharLiteral charLiteral => new CharLiteralExpression(charLiteral.CharValue, charLiteral.Position),
            BoolLiteral boolLiteral => new BoolLiteralExpression(boolLiteral.BoolValue, boolLiteral.Position),
            StringLiteral stringLiteral => new StringLiteralExpression(stringLiteral.StringValue, stringLiteral.Position),
            _ => throw new ParserException("Unexpected token: " + token, token.Position)
        };
    }

    private SExpression ParseDereference(Token startToken)
    {
        SExpression? expression = ParseSExpression();
        CheckNextToken(TokenType.RightParen);
        return new DereferenceExpression(expression!, startToken.Position);
    }

    private SExpression ParseIdentifier()
    {
        Token token = lexer.NextToken();
        return new IdentifierExpression(token.Value, token.Position);
    }

    private Type ParseType()
    {
        Token token = lexer.NextToken();
        // int 
        if (token.Type == TokenType.Int)
        {
            return new PrimitiveType(PrimitiveTypes.Int);
        }
        // char
        else if (token.Type == TokenType.Char)
        {
            return new PrimitiveType(PrimitiveTypes.Char);
        }
        // bool
        else if (token.Type == TokenType.Bool)
        {
            return new PrimitiveType(PrimitiveTypes.Bool);
        }
        // void
        else if (token.Type == TokenType.Void)
        {
            return new PrimitiveType(PrimitiveTypes.Void);
        }
        else if(token.Type == TokenType.Int16)
        {
            return new PrimitiveType(PrimitiveTypes.Int16);
        }
        else if(token.Type == TokenType.Int32)
        {
            return new PrimitiveType(PrimitiveTypes.Int32);
        }
        else if(token.Type == TokenType.UInt)
        {
            return new PrimitiveType(PrimitiveTypes.UInt);
        }
        else if(token.Type == TokenType.UInt16)
        {
            return new PrimitiveType(PrimitiveTypes.UInt16);
        }
        else if(token.Type == TokenType.UInt32)
        {
            return new PrimitiveType(PrimitiveTypes.UInt32);
        }
        else if(token.Type == TokenType.UChar)
        {
            return new PrimitiveType(PrimitiveTypes.UChar);
        }
        else if(token.Type == TokenType.LeftParen) // pointer    
        {
            token = lexer.NextToken();
            if(token.Type == TokenType.Multiply)
            {
                Type innerType = ParseType();
                // consume/assert right paren
                CheckNextToken(TokenType.RightParen);
                return new PointerType(innerType);
            }
            else if (token.Type == TokenType.Func) // function pointer
            {
                // func format is (func (types) type)
                List<Type> parameterTypes = new List<Type>();

                // check next token is paren
                CheckNextToken(TokenType.LeftParen);

                // parse parameter types
                while (lexer.Peek().Type != TokenType.RightParen)
                {
                    parameterTypes.Add(ParseType());
                    Console.WriteLine("Parameter type: " + parameterTypes.Last().ToString());
                }

                // consume right paren
                CheckNextToken(TokenType.RightParen);
                
                Console.WriteLine("Parameter types: " + string.Join(", ", parameterTypes.Select(t => t.ToString())));

                // parse return type
                Type returnType = ParseType();

                // consume right paren
                CheckNextToken(TokenType.RightParen);

                // print out next token

                return new FunctionType(returnType, parameterTypes);
            }

        }
        throw new ParserException("Unexpected token: " + token + " expected type", token.Position);        
    }

    private void CheckNextToken(TokenType expectedType)
    {
        Token token = lexer.NextToken();
        if (token.Type != expectedType)
        {
            throw new ParserException("Unexpected token: " + token.Value + " expected " + expectedType, token.Position);
        }
    }

    private SExpression ParseAddressOf(Token startToken)
    {
        // check for the identifier
        Token token = lexer.NextToken();
        if (token.Type != TokenType.Identifier)
        {
            throw new ParserException("Expected identifier", token.Position);
        }
        SExpression expression = new AddressOfExpression(token.Value, token.Position);
        CheckNextToken(TokenType.RightParen);
        return expression;
    }

    private SExpression ParseFunctionCall(Token startToken)
    {
        // function name on call is an expression
        SExpression? function = ParseSExpression();

        // parse arguments
        List<SExpression> arguments = new();

        while (lexer.Peek().Type != TokenType.RightParen)
        {
            arguments.Add(ParseSExpression()!);
        }
        CheckNextToken(TokenType.RightParen);

        return new FunctionCallExpression(function!, arguments.ToArray(), startToken.Position);
    }

    private SExpression ParseNot(Token startToken)
    {
        SExpression? expression = ParseSExpression();
        CheckNextToken(TokenType.RightParen);
        return new NotExpression(expression!, startToken.Position);
    }

    private SExpression ParseArithmetic(Token op)
    {
        SExpression? left = ParseSExpression();
        SExpression? right = ParseSExpression();
        CheckNextToken(TokenType.RightParen);
        return new ArithmeticExpression(op, left!, right!, op.Position);
    }

    private SExpression ParseComparison(Token op)
    {
        SExpression? left = ParseSExpression();
        SExpression? right = ParseSExpression();
        CheckNextToken(TokenType.RightParen);
        return new ComparisonExpression(op, left!, right!, op.Position);
    }

    private SExpression ParseLogical(Token op)
    {
        SExpression? left = ParseSExpression();
        SExpression? right = ParseSExpression();
        CheckNextToken(TokenType.RightParen);
        return new LogicalExpression(op, left!, right!, op.Position);
    }

    // (func name ((type name) (type name)) type body)
    public SExpression ParseFunction(Token startToken)
    {
        // (func already consumed
        Token token = lexer.NextToken();
        if (token.Type != TokenType.Identifier)
        {
            throw new ParserException("Expected identifier", token.Position);
        }

        string name = token.Value;

        // parse parameters
        List<TypedIdentifier> parameters = new List<TypedIdentifier>();
        CheckNextToken(TokenType.LeftParen);

        while(lexer.Peek().Type != TokenType.RightParen)
        {
            // we have a parameter, so consume left paren
            CheckNextToken(TokenType.LeftParen);
            Type type = ParseType();
            token = lexer.NextToken();
            if (token.Type != TokenType.Identifier)
            {
                throw new ParserException("Expected identifier", token.Position);
            }
            parameters.Add(new TypedIdentifier(token.Value, type));
            CheckNextToken(TokenType.RightParen);
        }

        // consume right paren
        CheckNextToken(TokenType.RightParen);

        // parse return type
        Type returnType = ParseType();

        // parse body list

        List<SExpression> body = new();
        while(lexer.Peek().Type != TokenType.RightParen && lexer.Peek().Type != TokenType.Eof)
        {
            body.Add(ParseSExpression()!);
        }
        CheckNextToken(TokenType.RightParen); // end body list/func

        return new FuncExpression(name, parameters.ToArray(), returnType, body, startToken.Position);
    }

    private SExpression ParseExtern(Token startToken)
    {        
        // (extern already consumed
        Token token = lexer.NextToken();
        if (token.Type != TokenType.Identifier)
        {
            throw new ParserException($"Expected identifier, not {token}", token.Position);
        }

        string name = token.Value;

        List<Type> parameterTypes = new List<Type>();
        // check next token is paren
        CheckNextToken(TokenType.LeftParen);

        bool isVarargs = false;
        // first check if this is a varargs function
        if(lexer.Peek().Type == TokenType.Variadic)
        {
            // consume the ampersand
            lexer.NextToken();
            isVarargs = true; // varargs is only available for external functions, not internal
        }
        else

        // parse parameter types
        while (lexer.Peek().Type != TokenType.RightParen)
        {
            parameterTypes.Add(ParseType());
        }

        // consume right paren
        CheckNextToken(TokenType.RightParen);

        // parse return type
        Type returnType = ParseType();

        // parse the string literal for the dll the function is in
        token = lexer.NextToken();
        if(token == null || token.Type != TokenType.Literal || token is not StringLiteral)
        {
            throw new ParserException("Expected string literal", token!.Position);
        }

        StringLiteral stringLiteral = (token as StringLiteral)!;
        string dll = stringLiteral.StringValue;

        CheckNextToken(TokenType.RightParen); // end body list/func
        
        return new ExternExpression(name, parameterTypes.ToArray(), returnType, startToken.Position, dll, isVarargs);
    }

    private SExpression ParseReturn(Token startToken)
    {
        SExpression? expression = ParseSExpression();
        CheckNextToken(TokenType.RightParen);
        return new ReturnExpression(expression, startToken.Position);
    }

    private SExpression ParseIf(Token startToken)
    {
        SExpression? condition = ParseSExpression();
        SExpression? then = ParseSExpression();


        // check for else
        SExpression? @else = null;
        if(lexer.Peek().Type != TokenType.RightParen)
            @else = ParseSExpression();
        CheckNextToken(TokenType.RightParen);
        return new IfExpression(condition!, then!, @else, startToken.Position);
    }

    // (vardec (type) name)
    private SExpression ParseVarDec(Token startToken)
    {
        Type type = ParseType();
        Token token = lexer.NextToken();
        if (token.Type != TokenType.Identifier)
        {
            throw new ParserException("Expected identifier, got " + token, startToken.Position);
        }
        string name = token.Value;
        CheckNextToken(TokenType.RightParen);
        return new VardecExpression(name, type, startToken.Position);
    }

    private SExpression ParseAssign(Token startToken)
    {
        Token token = lexer.NextToken();
        if (token.Type != TokenType.Identifier)
        {
            throw new ParserException("Expected identifier", startToken.Position);
        }
        string name = token.Value;
        SExpression? expression = ParseSExpression();
        CheckNextToken(TokenType.RightParen);
        return new AssignExpression(name, expression!, startToken.Position);
    }

    private SExpression ParseBlock(Token startToken)
    {
        List<SExpression> expressions = new();
        while (lexer.Peek().Type != TokenType.RightParen)
        {
            expressions.Add(ParseSExpression()!);
        }
        CheckNextToken(TokenType.RightParen);
        return new BlockExpression(expressions.ToArray(), startToken.Position);
    }

    private SExpression ParseSizeOf(Token startToken)
    {
        Type type = ParseType();
        CheckNextToken(TokenType.RightParen);
        return new SizeOfExpression(type, startToken.Position);
    }

    private SExpression ParseIndex(Token startToken)
    {
        SExpression? array = ParseSExpression();
        SExpression? index = ParseSExpression();
        CheckNextToken(TokenType.RightParen);
        return new IndexExpression(array!, index!, startToken.Position);
    }
}