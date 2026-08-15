using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// `new undefined()` and `new null()` reported "cannot create instance of undefined" / "... of null".
// No browser words it that way — all three major engines say "X is not a constructor" — and neither
// does the rest of THIS engine: JSFunction, JSSymbol, JSGenerator, JSGeneratorFunctionV2, JSReflect
// and JSPromisePrototype all raise "... is not a constructor". These two were the only sites left
// with a wording of their own.
//
// Where it showed up: html5test.com probes for WebRTC with
//
//     new (window.RTCPeerConnection || window.msRTCPeerConnection ||
//          window.mozRTCPeerConnection || window.webkitRTCPeerConnection)(null)
//
// All four are undefined in an engine with no WebRTC, so this throws — correctly, and the page
// catches it and records "unsupported". But the reported trace then carried a message no browser
// produces, which reads as an engine fault rather than as the expected answer to a feature probe.
// The throw is right; only the wording was wrong.
public class ConstructNonConstructorMessageTests
{
    private static string MessageOf(string source)
    {
        using var context = new JSContext();
        return context.Eval(
            """
            String((function () {
                try { SOURCE }
                catch (e) { return e.message; }
                return 'no throw';
            })())
            """.Replace("SOURCE", source),
            "t.js").ToString();
    }

    [Fact]
    public void New_Undefined_Is_Not_A_Constructor()
    {
        Assert.Equal("undefined is not a constructor", MessageOf("new undefined();"));
    }

    [Fact]
    public void New_Null_Is_Not_A_Constructor()
    {
        Assert.Equal("null is not a constructor", MessageOf("new null();"));
    }

    [Fact]
    public void An_Undefined_Property_Constructed_Reports_The_Same_Way()
    {
        // The shape a feature probe actually takes: every alternative is missing, so the whole
        // parenthesised expression is undefined and `new` is applied to that.
        Assert.Equal(
            "undefined is not a constructor",
            MessageOf("var w = {}; new (w.A || w.B || w.C)(null);"));
    }

    [Fact]
    public void Calling_Undefined_Still_Reports_Not_A_Function()
    {
        // Unchanged, and deliberately so: "undefined is not a function" is already the browser
        // wording. Only the construct path was inconsistent, and a sweep that "tidied" this one
        // too would have moved it away from the message every engine agrees on.
        Assert.Equal("undefined is not a function", MessageOf("var f; f();"));
    }

    [Fact]
    public void The_Message_Matches_The_Wording_Used_For_A_Real_Non_Constructor()
    {
        // The point of the change is consistency, so pin the neighbour it now agrees with: an
        // arrow function is not a constructor either, and has always said so.
        Assert.EndsWith("is not a constructor", MessageOf("var f = () => 1; new f();"));
    }
}
