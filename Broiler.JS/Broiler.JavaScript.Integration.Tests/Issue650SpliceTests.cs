using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/650 — Problem 7.
//
// Array.prototype.splice on array-like objects whose `length` exceeds the 32-bit
// array-index range (up to 2^53-1) previously threw RangeError "The array is too
// long". Per spec, splice must operate on such objects via numeric property keys,
// clamp `length` through ToLength, and only throw a TypeError when the resulting
// length would exceed 2^53-1. Mirrors test/built-ins/Array/prototype/splice/
// {clamps-length-to-integer-limit, length-exceeding-integer-limit-shrink-array,
//  length-and-deleteCount-exceeding-integer-limit, length-near-integer-limit-grow-array,
//  S15.4.4.12_A3_T1}.js
//
// NB: explicit decimal literals are used instead of `2 ** 53 - 1` because Broiler
// currently mis-parses the precedence of `**` against `-` (a separate bug); these
// literals are the exact integer values a conformant engine computes.
//   2^53-1 = 9007199254740991   2^53 = 9007199254740992   2^53+2 = 9007199254740994
//   2^53-2 = 9007199254740990   2^53-3 = 9007199254740989  2^53+4 = 9007199254740996
public class Issue650SpliceTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ToLength clamps oversized lengths to 2^53-1; splice() with no args leaves
    // the (clamped) length in place rather than throwing.
    [Fact]
    public void ClampsLengthToIntegerLimit()
        => Assert.Equal("9007199254740991 9007199254740991 9007199254740991 9007199254740991", Eval(@"
var a = {}; var out = [];
a.length = 9007199254740991; Array.prototype.splice.call(a); out.push(a.length);
a.length = 9007199254740992; Array.prototype.splice.call(a); out.push(a.length);
a.length = 9007199254740994; Array.prototype.splice.call(a); out.push(a.length);
a.length = Infinity;         Array.prototype.splice.call(a); out.push(a.length);
out.join(' ')"));

    // Removing one element near the integer limit shifts the tail down by one.
    [Fact]
    public void ShrinkArrayNearIntegerLimit()
        => Assert.Equal("9007199254740987|9007199254740990|9007199254740988|false|9007199254740990|false|9007199254740991", Eval(@"
var a = {
  '9007199254740986':'9007199254740986',
  '9007199254740987':'9007199254740987',
  '9007199254740988':'9007199254740988',
  '9007199254740990':'9007199254740990',
  '9007199254740991':'9007199254740991',
  length: 9007199254740994 };
var r = Array.prototype.splice.call(a, 9007199254740987, 1);
[r[0], a.length, a['9007199254740987'],
 ('9007199254740988' in a), a['9007199254740989'],
 ('9007199254740990' in a), a['9007199254740991']].join('|')"));

    // deleteCount exceeding the limit is clamped to len - start.
    [Fact]
    public void DeleteCountExceedingIntegerLimit()
        => Assert.Equal("9007199254740989,9007199254740990|9007199254740989|9007199254740988|false|false|9007199254740991", Eval(@"
var a = {
  '9007199254740988':'9007199254740988',
  '9007199254740989':'9007199254740989',
  '9007199254740990':'9007199254740990',
  '9007199254740991':'9007199254740991',
  length: 9007199254740994 };
var r = Array.prototype.splice.call(a, 9007199254740989, 9007199254740996);
[r.join(','), a.length, a['9007199254740988'],
 ('9007199254740989' in a), ('9007199254740990' in a), a['9007199254740991']].join('|')"));

    // Inserting one element grows length from 2^53-2 to exactly 2^53-1 (no throw).
    [Fact]
    public void GrowArrayNearIntegerLimit()
        => Assert.Equal("0|9007199254740991|new-value", Eval(@"
var a = {
  '9007199254740986':'9007199254740986',
  '9007199254740987':'9007199254740987',
  '9007199254740988':'9007199254740988',
  '9007199254740989':'9007199254740989',
  length: 9007199254740990 };
var r = Array.prototype.splice.call(a, 9007199254740986, 0, 'new-value');
[r.length, a.length, a['9007199254740986']].join('|')"));

    // Sputnik S15.4.4.12_A3_T1: length 2^32, splice the last (2^32-1) element.
    // (join renders the now-undefined obj[4294967295] as an empty field.)
    [Fact]
    public void ToLengthForNonArrayObjectAtUint32Limit()
        => Assert.Equal("1|4294967295|x||y", Eval(@"
var obj = {}; obj.splice = Array.prototype.splice;
obj[0] = 'x'; obj[4294967295] = 'y'; obj.length = 4294967296;
var arr = obj.splice(4294967295, 1);
[arr.length, obj.length, obj[0], obj[4294967295], arr[0]].join('|')"));

    // A result length exceeding 2^53-1 is a TypeError (spec step 7).
    [Fact]
    public void ResultLengthExceedingSafeIntegerThrowsTypeError()
        => Assert.Equal("TypeError", Eval(@"
var a = { length: 9007199254740991 };
var c = 'no throw';
try { Array.prototype.splice.call(a, 0, 0, 'x'); } catch (e) { c = e.constructor.name; }
c"));

    // A real (32-bit) array still splices correctly via the fast path.
    [Fact]
    public void OrdinaryArraySpliceUnaffected()
        => Assert.Equal("2,3|1,9,4", Eval(
            "var a=[1,2,3,4]; var r=a.splice(1,2,9); r.join(',') + '|' + a.join(',')"));
}
