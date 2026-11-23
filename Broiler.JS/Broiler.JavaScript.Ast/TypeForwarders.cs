using System.Runtime.CompilerServices;

// The non-tree types decision D50 moves to Broiler.JavaScript.Expressions at window W2 of milestone SJL-14, each with its
// namespace (bundle sjl-14-001's dispositions.md, "moved-forwarded"; SJL-D-25 (a)): an assembly built against this one
// still binds to them through these forwarders. A nested type travels with the type it is nested in.
// FastParseException, which dispositions.md also lists as moved, stays here: its public field Token is a FastToken, a
// tree type that stays in this assembly, and Broiler.JavaScript.Expressions cannot name it (this assembly references
// that one). The departure is recorded by a dated note in dispositions.md.
[assembly: TypeForwardedTo(typeof(Broiler.JavaScript.Ast.Misc.StringSpan))]
[assembly: TypeForwardedTo(typeof(Broiler.JavaScript.Ast.Misc.StringSpanReader))]
[assembly: TypeForwardedTo(typeof(Broiler.JavaScript.Ast.Misc.ArraySpan<>))]
[assembly: TypeForwardedTo(typeof(Broiler.JavaScript.Ast.Misc.IPropertyAccessor))]
[assembly: TypeForwardedTo(typeof(Broiler.JavaScript.Ast.Misc.IPropertyValue))]
[assembly: TypeForwardedTo(typeof(Broiler.JavaScript.Ast.Misc.UnaryOperator))]
[assembly: TypeForwardedTo(typeof(Broiler.JavaScript.Ast.Misc.TokenTypes))]
[assembly: TypeForwardedTo(typeof(Broiler.JavaScript.Runtime.CancellableDisposableAction))]
[assembly: TypeForwardedTo(typeof(Broiler.JavaScript.Runtime.RegExpUnicodeValidator))]
