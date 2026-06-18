using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/836
//
// Fixed here:
//
//   Problem 77 (Object.keys included a non-enumerable array index) — an array's
//   indexed elements are walked by a specialised, hole-aware enumerator that
//   assumed every stored index is enumerable. Object.defineProperty can store an
//   indexed element with enumerable:false, so enumerable-only key walks
//   (Object.keys / Object.values / Object.entries / for-in / spread) wrongly
//   surfaced it. The array's own-index enumeration now honours the enumerable
//   filter, while element iteration (for-of / Array iterator) still visits every
//   index regardless.
public class Issue836Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Fact]
    public void ObjectKeysSkipsNonEnumerableArrayIndex()
        => Assert.Equal("0,2,4,10000", Eval(
            "var obj = [1, , 3, , 5];" +
            "Object.defineProperty(obj, 5, { value: 7, enumerable: false, configurable: true });" +
            "Object.defineProperty(obj, 10000, { value: 'x', enumerable: true, configurable: true });" +
            "Object.keys(obj).join(',')"));

    [Fact]
    public void ObjectKeysLengthExcludesNonEnumerableIndex()
        => Assert.Equal("4", Eval(
            "var obj = [1, , 3, , 5];" +
            "Object.defineProperty(obj, 5, { value: 7, enumerable: false, configurable: true });" +
            "Object.defineProperty(obj, 10000, { value: 'x', enumerable: true, configurable: true });" +
            "String(Object.keys(obj).length)"));

    [Fact]
    public void ObjectValuesSkipsNonEnumerableArrayIndex()
        => Assert.Equal("1,2", Eval(
            "var a = [1];" +
            "Object.defineProperty(a, 1, { value: 99, enumerable: false, configurable: true });" +
            "Object.defineProperty(a, 2, { value: 2, enumerable: true, configurable: true });" +
            "Object.values(a).join(',')"));

    [Fact]
    public void ForInSkipsNonEnumerableArrayIndex()
        => Assert.Equal("0,2", Eval(
            "var a = [1];" +
            "Object.defineProperty(a, 1, { value: 99, enumerable: false, configurable: true });" +
            "Object.defineProperty(a, 2, { value: 2, enumerable: true, configurable: true });" +
            "var keys = []; for (var k in a) keys.push(k); keys.join(',')"));

    [Fact]
    public void SpreadOwnEnumerablePropertiesSkipsNonEnumerableArrayIndex()
        => Assert.Equal("0,2", Eval(
            "var a = [1];" +
            "Object.defineProperty(a, 1, { value: 99, enumerable: false, configurable: true });" +
            "Object.defineProperty(a, 2, { value: 2, enumerable: true, configurable: true });" +
            "Object.keys({ ...a }).join(',')"));

    // getOwnPropertyNames is NOT enumerable-filtered: the non-enumerable index
    // must still appear (alongside "length").
    [Fact]
    public void GetOwnPropertyNamesStillIncludesNonEnumerableIndex()
        => Assert.Equal("0,1,2,length", Eval(
            "var a = [1];" +
            "Object.defineProperty(a, 1, { value: 99, enumerable: false, configurable: true });" +
            "Object.defineProperty(a, 2, { value: 2, enumerable: true, configurable: true });" +
            "Object.getOwnPropertyNames(a).join(',')"));

    // for-of / Array iteration visits every index regardless of enumerability.
    [Fact]
    public void ForOfVisitsNonEnumerableArrayIndex()
        => Assert.Equal("1,99,2", Eval(
            "var a = [1];" +
            "Object.defineProperty(a, 1, { value: 99, enumerable: false, configurable: true });" +
            "Object.defineProperty(a, 2, { value: 2, enumerable: true, configurable: true });" +
            "var out = []; for (var v of a) out.push(v); out.join(',')"));
}
