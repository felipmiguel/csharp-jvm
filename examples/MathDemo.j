// Generated JVM Bytecode for class MathDemo
// This is a textual representation of JVM bytecode

.class public MathDemo
.super java/lang/Object

.method public static Main([Ljava/lang/String;)V
  .limit stack 3
  .limit locals 5

  bipush 10
  istore 1
  bipush 20
  istore 2
  iload 1
  iload 2
  iadd
  istore 3
  getstatic java/lang/System/out Ljava/io/PrintStream;
  iload 3
  invokevirtual java/io/PrintStream/println(I)V
  iload 1
  iload 2
  imul
  istore 4
  getstatic java/lang/System/out Ljava/io/PrintStream;
  iload 4
  invokevirtual java/io/PrintStream/println(I)V
  return
.end method

