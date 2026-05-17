using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

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
}
