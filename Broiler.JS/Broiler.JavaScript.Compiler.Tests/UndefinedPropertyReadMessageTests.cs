using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// Naming the property in "Cannot read properties of undefined".
//
// The static path (`u.foo`) already said which property. The COMPUTED path (`u[k]`) did not, and
// that is the one minified code takes: a bundle reads `I[Y]`, so the message named nothing at all
// and the trace gave a column in a 61 908-character line. Browsers append "(reading 'foo')"; so
// does this now.
public class UndefinedPropertyReadMessageTests
{
    private static string MessageOf(string source)
    {
        using var context = new JSContext();
        return context.Eval($"String((function(){{ try {{ {source} }} catch (e) {{ return e.message; }} return 'no throw'; }})())", "t.js")
            .ToString();
    }

    // The reported shape: a computed read off a call that returned undefined.
    [Fact(Timeout = 600000)]
    public void AComputedRead_NamesTheProperty()
    {
        Assert.Equal(
            "Cannot read properties of undefined (reading 'p')",
            MessageOf("var A = function(){ return undefined; }; var I = A(); var Y = 'p'; return I[Y];"));
    }

    // A key held in a variable is what reaches the dynamic path; a literal one is folded into the
    // static read, which has its own (already-naming) message.
    //
    // Only the string key is covered: a NUMERIC variable key (`var k = 3; u[k]`) does not throw at
    // all today, it evaluates to undefined. That is a separate defect in the indexed read path,
    // not something this message change causes or fixes, and it is left for its own change rather
    // than smuggled in here.
    [Theory]
    [InlineData("var u; var k = 'bar'; return u[k];", "Cannot read properties of undefined (reading 'bar')")]
    public void PrimitiveKeys_AreNamed(string source, string expected)
    {
        Assert.Equal(expected, MessageOf(source));
    }

    // An OBJECT key is deliberately left undescribed. GetValue throws before ToPropertyKey because
    // ToObject(base) comes first (6.2.5.5); describing the key would run its toString/@@toPrimitive
    // — user code, in an order the spec forbids. A diagnostic does not get to change evaluation.
    [Fact(Timeout = 600000)]
    public void AnObjectKey_IsNotCoercedForTheMessage()
    {
        var message = MessageOf("var u; var k = { toString: function(){ throw new Error('key coerced'); } }; return u[k];");

        Assert.Equal("Cannot read properties of undefined", message);
    }

    // The static path already named the property and keeps its own wording. It has since gained
    // the "(evaluating '…')" clause, which names the ACCESS rather than the key — a separate
    // change, covered by NullishAccessMessageTests, and asserted here only so that this test says
    // what the static message is rather than a prefix of it.
    //
    // `null.foo` is a literal base, so there is no expression to name that the message does not
    // already contain; the clause is emitted all the same, because a description is recorded per
    // emitted access and not per how interesting the access looks.
    [Fact(Timeout = 600000)]
    public void AStaticReadIsUnchanged()
    {
        Assert.Equal("Cannot get property foo of undefined (evaluating 'u.foo')", MessageOf("var u; return u.foo;"));
        Assert.Equal("Cannot get property foo of null (evaluating 'null.foo')", MessageOf("return null.foo;"));
    }

    // A read that succeeds is untouched.
    [Fact(Timeout = 600000)]
    public void ASuccessfulReadStillSucceeds()
    {
        Assert.Equal("1", MessageOf("var o = { a: 1 }; return String(o['a']);"));
    }
}
