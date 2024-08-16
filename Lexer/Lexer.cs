using System.Text;

namespace BaD;

public class LexerException : Exception
{
    public LexerException(string message) : base(message) { }
}

public class Lexer
{
    private readonly Stack<ICharacterSource> sourceStack;
    private ICharacterSource source => sourceStack.Peek();

    private string lastToken = ""; // used for peeking while retaining line/column info

    public Lexer(ICharacterSource source)
    {
        sourceStack = new Stack<ICharacterSource>();
        sourceStack.Push(source);
    }

    public int Line { get; private set; }
    public int Column { get; private set; }

    readonly Dictionary<string, TokenType> reservedWords = new Dictionary<string, TokenType>
    {
        { "int", TokenType.Int },
        { "char", TokenType.Char },
        { "int16", TokenType.Int16},
        { "int32", TokenType.Int32},
        { "void", TokenType.Void },
        { "vardec", TokenType.VarDec },
        { "assign", TokenType.Assign },
        { "while", TokenType.While },
        { "if", TokenType.If },
        { "return", TokenType.Return },
        { "block", TokenType.Block },
        { "stmt", TokenType.Stmt },
        { "call", TokenType.Call },
        { "func", TokenType.Func },
        { "extern", TokenType.Extern },
        { "assignptr", TokenType.AssignPtr },
        { "bool", TokenType.Bool },
        { "index", TokenType.Index },
        { "sizeof", TokenType.SizeOf },
        { "array" , TokenType.Array },
        { "cast", TokenType.Cast },
        { "uint", TokenType.UInt },
        { "uint16", TokenType.UInt16 },
        { "uint32", TokenType.UInt32 },
        { "uchar", TokenType.UChar }
    };

    readonly Dictionary<string, TokenType> operators = new()
    {
        { "+", TokenType.Plus },
        { "-", TokenType.Minus },
        { "*", TokenType.Multiply },
        { "/", TokenType.Divide },
        { "<", TokenType.LessThan },
        { ">", TokenType.GreaterThan },
        { "<=", TokenType.LessThanOrEqual },
        { ">=", TokenType.GreaterThanOrEqual },
        { "==", TokenType.Equal },
        { "!=", TokenType.NotEqual },
        { "(", TokenType.LeftParen },
        { ")", TokenType.RightParen },
        { "&", TokenType.Ampersand },
        { "&&", TokenType.And },
        { "||", TokenType.Or },
        { "!", TokenType.Not },
        { "...", TokenType.Variadic },
        { "@", TokenType.AtSymbol }
    };

    // get current location in the source
    public string GetStreamPosition()
    {
        return source.GetStreamPosition().ToString();
    }

    public Token NextToken()
    {
        lastToken = ""; // clear the last token

        while (!source.IsEof() && char.IsWhiteSpace(source.Peek()))
            lastToken += source.Get(); // skip whitespace and add to the last token (for peeking)

        StreamPosition position = source.GetStreamPosition();
        Line = position.Line;
        Column = position.Column;

        // end of file
        if (source.IsEof())
            return new Token(TokenType.Eof, "", Line, Column); // end of file

        // check for comments (only support single line comments)
        if (source.Peek() == '/' && source.IsValidPeek(1) && source.Peek(1) == '/')
        {
            while (!source.IsEof() && source.Peek() != '\n')
                source.Get(); // skip the comment
            return NextToken(); // get the next token
        }

        // if it starts with a negative and is followed by a digit, it's a number, or if it starts with a digit
        if((source.Peek() == '-' && source.IsValidPeek(1) && char.IsDigit(source.Peek(1))) || char.IsDigit(source.Peek()))
        {
            return GetNumber();
        }
        
        if (char.IsLetter(source.Peek())) // if it starts with a letter, it's an identifier
            return GetIdentifier();

        if (source.Peek() == '\'') // if it starts with a single quote, it's a character literal
            return GetCharLiteral();
        
        if (source.Peek() == '"') // if it starts with a double quote, it's a string literal
            return GetStringLiteral();

        // default to an operator, or throw an exception if it's not a valid operator
        return GetOperator();
    }

    public Token GetCharLiteral()
    {
        char openingQuote = source.Get(); // skip the opening quote
        char c = GetChar();
        // check if we have a closing quote
        if (source.IsEof() || source.Get() != '\'')
        {
            throw new LexerException("Expected closing quote");
        }

        // add the character literal to the last token
        lastToken += "'" + c + "'";
        return new CharLiteral(c, Line, Column);
    }

    public Token Peek()
    {
        // get a full token and then unget it
        Token token = NextToken();

        // remember we have to unget in reverse order
        string s = token.Value;
        for (int i = s.Length - 1; i >= 0; i--)
        {
            source.Unget(s[i]);
        }

        return token;
    }

    Token GetStringLiteral()
    {
        char openingQuote = source.Get(); // skip the opening quote
        StringBuilder sb = new StringBuilder();
        while (source.Peek() != '"')
        {
            sb.Append(GetChar());
        }
        source.Get(); // skip the closing quote

        lastToken += "\"" + sb.ToString() + "\"";
        return new StringLiteral(sb.ToString(), Line, Column);
    }

    char GetChar()
    {
        // gets a single character from the source, can be used for parsing single characters or string literals

        if(source.Peek() == '\\')
        {
            source.Get(); // skip the backslash
            char c = source.Get();
            return c switch
            {
                'n' => '\n',
                't' => '\t',
                'r' => '\r',
                '0' => '\0',
                '\'' => '\'',
                '"' => '\"',

                // invalid escape sequence
                _ => throw new LexerException("Invalid escape sequence: \\" + c),
            };
        }
        return source.Get();
    }

    Token GetNumber()
    {
        // check for negative number
        long sign = 1;
        if (source.Peek() == '-')
        {
            sign = -1;
            source.Get(); // skip the negative sign
        }
        
        StringBuilder sb = new StringBuilder();
        while (!source.IsEof() && char.IsDigit(source.Peek()))
        {
            sb.Append(source.Get());
        }

        long value = long.Parse(sb.ToString());
        return new IntLiteral(value * sign, Line, Column);
    }

    public Token GetOperator()
    {
        
        StringBuilder op = new();

        while (!source.IsEof() && !char.IsLetterOrDigit(source.Peek()) && !char.IsWhiteSpace(source.Peek()))
        {
            op.Append(source.Get());
            // add the operator to the last token
            lastToken += op.ToString();

            if (operators.TryGetValue(op.ToString(), out TokenType type))
            {
                // check for multi-character operators
                if(!source.IsEof() && operators.TryGetValue(op.ToString() + source.Peek(), out TokenType multiType))
                {
                    char character = source.Get();
                    op.Append(character);
                    lastToken += character;
                    return new Token(multiType, op.ToString(), Line, Column);
                }


                return new Token(type, op.ToString(), Line, Column);
            }
        }

        throw new LexerException("Unexpected character: " + source.Peek());
    }

    Token GetIdentifier()
    {
        StringBuilder sb = new StringBuilder();
        while(!source.IsEof() && char.IsLetterOrDigit(source.Peek()))
        {
            sb.Append(source.Get());
        }
        if (reservedWords.TryGetValue(sb.ToString(), out TokenType type))
        {
            return new Token(type, sb.ToString(), Line, Column);
        }

        // true/false
        if (sb.ToString() == "true")
            return new BoolLiteral(true, Line, Column);
        if (sb.ToString() == "false")
            return new BoolLiteral(false, Line, Column);
        
        return new Token(TokenType.Identifier, sb.ToString(), Line, Column);
    }
}