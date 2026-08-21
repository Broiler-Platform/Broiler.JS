using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// Naming the EXPRESSION in a "of undefined" / "of null" message.
//
// "Cannot get property enabled of undefined" says which property was asked for and never says
// what was undefined — and on `config.server.tls.enabled` that is the whole question, because
// the answer (`config.server.tls`) appears nowhere: not in the message, not in the property
// name, and not in the stack, whose innermost frame points at the statement and a statement can
// hold several accesses all ending in `.enabled`. JavaScriptCore appends "(evaluating '…')" and
// SpiderMonkey names the base; this engine named neither. It does now, for the accesses that
// carry a description — see NullishAccess for which those are and why the rest do not.
public class NullishAccessMessageTests
{
    private static string MessageOf(string source)
    {
        using var context = new JSContext();
        return context.Eval(
            $"String((function(){{ try {{ {source} }} catch (e) {{ return e.message; }} return 'no throw'; }})())",
            "t.js")
            .ToString();
    }

    // The shape the whole change is for: a chained read whose middle link is undefined.
    [Fact(Timeout = 600000)]
    public void AChainedReadNamesTheWholeAccess()
    {
        Assert.Equal(
            "Cannot get property enabled of undefined (evaluating 'config.server.tls.enabled')",
            MessageOf("var config = { server: {} }; return config.server.tls.enabled;"));
    }

    // A null base takes the same clause. The two singletons word their messages separately, so
    // covering only one would leave the other free to drift.
    [Fact(Timeout = 600000)]
    public void ANullBaseIsNamedToo()
    {
        Assert.Equal(
            "Cannot get property x of null (evaluating 'holder.value.x')",
            MessageOf("var holder = { value: null }; return holder.value.x;"));
    }

    // The write side, which has its own cache, its own site numbering and its own message.
    [Fact(Timeout = 600000)]
    public void AWriteNamesTheAccess()
    {
        Assert.Equal(
            "Cannot set property enabled of undefined (evaluating 'config.server.tls.enabled')",
            MessageOf("var config = { server: {} }; config.server.tls.enabled = true;"));
    }

    // A compound assignment and an update read through the read cache and write back through the
    // store cache; a nullish base fails on the READ, before the operator runs.
    [Theory]
    [InlineData("config.server.tls.count += 1;")]
    [InlineData("config.server.tls.count++;")]
    public void ACompoundedAccessNamesTheAccess(string source)
    {
        Assert.Equal(
            "Cannot get property count of undefined (evaluating 'config.server.tls.count')",
            MessageOf("var config = { server: {} }; " + source));
    }

    // `o.m()` resolves its callee through the same read cache a bare `o.m` does, so the callee
    // read carries a description. The call itself does not: "undefined is not a function" is
    // raised at the invocation, which has no site to hang one on (see NullishAccess).
    [Fact(Timeout = 600000)]
    public void AMethodCallNamesTheCalleeRead()
    {
        Assert.Equal(
            "Cannot get property connect of undefined (evaluating 'config.server.tls.connect')",
            MessageOf("var config = { server: {} }; return config.server.tls.connect();"));
    }

    [Fact(Timeout = 600000)]
    public void ACallOnAnUndefinedCalleeIsUnchanged()
    {
        Assert.Equal(
            "undefined is not a function",
            MessageOf("var config = { server: {} }; return config.server.connect();"));
    }

    // A chain broken over several lines is the ordinary way this is written, and it has to read
    // back as the thing that can be searched for. See NullishAccess.Quote.
    [Fact(Timeout = 600000)]
    public void AMultiLineChainReadsBackAsOneExpression()
    {
        Assert.Equal(
            "Cannot get property enabled of undefined (evaluating 'config.server.tls.enabled')",
            MessageOf(
                """
                var config = { server: {} };
                return config
                    .server
                    .tls
                    .enabled;
                """));
    }

    // A base with an effect is quoted as written, and the single quotes the clause is delimited
    // by are escaped rather than dropped, so the text stays the text that is in the file.
    [Fact(Timeout = 600000)]
    public void AQuoteInsideTheAccessIsEscaped()
    {
        Assert.Equal(
            "Cannot get property b of undefined (evaluating 'pick(\\'a\\').b')",
            MessageOf("function pick(k) { return undefined; } return pick('a').b;"));
    }

    // A COMPUTED access has no well-formed span to quote — its last token is the key, not the
    // bracket that closes it — so it is left undescribed rather than quoted as `a.b["x"`. The
    // message is what it was, key naming included.
    [Fact(Timeout = 600000)]
    public void AComputedAccessIsNotDescribed()
    {
        Assert.Equal(
            "Cannot get property enabled of undefined",
            MessageOf("var config = { server: {} }; return config.server.tls['enabled'];"));
    }

    // A computed access NESTED inside the span is fine: the access still ends at an identifier.
    [Fact(Timeout = 600000)]
    public void AComputedLinkInsideTheAccessIsKept()
    {
        Assert.Equal(
            "Cannot get property name of undefined (evaluating 'rows[0].name')",
            MessageOf("var rows = []; return rows[0].name;"));
    }

    // A parenthesized base cannot be quoted from token positions — the opening parenthesis is
    // not part of any token — so the access is left undescribed rather than quoted as text that
    // is not in the file.
    [Fact(Timeout = 600000)]
    public void AParenthesizedBaseIsNotDescribed()
    {
        Assert.Equal(
            "Cannot get property enabled of undefined",
            MessageOf("var config = { server: {} }; return (config.server.tls).enabled;"));
    }

    // The description belongs to one message. A failure that carries none must not inherit the
    // last one that did — which is what a description left lying on the thread would do.
    [Fact(Timeout = 600000)]
    public void ADescriptionDoesNotOutliveItsMessage()
    {
        Assert.Equal(
            "Cannot read properties of undefined (reading 'later')",
            MessageOf(
                """
                var config = { server: {} };
                try { config.server.tls.enabled; } catch (e) { }
                var u; var k = 'later';
                return u[k];
                """));
    }

    // Spread and for-of enter iteration through a different method than the one JSUndefined and
    // JSNull override, so they reported "Value is not iterable" — the one message in the engine
    // that named neither the value nor its type.
    [Theory]
    [InlineData("var config = { server: {} }; return [...config.server.hosts];", "undefined is not iterable")]
    [InlineData("var config = { hosts: null }; for (var h of config.hosts) { }", "null is not iterable")]
    public void SpreadingANullishValueNamesIt(string source, string expected)
    {
        Assert.Equal(expected, MessageOf(source));
    }

    // Every other kind of value already named itself here — an object and a primitive resolve
    // @@iterator and fall back to naming the receiver — which is what made the two singletons the
    // odd ones out rather than the wording being wrong. Those paths are untouched.
    [Theory]
    [InlineData("return [...{}];", "[object Object] is not iterable")]
    [InlineData("return [...5];", "5 is not iterable")]
    public void SpreadingAnOtherNonIterableIsUnchanged(string source, string expected)
    {
        Assert.Equal(expected, MessageOf(source));
    }

    // A read that succeeds is untouched, on the path the description is recorded against.
    [Fact(Timeout = 600000)]
    public void ASuccessfulReadStillSucceeds()
    {
        Assert.Equal("1", MessageOf("var o = { a: { b: 1 } }; return String(o.a.b);"));
    }

    // The site a description is recorded against is warmed by repetition, and a hit takes a path
    // that never looks at one. The message has to be the same on the hundredth failure as on the
    // first — including when the same site has been succeeding in between.
    [Fact(Timeout = 600000)]
    public void TheClauseSurvivesAWarmedSite()
    {
        Assert.Equal(
            "Cannot get property enabled of undefined (evaluating 'config.server.tls.enabled')",
            MessageOf(
                """
                var config = { server: { tls: { enabled: true } } };
                for (var i = 0; i < 200; i++) { config.server.tls.enabled; }
                config.server = {};
                return config.server.tls.enabled;
                """));
    }

    // Nothing bounds the source text of an access — a base can hold a whole call — and a message
    // is read in a log line, so it is cut and marked rather than left to run.
    [Fact(Timeout = 600000)]
    public void AVeryLongAccessIsTruncated()
    {
        var message = MessageOf(
            "var averyLongNameHolderObjectForTheTest = { nestedContainerWithAnotherLongName: {} }; "
            + "return averyLongNameHolderObjectForTheTest.nestedContainerWithAnotherLongName"
            + ".missingIntermediateValue.finalProperty;");

        Assert.StartsWith(
            "Cannot get property finalProperty of undefined (evaluating 'averyLongNameHolderObjectForTheTest",
            message);
        Assert.EndsWith("…')", message);
        Assert.DoesNotContain("finalProperty')", message);
    }
}
