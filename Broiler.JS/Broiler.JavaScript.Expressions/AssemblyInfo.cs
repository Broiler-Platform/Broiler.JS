using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Broiler.JavaScript.BuiltIns")]

// The bridge reads CompilationStack.CurrentWorkerStackBytes (milestone SJL-14.2, decision D47).
[assembly: InternalsVisibleTo("Broiler.JavaScript.Parser")]
