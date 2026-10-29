using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// RegExp.prototype[Symbol.replace] with a functional replacement passes the match result's raw "groups"
// value as the final argument (null, a number, an object, ...) without ToObject coercion, omitting only
// an undefined value. The ToObject/TypeError-for-null check belongs to the string-substitution path
// only. Issue #810 problem 73.
public class Issue810RegExpReplaceNamedGroupsTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    // A fake match-producing object lets us control the "groups" property exactly, mirroring
    // test/built-ins/RegExp/prototype/Symbol.replace/named-groups-fn.js.
    private const string Harness = """
        function lastArgFor(groups) {
            var result = ["a"];
            result.index = 0;
            result.length = 1;
            result.groups = groups;
            var fake = {
                exec: function () { if (this.done) return null; this.done = true; return result; },
                done: false,
                flags: "",
                global: false,
                lastIndex: 0
            };
            var captured;
            RegExp.prototype[Symbol.replace].call(fake, "a", function () {
                captured = arguments[arguments.length - 1];
                return "";
            });
            return captured;
        }
        """;

    [Fact]
    public void FunctionalReplace_NullGroups_PassedThrough()
        => Assert.Equal("true", Eval(Harness + "\nString(lastArgFor(null) === null);"));

    [Fact]
    public void FunctionalReplace_NumberGroups_PassedThrough()
        => Assert.Equal("true", Eval(Harness + "\nString(lastArgFor(10) === 10);"));

    [Fact]
    public void FunctionalReplace_ObjectGroups_PassedThrough()
        => Assert.Equal("true", Eval(Harness + "\nvar o = {}; String(lastArgFor(o) === o);"));

    [Fact]
    public void FunctionalReplace_UndefinedGroups_UsesMatchedString()
        => Assert.Equal("a", Eval(Harness + "\nString(lastArgFor(undefined));"));
}
