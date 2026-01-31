using System.Text;

namespace CSharpJvmCompiler;

public class JvmBytecodeGenerator
{
    private readonly ProgramNode _program;
    private readonly Dictionary<string, int> _localVariables = new();
    private int _nextLocalIndex = 0;

    public JvmBytecodeGenerator(ProgramNode program)
    {
        _program = program;
    }

    public string GenerateClassFile(string className)
    {
        var sb = new StringBuilder();
        
        // JVM Class File Format (simplified text representation)
        sb.AppendLine($"// Generated JVM Bytecode for class {className}");
        sb.AppendLine("// This is a textual representation of JVM bytecode");
        sb.AppendLine();
        
        foreach (var classNode in _program.Classes)
        {
            sb.AppendLine($".class public {classNode.Name}");
            sb.AppendLine($".super java/lang/Object");
            sb.AppendLine();
            
            foreach (var method in classNode.Methods)
            {
                GenerateMethod(sb, method);
            }
        }
        
        return sb.ToString();
    }

    private void GenerateMethod(StringBuilder sb, MethodNode method)
    {
        _localVariables.Clear();
        _nextLocalIndex = method.IsStatic ? 0 : 1; // Reserve slot 0 for 'this' if non-static

        // Reserve local variable slots for parameters
        foreach (var param in method.Parameters)
        {
            _localVariables[param.Name] = _nextLocalIndex++;
        }

        var methodModifiers = new List<string>();
        if (method.IsPublic) methodModifiers.Add("public");
        if (method.IsStatic) methodModifiers.Add("static");
        
        var modifierStr = string.Join(" ", methodModifiers);
        var returnTypeDescriptor = GetTypeDescriptor(method.ReturnType);
        var paramDescriptors = string.Join("", method.Parameters.Select(p => GetTypeDescriptor(p.Type)));
        
        sb.AppendLine($".method {modifierStr} {method.Name}({paramDescriptors}){returnTypeDescriptor}");
        sb.AppendLine($"  .limit stack {CalculateMaxStack(method)}");
        sb.AppendLine($"  .limit locals {_nextLocalIndex + CountLocalVariables(method)}");
        sb.AppendLine();
        
        // Generate method body
        foreach (var statement in method.Body)
        {
            GenerateStatement(sb, statement);
        }
        
        // Add default return if void
        if (method.ReturnType == "void")
        {
            sb.AppendLine("  return");
        }
        
        sb.AppendLine(".end method");
        sb.AppendLine();
    }

    private void GenerateStatement(StringBuilder sb, StatementNode statement)
    {
        switch (statement)
        {
            case VarDeclNode varDecl:
                GenerateVarDecl(sb, varDecl);
                break;
            case ReturnNode returnNode:
                GenerateReturn(sb, returnNode);
                break;
            case ExprStatementNode exprStmt:
                GenerateExpression(sb, exprStmt.Expression);
                // Pop result if expression produces a value
                if (ProducesValue(exprStmt.Expression))
                {
                    sb.AppendLine("  pop");
                }
                break;
        }
    }

    private void GenerateVarDecl(StringBuilder sb, VarDeclNode varDecl)
    {
        // Allocate local variable slot
        _localVariables[varDecl.Name] = _nextLocalIndex++;
        
        if (varDecl.Initializer != null)
        {
            GenerateExpression(sb, varDecl.Initializer);
            
            if (varDecl.Type == "int")
            {
                sb.AppendLine($"  istore {_localVariables[varDecl.Name]}");
            }
            else
            {
                sb.AppendLine($"  astore {_localVariables[varDecl.Name]}");
            }
        }
    }

    private void GenerateReturn(StringBuilder sb, ReturnNode returnNode)
    {
        if (returnNode.Expression != null)
        {
            GenerateExpression(sb, returnNode.Expression);
            
            // Determine return type
            if (returnNode.Expression is IntLiteralNode || returnNode.Expression is BinaryOpNode)
            {
                sb.AppendLine("  ireturn");
            }
            else
            {
                sb.AppendLine("  areturn");
            }
        }
        else
        {
            sb.AppendLine("  return");
        }
    }

    private void GenerateExpression(StringBuilder sb, ExpressionNode expression)
    {
        switch (expression)
        {
            case IntLiteralNode intLit:
                GenerateIntLiteral(sb, intLit);
                break;
            case StringLiteralNode strLit:
                GenerateStringLiteral(sb, strLit);
                break;
            case VarRefNode varRef:
                GenerateVarRef(sb, varRef);
                break;
            case BinaryOpNode binaryOp:
                GenerateBinaryOp(sb, binaryOp);
                break;
            case MethodCallNode methodCall:
                GenerateMethodCall(sb, methodCall);
                break;
        }
    }

    private void GenerateIntLiteral(StringBuilder sb, IntLiteralNode intLit)
    {
        if (intLit.Value >= -1 && intLit.Value <= 5)
        {
            sb.AppendLine($"  iconst_{intLit.Value}");
        }
        else if (intLit.Value >= -128 && intLit.Value <= 127)
        {
            sb.AppendLine($"  bipush {intLit.Value}");
        }
        else if (intLit.Value >= -32768 && intLit.Value <= 32767)
        {
            sb.AppendLine($"  sipush {intLit.Value}");
        }
        else
        {
            sb.AppendLine($"  ldc {intLit.Value}");
        }
    }

    private void GenerateStringLiteral(StringBuilder sb, StringLiteralNode strLit)
    {
        // Escape special characters for JVM
        var escaped = strLit.Value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
        sb.AppendLine($"  ldc \"{escaped}\"");
    }

    private void GenerateVarRef(StringBuilder sb, VarRefNode varRef)
    {
        if (_localVariables.TryGetValue(varRef.Name, out var index))
        {
            sb.AppendLine($"  iload {index}");
        }
        else
        {
            throw new Exception($"Undefined variable: {varRef.Name}");
        }
    }

    private void GenerateBinaryOp(StringBuilder sb, BinaryOpNode binaryOp)
    {
        GenerateExpression(sb, binaryOp.Left);
        GenerateExpression(sb, binaryOp.Right);
        
        switch (binaryOp.Operator)
        {
            case "+":
                sb.AppendLine("  iadd");
                break;
            case "-":
                sb.AppendLine("  isub");
                break;
            case "*":
                sb.AppendLine("  imul");
                break;
            case "/":
                sb.AppendLine("  idiv");
                break;
            default:
                throw new Exception($"Unknown operator: {binaryOp.Operator}");
        }
    }

    private void GenerateMethodCall(StringBuilder sb, MethodCallNode methodCall)
    {
        // Special handling for System.Console.WriteLine
        if (methodCall.Target is VarRefNode targetVar)
        {
            if (targetVar.Name == "Console" && methodCall.MethodName == "WriteLine")
            {
                sb.AppendLine("  getstatic java/lang/System/out Ljava/io/PrintStream;");
                
                foreach (var arg in methodCall.Arguments)
                {
                    GenerateExpression(sb, arg);
                }
                
                // Determine the println signature based on argument type
                if (methodCall.Arguments.Count > 0)
                {
                    var arg = methodCall.Arguments[0];
                    if (arg is StringLiteralNode)
                    {
                        sb.AppendLine("  invokevirtual java/io/PrintStream/println(Ljava/lang/String;)V");
                    }
                    else if (arg is IntLiteralNode || arg is BinaryOpNode || arg is VarRefNode)
                    {
                        sb.AppendLine("  invokevirtual java/io/PrintStream/println(I)V");
                    }
                }
                else
                {
                    sb.AppendLine("  invokevirtual java/io/PrintStream/println()V");
                }
            }
        }
    }

    private string GetTypeDescriptor(string type)
    {
        return type switch
        {
            "void" => "V",
            "int" => "I",
            "string" => "Ljava/lang/String;",
            "string[]" => "[Ljava/lang/String;",
            _ => "Ljava/lang/Object;"
        };
    }

    private int CalculateMaxStack(MethodNode method)
    {
        // Simple heuristic - calculate based on deepest expression
        int maxDepth = 0;
        
        foreach (var statement in method.Body)
        {
            maxDepth = Math.Max(maxDepth, CalculateStatementStack(statement));
        }
        
        return Math.Max(maxDepth, 2); // Minimum of 2 for safety
    }

    private int CalculateStatementStack(StatementNode statement)
    {
        return statement switch
        {
            VarDeclNode varDecl => varDecl.Initializer != null ? CalculateExpressionStack(varDecl.Initializer) : 0,
            ReturnNode returnNode => returnNode.Expression != null ? CalculateExpressionStack(returnNode.Expression) : 0,
            ExprStatementNode exprStmt => CalculateExpressionStack(exprStmt.Expression),
            _ => 0
        };
    }

    private int CalculateExpressionStack(ExpressionNode expression)
    {
        return expression switch
        {
            IntLiteralNode => 1,
            StringLiteralNode => 1,
            VarRefNode => 1,
            BinaryOpNode binOp => Math.Max(CalculateExpressionStack(binOp.Left), 
                                           1 + CalculateExpressionStack(binOp.Right)),
            MethodCallNode methodCall => 2 + methodCall.Arguments.Count,
            _ => 1
        };
    }

    private int CountLocalVariables(MethodNode method)
    {
        int count = 0;
        
        foreach (var statement in method.Body)
        {
            if (statement is VarDeclNode)
            {
                count++;
            }
        }
        
        return count;
    }

    private bool ProducesValue(ExpressionNode expression)
    {
        // Method calls that return void don't produce a value
        if (expression is MethodCallNode methodCall)
        {
            // Console.WriteLine returns void
            if (methodCall.Target is VarRefNode targetVar && 
                targetVar.Name == "Console" && 
                methodCall.MethodName == "WriteLine")
            {
                return false;
            }
        }
        
        return true;
    }
}
