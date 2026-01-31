using System.Text;

namespace CSharpJvmCompiler;

public class Lexer
{
    private readonly string _source;
    private int _position;
    private int _line = 1;
    private int _column = 1;

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        { "class", TokenType.Class },
        { "public", TokenType.Public },
        { "static", TokenType.Static },
        { "void", TokenType.Void },
        { "int", TokenType.Int },
        { "string", TokenType.String },
        { "return", TokenType.Return }
    };

    public Lexer(string source)
    {
        _source = source;
        _position = 0;
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        
        while (_position < _source.Length)
        {
            var token = NextToken();
            if (token.Type != TokenType.Unknown)
            {
                tokens.Add(token);
            }
        }
        
        tokens.Add(new Token(TokenType.Eof, "", _line, _column));
        return tokens;
    }

    private Token NextToken()
    {
        SkipWhitespace();
        
        if (_position >= _source.Length)
        {
            return new Token(TokenType.Eof, "", _line, _column);
        }

        var currentChar = _source[_position];
        var startLine = _line;
        var startColumn = _column;

        // String literals
        if (currentChar == '"')
        {
            return ReadStringLiteral(startLine, startColumn);
        }

        // Numbers
        if (char.IsDigit(currentChar))
        {
            return ReadNumber(startLine, startColumn);
        }

        // Identifiers and keywords
        if (char.IsLetter(currentChar) || currentChar == '_')
        {
            return ReadIdentifier(startLine, startColumn);
        }

        // Single character tokens
        _position++;
        _column++;
        
        return currentChar switch
        {
            '+' => new Token(TokenType.Plus, "+", startLine, startColumn),
            '-' => new Token(TokenType.Minus, "-", startLine, startColumn),
            '*' => new Token(TokenType.Star, "*", startLine, startColumn),
            '/' => new Token(TokenType.Slash, "/", startLine, startColumn),
            '=' => new Token(TokenType.Assign, "=", startLine, startColumn),
            '(' => new Token(TokenType.LeftParen, "(", startLine, startColumn),
            ')' => new Token(TokenType.RightParen, ")", startLine, startColumn),
            '{' => new Token(TokenType.LeftBrace, "{", startLine, startColumn),
            '}' => new Token(TokenType.RightBrace, "}", startLine, startColumn),
            '[' => new Token(TokenType.LeftBracket, "[", startLine, startColumn),
            ']' => new Token(TokenType.RightBracket, "]", startLine, startColumn),
            ';' => new Token(TokenType.Semicolon, ";", startLine, startColumn),
            ',' => new Token(TokenType.Comma, ",", startLine, startColumn),
            '.' => new Token(TokenType.Dot, ".", startLine, startColumn),
            _ => new Token(TokenType.Unknown, currentChar.ToString(), startLine, startColumn)
        };
    }

    private void SkipWhitespace()
    {
        while (_position < _source.Length && char.IsWhiteSpace(_source[_position]))
        {
            if (_source[_position] == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
            _position++;
        }
    }

    private Token ReadStringLiteral(int startLine, int startColumn)
    {
        _position++; // Skip opening quote
        _column++;
        var sb = new StringBuilder();
        
        while (_position < _source.Length && _source[_position] != '"')
        {
            if (_source[_position] == '\\' && _position + 1 < _source.Length)
            {
                _position++;
                _column++;
                // Handle escape sequences
                switch (_source[_position])
                {
                    case 'n':
                        sb.Append('\n');
                        break;
                    case 't':
                        sb.Append('\t');
                        break;
                    case '\\':
                        sb.Append('\\');
                        break;
                    case '"':
                        sb.Append('"');
                        break;
                    default:
                        sb.Append(_source[_position]);
                        break;
                }
            }
            else
            {
                sb.Append(_source[_position]);
            }
            _position++;
            _column++;
        }
        
        if (_position < _source.Length)
        {
            _position++; // Skip closing quote
            _column++;
        }
        
        return new Token(TokenType.StringLiteral, sb.ToString(), startLine, startColumn);
    }

    private Token ReadNumber(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        
        while (_position < _source.Length && char.IsDigit(_source[_position]))
        {
            sb.Append(_source[_position]);
            _position++;
            _column++;
        }
        
        return new Token(TokenType.IntLiteral, sb.ToString(), startLine, startColumn);
    }

    private Token ReadIdentifier(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        
        while (_position < _source.Length && 
               (char.IsLetterOrDigit(_source[_position]) || _source[_position] == '_'))
        {
            sb.Append(_source[_position]);
            _position++;
            _column++;
        }
        
        var value = sb.ToString();
        var type = Keywords.ContainsKey(value) ? Keywords[value] : TokenType.Identifier;
        
        return new Token(type, value, startLine, startColumn);
    }
}
