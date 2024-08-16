namespace BaD;

// tokens expected
// types: int, char, void
// arithmetic: +, -, *, /, <, >, <=, >=, ==, !=, &&, ||, !
// parens: (, )
// operators: &
// reserved words: vardec, assign, while, if, return, block, stmt
// identifiers: [a-z, A-Z][a-z, A-Z, 0-9]*
// numbers: [0-9]+
// literals: [1-9][0-9]*, 0, true, false, 'a-z', "[a-z, A-Z][a-z,A-Z,0-9]*"

public enum TokenType
{
    // types
    Int, Char, Void, Bool, Int16, Int32,
    UInt, UInt16, UInt32, UChar,
    // arithmetic
    Plus, Minus, Multiply, Divide,
    // comparison
    LessThan, GreaterThan, LessThanOrEqual, GreaterThanOrEqual, Equal, NotEqual,
    // parens
    LeftParen, RightParen,
    // reserved words
    VarDec, Assign,
    // control flow
    While, Call, If, Return, Func, Block, Stmt, AssignPtr, Index, SizeOf, Array, Cast,
    Ampersand, AtSymbol,
    Variadic, // for the ... parameter
    And, Or, Not,
    Identifier,
    Literal,
    Extern, // for the extern keyword (for calling external functions)
    Eof
}

public class Token
{
    public TokenType Type { get; }
    public string Value { get; }

    public Token(TokenType type, string value, int line, int column)
    {
        Type = type;
        Value = value;
        Position = new StreamPosition(line, column);
    }

    public override string ToString()
    {
        return Value; // get the value of the token
    }

    public override bool Equals(object? obj)
    {
        if (obj is Token t)
        {
            return Type == t.Type && Value == t.Value;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Type.GetHashCode() ^ Value.GetHashCode();
    }
    
    // store the token location for error reporting
    public StreamPosition Position { get; }
}

public class IntLiteral : Token
{
    public long IntValue { get; }

    public IntLiteral(long value, int line, int column) : base(TokenType.Literal, value.ToString(), line, column)
    {
        IntValue = value;
    }
}

public class CharLiteral : Token
{
    public char CharValue { get; }

    public CharLiteral(char value, int line, int column) : base(TokenType.Literal, "'" + value.ToString() + "'", line, column)
    {
        CharValue = value;
    }
}

public class BoolLiteral : Token
{
    public bool BoolValue { get; }

    public BoolLiteral(bool value, int line, int column) : base(TokenType.Literal, value.ToString(), line, column)
    {
        BoolValue = value;
    }
}

public class StringLiteral : Token
{
    public string StringValue { get; }

    public StringLiteral(string value, int line, int column) : base(TokenType.Literal, "\"" + value + "\"", line, column)
    {
        StringValue = value;
    }
}