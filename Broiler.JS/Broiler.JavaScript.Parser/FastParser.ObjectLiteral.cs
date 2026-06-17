using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    FastToken lastObjectPropertyIndex;
    int classStaticBlockDepth;

    bool ObjectProperty(out AstClassProperty property, bool checkContextualKeyword = true, bool isAsync = false, bool isClass = false)
    {
        PreventStackoverFlow(ref lastObjectPropertyIndex);

        var begin = BeginUndo();
        var current = begin.Token;

        var isStatic = false;
        if (isClass && stream.CheckAndConsume(FastKeywords.@static))
        {
            // `static` is a class-element modifier only when followed by something
            // that can begin a class element name. When the next token terminates
            // the element (`;`, `}`, EOF), starts an initializer (`=`), or opens a
            // parameter list (`(`), `static` is itself the field/method name — e.g.
            // `class C { static; }`, `static = 1`, `static() {}`.
            if (stream.Current.Type is TokenTypes.Assign or TokenTypes.SemiColon
                or TokenTypes.CurlyBracketEnd or TokenTypes.EOF or TokenTypes.BracketStart)
            {
                begin.Reset();
            }
            else
            {
                isStatic = true;
            }
        }

        if (isStatic && stream.Current.Type == TokenTypes.CurlyBracketStart)
        {
            stream.Consume();
            AstBlock block = null;
            classStaticBlockDepth++;
            try
            {
                if (!Block(out block))
                    throw stream.Unexpected();
            }
            finally
            {
                classStaticBlockDepth--;
            }

            var function = new AstFunctionExpression(current, PreviousToken, false, false, false, null, Sequence<VariableDeclarator>.Empty, block);
            property = new AstClassProperty(current, PreviousToken, AstPropertyKind.Init, isPrivate: false, isStatic: true, propertyName: null, computed: false, init: function);
            return true;
        }

        // The MethodDefinition source text reported by Function.prototype.toString
        // begins at the method's own first token (the `async`/`get`/`set`/`*` prefix
        // or the property name) and excludes the `static` ClassElement modifier. Once
        // `static` is consumed, the current token is that start.
        var methodStart = stream.Current;

        // Check for async methods first. `async get foo()` / `async set foo()` remain
        // invalid ECMAScript syntax; `async get()` / `async set()` are async methods
        // whose property names happen to be `get` / `set`.
        if (stream.CheckAndConsume(FastKeywords.async))
        {
            if (ObjectProperty(out property, true, isClass: isClass, isAsync: true))
            {
                if (property.Kind == AstPropertyKind.Get || property.Kind == AstPropertyKind.Set)
                    throw stream.Unexpected();

                property = new AstClassProperty(current, property.End, AstPropertyKind.Method, property.IsPrivate, isStatic, property.Key, property.Computed, RebaseFunctionStart(property.Init, methodStart), property.UsesColon, property.UsesAssign);
                return true;
            }

            begin.Reset();
        }

        stream.SkipNewLines();

        // `accessor` auto-accessor field (decorators proposal):
        //   accessor [no LineTerminator here] ClassElementName Initializer?
        // Only a modifier inside a class body, and only when a ClassElementName
        // follows on the same line; otherwise `accessor` is itself the element name
        // (`class C { accessor; }`, `accessor = 1`, `accessor() {}`). Modelled as a
        // plain data field — the auto-accessor getter/setter + private backing
        // storage semantics are not implemented, which is sufficient for the field
        // grammar to parse and evaluate.
        if (isClass && checkContextualKeyword && !isAsync
            && stream.Current.ContextualKeyword == FastKeywords.accessor)
        {
            var accUndo = Location;
            stream.Consume(); // 'accessor'

            var nextType = stream.Current.Type;
            if (nextType is not (TokenTypes.LineTerminator or TokenTypes.Assign
                    or TokenTypes.SemiColon or TokenTypes.Colon or TokenTypes.BracketStart
                    or TokenTypes.CurlyBracketEnd or TokenTypes.EOF or TokenTypes.Multiply)
                && PropertyName(out var accKey, out var accComputed, out var accPrivate, acceptKeywords: true))
            {
                AstExpression accInit = null;
                if (stream.CheckAndConsume(TokenTypes.Assign) && !Expression(out accInit))
                    throw stream.Unexpected();

                property = new AstClassProperty(current, PreviousToken, AstPropertyKind.Data, accPrivate, isStatic, accKey, accComputed, accInit);
                stream.CheckAndConsume(TokenTypes.SemiColon);
                return true;
            }

            accUndo.Reset();
        }

        var sc = stream.Current;
        var isGet = sc.ContextualKeyword == FastKeywords.get;
        var isSet = sc.ContextualKeyword == FastKeywords.set;

        bool isGenerator = stream.CheckAndConsume(TokenTypes.Multiply);
        if (PropertyName(out var key, out var computed, out var isPrivate, acceptKeywords: true))
        {
            // A getter/setter is never a generator, so `get`/`set` followed by a
            // generator marker `*` on a NEW line is not an accessor: ASI makes
            // `get`/`set` a (data) field and the `*name(){}` a separate generator
            // method (test262 grammar-field-named-{get,set}-followed-by-generator-asi).
            // Leave it to the field path below. `get *a(){}` on the SAME line stays
            // a SyntaxError (handled by the accessor recursion).
            var getSetGeneratorAsi = isClass && (isSet || isGet);
            if (getSetGeneratorAsi)
            {
                var genPeek = stream.SkipNewLines();
                getSetGeneratorAsi = genPeek.LinesSkipped && stream.Current.Type == TokenTypes.Multiply;
                genPeek.Undo();
            }

            if (checkContextualKeyword && (isSet || isGet) && !getSetGeneratorAsi)
            {
                var accessorNameStart = Location;
                if (ObjectProperty(out property, isClass: isClass, isAsync: isAsync))
                {
                    // A method named `constructor` is classified as Constructor by the
                    // recursion. As the body of an accessor it is just a method whose
                    // name is "constructor", which is valid only for a STATIC accessor
                    // (`static get constructor(){}`); an instance `get`/`set
                    // constructor` falls through and is reported as a SyntaxError.
                    if (property.Kind == AstPropertyKind.Method
                        || (property.Kind == AstPropertyKind.Constructor && isStatic))
                    {
                        // A getter/setter is never a generator: `{get *a(){}}` /
                        // `{set *a(c){}}` are SyntaxErrors (the accessor method the
                        // recursion produced carries the `*`).
                        if (property.Init is AstFunctionExpression { Generator: true })
                            throw stream.Unexpected();

                        property = new AstClassProperty(current, property.End, isSet ? AstPropertyKind.Set : AstPropertyKind.Get, property.IsPrivate, isStatic, property.Key, property.Computed, RebaseFunctionStart(property.Init, methodStart), property.UsesColon, property.UsesAssign);
                        return true;
                    }

                    accessorNameStart.Reset();
                }
            }

            var propertyNameEnd = PreviousToken;
            var separator = stream.SkipNewLines();

            // A generator marker (`*name`) only introduces a method definition, so
            // the property name must be followed by a parameter list. `{*a : 1}`,
            // `{*a}`, `{*a = 1}` etc. are SyntaxErrors.
            if (isGenerator && stream.Current.Type != TokenTypes.BracketStart)
                throw stream.Unexpected();

            if (stream.CheckAndConsume(TokenTypes.Assign))
            {
                if (!checkContextualKeyword)
                    throw stream.Unexpected();

                // A class field initializer is its own [~Yield, ~Await] function
                // boundary: `await`/`yield` are plain IdentifierReferences inside it
                // even when the class is nested in an async/generator function. (The
                // surrounding [?Yield, ?Await] only governs the computed
                // ClassElementName and the heritage expression, which are parsed
                // outside this scope.) So `class { x = await }` inside `async
                // function f(){…}` reads `await` as an identifier.
                var previousInGeneratorBody = inGeneratorBody;
                var previousInAsyncFunctionBody = inAsyncFunctionBody;
                if (isClass)
                {
                    inGeneratorBody = false;
                    inAsyncFunctionBody = false;
                }

                AstExpression value;
                try
                {
                    if (!Expression(out value))
                        throw stream.Unexpected();
                }
                finally
                {
                    inGeneratorBody = previousInGeneratorBody;
                    inAsyncFunctionBody = previousInAsyncFunctionBody;
                }

                property = new AstClassProperty(current, PreviousToken, AstPropertyKind.Data, isPrivate, isStatic, key, computed, value, usesAssign: true);

                // A FieldDefinition must be terminated by `;`, a line terminator
                // (ASI), `}`, or EOF. Enforcing this rejects an initializer that runs
                // straight into the next token, e.g. `class { x = await 1 }`, where
                // `await` is the (bare) identifier and `1` cannot follow it.
                if (isClass)
                {
                    if (!EndOfStatement())
                        throw stream.Unexpected();
                }
                else
                {
                    stream.CheckAndConsume(TokenTypes.SemiColon);
                }

                return true;
            }

            if (stream.CheckAndConsume(TokenTypes.Colon))
            {
                // A `key: value` colon is object-literal syntax; it is never a valid
                // ClassElement. `class X { x: 1 }` is a SyntaxError (the ClassElement
                // productions are methods, accessors, fields and static blocks only).
                if (!checkContextualKeyword || isClass)
                    throw stream.Unexpected();

                if (!Expression(out var value))
                    throw stream.Unexpected();

                property = new AstClassProperty(current, PreviousToken, AstPropertyKind.Data, isPrivate, isStatic, key, computed, value, usesColon: true);
                return true;
            }
            else if (stream.CheckAndConsume(TokenTypes.BracketStart))
            {
                // add the scope...
                var scope = variableScope.Push(PreviousToken, FastNodeType.FunctionExpression);
                try
                {

                    if (!Parameters(out var parameters, checkForBracketStart: false))
                        throw stream.Unexpected();

                    functionDepth++;
                    var previousInGeneratorBody = inGeneratorBody;
                    var previousInAsyncFunctionBody = inAsyncFunctionBody;
                    inGeneratorBody = isGenerator;
                    inAsyncFunctionBody = isAsync;
                    AstStatement body;
                    try
                    {
                        if (!Statement(out body))
                            throw stream.Unexpected();
                    }
                    finally
                    {
                        inGeneratorBody = previousInGeneratorBody;
                        inAsyncFunctionBody = previousInAsyncFunctionBody;
                        functionDepth--;
                    }

                    if (body.Type != FastNodeType.Block)
                        throw stream.Unexpected();

                    var fx = new AstFunctionExpression(methodStart, PreviousToken, false, isAsync, isGenerator, null, parameters, body);

                    var isConstructor = isClass
                        && !computed
                        && !isPrivate
                        && !isStatic
                        && (key is AstIdentifier keyIdentifier && keyIdentifier.Name.Value == "constructor"
                            || key is AstLiteral { TokenType: TokenTypes.String, StringValue: "constructor" });
                    property = new AstClassProperty(current, PreviousToken, isConstructor ? AstPropertyKind.Constructor : AstPropertyKind.Method,
                        isPrivate, isStatic, key, computed, fx);
                    return true;
                }
                finally
                {
                    scope.Dispose();
                }
            }
            else if (isClass && (separator.LinesSkipped || stream.Current.Type == TokenTypes.SemiColon || stream.Current.Type == TokenTypes.CurlyBracketEnd || stream.Current.Type == TokenTypes.EOF))
            {
                property = new AstClassProperty(current, propertyNameEnd, AstPropertyKind.Data, isPrivate, isStatic, key, computed, null);
                return true;
            }
            else if (stream.Current.Type == TokenTypes.Comma || stream.Current.Type == TokenTypes.CurlyBracketEnd || stream.Current.Type == TokenTypes.EOF)
            {
                if (computed || key is AstLiteral)
                    throw stream.Unexpected();
                property = new AstClassProperty(current, PreviousToken, AstPropertyKind.Data, isPrivate, isStatic, key, computed, key);
                return true;
            }
            else throw stream.Unexpected();
        }

        property = default;
        return begin.Reset();
    }

    // Re-anchor a method's function-expression source span to <paramref name="start"/>
    // so Function.prototype.toString includes the `get`/`set`/`async`/`*` prefix that
    // precedes the property name in the original source.
    static AstExpression RebaseFunctionStart(AstExpression init, FastToken start)
    {
        if (init is AstFunctionExpression fx)
            return new AstFunctionExpression(start, fx.End, fx.IsArrowFunction, fx.Async, fx.Generator, fx.Id, fx.Params, fx.Body, fx.IsStatement);

        return init;
    }

    bool ObjectLiteral(out AstExpression node)
    {
        var begin = stream.Current;
        node = default;
        stream.Consume();

        var nodes = new Sequence<AstNode>();
        SkipNewLines();

        while (!stream.CheckAndConsumeAny(TokenTypes.CurlyBracketEnd, TokenTypes.EOF))
        {
            SkipNewLines();
            var current = stream.Current;

            if (stream.CheckAndConsume(TokenTypes.TripleDots))
            {
                if (!Expression(out var exp))
                    throw stream.Unexpected();

                nodes.Add(new AstSpreadElement(current, exp.End, exp));
            }
            else if (ObjectProperty(out var property))
            {
                nodes.Add(property);
            }
            else
            {
                // The while-condition already consumed a closing `}`/EOF, so any
                // position that is neither a spread nor a property here (e.g. an
                // elision such as `{,a(){}}` or `{a,,b}`) is a SyntaxError.
                throw stream.Unexpected();
            }

            if (stream.CheckAndConsume(TokenTypes.Comma))
                continue;
        }

        node = new AstObjectLiteral(begin, PreviousToken, nodes);
        return true;
    }
}
