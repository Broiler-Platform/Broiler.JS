using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler;
using System;

namespace Broiler.JavaScript.Ast;


// Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
// Broiler-Falsified-If: a script nested deeply enough to exhaust the compiling thread's stack aborts the process during a lowering or syntax-validation walk instead of segmenting onto a fresh stack
// Broiler-Human:        PENDING
public abstract class AstMapVisitor<T>
{
    public bool IsStrictMode { get; set; } = false;

    public bool Debug { get; set; } = true;

    // Roadmap item 1-2. This walk recurses once per level of source NESTING, so a deeply nested
    // expression consumes the compiling thread's stack in proportion to its depth — and it is
    // this pass, not the IL emitter, that overflows first on a chain of ~19 400 operators:
    // AstReduce.VisitBinaryExpression under SyntaxValidation.StrictModeValidator. StackGuard
    // covered the emitter's visitors and never this one, which is why repairing StackGuard alone
    // did not move the repro.
    private StackSegment segment;

    // Broiler-AI:           Origin=AI; IP=Low; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: a 20 000-operator chain compiled on a 1 MiB thread overflows the stack inside this walk instead of segmenting or unwinding with StackSegmentExhausted
    // Broiler-Human:        PENDING
    public unsafe T Visit(AstNode node) {

        if (node == null)
            return default;

        int self;
        var current = (nint)(&self);
        var outermost = !segment.IsAnchored;

        if (segment.ShouldSegment(current))
            return segment.Continue(() => VisitCore(node));

        if (!outermost)
            return VisitCore(node);

        try
        {
            return VisitCore(node);
        }
        finally
        {
            segment.Release();
        }
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: a FastNodeType the parser can produce has no arm, so compiling that construct throws NotImplementedException out of the compiler instead of a SyntaxError
    // Broiler-Human:        PENDING
    private T VisitCore(AstNode node) {

        return node.Type switch
        {
            FastNodeType.ArrayPattern => VisitArrayPattern(node as AstArrayPattern),
            FastNodeType.Block => VisitBlock(node as AstBlock),
            FastNodeType.Program => VisitProgram(node as AstProgram),
            FastNodeType.BreakStatement => VisitBreakStatement(node as AstBreakStatement),
            FastNodeType.BinaryExpression => VisitBinaryExpression(node as AstBinaryExpression),
            FastNodeType.VariableDeclaration => VisitVariableDeclaration(node as AstVariableDeclaration),
            FastNodeType.ExpressionStatement => VisitExpressionStatement(node as AstExpressionStatement),
            FastNodeType.FunctionExpression => VisitFunctionExpression(node as AstFunctionExpression),
            FastNodeType.Identifier => VisitIdentifier(node as AstIdentifier),
            FastNodeType.ObjectPattern => VisitObjectPattern(node as AstObjectPattern),
            FastNodeType.SpreadElement => VisitSpreadElement(node as AstSpreadElement),
            FastNodeType.IfStatement => VisitIfStatement(node as AstIfStatement),
            FastNodeType.WhileStatement => VisitWhileStatement(node as AstWhileStatement),
            FastNodeType.DoWhileStatement => VisitDoWhileStatement(node as AstDoWhileStatement),
            FastNodeType.SequenceExpression => VisitSequenceExpression(node as AstSequenceExpression),
            FastNodeType.ForStatement => VisitForStatement(node as AstForStatement),
            FastNodeType.ForInStatement => VisitForInStatement(node as AstForInStatement),
            FastNodeType.ForOfStatement => VisitForOfStatement(node as AstForOfStatement),
            FastNodeType.ContinueStatement => VisitContinueStatement(node as AstContinueStatement),
            FastNodeType.ThrowStatement => VisitThrowStatement(node as AstThrowStatement),
            FastNodeType.TryStatement => VisitTryStatement(node as AstTryStatement),
            FastNodeType.WithStatement => VisitWithStatement(node as AstWithStatement),
            FastNodeType.DebuggerStatement => VisitDebuggerStatement(node as AstDebuggerStatement),
            FastNodeType.LabeledStatement => VisitLabeledStatement(node as AstLabeledStatement),
            FastNodeType.Literal => VisitLiteral(node as AstLiteral),
            FastNodeType.MemberExpression => VisitMemberExpression(node as AstMemberExpression),
            FastNodeType.ClassStatement => VisitClassStatement(node as AstClassExpression),
            FastNodeType.SwitchStatement => VisitSwitchStatement(node as AstSwitchStatement),
            FastNodeType.EmptyExpression => VisitEmptyExpression(node as AstEmptyExpression),
            FastNodeType.ArrayExpression => VisitArrayExpression(node as AstArrayExpression),
            FastNodeType.ObjectLiteral => VisitObjectLiteral(node as AstObjectLiteral),
            FastNodeType.TemplateExpression => VisitTemplateExpression(node as AstTemplateExpression),
            FastNodeType.UnaryExpression => VisitUnaryExpression(node as AstUnaryExpression),
            FastNodeType.CallExpression => VisitCallExpression(node as AstCallExpression),
            FastNodeType.ConditionalExpression => VisitConditionalExpression(node as AstConditionalExpression),
            FastNodeType.YieldExpression => VisitYieldExpression(node as AstYieldExpression),
            FastNodeType.ClassProperty => VisitClassProperty(node as AstClassProperty),
            FastNodeType.ReturnStatement => VisitReturnStatement(node as AstReturnStatement),
            FastNodeType.NewExpression => VisitNewExpression(node as AstNewExpression),
            FastNodeType.ImportStatement => VisitImportStatement(node as AstImportStatement),
            FastNodeType.ExportStatement => VisitExportStatement(node as AstExportStatement),
            FastNodeType.Meta => VisitMeta(node as AstMeta),
            FastNodeType.TaggedTemplateExpression => VisitTaggedTemplateExpression(node as AstTaggedTemplateExpression),
            FastNodeType.AwaitExpression => VisitAwaitExpression(node as AstAwaitExpression),
            FastNodeType.ImportCall => VisitImportCall(node as AstImportCall),
            FastNodeType.OptionalChain => VisitOptionalChain(node as AstOptionalChain),
            FastNodeType.Super => VisitSuper(node as AstSuper),
            _ => throw new NotImplementedException($"No implementation for {node.Type}"),
        };
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation lowers `await x` without a suspension point, so code after it runs before the awaited promise settles
    // Broiler-Human:        PENDING
    protected abstract T VisitAwaitExpression(AstAwaitExpression node);
    // Broiler-AI:           Origin=AI; IP=Low; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation lets a specifier whose ToString throws in `import(x)` escape synchronously instead of returning a rejected promise
    // Broiler-Human:        PENDING
    protected abstract T VisitImportCall(AstImportCall node);
    // Broiler-AI:           Origin=AI; IP=Low; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation evaluates the rest of `a?.b.c` or throws when `a` is nullish instead of yielding undefined
    // Broiler-Human:        PENDING
    protected abstract T VisitOptionalChain(AstOptionalChain node);
    // Broiler-AI:           Origin=AI; IP=Low; Security=Critical; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation lowers a `super` reference outside a method that has a home object instead of raising an early SyntaxError
    // Broiler-Human:        PENDING
    protected abstract T VisitSuper(AstSuper super);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation passes the tag a fresh strings array on each evaluation of the same site, so a tagged template in a loop sees different template objects
    // Broiler-Human:        PENDING
    protected abstract T VisitTaggedTemplateExpression(AstTaggedTemplateExpression astTaggedTemplateExpression);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation makes `new.target` in a function called without `new` evaluate to something other than undefined
    // Broiler-Human:        PENDING
    protected abstract T VisitMeta(AstMeta astMeta);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation exports a copy of a binding, so an importer reads a stale value after the exporting module reassigns it
    // Broiler-Human:        PENDING
    protected abstract T VisitExportStatement(AstExportStatement astExportStatement);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation binds an imported name as a writable local, so assigning to it in the importing module succeeds instead of throwing TypeError
    // Broiler-Human:        PENDING
    protected abstract T VisitImportStatement(AstImportStatement astImportStatement);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation does not call the iterator's return method when `[a, b] = iterable` stops before the iterator is done
    // Broiler-Human:        PENDING
    protected abstract T VisitArrayPattern(AstArrayPattern arrayPattern);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation lowers `new f()` on an arrow function or method to a call instead of throwing TypeError
    // Broiler-Human:        PENDING
    protected abstract T VisitNewExpression(AstNewExpression newExpression);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation lets a `return` inside `try` leave the function without running its `finally` block
    // Broiler-Human:        PENDING
    protected abstract T VisitReturnStatement(AstReturnStatement returnStatement);
    protected virtual T VisitClassProperty(AstClassProperty property) => default;
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation lowers `break L` to a jump when no enclosing statement carries label L instead of raising a SyntaxError
    // Broiler-Human:        PENDING
    protected abstract T VisitBreakStatement(AstBreakStatement breakStatement);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation lowers a labelled loop without handing the label to the loop visitor, so `continue L` inside it leaves the loop instead of starting its next iteration
    // Broiler-Human:        PENDING
    protected abstract T VisitLabeledStatement(AstLabeledStatement labeledStatement);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation lowers `yield* inner` as one yield of the iterator object instead of delegating each value of inner
    // Broiler-Human:        PENDING
    protected abstract T VisitYieldExpression(AstYieldExpression yieldExpression);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation lowers a method call `o.m()` with an undefined receiver, so `this` inside m is not `o`
    // Broiler-Human:        PENDING
    protected abstract T VisitCallExpression(AstCallExpression callExpression);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation makes `typeof undeclaredName` throw ReferenceError instead of yielding the string undefined
    // Broiler-Human:        PENDING
    protected abstract T VisitUnaryExpression(AstUnaryExpression unaryExpression);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation converts a substitution without ToString, so an object whose toString and valueOf differ is rendered through valueOf
    // Broiler-Human:        PENDING
    protected abstract T VisitTemplateExpression(AstTemplateExpression templateExpression);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation turns `{ __proto__: p }` into an own property named __proto__ instead of setting the new object's prototype to p
    // Broiler-Human:        PENDING
    protected abstract T VisitObjectLiteral(AstObjectLiteral objectLiteral);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation lowers the hole in `[1, , 3]` as an own element holding undefined, so `1 in arr` is true
    // Broiler-Human:        PENDING
    protected abstract T VisitArrayExpression(AstArrayExpression arrayExpression);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation lets an empty statement change the completion value, so `eval('1;;')` returns something other than 1
    // Broiler-Human:        PENDING
    protected abstract T VisitEmptyExpression(AstEmptyExpression emptyExpression);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation matches case labels with loose equality, so `switch (1) { case '1': }` enters that case
    // Broiler-Human:        PENDING
    protected abstract T VisitSwitchStatement(AstSwitchStatement switchStatement);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation defines class methods as enumerable prototype properties, so `for (k in new C)` lists them
    // Broiler-Human:        PENDING
    protected abstract T VisitClassStatement(AstClassExpression classStatement);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation lets `obj.#x` on an object without that private brand read undefined instead of throwing TypeError
    // Broiler-Human:        PENDING
    protected abstract T VisitMemberExpression(AstMemberExpression memberExpression);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation returns the same RegExp object each time a regex literal is evaluated, so lastIndex carries over between loop iterations
    // Broiler-Human:        PENDING
    protected abstract T VisitLiteral(AstLiteral literal);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation makes a `debugger` statement throw or stop execution when no debugger is attached instead of doing nothing
    // Broiler-Human:        PENDING
    protected abstract T VisitDebuggerStatement(AstDebuggerStatement debuggerStatement);
    // Broiler-AI:           Origin=AI; IP=Low; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation resolves a name inside `with (o)` to o's property even when o's Symbol.unscopables lists it, instead of the outer binding
    // Broiler-Human:        PENDING
    protected abstract T VisitWithStatement(AstWithStatement withStatement);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation skips the `finally` block when the `catch` block itself throws
    // Broiler-Human:        PENDING
    protected abstract T VisitTryStatement(AstTryStatement tryStatement);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation delivers something other than the thrown value to the catch binding, so `throw 1` is caught as a wrapped host exception
    // Broiler-Human:        PENDING
    protected abstract T VisitThrowStatement(AstThrowStatement throwStatement);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation jumps `continue L` to the labelled loop's break target, leaving the loop instead of starting its next iteration
    // Broiler-Human:        PENDING
    protected abstract T VisitContinueStatement(AstContinueStatement continueStatement);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation does not call the iterator's return method when the body of `for (x of it)` breaks out of the loop
    // Broiler-Human:        PENDING
    protected abstract T VisitForOfStatement(AstForOfStatement forOfStatement, string label = null);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation still visits a key that the loop body deleted before it was reached in `for (k in o)`
    // Broiler-Human:        PENDING
    protected abstract T VisitForInStatement(AstForInStatement forInStatement, string label = null);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation shares one binding across iterations of `for (let i = 0; i < 3; i++)`, so closures made in the body all see the final i
    // Broiler-Human:        PENDING
    protected abstract T VisitForStatement(AstForStatement forStatement, string label = null);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation skips evaluating a() in `(a(), b())` or yields a()'s value instead of b()'s
    // Broiler-Human:        PENDING
    protected abstract T VisitSequenceExpression(AstSequenceExpression sequenceExpression);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation lets `continue` inside `do { } while (c)` skip the test of c
    // Broiler-Human:        PENDING
    protected abstract T VisitDoWhileStatement(AstDoWhileStatement doWhileStatement, string label = null);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation lets `continue` inside `while (c)` re-enter the body without testing c again
    // Broiler-Human:        PENDING
    protected abstract T VisitWhileStatement(AstWhileStatement whileStatement, string label = null);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation tests the condition with something other than ToBoolean, so `if (0n)` or an empty-string condition takes the consequent
    // Broiler-Human:        PENDING
    protected abstract T VisitIfStatement(AstIfStatement ifStatement);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation lets a spread element outside an array literal, call or object literal escape as a host NotImplementedException instead of a SyntaxError
    // Broiler-Human:        PENDING
    protected abstract T VisitSpreadElement(AstSpreadElement spreadElement);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation copies non-enumerable or already destructured properties into `rest` for `const { a, ...rest } = o`
    // Broiler-Human:        PENDING
    protected abstract T VisitObjectPattern(AstObjectPattern objectPattern);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=4; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation lets a read of a `let` binding before its declaration yield undefined instead of throwing ReferenceError
    // Broiler-Human:        PENDING
    protected abstract T VisitIdentifier(AstIdentifier identifier);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation gives an arrow function its own `this`, so `this` inside it differs from the enclosing function's
    // Broiler-Human:        PENDING
    protected abstract T VisitFunctionExpression(AstFunctionExpression functionExpression);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation lets a function declaration statement update the completion value, so `eval('1; function f(){}')` returns the function instead of 1
    // Broiler-Human:        PENDING
    protected abstract T VisitExpressionStatement(AstExpressionStatement expressionStatement);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation lets an assignment to a `const` binding after its declaration succeed instead of throwing TypeError
    // Broiler-Human:        PENDING
    protected abstract T VisitVariableDeclaration(AstVariableDeclaration variableDeclaration);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation evaluates b in `a && b` when a is falsy
    // Broiler-Human:        PENDING
    protected abstract T VisitBinaryExpression(AstBinaryExpression binaryExpression);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation does not hoist a top-level function declaration, so calling it on a line before the declaration throws
    // Broiler-Human:        PENDING
    protected abstract T VisitProgram(AstProgram program);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation leaves a `let` declared inside a block visible to code after the block ends
    // Broiler-Human:        PENDING
    protected abstract T VisitBlock(AstBlock block);
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Critical; Resources=6; Fingerprint=TBF
    // Broiler-Falsified-If: an implementation evaluates both a() and b() in `c ? a() : b()`
    // Broiler-Human:        PENDING
    protected abstract T VisitConditionalExpression(AstConditionalExpression conditionalExpression);
}
