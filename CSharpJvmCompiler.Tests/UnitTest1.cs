using CSharpJvmCompiler;

namespace CSharpJvmCompiler.Tests;

public class CompilerTests
{
    [Fact]
    public void Lexer_TokenizesSimpleClass()
    {
        // Arrange
        var source = "public class Test { }";
        var lexer = new Lexer(source);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        Assert.Contains(tokens, t => t.Type == TokenType.Public);
        Assert.Contains(tokens, t => t.Type == TokenType.Class);
        Assert.Contains(tokens, t => t.Type == TokenType.Identifier && t.Value == "Test");
        Assert.Contains(tokens, t => t.Type == TokenType.LeftBrace);
        Assert.Contains(tokens, t => t.Type == TokenType.RightBrace);
    }

    [Fact]
    public void Lexer_TokenizesIntegerLiterals()
    {
        // Arrange
        var source = "int x = 42;";
        var lexer = new Lexer(source);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        Assert.Contains(tokens, t => t.Type == TokenType.IntLiteral && t.Value == "42");
    }

    [Fact]
    public void Lexer_TokenizesStringLiterals()
    {
        // Arrange
        var source = "\"Hello, World!\"";
        var lexer = new Lexer(source);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        Assert.Contains(tokens, t => t.Type == TokenType.StringLiteral && t.Value == "Hello, World!");
    }

    [Fact]
    public void Parser_ParsesSimpleClass()
    {
        // Arrange
        var source = @"
            public class TestClass
            {
                public static void Main(string[] args)
                {
                }
            }
        ";
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);

        // Act
        var ast = parser.Parse();

        // Assert
        Assert.Single(ast.Classes);
        Assert.Equal("TestClass", ast.Classes[0].Name);
        Assert.Single(ast.Classes[0].Methods);
        Assert.Equal("Main", ast.Classes[0].Methods[0].Name);
    }

    [Fact]
    public void Parser_ParsesVariableDeclaration()
    {
        // Arrange
        var source = @"
            public class Test
            {
                public static void Main(string[] args)
                {
                    int x = 10;
                }
            }
        ";
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);

        // Act
        var ast = parser.Parse();

        // Assert
        var method = ast.Classes[0].Methods[0];
        Assert.Single(method.Body);
        var varDecl = Assert.IsType<VarDeclNode>(method.Body[0]);
        Assert.Equal("x", varDecl.Name);
        Assert.Equal("int", varDecl.Type);
    }

    [Fact]
    public void Parser_ParsesBinaryOperations()
    {
        // Arrange
        var source = @"
            public class Test
            {
                public static void Main(string[] args)
                {
                    int result = 10 + 20;
                }
            }
        ";
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);

        // Act
        var ast = parser.Parse();

        // Assert
        var method = ast.Classes[0].Methods[0];
        var varDecl = Assert.IsType<VarDeclNode>(method.Body[0]);
        var binaryOp = Assert.IsType<BinaryOpNode>(varDecl.Initializer);
        Assert.Equal("+", binaryOp.Operator);
    }

    [Fact]
    public void CodeGenerator_GeneratesClassDefinition()
    {
        // Arrange
        var source = @"
            public class HelloWorld
            {
                public static void Main(string[] args)
                {
                }
            }
        ";
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);
        var ast = parser.Parse();
        var generator = new JvmBytecodeGenerator(ast);

        // Act
        var bytecode = generator.GenerateClassFile("HelloWorld");

        // Assert
        Assert.Contains(".class public HelloWorld", bytecode);
        Assert.Contains(".super java/lang/Object", bytecode);
        Assert.Contains(".method public static Main", bytecode);
    }

    [Fact]
    public void CodeGenerator_GeneratesConsoleWriteLine()
    {
        // Arrange
        var source = @"
            public class Test
            {
                public static void Main(string[] args)
                {
                    Console.WriteLine(""Hello"");
                }
            }
        ";
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);
        var ast = parser.Parse();
        var generator = new JvmBytecodeGenerator(ast);

        // Act
        var bytecode = generator.GenerateClassFile("Test");

        // Assert
        Assert.Contains("getstatic java/lang/System/out", bytecode);
        Assert.Contains("invokevirtual java/io/PrintStream/println", bytecode);
        Assert.Contains("Hello", bytecode);
    }

    [Fact]
    public void CodeGenerator_GeneratesArithmeticInstructions()
    {
        // Arrange
        var source = @"
            public class Test
            {
                public static void Main(string[] args)
                {
                    int result = 10 + 20;
                }
            }
        ";
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);
        var ast = parser.Parse();
        var generator = new JvmBytecodeGenerator(ast);

        // Act
        var bytecode = generator.GenerateClassFile("Test");

        // Assert
        Assert.Contains("bipush 10", bytecode);
        Assert.Contains("bipush 20", bytecode);
        Assert.Contains("iadd", bytecode);
        Assert.Contains("istore", bytecode);
    }

    [Fact]
    public void EndToEnd_CompilesHelloWorld()
    {
        // Arrange
        var source = @"
            public class HelloWorld
            {
                public static void Main(string[] args)
                {
                    Console.WriteLine(""Hello from C# compiled to JVM!"");
                }
            }
        ";

        // Act
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);
        var ast = parser.Parse();
        var generator = new JvmBytecodeGenerator(ast);
        var bytecode = generator.GenerateClassFile("HelloWorld");

        // Assert - verify key components are present
        Assert.Contains(".class public HelloWorld", bytecode);
        Assert.Contains(".method public static Main", bytecode);
        Assert.Contains("getstatic java/lang/System/out", bytecode);
        Assert.Contains("Hello from C# compiled to JVM!", bytecode);
        Assert.Contains("invokevirtual java/io/PrintStream/println", bytecode);
        Assert.Contains("return", bytecode);
    }
}
