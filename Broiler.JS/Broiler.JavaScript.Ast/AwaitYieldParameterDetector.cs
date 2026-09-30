using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Statements;

namespace Broiler.JavaScript.Ast;

/// <summary>
/// Detects whether an expression that is about to be reinterpreted as arrow-function
/// parameters Contains an AwaitExpression or YieldExpression (ECMA-262 early error:
/// "ArrowParameters Contains AwaitExpression / YieldExpression"). The "Contains" static
/// semantic does not descend into nested function or class boundaries — an
/// await/yield there belongs to that inner scope — so those nodes are not traversed.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s15.3.1; IP=Low; Security=High; Resources=5; Fingerprint=TBF
// Broiler-Falsified-If: an await or yield inside a class heritage or a computed class-member key within arrow parameters, such as `(a = class extends (await 0) {}) => 0` in an async function, is accepted instead of raising the early SyntaxError
// Broiler-Human:        PENDING
public sealed class AwaitYieldParameterDetector : AstReduce
{
    private bool found;

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=5; Fingerprint=TBF
    // Broiler-Falsified-If: an await nested in a destructuring default or another non-function subexpression of arrow parameters, such as `async ({a = await x}) => 0`, makes Contains return false
    // Broiler-Human:        PENDING
    public static bool Contains(AstNode node)
    {
        if (node == null)
            return false;

        var detector = new AwaitYieldParameterDetector();
        detector.Visit(node);
        return detector.found;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: an AwaitExpression reached by the walk leaves found unset, so `async (a = await x) => 0` parses without an early SyntaxError
    // Broiler-Human:        PENDING
    protected override AstNode VisitAwaitExpression(AstAwaitExpression node)
    {
        found = true;
        return node;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: a YieldExpression reached by the walk leaves found unset, so `(a = yield) => 0` inside a generator parses without an early SyntaxError
    // Broiler-Human:        PENDING
    protected override AstNode VisitYieldExpression(AstYieldExpression yieldExpression)
    {
        found = true;
        return yieldExpression;
    }

    // Nested function / arrow / class bodies open their own [Await]/[Yield] scope, so an
    // await/yield inside them is not part of the enclosing arrow's parameters: stop here.
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: an await inside a nested function body within arrow parameters, such as `async (a = async function () { await x }) => 0`, is reported as contained and the valid arrow is rejected
    // Broiler-Human:        PENDING
    protected override AstNode VisitFunctionExpression(AstFunctionExpression functionExpression)
        => functionExpression;

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: an await or yield in a class heritage or computed member key inside arrow parameters, such as `(a = class { [await 0]() {} }) => 0` in an async function, is never visited and the arrow is accepted
    // Broiler-Human:        PENDING
    protected override AstNode VisitClassStatement(AstClassExpression classStatement)
        => classStatement;
}
