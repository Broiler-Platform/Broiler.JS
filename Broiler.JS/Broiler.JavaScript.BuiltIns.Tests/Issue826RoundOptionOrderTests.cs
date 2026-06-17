using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Option getters on Temporal round() must run in the spec order, and all before any
// algorithmic validation of the increment. Issue #826:
//   P100 ZonedDateTime.round (options-read-before-algorithmic-validation): the order is
//        roundingIncrement, roundingMode, smallestUnit, and an increment that is invalid for
//        the smallestUnit still reads every option first. Broiler read smallestUnit first.
//   P99  Duration.round (order-of-operations): the order is largestUnit, relativeTo,
//        roundingIncrement, roundingMode, smallestUnit. Broiler read smallestUnit/largestUnit
//        before relativeTo/roundingIncrement/roundingMode.
public class Issue826RoundOptionOrderTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    // Shared JS helper: an option bag whose primitive values record every get/coerce into `actual`.
    private const string Observer = """
        var actual = [];
        function primitiveObserver(value, name) {
          return {
            get toString() { actual.push("get " + name + ".toString"); return function () { actual.push("call " + name + ".toString"); return value; }; },
            get valueOf() { actual.push("get " + name + ".valueOf"); return function () { actual.push("call " + name + ".valueOf"); return value; }; },
          };
        }
        function bag(target, name, skip) {
          skip = skip || [];
          var o = {};
          Object.keys(target).forEach(function (k) {
            Object.defineProperty(o, k, { enumerable: true, configurable: true, get: function () {
              actual.push("get " + name + "." + k);
              var v = target[k];
              if (skip.indexOf(k) >= 0 || v === undefined || v === null || typeof v === "object") return v;
              return primitiveObserver(v, name + "." + k);
            }});
          });
          return o;
        }
        """;

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(Observer + "\n" + source).ToString();
    }

    [Fact]
    public void ZonedDateTimeRound_ReadsAllOptionsBeforeIncrementValidation()
        => Assert.Equal(
            "RangeError|" + string.Join(",",
                "get options.roundingIncrement", "get options.roundingIncrement.valueOf", "call options.roundingIncrement.valueOf",
                "get options.roundingMode", "get options.roundingMode.toString", "call options.roundingMode.toString",
                "get options.smallestUnit", "get options.smallestUnit.toString", "call options.smallestUnit.toString"),
            Eval("""
                var options = bag({ smallestUnit: "hour", roundingIncrement: 25, roundingMode: "expand" }, "options");
                var instance = new Temporal.ZonedDateTime(1n, "UTC");
                var thrown = "no throw";
                try { instance.round(options); } catch (e) { thrown = e.constructor.name; }
                thrown + "|" + actual.join(",");
            """));

    [Theory]
    [InlineData("undefined")]
    [InlineData("new Temporal.PlainDate(2026, 3, 6)")]
    [InlineData("new Temporal.ZonedDateTime(1772751600000000000n, \"UTC\")")]
    public void DurationRound_OptionOrder_TopLevel(string relativeTo)
        => Assert.Equal(
            string.Join(",",
                "get options.largestUnit", "get options.largestUnit.toString", "call options.largestUnit.toString",
                "get options.relativeTo",
                "get options.roundingIncrement", "get options.roundingIncrement.valueOf", "call options.roundingIncrement.valueOf",
                "get options.roundingMode", "get options.roundingMode.toString", "call options.roundingMode.toString",
                "get options.smallestUnit", "get options.smallestUnit.toString", "call options.smallestUnit.toString"),
            Eval($$"""
                var options = bag({ smallestUnit: "microseconds", largestUnit: "auto", roundingMode: "halfExpand", roundingIncrement: 1, relativeTo: {{relativeTo}} }, "options", ["relativeTo"]);
                new Temporal.Duration(0, 0, 0, 0, 2400).round(options);
                actual.join(",");
            """));
}
