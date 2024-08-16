namespace BaD.Tests;

public class LexerTests
{
    [Fact]
    public void ReservedWordsTest()
    {
        Dictionary<string, TokenType> reservedWords = new Dictionary<string, TokenType>
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

        // for each, create a lexer, and then call NextToken() and check the TokenType
        foreach (var (key, value) in reservedWords)
        {
            var source = new StringCharacterSource(key);
            var lexer = new Lexer(source);
            var token = lexer.NextToken();
            Assert.Equal(value, token.Type);
        }
    }

    [Fact]
    public void CommentTest()
    {
        // test comments
        var source = new StringCharacterSource("// this is a comment\nint");
        var lexer = new Lexer(source);
        var token = lexer.NextToken();
        Assert.Equal(TokenType.Int, token.Type);
    }

    [Fact]
    public void OperatorTest()
    {
        // test operators
        Dictionary<string, TokenType> operators = new Dictionary<string, TokenType>
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

        foreach (var (key, value) in operators)
        {
            var source = new StringCharacterSource(key);
            var lexer = new Lexer(source);
            var token = lexer.NextToken();
            Assert.Equal(value, token.Type);
        }
    }

    [Fact]
    public void EscapeSequences()
    {
        // test escape sequences
        List<(string, char)> escapeSequences = new List<(string, char)>
        {
            ("\\n", '\n'),
            ("\\t", '\t'),
            ("\\r", '\r'),
            ("\\0", '\0'),
            ("\\'", '\''),
            ("\\\"", '\"'),
        };

        foreach (var (key, value) in escapeSequences)
        {
            var source = new StringCharacterSource($"\"{key}\"");
            var lexer = new Lexer(source);
            var token = lexer.NextToken();
            Assert.Equal(TokenType.Literal, token.Type);
            Assert.IsType<StringLiteral>(token);
            Assert.Equal(value.ToString(), ((StringLiteral)token).StringValue);
        }
    }

    [Fact]
    public void NegativeTest()
    {
        // test negative numbers
        var source = new StringCharacterSource("-123");
        var lexer = new Lexer(source);
        var token = lexer.NextToken();

        Assert.Equal(TokenType.Literal, token.Type);
        Assert.IsType<IntLiteral>(token);
        IntLiteral intLiteral = (IntLiteral)token;
        Assert.Equal(-123, intLiteral.IntValue);

        // negative with space between - and number
        source = new StringCharacterSource("- 123");
        lexer = new Lexer(source);
        token = lexer.NextToken();
        
        Assert.Equal(TokenType.Minus, token.Type);
        token = lexer.NextToken();
        Assert.Equal(TokenType.Literal, token.Type);
        Assert.IsType<IntLiteral>(token);
        intLiteral = (IntLiteral)token;
        Assert.Equal(123, intLiteral.IntValue);
    }

    [Fact]
    public void TestExceptions()
    {
        // check for invalid escape sequence
        var source = new StringCharacterSource("\"\\a\"");
        var lexer = new Lexer(source);
        Assert.Throws<LexerException>(() => lexer.NextToken());

        // check for missing closing quote on char literal
        source = new StringCharacterSource("'a");
        lexer = new Lexer(source);
        Assert.Throws<LexerException>(() => lexer.NextToken());

        // check for missing closing quote on string literal
        source = new StringCharacterSource("\"hello");
        lexer = new Lexer(source);
        Assert.Throws<CharacterSourceException>(() => lexer.NextToken());
        
        // check for operator exception
        source = new StringCharacterSource("hello$");
        lexer = new Lexer(source);
        Assert.Throws<LexerException>(() => lexer.GetOperator());
    }

    [Fact]
    public void IdentifierTest()
    {
        // test identifiers
        var source = new StringCharacterSource("hello");
        var lexer = new Lexer(source);
        var token = lexer.NextToken();
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("hello", token.Value);
    }

    [Fact]
    public void TestTokenPeek()
    {
        // test peeking
        var source = new StringCharacterSource("hello");
        var lexer = new Lexer(source);
        var token = lexer.Peek();
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("hello", token.Value);

        // test that get gives the same token
        token = lexer.NextToken();
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("hello", token.Value);
        
        // test that peeking after getting the token gives Eof
        token = lexer.Peek();
        Assert.Equal(TokenType.Eof, token.Type);

    }

    [Fact]    
    public void TestStreamPosition() 
    {
        var source = new StringCharacterSource("123");
        var lexer = new Lexer(source);
        Assert.Equal("line 1, column 1", lexer.GetStreamPosition());
    }

    [Fact]

    public void TestHashCode()
    {
        var token = new Token(TokenType.Int32, "1", 1, 1);
        Assert.IsType<Int32>(token.GetHashCode());
    }

    [Fact]
    public void LiteralTest()
    {
        // create a lexer, and then call NextToken() and check the TokenType
        var source = new StringCharacterSource("123");
        var lexer = new Lexer(source);
        var token = lexer.NextToken();
        Assert.Equal(TokenType.Literal, token.Type);
        Assert.IsType<IntLiteral>(token);
        
        IntLiteral intLiteral = (IntLiteral)token;
        Assert.Equal(123, intLiteral.IntValue);

        // string literal
        source = new StringCharacterSource("\"hello\"");
        lexer = new Lexer(source);
        token = lexer.NextToken();
        Assert.Equal(TokenType.Literal, token.Type);
        Assert.IsType<StringLiteral>(token);
        Assert.Equal("hello", ((StringLiteral)token).StringValue);

        // char literal
        source = new StringCharacterSource("'a'");
        lexer = new Lexer(source);
        token = lexer.NextToken();
        Assert.Equal(TokenType.Literal, token.Type);
        Assert.IsType<CharLiteral>(token);

        CharLiteral charLiteral = (CharLiteral)token;
        Assert.Equal('a', charLiteral.CharValue);

        // true and false
        source = new StringCharacterSource("true");
        lexer = new Lexer(source);
        token = lexer.NextToken();
        Assert.Equal(TokenType.Literal, token.Type);
        Assert.IsType<BoolLiteral>(token);
        Assert.True(((BoolLiteral)token).BoolValue);

        source = new StringCharacterSource("false");
        lexer = new Lexer(source);
        token = lexer.NextToken();
        Assert.Equal(TokenType.Literal, token.Type);
        Assert.IsType<BoolLiteral>(token);
        Assert.False(((BoolLiteral)token).BoolValue);
    }


    // test token functions
    [Fact]
    public void TestTokenToString()
    {
        var token = new Token(TokenType.Identifier, "hello", 1, 1);
        Assert.Equal("hello", token.ToString());

        token = new Token(TokenType.Literal, "123", 1, 1);
        Assert.Equal("123", token.ToString());

        token = new Token(TokenType.Plus, "+", 1, 1);
        Assert.Equal("+", token.ToString());
    }

    [Fact]
    public void TestTokenEquals()
    {
        var token1 = new Token(TokenType.Identifier, "hello", 1, 1);
        var token2 = new Token(TokenType.Identifier, "hello", 1, 1);
        Assert.Equal(token1, token2);

        token1 = new Token(TokenType.Literal, "123", 1, 1);
        token2 = new Token(TokenType.Literal, "123", 1, 1);
        Assert.Equal(token1, token2);

        token1 = new Token(TokenType.Plus, "+", 1, 1);
        token2 = new Token(TokenType.Plus, "+", 1, 1);
        Assert.Equal(token1, token2);

        Assert.False(token1.Equals("hello"));
    }

    [Fact]
    public void TestNewLine()
    {
        var source = new StringCharacterSource("hello\nworld");
        var lexer = new Lexer(source);
        var token = lexer.NextToken();
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("hello", token.Value);
        Assert.Equal(1, token.Position.Line);
        Assert.Equal(1, token.Position.Column);

        token = lexer.NextToken();
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("world", token.Value);
        Assert.Equal(2, token.Position.Line);
        Assert.Equal(1, token.Position.Column);
    }

    [Fact]
    public void TestFilestream()
    {
        var source = new FileCharacterSource("testfile.txt");
        var lexer = new Lexer(source);

        // test peeking
        var token = lexer.Peek();
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("hello", token.Value);

        // test that get gives the same token

        token = lexer.NextToken();
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("hello", token.Value);
        Assert.Equal(1, token.Position.Line);
        Assert.Equal(1, token.Position.Column);

        token = lexer.NextToken();
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("world", token.Value);
        Assert.Equal(2, token.Position.Line);
        Assert.Equal(1, token.Position.Column);

        // test that get gives eof
        token = lexer.NextToken();
        Assert.Equal(TokenType.Eof, token.Type);
    }

    // test file character source
    [Fact]
    public void TestFileCharacterSource()
    {
        var source = new FileCharacterSource("testfile.txt");
        
        // test peeking ahead by multiple characters (should be able to peek 3 characters ahead)
        Assert.Equal('h', source.Peek());
        Assert.Equal('e', source.Peek(1));
        Assert.Equal('l', source.Peek(2));
        Assert.Equal('l', source.Peek(3));

        // test getting characters
        Assert.Equal('h', source.Get());
        Assert.Equal('e', source.Get());
        Assert.Equal('l', source.Get());

        // test that peeking after getting the characters gives the right character
        Assert.Equal('l', source.Peek());

        // test that unget \n decreases the line number
        int line = source.GetStreamPosition().Line;
        source.Unget('\n');
        Assert.Equal(line - 1, source.GetStreamPosition().Line);

        // test that getting a peek after eof throws an exception
        Assert.Throws<CharacterSourceException>(() => source.Peek(50));

        // test validity of a peek
        Assert.True(source.IsValidPeek(1));

        // test get peek
        Assert.NotEqual('h', source.Get());
    }

    [Fact]
    public void StringCharacterSourceTest()
    {
        var source = new StringCharacterSource("hello\nworld");
        Assert.Equal('h', source.Get());
        Assert.Equal('e', source.Get());
        Assert.Equal('l', source.Get());
        Assert.Equal('l', source.Get());
        Assert.Equal('o', source.Get());
        Assert.Equal('\n', source.Get());

        // test that peeking after getting the characters gives the right character
        Assert.Equal('w', source.Peek());

        // test that unget \n decreases the line number
        int line = source.GetStreamPosition().Line;
        source.Unget('\n');
        Assert.Equal(line - 1, source.GetStreamPosition().Line);

        // test that getting a peek after eof throws an exception
        Assert.Throws<CharacterSourceException>(() => source.Peek(50));
    }
}
