namespace CSharpJvmCompiler;

// Base AST node
public abstract class AstNode
{
}

// Program node
public class ProgramNode : AstNode
{
    public List<ClassNode> Classes { get; set; } = new();
}

// Class node
public class ClassNode : AstNode
{
    public string Name { get; set; } = "";
    public List<MethodNode> Methods { get; set; } = new();
}

// Method node
public class MethodNode : AstNode
{
    public string Name { get; set; } = "";
    public string ReturnType { get; set; } = "";
    public bool IsPublic { get; set; }
    public bool IsStatic { get; set; }
    public List<ParameterNode> Parameters { get; set; } = new();
    public List<StatementNode> Body { get; set; } = new();
}

// Parameter node
public class ParameterNode : AstNode
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
}

// Base statement node
public abstract class StatementNode : AstNode
{
}

// Variable declaration
public class VarDeclNode : StatementNode
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public ExpressionNode? Initializer { get; set; }
}

// Return statement
public class ReturnNode : StatementNode
{
    public ExpressionNode? Expression { get; set; }
}

// Expression statement
public class ExprStatementNode : StatementNode
{
    public ExpressionNode Expression { get; set; } = null!;
}

// Base expression node
public abstract class ExpressionNode : AstNode
{
}

// Binary operation
public class BinaryOpNode : ExpressionNode
{
    public ExpressionNode Left { get; set; } = null!;
    public string Operator { get; set; } = "";
    public ExpressionNode Right { get; set; } = null!;
}

// Integer literal
public class IntLiteralNode : ExpressionNode
{
    public int Value { get; set; }
}

// String literal
public class StringLiteralNode : ExpressionNode
{
    public string Value { get; set; } = "";
}

// Variable reference
public class VarRefNode : ExpressionNode
{
    public string Name { get; set; } = "";
}

// Method call
public class MethodCallNode : ExpressionNode
{
    public ExpressionNode? Target { get; set; }
    public string MethodName { get; set; } = "";
    public List<ExpressionNode> Arguments { get; set; } = new();
}
