# Technical Documentation: C# to JVM Compiler

## Overview

This document provides technical details about how the C# to JVM bytecode compiler works.

## Compilation Pipeline

The compiler follows a traditional multi-stage compilation pipeline:

```
C# Source Code → Lexer → Tokens → Parser → AST → Code Generator → JVM Bytecode
```

### 1. Lexical Analysis (Lexer)

**File**: `Lexer.cs`

The lexer performs lexical analysis, converting the source code text into a stream of tokens. It:

- Recognizes keywords: `class`, `public`, `static`, `void`, `int`, `string`, `return`
- Identifies literals: integers, strings
- Extracts identifiers (variable and method names)
- Detects operators: `+`, `-`, `*`, `/`, `=`
- Finds delimiters: `()`, `{}`, `[]`, `;`, `,`, `.`

**Example**:
```csharp
Input:  "int x = 42;"
Output: [Int, Identifier("x"), Assign, IntLiteral("42"), Semicolon]
```

### 2. Syntax Analysis (Parser)

**File**: `Parser.cs`

The parser performs syntax analysis, building an Abstract Syntax Tree (AST) from the token stream. It implements a recursive descent parser with the following grammar:

```
Program      → ClassDecl*
ClassDecl    → 'public'? 'class' Identifier '{' MethodDecl* '}'
MethodDecl   → Modifier* ReturnType Identifier '(' Parameters ')' '{' Statement* '}'
Statement    → VarDecl | Return | ExprStatement
Expression   → Additive
Additive     → Multiplicative (('+' | '-') Multiplicative)*
Multiplicative → Primary (('*' | '/') Primary)*
Primary      → IntLiteral | StringLiteral | Identifier | MethodCall | '(' Expression ')'
```

### 3. Abstract Syntax Tree (AST)

**File**: `Ast.cs`

The AST represents the hierarchical structure of the program:

- **ProgramNode**: Root node containing classes
- **ClassNode**: Represents a class with methods
- **MethodNode**: Represents a method with parameters and body
- **StatementNode**: Base for all statements (variable declarations, returns, etc.)
- **ExpressionNode**: Base for all expressions (literals, binary operations, method calls)

### 4. Code Generation

**File**: `JvmBytecodeGenerator.cs`

The code generator traverses the AST and emits JVM bytecode in Jasmin assembly format.

#### JVM Bytecode Instructions Generated

##### Integer Operations
- `iconst_<n>`: Push integer constant -1 to 5
- `bipush`: Push byte value (-128 to 127)
- `sipush`: Push short value (-32768 to 32767)
- `ldc`: Load constant (larger values)
- `iload <n>`: Load integer from local variable
- `istore <n>`: Store integer to local variable

##### Arithmetic Operations
- `iadd`: Integer addition
- `isub`: Integer subtraction
- `imul`: Integer multiplication
- `idiv`: Integer division

##### Method Invocation
- `invokevirtual`: Invoke instance method
- `getstatic`: Get static field (used for System.out)

##### Control Flow
- `return`: Return void from method
- `ireturn`: Return integer from method

#### Type Descriptors

JVM uses type descriptors to represent types:

| C# Type    | JVM Descriptor         |
|------------|------------------------|
| `void`     | `V`                    |
| `int`      | `I`                    |
| `string`   | `Ljava/lang/String;`   |
| `string[]` | `[Ljava/lang/String;`  |

#### Method Descriptors

Method signatures are encoded as: `(ParameterTypes)ReturnType`

Examples:
- `Main(string[] args)` → `([Ljava/lang/String;)V`
- `Add(int x, int y)` → `(II)I`

## Example Compilation

### Input C# Code

```csharp
public class MathDemo
{
    public static void Main(string[] args)
    {
        int a = 10;
        int b = 20;
        int sum = a + b;
        Console.WriteLine(sum);
    }
}
```

### Generated JVM Bytecode

```jasmin
.class public MathDemo
.super java/lang/Object

.method public static Main([Ljava/lang/String;)V
  .limit stack 3
  .limit locals 4

  bipush 10          // Load constant 10
  istore 1           // Store in local variable 1 (a)
  
  bipush 20          // Load constant 20
  istore 2           // Store in local variable 2 (b)
  
  iload 1            // Load a
  iload 2            // Load b
  iadd               // Add them
  istore 3           // Store result in local variable 3 (sum)
  
  getstatic java/lang/System/out Ljava/io/PrintStream;
  iload 3            // Load sum
  invokevirtual java/io/PrintStream/println(I)V
  
  return
.end method
```

## Stack Management

The JVM is a stack-based virtual machine. Operations push and pop values from an operand stack:

1. **Constants** are pushed onto the stack
2. **Binary operations** pop two values, perform the operation, and push the result
3. **Method calls** pop arguments from the stack
4. **Local variables** are loaded onto the stack (iload) or stored from the stack (istore)

### Stack Size Calculation

The `.limit stack` directive specifies the maximum stack depth needed by a method. The compiler calculates this based on expression depth.

Example for `a + b * c`:
```
iload a      // Stack: [a]
iload b      // Stack: [a, b]
iload c      // Stack: [a, b, c]
imul         // Stack: [a, (b*c)]
iadd         // Stack: [(a+(b*c))]
```
Max stack depth: 3

## Local Variables

Local variables are stored in numbered slots:

- Slot 0: `this` reference (for non-static methods)
- Slots 1+: Method parameters
- Following slots: Local variables in order of declaration

Example for `Main(string[] args)`:
- Static method, so no `this`
- Slot 0: `args` parameter
- Slot 1: First local variable
- Slot 2: Second local variable
- etc.

## Special Handling

### Console.WriteLine Mapping

C# `Console.WriteLine` is mapped to Java's `System.out.println`:

```csharp
Console.WriteLine("Hello");
```

Generates:
```jasmin
getstatic java/lang/System/out Ljava/io/PrintStream;
ldc "Hello"
invokevirtual java/io/PrintStream/println(Ljava/lang/String;)V
```

The compiler automatically selects the correct `println` overload based on the argument type:
- `println(Ljava/lang/String;)V` for strings
- `println(I)V` for integers

## Limitations and Future Work

### Current Limitations

1. **Limited type system**: Only `int`, `string`, `void`, `string[]`
2. **No control flow**: Missing `if`, `while`, `for`, `foreach`
3. **No classes with state**: No fields or properties
4. **No constructors**: Can't instantiate objects
5. **No arrays** (except string[] for Main parameter)
6. **No exceptions**: No try-catch-finally
7. **Single file compilation**: No multi-file projects

### Potential Enhancements

1. **Control Flow**:
   - Add `if-else` statements (generates `ifeq`, `ifne`, `goto`)
   - Add loops (generates labels and conditional jumps)

2. **Type System**:
   - Add `bool`, `float`, `double`, `long`, `char`
   - Support arrays and collections
   - Add generics support

3. **Object-Oriented Features**:
   - Class fields (generates `getfield`, `putfield`)
   - Constructors (generates `<init>` methods)
   - Inheritance and interfaces
   - Properties and indexers

4. **Advanced Features**:
   - LINQ (complex transformation to iterator pattern)
   - Async/await (state machine transformation)
   - Lambdas and delegates (generate anonymous classes)
   - Attributes (map to Java annotations)

5. **Standard Library**:
   - Map more .NET framework classes to Java equivalents
   - Collections: List<T> → ArrayList<T>
   - IO: File, Stream classes
   - String manipulation methods

6. **Optimization**:
   - Constant folding
   - Dead code elimination
   - Peephole optimization

## References

- [JVM Specification](https://docs.oracle.com/javase/specs/jvms/se8/html/)
- [Jasmin Assembler](http://jasmin.sourceforge.net/)
- [JVM Instruction Set](https://en.wikipedia.org/wiki/Java_bytecode_instruction_listings)
