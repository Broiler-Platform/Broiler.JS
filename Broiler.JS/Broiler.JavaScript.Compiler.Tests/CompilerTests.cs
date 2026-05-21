using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Compiler.Tests;

public class CompilerTests
{
    [Fact]
    public void Compile_SimpleExpression_ProducesResult()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("1 + 2 * 3");
        Assert.Equal(7.0, result.DoubleValue);
    }

    [Fact]
    public void Compile_LetConst_ScopingWorks()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("let x = 5; const y = 10; x + y;");
        Assert.Equal(15.0, result.DoubleValue);
    }

    [Fact]
    public void Compile_ArrowFunction_Works()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("((x) => x * 2)(21)");
        Assert.Equal(42.0, result.DoubleValue);
    }

    [Fact]
    public void Compile_ArrowFunction_ArrayDestructuringElisions_Work()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var first = 0;
                var second = 0;

                function* g() {
                    first += 1;
                    yield 0;
                    second += 1;
                }

                var parts = [];
                (([,]) => { parts.push(first + '|' + second); })(g());

                first = 0;
                second = 0;
                (([[,] = g()]) => { parts.push(first + '|' + second); })([]);

                first = 0;
                second = 0;
                (([...[,]]) => { parts.push(first + '|' + second); })(g());

                return parts.join(';');
            })()
            """);

        Assert.Equal("1|0;1|0;1|1", result.ToString());
    }

    [Fact]
    public void Compile_ArrowFunction_ArrayDestructuringElisions_Work_With_BareYield_Generator()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var first = 0;
                var second = 0;

                function* g() {
                    first += 1;
                    yield;
                    second += 1;
                }

                var callCount = 0;
                var f = ([,]) => {
                    callCount += 1;
                    return first + '|' + second;
                };

                var outcome = f(g());
                return outcome + ';' + callCount;
            })()
            """);

        Assert.Equal("1|0;1", result.ToString());
    }

    [Fact]
    public void Compile_ArgumentsObject_WorksWithoutExplicitModulesLoad()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("(function () { return arguments.length; })(1, 2, 3)");
        Assert.Equal(3.0, result.DoubleValue);
    }

    [Fact]
    public void JSContext_LoadsClrInteropWithoutExplicitClrReference()
    {
        using var ctx = new JSContext();

        Assert.Equal("Broiler.JavaScript.Clr.DefaultClrInterop", JSEngine.ClrInterop.GetType().FullName);
        Assert.NotNull(JSEngine.ClrModuleProvider);
    }

    [Fact]
    public void Compile_ObjectDestructuringDefault_OnlyFallsBackForUndefined()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var initCount = 0;
                var source = { a: null, b: undefined };
                var { a = (++initCount, 'fallback-a'), b = (++initCount, 'fallback-b') } = source;
                return String(a) + '|' + b + '|' + initCount;
            })()
            """);

        Assert.Equal("null|fallback-b|1", result.ToString());
    }

    [Fact]
    public void Compile_ObjectDestructuringParameterDefault_KeepsNullValues()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var initCount = 0;
                return (({ a = (++initCount, 'fallback') }) => String(a) + '|' + initCount)({ a: null });
            })()
            """);

        Assert.Equal("null|0", result.ToString());
    }

    [Fact]
    public void Compile_DestructuringDefaults_Infer_Anonymous_Arrow_Function_Names()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var arrayName = (([arrow = () => {}] = []) => arrow.name)();
                var objectName = (({ arrow = () => {} } = {}) => arrow.name)();
                return arrayName + '|' + objectName;
            })()
            """);

        Assert.Equal("arrow|arrow", result.ToString());
    }

    [Fact]
    public void Compile_DestructuringDefaults_Infer_Anonymous_Class_Names()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var arrayName = (([cls = class {}] = []) => cls.name)();
                var objectName = (({ cls = class {} } = {}) => cls.name)();
                return arrayName + '|' + objectName;
            })()
            """);

        Assert.Equal("cls|cls", result.ToString());
    }

    [Fact]
    public void Compile_Class_Accessor_Literal_Names_Are_Canonicalized_And_Inferred()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                class C {
                    get "string"() { return 1; }
                    set "string"(value) {}
                    get 0b10() { return 2; }
                    static get "\u0073tatic"() { return 3; }
                    static set "\u0073tatic"(value) {}
                    static get 1e2() { return 4; }
                }

                var stringDesc = Object.getOwnPropertyDescriptor(C.prototype, "string");
                var numericDesc = Object.getOwnPropertyDescriptor(C.prototype, 2);
                var staticStringDesc = Object.getOwnPropertyDescriptor(C, "static");
                var staticNumericDesc = Object.getOwnPropertyDescriptor(C, 100);

                return [
                    stringDesc.get.name,
                    stringDesc.set.name,
                    numericDesc === undefined ? "missing" : numericDesc.get.name,
                    staticStringDesc.get.name,
                    staticStringDesc.set.name,
                    staticNumericDesc === undefined ? "missing" : staticNumericDesc.get.name
                ].join('|');
            })()
            """);

        Assert.Equal("get string|set string|get 2|get static|set static|get 100", result.ToString());
    }

    [Fact]
    public void Compile_DestructuringDefaults_DoNotInfer_Function_Names_Through_Cover_Grammar()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var parameterName = (({ xCover = (0, function () {}) } = {}) => xCover.name)();
                var arrayName = (([xCover = (0, function () {})] = []) => xCover.name)();
                return (parameterName === '') + '|' + (arrayName === '');
            })()
            """);

        Assert.Equal("true|true", result.ToString());
    }

    [Fact]
    public void Compile_FunctionLength_Ignores_Trailing_Commas_And_Stops_At_First_Initializer_Or_Rest()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                class C {
                    method(a,) {}
                    methodDefault(a = 1,) {}
                    *gen(a,) {}
                    *genDefault(a = 1,) {}
                    async asyncMethod(a,) {}
                    async asyncMethodDefault(a = 1,) {}
                    async *asyncGen(a,) {}
                    async *asyncGenDefault(a = 1,) {}
                }

                return [
                    (function (a,) {}).length,
                    (function (a = 1,) {}).length,
                    (function (...rest) {}).length,
                    (async function (a,) {}).length,
                    (async function (a = 1,) {}).length,
                    (function* (a,) {}).length,
                    (function* (a = 1,) {}).length,
                    (async function* (a,) {}).length,
                    (async function* (a = 1,) {}).length,
                    ((a,) => {}).length,
                    ((a = 1,) => {}).length,
                    C.prototype.method.length,
                    C.prototype.methodDefault.length,
                    C.prototype.gen.length,
                    C.prototype.genDefault.length,
                    C.prototype.asyncMethod.length,
                    C.prototype.asyncMethodDefault.length,
                    C.prototype.asyncGen.length,
                    C.prototype.asyncGenDefault.length
                ].join('|');
            })()
            """);

        Assert.Equal("1|0|0|1|0|1|0|1|0|1|0|1|0|1|0|1|0|1|0", result.ToString());
    }

    [Fact]
    public void Compile_TaggedTemplate_NoSubstitution_Invokes_Tag_Function()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var calls = 0;
                var cooked = '';
                var raw = '';

                (function(parts) {
                    calls += 1;
                    cooked = parts[0];
                    raw = parts.raw[0];
                })`\x41`;

                return [calls, cooked, raw].join('|');
            })()
            """);

        Assert.Equal(@"1|A|\x41", result.ToString());
    }

    [Fact]
    public void Compile_TemplateLiteral_WithSubstitution_Returns_Interpolated_String()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                function printCodePoint(codePoint) {
                    var hex = codePoint
                        .toString(16)
                        .toUpperCase()
                        .padStart(6, "0");
                    return `U+${hex}`;
                }

                return printCodePoint(255);
            })()
            """);

        Assert.Equal("U+0000FF", result.ToString());
    }

    [Fact]
    public void Compile_DestructuringAssignmentProperties_OnlyInfer_Direct_Anonymous_Function_Names()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var xCover;
                var cover;
                ({ x: xCover = (0, function () {}), y: cover = (function () {}) } = {});
                return (xCover.name === '') + '|' + cover.name;
            })()
            """);

        Assert.Equal("true|cover", result.ToString());
    }

    [Fact]
    public void Compile_StaticPrivateAsyncGeneratorMethod_DoesNotAppear_In_Property_Introspection()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                class C {
                    static async *#gen() { yield 1; }
                    static get gen() { return this.#gen; }
                }

                var iter = C.gen();
                return [
                    Object.prototype.hasOwnProperty.call(C, '#gen'),
                    Object.prototype.hasOwnProperty.call(C.prototype, '#gen'),
                    Object.getOwnPropertyNames(C).includes('#gen'),
                    Object.getOwnPropertyDescriptor(C, '#gen') === undefined,
                    typeof iter.next
                ].join('|');
            })()
            """);

        Assert.Equal("false|false|false|true|function", result.ToString());
    }

    [Fact]
    public void Compile_Strict_Function_Unresolved_Assignment_Throws_ReferenceError()
    {
        using var ctx = new JSContext();

        var ex = Assert.Throws<JSException>(() => ctx.Eval("""
            (function () {
                "use strict";
                missing = 1;
            })();
            """));

        Assert.Equal("ReferenceError", ex.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("missing is not defined", ex.Error[KeyStrings.message].ToString());
    }

    [Fact]
    public void Compile_Eval_Strict_Directive_And_Top_Level_Lexical_Tdz_Throw_ReferenceError()
    {
        using var ctx = new JSContext();

        var strictEval = Assert.Throws<JSException>(() => ctx.Eval("""eval('"use strict"; missing = 1;');"""));
        Assert.Equal("ReferenceError", strictEval.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("missing is not defined", strictEval.Error[KeyStrings.message].ToString());

        var lexicalEval = Assert.Throws<JSException>(() => ctx.Eval("""eval('typeof x; let x;');"""));
        Assert.Equal("ReferenceError", lexicalEval.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("Cannot access 'x' before initialization", lexicalEval.Error[KeyStrings.message].ToString());

        var indirectLexicalEval = Assert.Throws<JSException>(() => ctx.Eval("""(0, eval)('typeof y; class y {}');"""));
        Assert.Equal("ReferenceError", indirectLexicalEval.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("Cannot access 'y' before initialization", indirectLexicalEval.Error[KeyStrings.message].ToString());
    }

    [Fact]
    public void Compile_Syntax_Errors_Are_Reported_For_Strict_And_Direct_Eval_Cases()
    {
        using var ctx = new JSContext();

        void AssertSyntaxError(string source)
        {
            var ex = Assert.Throws<JSException>(() => ctx.Eval(source));
            Assert.Equal("SyntaxError", ex.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        }

        AssertSyntaxError("""
            (function () {
                let x;
                eval('var x;');
            })();
            """);

        AssertSyntaxError("""eval('"use strict"; ({ x: function eval() {} });');""");
        AssertSyntaxError("""
            "use strict";
            eval('(function () { var eval; })');
            """);
        AssertSyntaxError("""(0, eval)('"use strict"; var arguments;');""");
        AssertSyntaxError("""
            "use strict";
            eval("try {} catch (arguments) { }");
            """);
        AssertSyntaxError("""eval("/\\\rn/;");""");
        AssertSyntaxError("""
            "use strict";
            eval("a = 0x1; a = 01;");
            """);
        AssertSyntaxError("""eval("await 10");""");
    }

    [Fact]
    public void Compile_Default_Parameter_Self_Reference_Throws_ReferenceError()
    {
        using var ctx = new JSContext();

        var ex = Assert.Throws<JSException>(() => ctx.Eval("""
            var f;
            f = (x = x) => x;
            f();
            """));

        Assert.Equal("ReferenceError", ex.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("Cannot access 'x' before initialization", ex.Error[KeyStrings.message].ToString());
    }
}
