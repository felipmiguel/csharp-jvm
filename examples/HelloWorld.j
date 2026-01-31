// Generated JVM Bytecode for class HelloWorld
// This is a textual representation of JVM bytecode

.class public HelloWorld
.super java/lang/Object

.method public static Main([Ljava/lang/String;)V
  .limit stack 3
  .limit locals 1

  getstatic java/lang/System/out Ljava/io/PrintStream;
  ldc "Hello from C# compiled to JVM!"
  invokevirtual java/io/PrintStream/println(Ljava/lang/String;)V
  return
.end method

