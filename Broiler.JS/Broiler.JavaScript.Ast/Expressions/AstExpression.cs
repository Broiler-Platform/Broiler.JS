using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Expressions;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=1; Fingerprint=TBF
// Broiler-Human:        PENDING
public class AstExpression(FastToken start, FastNodeType type, FastToken end, bool isBinding = false) : AstNode(start, type, end, false, isBinding) { }
