namespace CSharpJvmCompiler;

public class Parser
{
    private readonly List<Token> _tokens;
    private int _position;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
        _position = 0;
    }

    private Token Current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];
    private Token Peek(int offset = 1) => _position + offset < _tokens.Count ? _tokens[_position + offset] : _tokens[^1];

    public ProgramNode Parse()
    {
        var program = new ProgramNode();
        
        while (Current.Type != TokenType.Eof)
        {
            if (Current.Type == TokenType.Public || Current.Type == TokenType.Class)
            {
                program.Classes.Add(ParseClass());
            }
            else
            {
                throw new Exception($"Unexpected token: {Current}");
            }
        }
        
        return program;
    }

    private ClassNode ParseClass()
    {
        var classNode = new ClassNode();
        
        // Optional 'public'
        if (Current.Type == TokenType.Public)
        {
            Consume(TokenType.Public);
        }
        
        Consume(TokenType.Class);
        classNode.Name = Consume(TokenType.Identifier).Value;
        Consume(TokenType.LeftBrace);
        
        while (Current.Type != TokenType.RightBrace && Current.Type != TokenType.Eof)
        {
            classNode.Methods.Add(ParseMethod());
        }
        
        Consume(TokenType.RightBrace);
        return classNode;
    }

    private MethodNode ParseMethod()
    {
        var method = new MethodNode();
        
        // Parse modifiers
        if (Current.Type == TokenType.Public)
        {
            method.IsPublic = true;
            Consume(TokenType.Public);
        }
        
        if (Current.Type == TokenType.Static)
        {
            method.IsStatic = true;
            Consume(TokenType.Static);
        }
        
        // Parse return type
        if (Current.Type == TokenType.Void)
        {
            method.ReturnType = "void";
            Consume(TokenType.Void);
        }
        else if (Current.Type == TokenType.Int)
        {
            method.ReturnType = "int";
            Consume(TokenType.Int);
        }
        else
        {
            throw new Exception($"Expected return type, got: {Current}");
        }
        
        // Parse method name
        method.Name = Consume(TokenType.Identifier).Value;
        
        // Parse parameters
        Consume(TokenType.LeftParen);
        while (Current.Type != TokenType.RightParen)
        {
            method.Parameters.Add(ParseParameter());
            if (Current.Type == TokenType.Comma)
            {
                Consume(TokenType.Comma);
            }
        }
        Consume(TokenType.RightParen);
        
        // Parse method body
        Consume(TokenType.LeftBrace);
        while (Current.Type != TokenType.RightBrace && Current.Type != TokenType.Eof)
        {
            method.Body.Add(ParseStatement());
        }
        Consume(TokenType.RightBrace);
        
        return method;
    }

    private ParameterNode ParseParameter()
    {
        var param = new ParameterNode();
        
        if (Current.Type == TokenType.String)
        {
            param.Type = "string[]";
            Consume(TokenType.String);
            Consume(TokenType.LeftBracket);
            Consume(TokenType.RightBracket);
        }
        else if (Current.Type == TokenType.Int)
        {
            param.Type = "int";
            Consume(TokenType.Int);
        }
        else
        {
            throw new Exception($"Expected parameter type, got: {Current}");
        }
        
        param.Name = Consume(TokenType.Identifier).Value;
        return param;
    }

    private StatementNode ParseStatement()
    {
        // Variable declaration
        if (Current.Type == TokenType.Int)
        {
            return ParseVarDecl();
        }
        
        // Return statement
        if (Current.Type == TokenType.Return)
        {
            return ParseReturn();
        }
        
        // Expression statement (method call, assignment, etc.)
        var expr = ParseExpression();
        Consume(TokenType.Semicolon);
        return new ExprStatementNode { Expression = expr };
    }

    private VarDeclNode ParseVarDecl()
    {
        var varDecl = new VarDeclNode();
        
        if (Current.Type == TokenType.Int)
        {
            varDecl.Type = "int";
            Consume(TokenType.Int);
        }
        else
        {
            throw new Exception($"Expected type, got: {Current}");
        }
        
        varDecl.Name = Consume(TokenType.Identifier).Value;
        
        if (Current.Type == TokenType.Assign)
        {
            Consume(TokenType.Assign);
            varDecl.Initializer = ParseExpression();
        }
        
        Consume(TokenType.Semicolon);
        return varDecl;
    }

    private ReturnNode ParseReturn()
    {
        Consume(TokenType.Return);
        
        var returnNode = new ReturnNode();
        if (Current.Type != TokenType.Semicolon)
        {
            returnNode.Expression = ParseExpression();
        }
        
        Consume(TokenType.Semicolon);
        return returnNode;
    }

    private ExpressionNode ParseExpression()
    {
        return ParseAdditive();
    }

    private ExpressionNode ParseAdditive()
    {
        var left = ParseMultiplicative();
        
        while (Current.Type == TokenType.Plus || Current.Type == TokenType.Minus)
        {
            var op = Current.Value;
            Consume(Current.Type);
            var right = ParseMultiplicative();
            left = new BinaryOpNode { Left = left, Operator = op, Right = right };
        }
        
        return left;
    }

    private ExpressionNode ParseMultiplicative()
    {
        var left = ParsePrimary();
        
        while (Current.Type == TokenType.Star || Current.Type == TokenType.Slash)
        {
            var op = Current.Value;
            Consume(Current.Type);
            var right = ParsePrimary();
            left = new BinaryOpNode { Left = left, Operator = op, Right = right };
        }
        
        return left;
    }

    private ExpressionNode ParsePrimary()
    {
        // Integer literal
        if (Current.Type == TokenType.IntLiteral)
        {
            var value = int.Parse(Current.Value);
            Consume(TokenType.IntLiteral);
            return new IntLiteralNode { Value = value };
        }
        
        // String literal
        if (Current.Type == TokenType.StringLiteral)
        {
            var value = Current.Value;
            Consume(TokenType.StringLiteral);
            return new StringLiteralNode { Value = value };
        }
        
        // Identifier (variable or method call)
        if (Current.Type == TokenType.Identifier)
        {
            var name = Current.Value;
            Consume(TokenType.Identifier);
            
            // Method call or member access
            if (Current.Type == TokenType.Dot)
            {
                var target = new VarRefNode { Name = name };
                return ParseMemberAccess(target);
            }
            
            // Just a variable reference
            return new VarRefNode { Name = name };
        }
        
        // Parenthesized expression
        if (Current.Type == TokenType.LeftParen)
        {
            Consume(TokenType.LeftParen);
            var expr = ParseExpression();
            Consume(TokenType.RightParen);
            return expr;
        }
        
        throw new Exception($"Unexpected token in expression: {Current}");
    }

    private ExpressionNode ParseMemberAccess(ExpressionNode target)
    {
        while (Current.Type == TokenType.Dot)
        {
            Consume(TokenType.Dot);
            var memberName = Consume(TokenType.Identifier).Value;
            
            // Check if it's a method call
            if (Current.Type == TokenType.LeftParen)
            {
                Consume(TokenType.LeftParen);
                var args = new List<ExpressionNode>();
                
                while (Current.Type != TokenType.RightParen)
                {
                    args.Add(ParseExpression());
                    if (Current.Type == TokenType.Comma)
                    {
                        Consume(TokenType.Comma);
                    }
                }
                
                Consume(TokenType.RightParen);
                target = new MethodCallNode { Target = target, MethodName = memberName, Arguments = args };
            }
            else
            {
                // Property access - treat as variable for now
                target = new VarRefNode { Name = memberName };
            }
        }
        
        return target;
    }

    private Token Consume(TokenType expected)
    {
        if (Current.Type != expected)
        {
            throw new Exception($"Expected {expected}, got {Current}");
        }
        
        var token = Current;
        _position++;
        return token;
    }
}
