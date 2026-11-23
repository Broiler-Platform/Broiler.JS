using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Patterns;

[System.Obsolete(ObsoleteMarks.Ast, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public class AstBindingPattern(FastToken start, FastNodeType type, FastToken end) : AstExpression(start, type, end, true) { }
