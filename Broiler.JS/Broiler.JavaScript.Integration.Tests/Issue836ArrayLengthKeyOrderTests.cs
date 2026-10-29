using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/836
//
// Fixed here (Problem 33): an array's "length" own key must appear right after the
// integer indices and before any other string key in [[OwnPropertyKeys]] order,
// regardless of when it was materialized into the property store. Materializing it via
// Object.defineProperty(arr, "length", …) previously appended it after earlier-created
// string keys, so Object.getOwnPropertyNames / Reflect.ownKeys reported e.g. ["a",
// "length"] instead of ["length", "a"]. The own-key walk now emits "length" at the
// index/string boundary and drops the stored copy.
public class Issue836ArrayLengthKeyOrderTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Fact]
    public void LengthPrecedesLaterStringKeyAfterDefineProperty()
        => Assert.Equal("length,a", Eval(
            "var arr = []; arr.a = 1; Object.defineProperty(arr, 'length', { value: 2 });" +
            "Object.getOwnPropertyNames(arr).join(',')"));

    [Fact]
    public void ReflectOwnKeysIndicesThenLengthThenStrings()
        => Assert.Equal("0,1,length,a", Eval(
            "var arr = ['x','y']; arr.a = 1; Object.defineProperty(arr, 'length', { value: 2 });" +
            "Reflect.ownKeys(arr).join(',')"));

    [Fact]
    public void LengthFirstWhenSoleStringKey()
        => Assert.Equal("length", Eval(
            "var arr = []; Object.defineProperty(arr, 'length', { value: 0 });" +
            "Object.getOwnPropertyNames(arr).join(',')"));

    [Fact]
    public void SyntheticLengthStillOrderedBeforeStrings()
        => Assert.Equal("length,a", Eval(
            "var arr = []; arr.a = 1; Object.getOwnPropertyNames(arr).join(',')"));

    // "length" stays non-enumerable, so enumerable-only walks never surface it.
    [Fact]
    public void ObjectKeysExcludesLengthAfterDefineProperty()
        => Assert.Equal("a", Eval(
            "var arr = []; arr.a = 1; Object.defineProperty(arr, 'length', { value: 2 });" +
            "Object.keys(arr).join(',')"));
}
