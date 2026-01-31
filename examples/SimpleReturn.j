// Generated JVM Bytecode for class SimpleReturn
// This is a textual representation of JVM bytecode

.class public SimpleReturn
.super java/lang/Object

.method public static Add(II)I
  .limit stack 2
  .limit locals 2

  iload 0
  iload 1
  iadd
  ireturn
.end method

.method public static Main([Ljava/lang/String;)V
  .limit stack 3
  .limit locals 1

  getstatic java/lang/System/out Ljava/io/PrintStream;
  ldc "Starting program"
  invokevirtual java/io/PrintStream/println(Ljava/lang/String;)V
  return
.end method

