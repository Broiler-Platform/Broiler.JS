using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Array.from stores elements with CreateDataPropertyOrThrow: a failed [[DefineOwnProperty]] — a
// non-configurable existing element on the constructed object, or a non-extensible target — is a
// TypeError rather than a silent no-op. Issue #808 problem 5 (staging/sm/Array/from_errors.js).
public class Issue808ArrayFromCreateDataPropertyTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void From_ReadOnlyElementTarget_Throws()
        => Assert.Equal("TypeError", Eval("""
            function ReadOnly() { Object.defineProperty(this, "0", { value: null }); this.length = 0; }
            ReadOnly.from = Array.from;
            var err = "none";
            try { ReadOnly.from([1]); }
            catch (e) { err = e.constructor.name; }
            err;
        """));

    [Fact]
    public void From_InextensibleTarget_Throws()
        => Assert.Equal("TypeError", Eval("""
            function Inext() { Object.preventExtensions(this); }
            Inext.from = Array.from;
            var err = "none";
            try { Inext.from([1]); }
            catch (e) { err = e.constructor.name; }
            err;
        """));

    [Fact]
    public void From_EmptyIntoReadOnlyTarget_Succeeds()
        => Assert.Equal("0", Eval("""
            function ReadOnly() { Object.defineProperty(this, "0", { value: null }); this.length = 0; }
            ReadOnly.from = Array.from;
            String(ReadOnly.from([]).length);
        """));

    [Fact]
    public void From_Array_StillWorks()
        => Assert.Equal("1,2,3", Eval("Array.from([1, 2, 3]).join(',');"));

    [Fact]
    public void From_WithMapFn_StillWorks()
        => Assert.Equal("2,4,6", Eval("Array.from([1, 2, 3], function (x) { return x * 2; }).join(',');"));
}
