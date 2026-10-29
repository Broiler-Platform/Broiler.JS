using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/667
//
// Fixed here:
//   * Problems 9 & 10 — the scanner emitted a phantom duplicate of the previous
//                  token at EOF whenever a source ended in non-newline whitespace
//                  (spaces/tabs with no trailing line break). The whitespace-skip
//                  loop ran `first` to char.MaxValue but the only EOF guard sat
//                  *before* the loop, so it fell through the whole token switch to
//                  `throw Unexpected()`. Direct/indirect eval of generated test262
//                  strings ending in a trailing-space line continuation surfaced it
//                  as "Unexpected token } ...". A general lexer bug; any source
//                  ending in trailing spaces was affected.
//   * Problem 8  — a private name was not visible to a direct eval inside a class
//                  field initializer of a class with no explicit constructor. The
//                  synthetic default-constructor scope was built without the class's
//                  DirectEvalPrivateNames, so `#x` failed validation. The explicit-
//                  constructor path already passed them.
//   * Problem 6  — Set.prototype.difference / intersection / isSubsetOf observed the
//                  set-like argument in the wrong order and always probed `has`. Per
//                  spec GetSetRecord coerces `size` (invoking valueOf) before reading
//                  `has`/`keys`, and the operation iterates the smaller collection:
//                  when this set is larger than the argument it iterates the
//                  argument's keys() and must not call `has`. difference also now
//                  iterates a snapshot of this set so a mutating `has` cannot change
//                  which elements are probed.
//
// Problems 1 (sm deepEqual harness), 2 (dstr array-rest IteratorClose + Locale
// canonicalize), 3 (Intl.DateTimeFormat formatRange — needs CLDR), 4 (abrupt
// completion in `finally` overriding a pending throw — architectural IL-layer),
// 5 (compound-assignment PutValue ordering with direct-eval var binding) and 7
// (several unrelated root causes grouped by message) are triaged in the issue and
// remain out of scope for this change.
public class Issue667Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problems 9 & 10: trailing-whitespace EOF must not produce a phantom token ----

    [Fact]
    public void SourceEndingInTrailingSpacesParses()
        // A trailing VariableStatement completes with an empty value (so the program
        // completion carries the preceding expression's value); the point of this test
        // is that the trailing whitespace before EOF does not produce a phantom token.
        => Assert.Equal("1", Eval("var x = 1; x  "));

    [Fact]
    public void SwitchCaseFunctionDeclarationEndingInTrailingSpacesParses()
        => Assert.Equal("function", Eval("switch (1) { case 1: function f() {} }  typeof f  "));

    [Fact]
    public void EvalOfStringEndingInTrailingSpaces()
        => Assert.Equal("function", Eval(
            "eval('switch (1) {  case 1:    function f() {  }}  '); typeof f"));

    [Fact]
    public void TrailingSpacesAfterClosingBraceParses()
        => Assert.Equal("undefined", Eval("var o = {};  void 0  "));

    // ---- Problem 8: private name visible to direct eval in a field initializer ----

    [Fact]
    public void PrivateFieldVisibleToDirectEvalInInitializer()
        => Assert.Equal("44", Eval(
            "class C { #m = 44; v = eval('this.#m'); } new C().v"));

    [Fact]
    public void PrivateMethodVisibleToDirectEvalInInitializer()
        => Assert.Equal("7", Eval(
            "class C { #m() { return 7; } v = eval('this.#m()'); } new C().v"));

    [Fact]
    public void PrivateAccessorVisibleToDirectEvalInInitializer()
        => Assert.Equal("9", Eval(
            "class C { get #m() { return 9; } v = eval('this.#m'); } new C().v"));

    // ---- Problem 6: Set set-like operation order and small-side iteration ----

    [Fact]
    public void DifferenceCoercesSizeBeforeReadingHasAndKeys()
        => Assert.Equal("get-size,valueOf,get-has,get-keys", Eval(
            "var log = [];"
            + "var setLike = { get size(){ log.push('get-size');"
            + "    return { valueOf(){ log.push('valueOf'); return 0; } }; },"
            + "  get has(){ log.push('get-has'); return function(){ return false; }; },"
            + "  get keys(){ log.push('get-keys'); return function(){ return { next(){ return { done:true }; } }; }; } };"
            + "new Set().difference(setLike);"
            + "log.join(',')"));

    [Fact]
    public void DifferenceUsesKeysAndSkipsHasWhenReceiverIsLarger()
        => Assert.Equal("true", Eval(
            "var setLike = { size: 0,"
            + "  has: function(){ throw new Error('has must not be called'); },"
            + "  keys: function(){ return { next(){ return { done:true }; } }; } };"
            + "var r = new Set([1]).difference(setLike);"
            + "String(r.size === 1 && r.has(1))"));

    [Fact]
    public void IntersectionUsesKeysAndSkipsHasWhenReceiverIsLarger()
        => Assert.Equal("true", Eval(
            "var setLike = { size: 0,"
            + "  has: function(){ throw new Error('has must not be called'); },"
            + "  keys: function(){ return { next(){ return { done:true }; } }; } };"
            + "String(new Set([1, 2]).intersection(setLike).size === 0)"));

    [Fact]
    public void IsSubsetOfReturnsFalseWithoutProbingHasWhenReceiverIsSmaller()
        => Assert.Equal("false", Eval(
            "var setLike = { size: 0,"
            + "  has: function(){ throw new Error('has must not be called'); },"
            + "  keys: function(){ return { next(){ return { done:true }; } }; } };"
            + "String(new Set([1]).isSubsetOf(setLike))"));

    [Fact]
    public void DifferenceIteratesSnapshotWhenHasMutatesReceiver()
        => Assert.Equal("11,22", Eval(
            "var set = new Set([1, 2, 3, 4]);"
            + "var seen = [];"
            + "var setLike = { size: 100,"
            + "  has: function(v){ if (set.size === 4) { set.clear(); set.add(11); set.add(22); }"
            + "    seen.push(v); return true; },"
            + "  keys: function(){ throw new Error('keys must not be called'); } };"
            + "set.difference(setLike);"
            + "[...set].join(',')"));

    [Fact]
    public void DifferenceSnapshotProbesOnlyOriginalElements()
        => Assert.Equal("1,2,3,4", Eval(
            "var set = new Set([1, 2, 3, 4]);"
            + "var seen = [];"
            + "var setLike = { size: 100,"
            + "  has: function(v){ if (set.size === 4) { set.clear(); set.add(11); set.add(22); }"
            + "    seen.push(v); return true; },"
            + "  keys: function(){ throw new Error('keys must not be called'); } };"
            + "set.difference(setLike);"
            + "seen.join(',')"));
}
