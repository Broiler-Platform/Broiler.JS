using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/840
//
//   Problem 90 (Object.getOwnPropertyDescriptor(RegExp, "prototype") should be a data
//   descriptor with every attribute false) — per §22.2.5.1 the RegExp constructor's
//   "prototype" is { [[Writable]]: false, [[Enumerable]]: false, [[Configurable]]: false }.
//   The original generated RegExp constructor installed it correctly (ReadonlyValue), but
//   PatchRegExpPrototype replaces the constructor with a wrapper (for the §22.2.4.1 "return
//   the existing RegExp unchanged" call-form optimization) and re-added the "prototype"
//   property as a ConfigurableValue, leaving it writable AND configurable. test262's
//   built-ins/Object/getOwnPropertyDescriptor/15.2.3.3-4-211.js therefore saw
//   desc.writable === true. The wrapper now carries the same non-writable/non-configurable
//   data property as the original constructor.
public class Issue840Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Fact]
    public void RegExpPrototypeIsNonWritable()
        => Assert.Equal("false", Eval("Object.getOwnPropertyDescriptor(RegExp, 'prototype').writable"));

    [Fact]
    public void RegExpPrototypeIsNonEnumerable()
        => Assert.Equal("false", Eval("Object.getOwnPropertyDescriptor(RegExp, 'prototype').enumerable"));

    [Fact]
    public void RegExpPrototypeIsNonConfigurable()
        => Assert.Equal("false", Eval("Object.getOwnPropertyDescriptor(RegExp, 'prototype').configurable"));

    [Fact]
    public void RegExpPrototypeIsDataDescriptorWithoutAccessors()
        => Assert.Equal("false", Eval(
            "(function () {" +
            "  var d = Object.getOwnPropertyDescriptor(RegExp, 'prototype');" +
            "  return d.hasOwnProperty('get') || d.hasOwnProperty('set');" +
            "})()"));

    [Fact]
    public void AssigningRegExpPrototypeIsRejectedInStrictMode()
        => Assert.Equal("true", Eval(
            "(function () {" +
            "  'use strict';" +
            "  try { RegExp.prototype = {}; return false; }" +
            "  catch (e) { return e instanceof TypeError; }" +
            "})()"));

    [Fact]
    public void RegExpStillConstructsAndMatches()
        => Assert.Equal("true", Eval("/a(b)c/.test('abc')"));
}
