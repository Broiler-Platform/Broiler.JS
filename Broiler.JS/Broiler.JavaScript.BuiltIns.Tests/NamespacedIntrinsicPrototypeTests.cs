using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The Temporal and Intl classes are properties of namespace objects, not globals, so the
// per-realm intrinsic registry the parser/intrinsics slice added (see IntrinsicPrototypeTests)
// did not cover them: every Temporal object the engine created (`Temporal.PlainDate.from`,
// `plainDate.add`, `Date.prototype.toTemporalInstant`, …) and every Intl object created without
// a NewTarget (`Intl.NumberFormat()`, `locale.maximize()`) looked its prototype up as
// `globalThis.Temporal.X.prototype` / `globalThis.Intl.X.prototype` at that moment. Guest code
// that replaced the namespace or one of its members changed the prototype of those objects.
// The specification names the intrinsic (%Temporal.PlainDate.prototype%, %Intl.NumberFormat%):
// the realm now records each namespaced class under its qualified name and resolves it there.
//
// Likewise a generator function's `prototype` inherited from whatever the (non-standard)
// global `Generator` binding held when the function was created, where §27.3.4 names
// %GeneratorFunction.prototype.prototype%.
public class NamespacedIntrinsicPrototypeTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    // Captures `ns.cls.prototype` and the constructor, then replaces the namespace (or only the
    // member) and reports whether `make` (which may use `C` for the saved constructor and
    // `before` for an instance made beforehand) returns an object with the saved prototype.
    private static string AfterNamespaceIsReplaced(string ns, string cls, string make, bool memberOnly)
        => Eval($$"""
            var savedNamespace = {{ns}};
            var C = {{ns}}.{{cls}}, intrinsic = C.prototype, outcome;
            var getPrototypeOf = Object.getPrototypeOf;
            var before = {{(ns == "Temporal" ? "new C(" + TemporalArguments(cls) + ")" : "undefined")}};
            try {
                {{(memberOnly
                    ? $"{ns}.{cls} = function Impostor() {{}}; {ns}.{cls}.prototype = {{ impostor: true }};"
                    : $"globalThis.{ns} = {{ {cls}: function Impostor() {{}}, impostor: true }};")}}
                var made = {{make}};
                var proto = getPrototypeOf(made);
                outcome = proto === intrinsic ? 'intrinsic' : proto && proto.impostor ? 'impostor' : 'other';
            } catch (e) {
                outcome = 'threw ' + e;
            } finally {
                globalThis.{{ns}} = savedNamespace;
                savedNamespace.{{cls}} = C;
            }
            outcome;
        """);

    private static string TemporalArguments(string cls) => cls switch
    {
        "PlainDate" => "2020, 1, 1",
        "PlainTime" => "10",
        "PlainDateTime" => "2020, 1, 1",
        "PlainYearMonth" => "2020, 1",
        "PlainMonthDay" => "1, 1",
        "ZonedDateTime" => "0n, 'UTC'",
        "Instant" => "0n",
        "Duration" => "1",
        _ => "",
    };

    [Theory]
    [InlineData("PlainDate", "C.from('2020-01-01')")]
    [InlineData("PlainDate", "before.add({ days: 1 })")]
    [InlineData("PlainDate", "before.with({ day: 2 })")]
    [InlineData("PlainTime", "C.from('10:00')")]
    [InlineData("PlainTime", "before.add({ hours: 1 })")]
    [InlineData("PlainDateTime", "C.from('2020-01-01T10:00')")]
    [InlineData("PlainDateTime", "before.add({ days: 1 })")]
    [InlineData("PlainYearMonth", "C.from('2020-01')")]
    [InlineData("PlainMonthDay", "C.from('01-01')")]
    [InlineData("ZonedDateTime", "C.from('2020-01-01T00:00[UTC]')")]
    [InlineData("ZonedDateTime", "before.add({ hours: 1 })")]
    [InlineData("Instant", "C.fromEpochMilliseconds(0)")]
    [InlineData("Instant", "new Date(0).toTemporalInstant()")]
    [InlineData("Instant", "before.add({ hours: 1 })")]
    [InlineData("Duration", "C.from('P1D')")]
    [InlineData("Duration", "before.negated()")]
    public void EngineCreatedTemporalObjectsTakeTheIntrinsicPrototype(string cls, string make)
    {
        Assert.Equal("intrinsic", AfterNamespaceIsReplaced("Temporal", cls, make, memberOnly: false));
        Assert.Equal("intrinsic", AfterNamespaceIsReplaced("Temporal", cls, make, memberOnly: true));
    }

    [Theory]
    [InlineData("NumberFormat", "C()")]
    [InlineData("DateTimeFormat", "C()")]
    [InlineData("Collator", "C()")]
    [InlineData("Locale", "new C('en').maximize()")]
    [InlineData("Locale", "new C('en-Latn-US').minimize()")]
    // Constructed with `new`: the prototype comes from NewTarget and was already right.
    [InlineData("NumberFormat", "new C()")]
    [InlineData("PluralRules", "new C()")]
    [InlineData("Segmenter", "new C()")]
    [InlineData("RelativeTimeFormat", "new C()")]
    [InlineData("DurationFormat", "new C()")]
    [InlineData("ListFormat", "new C()")]
    public void EngineCreatedIntlObjectsTakeTheIntrinsicPrototype(string cls, string make)
    {
        Assert.Equal("intrinsic", AfterNamespaceIsReplaced("Intl", cls, make, memberOnly: false));
        Assert.Equal("intrinsic", AfterNamespaceIsReplaced("Intl", cls, make, memberOnly: true));
    }

    [Fact(Timeout = 600000)]
    public void DeletingTheNamespacesKeepsEngineCreatedObjectsIntact()
        => Assert.Equal("true,true", Eval("""
            var PD = Temporal.PlainDate, NF = Intl.NumberFormat;
            delete globalThis.Temporal; delete globalThis.Intl;
            [Object.getPrototypeOf(PD.from('2020-01-01')) === PD.prototype,
             Object.getPrototypeOf(NF()) === NF.prototype].join(',');
        """));

    [Fact(Timeout = 600000)]
    public void EachRealmKeepsItsOwnNamespacedIntrinsics()
    {
        Load();
        using var first = new JSContext();
        using var second = new JSContext();
        first.Eval("globalThis.PD = Temporal.PlainDate; Temporal = {}; Intl = {};");
        Assert.Equal("true", first.Eval("String(Object.getPrototypeOf(PD.from('2020-01-01')) === PD.prototype)").ToString());
        Assert.Equal("true", second.Eval(
            "String(Object.getPrototypeOf(Temporal.PlainDate.from('2020-01-01')) === Temporal.PlainDate.prototype && Object.getPrototypeOf(Intl.NumberFormat()) === Intl.NumberFormat.prototype)").ToString());
    }

    // With the lazy (default "full") profile, the namespaces are unrealized cells until first
    // read. An engine-created object needs the realm's intrinsics before that happens, and must
    // get the very prototypes the guest's namespace carries once it is read.
    [Fact(Timeout = 600000)]
    public void LazyNamespacesYieldTheSamePrototypesTheirGlobalsLaterExpose()
    {
        Load();
        using var ctx = new JSContext(options: new JSContextOptions { BootstrapProfile = JavaScriptBootstrapProfile.Full });
        Assert.Equal("true", ctx.Eval(
            "var made = new Date(0).toTemporalInstant(); String(Object.getPrototypeOf(made) === Temporal.Instant.prototype)").ToString());
    }

    [Fact(Timeout = 600000)]
    public void LazyNamespacesReplacedBeforeTheirFirstReadStillYieldTheIntrinsics()
    {
        Load();
        using var ctx = new JSContext(options: new JSContextOptions { BootstrapProfile = JavaScriptBootstrapProfile.Full });
        Assert.Equal("[object Temporal.Instant],0", ctx.Eval("""
            globalThis.Temporal = { impostor: true };
            var made = new Date(0).toTemporalInstant();
            [Object.prototype.toString.call(made), made.epochMilliseconds].join(',');
            """).ToString());
    }

    // §27.3.4: a generator function's `prototype` inherits from %GeneratorFunction.prototype.prototype%,
    // and the generator objects it creates reach its `next` through it.
    [Theory]
    [InlineData("globalThis.Generator = function Impostor() {}; Generator.prototype = { impostor: true };")]
    [InlineData("delete globalThis.Generator;")]
    [InlineData("globalThis.Generator = undefined;")]
    public void GeneratorFunctionsInheritFromTheIntrinsicGeneratorPrototype(string replace)
        => Assert.Equal("true,true,true,1", Eval($$"""
            var intrinsic = Object.getPrototypeOf(function* () {}).prototype;
            {{replace}}
            var g = function* () { yield 1; };
            var gen = g();
            var m = { *m() {} }.m;
            [Object.getPrototypeOf(g.prototype) === intrinsic,
             Object.getPrototypeOf(m.prototype) === intrinsic,
             Object.getPrototypeOf(Object.getPrototypeOf(gen)) === intrinsic,
             gen.next().value].join(',');
        """));

    [Fact(Timeout = 600000)]
    public void GeneratorFunctionWithANonObjectPrototypeFallsBackToTheIntrinsic()
        => Assert.Equal("true", Eval("""
            var intrinsic = Object.getPrototypeOf(function* () {}).prototype;
            globalThis.Generator = function Impostor() {}; Generator.prototype = { impostor: true };
            var g = function* () {};
            g.prototype = null;
            String(Object.getPrototypeOf(g()) === intrinsic);
        """));

    [Fact(Timeout = 600000)]
    public void GeneratorFunctionConstructorInheritsFromTheIntrinsicFunction()
        => Assert.Equal("true", Eval("""
            var F = Function;
            Function.prototype.constructor = function Impostor() {};
            var GeneratorFunction = Object.getPrototypeOf(eval('(function* () {})')).constructor;
            String(Object.getPrototypeOf(GeneratorFunction) === F);
        """));
}
