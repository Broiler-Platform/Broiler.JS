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
