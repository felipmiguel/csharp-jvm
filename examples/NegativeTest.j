// Generated JVM Bytecode for class NegativeTest
// This is a textual representation of JVM bytecode

.class public NegativeTest
.super java/lang/Object

.method public static Main([Ljava/lang/String;)V
  .limit stack 3
  .limit locals 3

  iconst_0
  istore 1
  iload 1
  iconst_5
  iadd
  istore 2
  getstatic java/lang/System/out Ljava/io/PrintStream;
  iload 2
  invokevirtual java/io/PrintStream/println(I)V
  return
.end method

