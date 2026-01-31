namespace CSharpJvmCompiler;

public enum TokenType
{
    // Keywords
    Class,
    Public,
    Static,
    Void,
    Int,
    String,
    Return,
    
    // Identifiers and literals
    Identifier,
    IntLiteral,
    StringLiteral,
    
    // Operators
    Plus,
    Minus,
    Star,
    Slash,
    Assign,
    
    // Delimiters
    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    LeftBracket,
    RightBracket,
    Semicolon,
    Comma,
    Dot,
    
    // Special
    Eof,
    Unknown
}

public class Token
{
    public TokenType Type { get; set; }
    public string Value { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }

    public Token(TokenType type, string value, int line, int column)
    {
        Type = type;
        Value = value;
        Line = line;
        Column = column;
    }

    public override string ToString() => $"{Type}({Value}) at {Line}:{Column}";
}
