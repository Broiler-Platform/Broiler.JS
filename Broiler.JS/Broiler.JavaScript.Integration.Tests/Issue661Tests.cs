using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/661
//
// Fixed here:
//   * Problem 1  — v-flag (unicodeSets) extended character classes with set
//                  operations (`A--B` difference, `A&&B` intersection), `\q{...}`
//                  string literals, and properties of strings are evaluated to a
//                  concrete set and emitted as a literal alternation.
//   * Problem 5  — statements after a try/catch whose catch ends in `return`
//                  (or any abrupt completion) were skipped when the try block
//                  completed normally. The generated `leave` for normal
//                  completion fell straight into the catch's return-hop, so the
//                  function returned `undefined`. (harness assert.throws-no-error)
//   * Problem 6  — object rest (`{...rest}`) in a `for` head with a lexical
//                  binding never declared the per-iteration temp, throwing
//                  "<N> is not defined" once the loop body ran.
//   * Problem 9  — Array.prototype.every skipped indices inherited from the
//                  prototype chain (e.g. a boxed primitive whose wrapper
//                  prototype defines indexed properties); it must use
//                  HasProperty/Get, not own-element access.
//
// Problems 2-4 (sm deepEqual harness, Intl.DateTimeFormat formatRange,
// Date.toISOString extended range), 7 (abrupt completion in `finally` overriding
// a pending throw), 8 (compound-assignment PutValue ordering with a direct-eval
// var binding) and 10 (IteratorClose on an abrupt completion during destructuring
// rest) are triaged in the issue and remain out of scope for this change.
public class Issue661Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 1: v-mode class set operations ----

    // Difference: digits minus keycap sequences = the digits (no overlap).
    [Fact]
    public void DifferenceDigitsMinusKeycapMatchesDigitsOnly()
        => Assert.Equal("true,false,false", Eval(
            "var re=/^[\\d--\\p{Emoji_Keycap_Sequence}]+$/v;"
            + "[re.test('0'), re.test('6\\uFE0F\\u20E3'), re.test('C')].join(',')"));

    // Difference: keycap sequences minus the digit chars = all keycaps
    // (a 3-code-unit keycap is never a single digit char).
    [Fact]
    public void DifferenceKeycapMinusDigitClassKeepsKeycaps()
        => Assert.Equal("true,true,false", Eval(
            "var re=/^[\\p{Emoji_Keycap_Sequence}--[0-9]]+$/v;"
            + "[re.test('0\\uFE0F\\u20E3'), re.test('#\\uFE0F\\u20E3'), re.test('7')].join(',')"));

    // Difference against a \q{...} string literal removes the named sequence.
    [Fact]
    public void DifferenceAgainstStringLiteralRemovesSequence()
        => Assert.Equal("false,true", Eval(
            "var re=/^[\\p{Emoji_Keycap_Sequence}--\\q{0|2|4|9\\uFE0F\\u20E3}]+$/v;"
            + "[re.test('9\\uFE0F\\u20E3'), re.test('8\\uFE0F\\u20E3')].join(',')"));

    // Intersection of a property of strings with itself is that property.
    [Fact]
    public void IntersectionOfKeycapWithItself()
        => Assert.Equal("true,false", Eval(
            "var re=/^[\\p{Emoji_Keycap_Sequence}&&\\p{Emoji_Keycap_Sequence}]+$/v;"
            + "[re.test('1\\uFE0F\\u20E3'), re.test('7')].join(',')"));

    // Intersection of a character property (ASCII_Hex_Digit) with a property of
    // strings is empty (no single hex digit is a keycap sequence).
    [Fact]
    public void IntersectionOfCharPropertyAndStringPropertyIsEmpty()
        => Assert.Equal("false,false", Eval(
            "var re=/[\\p{Emoji_Keycap_Sequence}&&\\p{ASCII_Hex_Digit}]/v;"
            + "[re.test('1\\uFE0F\\u20E3'), re.test('a')].join(',')"));

    // Plain v-mode classes (no set construct) are untouched and still work.
    [Fact]
    public void PlainUnicodeSetsClassesStillWork()
        => Assert.Equal("true,false,true", Eval(
            "var re=/^[a-z]+$/v; var neg=/^[^0-9]+$/v;"
            + "[re.test('abc'), re.test('ABC'), neg.test('abc')].join(',')"));

    // ---- Problem 5: code after try/catch with a returning catch ----

    [Fact]
    public void StatementAfterTryCatchRunsWhenTryCompletesNormally()
        => Assert.Equal("AFTER", Eval(
            "function g(){ try { 1; } catch(e){ return 'C'; } return 'AFTER'; } g();"));

    [Fact]
    public void ThrowAfterTryCatchWithReturningCatchIsReached()
        => Assert.Equal("FINAL", Eval(
            "var r='?'; function f(){} "
            + "function g(){ try { f(); } catch(e){ return 'C'; } return 'FINAL'; } r=g(); r"));

    [Fact]
    public void ReturningCatchStillReturnsWhenTryThrows()
        => Assert.Equal("C", Eval(
            "function g(){ try { throw 9; } catch(e){ return 'C'; } return 'AFTER'; } g();"));

    // assert.throws-style harness shape: assert.throws must throw a Test262Error
    // when the inner function does NOT throw, and the caller must observe it.
    [Fact]
    public void AssertThrowsShapeObservesThrownWhenFuncDoesNotThrow()
        => Assert.Equal("true", Eval(
            "function at(fn){ try{ fn(); }catch(t){ return; } throw new Error('final'); }"
            + "var threw=false; try{ at(function(){}); }catch(e){ threw=true; } String(threw);"));

    // ---- Problem 6: object rest in a lexical `for` head ----

    [Fact]
    public void ConstObjectRestInForHeadBindsRest()
        => Assert.Equal("1,2", Eval(
            "var o=''; for (const {...rest} = {x:1,y:2}; o===''; ) { o = rest.x+','+rest.y; } o"));

    [Fact]
    public void LetPartialObjectRestInForHeadExcludesDestructuredKeys()
        => Assert.Equal("1|3|false", Eval(
            "var o=''; for (let {x,...r} = {x:1,y:2,z:3}; o===''; ) { o = x+'|'+r.z+'|'+('x' in r); } o"));

    [Fact]
    public void ObjectRestPerIterationBindingIsFreshEachIteration()
        => Assert.Equal("1,1", Eval(
            "var seen=[]; for (const {...r} = {a:1}; seen.length<2; ) { seen.push(r.a); if(seen.length===2) break; } seen.join(',')"));

    [Fact]
    public void ComputedKeyInForHeadStillWorks()
        => Assert.Equal("42", Eval(
            "var k='q', o=''; for (const {[k]:v} = {q:42}; o===''; ) { o = String(v); } o"));

    // ---- Problem 9: Array.prototype.every with an inherited indexed property ----

    [Fact]
    public void EveryVisitsIndicesInheritedFromPrototype()
        => Assert.Equal("true", Eval(
            "Boolean.prototype[0]=1; Boolean.prototype.length=1; var accessed=false;"
            + "var res = Array.prototype.every.call(false, function(v,i,o){ accessed=true; return o instanceof Boolean; });"
            + "delete Boolean.prototype[0]; delete Boolean.prototype.length;"
            + "String(res && accessed)"));

    [Fact]
    public void EveryStillSkipsTrueHolesWithNoInheritedProperty()
        => Assert.Equal("0", Eval(
            "var count=0; [,,].every(function(){ count++; return true; }); String(count)"));
}
