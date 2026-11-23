using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Broiler.JavaScript.BuiltIns")]

// The bridge reads CompilationStack.CurrentWorkerStackBytes (milestone SJL-14.2, decision D47).
[assembly: InternalsVisibleTo("Broiler.JavaScript.Parser")]

// The Unicode tables and their adapter (Unicode/), internal since they were Runtime's and moved here from Ast at window
// W2 of milestone SJL-14 (decision D50): the parser's scanner, the bridge and its regular-expression check read them,
// Ast's RegExpUnicodeValidator and the built-ins' RegExp, String and Intl read them, the portable compiler parses over
// them (decision D53), and the tests hold the tables equal to the generator's output. The grant to the built-ins is the
// one above.
[assembly: InternalsVisibleTo("Broiler.JavaScript.Ast")]
[assembly: InternalsVisibleTo("Broiler.JavaScript.Portable.Compiler")]
[assembly: InternalsVisibleTo("Broiler.JavaScript.Ast.Tests")]
[assembly: InternalsVisibleTo("Broiler.JavaScript.Parser.Tests")]
