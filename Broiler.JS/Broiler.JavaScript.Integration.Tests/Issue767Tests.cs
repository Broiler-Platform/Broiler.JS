using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/767
//
// Fixed here:
//
//   Problem 3 — The abstract %Iterator% constructor (ES2025 §27.1.3.1) threw
//   unconditionally, so `class X extends Iterator { constructor(){ super(); } }`
//   could never be instantiated — every Iterator-helper test that builds its
//   fixture by subclassing Iterator failed at the `super()` call with
//   "Iterator is not intended to be called as a constructor". Per spec the
//   constructor throws ONLY when NewTarget is undefined (a plain call) or is the
//   %Iterator% intrinsic itself (`new Iterator()` / `Reflect.construct(Iterator,
//   [], Iterator)`); a genuine subclass passes a distinct NewTarget and allocates
//   an ordinary object from its prototype.
public class Issue767Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code)?.ToString();
    }

    // --- Problem 3: Iterator must be subclassable, but not directly constructible ---

    [Fact]
    public void DirectNewIteratorThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "let t; try { new Iterator(); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void CallingIteratorWithoutNewThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "let t; try { Iterator(); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void ReflectConstructWithIteratorAsNewTargetThrows()
        => Assert.Equal("TypeError", Eval(
            "let t; try { Reflect.construct(Iterator, [], Iterator); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void SubclassSuperCallSucceeds()
        => Assert.Equal("true", Eval(
            "class Iter extends Iterator { constructor() { super(); } }" +
            "((new Iter()) instanceof Iterator) + ''"));

    [Fact]
    public void SubclassInstanceUsesItsOwnPrototype()
        => Assert.Equal("true", Eval(
            "class Iter extends Iterator { constructor() { super(); } }" +
            "((new Iter()) instanceof Iter) + ''"));

    [Fact]
    public void SubclassInheritsIteratorHelpers()
        => Assert.Equal("function", Eval(
            "class Iter extends Iterator { constructor() { super(); } }" +
            "typeof (new Iter()).drop"));

    [Fact]
    public void ReflectConstructWithSubclassNewTargetSucceeds()
        => Assert.Equal("true", Eval(
            "class Iter extends Iterator {}" +
            "((Reflect.construct(Iterator, [], Iter)) instanceof Iter) + ''"));

    // A subclass that supplies a `next` method can drive the inherited lazy helpers
    // (the canonical shape of the Iterator/prototype/{drop,every,...} fixtures).
    [Fact]
    public void SubclassNextDrivesInheritedDrop()
        => Assert.Equal("2,3", Eval(
            "class Iter extends Iterator {" +
            "  constructor() { super(); this.i = 0; }" +
            "  next() { this.i++; return this.i > 3 ? { done: true, value: undefined } : { done: false, value: this.i }; }" +
            "}" +
            "Array.from(new Iter().drop(1)).join(',')"));

    // ---- Problems 6 & 8: DisposableStack / AsyncDisposableStack built-ins ----

    private static string Drive(string body)
    {
        using var ctx = new JSContext();
        ctx.Eval("globalThis.r = '<unset>';");
        ctx.Execute(body);
        return ctx.Eval("'' + globalThis.r").ToString();
    }

    [Fact]
    public void DisposableStackIsAConstructor()
        => Assert.Equal("function", Eval("typeof DisposableStack"));

    [Fact]
    public void DisposableStackDisposesLifo()
        => Assert.Equal("c,b,a", Eval(
            "let log = [];" +
            "const s = new DisposableStack();" +
            "s.use({ [Symbol.dispose]() { log.push('a'); } });" +
            "s.use({ [Symbol.dispose]() { log.push('b'); } });" +
            "s.defer(() => log.push('c'));" +
            "s.dispose();" +
            "log.join(',')"));

    [Fact]
    public void DisposableStackDisposedGetter()
        => Assert.Equal("false,true", Eval(
            "const s = new DisposableStack();" +
            "const before = s.disposed; s.dispose();" +
            "before + ',' + s.disposed"));

    [Fact]
    public void DisposableStackUseReturnsValueAndAdopt()
        => Assert.Equal("42,7", Eval(
            "let disposed = 0;" +
            "const s = new DisposableStack();" +
            "const a = s.use({ value: 42, [Symbol.dispose]() {} });" +
            "const b = s.adopt(7, v => { disposed = v; });" +
            "s.dispose();" +
            "a.value + ',' + disposed"));

    [Fact]
    public void DisposableStackUseNonDisposableThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "let t; const s = new DisposableStack();" +
            "try { s.use({}); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void DisposableStackOperationAfterDisposeThrowsReferenceError()
        => Assert.Equal("ReferenceError", Eval(
            "let t; const s = new DisposableStack(); s.dispose();" +
            "try { s.use({ [Symbol.dispose]() {} }); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void DisposableStackSymbolDisposeIsDisposeMethod()
        => Assert.Equal("true", Eval(
            "(DisposableStack.prototype[Symbol.dispose] === DisposableStack.prototype.dispose) + ''"));

    [Fact]
    public void DisposableStackToStringTag()
        => Assert.Equal("[object DisposableStack]", Eval(
            "Object.prototype.toString.call(new DisposableStack())"));

    [Fact]
    public void DisposableStackMoveTransfersResources()
        => Assert.Equal("moved,true", Eval(
            "let log = [];" +
            "const s = new DisposableStack();" +
            "s.defer(() => log.push('moved'));" +
            "const t = s.move();" +
            "const sWasDisposed = s.disposed;" +
            "s.dispose();" +            // no-op, resources moved out
            "t.dispose();" +
            "log.join(',') + ',' + sWasDisposed"));

    [Fact]
    public void DisposableStackAggregatesErrorsAsSuppressedError()
        => Assert.Equal("SuppressedError", Eval(
            "let t; const s = new DisposableStack();" +
            "s.defer(() => { throw new Error('first'); });" +
            "s.defer(() => { throw new Error('second'); });" +
            "try { s.dispose(); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void DisposableStackRequiresNew()
        => Assert.Equal("TypeError", Eval(
            "let t; try { DisposableStack(); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void AsyncDisposableStackIsAConstructor()
        => Assert.Equal("function", Eval("typeof AsyncDisposableStack"));

    [Fact]
    public void AsyncDisposableStackSymbolAsyncDispose()
        => Assert.Equal("true", Eval(
            "(AsyncDisposableStack.prototype[Symbol.asyncDispose] === AsyncDisposableStack.prototype.disposeAsync) + ''"));

    [Fact]
    public void AsyncDisposableStackDisposesAndAwaits()
        => Assert.Equal("a,b", Drive(
            "let log = [];" +
            "const s = new AsyncDisposableStack();" +
            "s.defer(() => log.push('b'));" +
            "s.use({ async [Symbol.asyncDispose]() { log.push('a'); } });" +
            "(async () => { await s.disposeAsync(); globalThis.r = log.join(','); })();"));

    [Fact]
    public void AsyncDisposableStackToStringTag()
        => Assert.Equal("[object AsyncDisposableStack]", Eval(
            "Object.prototype.toString.call(new AsyncDisposableStack())"));

    // ---- Problems 4, 7 & 10: resizable ArrayBuffer + length-tracking views ----

    [Fact]
    public void ArrayBufferResizeIsAFunction()
        => Assert.Equal("function", Eval("typeof ArrayBuffer.prototype.resize"));

    [Fact]
    public void ResizableArrayBufferReportsResizableAndMax()
        => Assert.Equal("true,16", Eval(
            "const ab = new ArrayBuffer(8, { maxByteLength: 16 });" +
            "ab.resizable + ',' + ab.maxByteLength"));

    [Fact]
    public void FixedArrayBufferIsNotResizable()
        => Assert.Equal("false,8", Eval(
            "const ab = new ArrayBuffer(8);" +
            "ab.resizable + ',' + ab.maxByteLength"));

    [Fact]
    public void ResizeGrowsByteLength()
        => Assert.Equal("8,16", Eval(
            "const ab = new ArrayBuffer(8, { maxByteLength: 16 });" +
            "const before = ab.byteLength; ab.resize(16);" +
            "before + ',' + ab.byteLength"));

    [Fact]
    public void ResizeShrinksAndZeroFillsOnRegrow()
        => Assert.Equal("9,0", Eval(
            "const ab = new ArrayBuffer(4, { maxByteLength: 8 });" +
            "const u = new Uint8Array(ab); u[0] = 9; u[3] = 7;" +
            "ab.resize(2); ab.resize(4);" +            // shrink past index 3, then grow back
            "u[0] + ',' + u[3]"));                     // u[0] preserved, u[3] re-zeroed

    [Fact]
    public void LengthTrackingTypedArrayFollowsResize()
        => Assert.Equal("4,2,7", Eval(
            "const ab = new ArrayBuffer(4, { maxByteLength: 8 });" +
            "const u = new Uint8Array(ab);" +          // no length → length-tracking
            "const grown = (ab.resize(7), u.length);" +
            "const shrunk = (ab.resize(2), u.length);" +
            "ab.resize(4); u.length = u.length;" +     // no-op write ignored
            "'' + 4 + ',' + shrunk + ',' + grown"));

    [Fact]
    public void FixedLengthViewBecomesOutOfBoundsAfterShrink()
        => Assert.Equal("4,0,4", Eval(
            "const ab = new ArrayBuffer(4, { maxByteLength: 8 });" +
            "const u = new Uint8Array(ab, 0, 4);" +    // explicit length → fixed
            "const a = u.length;" +
            "ab.resize(2); const b = u.length;" +      // view no longer fits → 0
            "ab.resize(4); const c = u.length;" +      // back in bounds → 4
            "a + ',' + b + ',' + c"));

    [Fact]
    public void ResizeBeyondMaxThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "let t; const ab = new ArrayBuffer(4, { maxByteLength: 8 });" +
            "try { ab.resize(9); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void ResizeNonResizableThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "let t; const ab = new ArrayBuffer(4);" +
            "try { ab.resize(2); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void ConstructWithLengthAboveMaxThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "let t; try { new ArrayBuffer(10, { maxByteLength: 5 }); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void DataViewLengthTrackingFollowsResize()
        => Assert.Equal("4,7", Eval(
            "const ab = new ArrayBuffer(4, { maxByteLength: 8 });" +
            "const dv = new DataView(ab);" +           // length-tracking
            "const a = dv.byteLength; ab.resize(7);" +
            "a + ',' + dv.byteLength"));

    [Fact]
    public void DataViewOutOfBoundsAccessThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "let t; const ab = new ArrayBuffer(8, { maxByteLength: 8 });" +
            "const dv = new DataView(ab, 0, 8);" +     // fixed length 8
            "ab.resize(4);" +                          // view no longer fits
            "try { dv.getInt8(0); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void TypedArrayForEachOverResizableBuffer()
        => Assert.Equal("3", Eval(
            "const ab = new ArrayBuffer(3, { maxByteLength: 8 });" +
            "const u = new Uint8Array(ab);" +
            "let count = 0; u.forEach(() => count++);" +
            "'' + count"));

    // ---- Problems 2 & 9: SharedArrayBuffer ----

    [Fact]
    public void SharedArrayBufferIsAConstructor()
        => Assert.Equal("function,8", Eval(
            "typeof SharedArrayBuffer + ',' + new SharedArrayBuffer(8).byteLength"));

    [Fact]
    public void SharedArrayBufferNegativeLengthThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "let t; try { new SharedArrayBuffer(-1); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void SharedArrayBufferMaxByteLengthExcessiveThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "let t; try { new SharedArrayBuffer(10, { maxByteLength: 5 }); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void GrowableSharedArrayBufferReportsGrowableAndMax()
        => Assert.Equal("true,16", Eval(
            "const sab = new SharedArrayBuffer(8, { maxByteLength: 16 });" +
            "sab.growable + ',' + sab.maxByteLength"));

    [Fact]
    public void SharedArrayBufferGrows()
        => Assert.Equal("8,16", Eval(
            "const sab = new SharedArrayBuffer(8, { maxByteLength: 16 });" +
            "const before = sab.byteLength; sab.grow(16);" +
            "before + ',' + sab.byteLength"));

    [Fact]
    public void SharedArrayBufferGrowBelowCurrentThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "let t; const sab = new SharedArrayBuffer(8, { maxByteLength: 16 });" +
            "try { sab.grow(4); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void ArrayBufferByteLengthGetterRejectsSharedReceiver()
        => Assert.Equal("TypeError", Eval(
            "let t; const sab = new SharedArrayBuffer(8);" +
            "const get = Object.getOwnPropertyDescriptor(ArrayBuffer.prototype, 'byteLength').get;" +
            "try { get.call(sab); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void ArrayBufferSliceRejectsSharedReceiver()
        => Assert.Equal("TypeError", Eval(
            "let t; const sab = new SharedArrayBuffer(8);" +
            "try { ArrayBuffer.prototype.slice.call(sab, 0); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void SharedArrayBufferIsNotAnArrayBuffer()
        => Assert.Equal("false", Eval(
            "((new SharedArrayBuffer(8)) instanceof ArrayBuffer) + ''"));

    [Fact]
    public void SharedArrayBufferToStringTag()
        => Assert.Equal("[object SharedArrayBuffer]", Eval(
            "Object.prototype.toString.call(new SharedArrayBuffer(8))"));

    [Fact]
    public void TypedArrayOverSharedArrayBufferReadsBack()
        => Assert.Equal("5", Eval(
            "const sab = new SharedArrayBuffer(8);" +
            "const ta = new Int32Array(sab); ta[0] = 5; '' + ta[0]"));

    [Fact]
    public void SharedArrayBufferSliceReturnsShared()
        => Assert.Equal("true,2", Eval(
            "const sab = new SharedArrayBuffer(8);" +
            "const s = sab.slice(2, 4);" +
            "(s instanceof SharedArrayBuffer) + ',' + s.byteLength"));

    // ---- Problems 2 & 9: Atomics operations ----

    [Fact]
    public void AtomicsAddReturnsOldAndUpdates()
        => Assert.Equal("5,8", Eval(
            "const ta = new Int32Array(new SharedArrayBuffer(8)); ta[0] = 5;" +
            "const old = Atomics.add(ta, 0, 3);" +
            "old + ',' + ta[0]"));

    [Fact]
    public void AtomicsWorksOverNonSharedBuffer()
        => Assert.Equal("10,17", Eval(
            "const ta = new Int32Array(4); ta[1] = 10;" +   // plain (non-shared) buffer
            "const old = Atomics.add(ta, 1, 7);" +
            "old + ',' + ta[1]"));

    [Fact]
    public void AtomicsSubAndOrXor()
        => Assert.Equal("6,4,7,2", Eval(
            "const ta = new Uint8Array(new SharedArrayBuffer(4));" +
            "ta[0]=10; Atomics.sub(ta,0,4);" +              // 6
            "ta[1]=6;  Atomics.and(ta,1,5);" +              // 6&5 = 4
            "ta[2]=3;  Atomics.or(ta,2,5);" +               // 3|5 = 7
            "ta[3]=6;  Atomics.xor(ta,3,4);" +              // 6^4 = 2
            "ta[0] + ',' + ta[1] + ',' + ta[2] + ',' + ta[3]"));

    [Fact]
    public void AtomicsExchangeAndCompareExchange()
        => Assert.Equal("1,9,9,9,9,5", Eval(
            "const ta = new Int32Array(new SharedArrayBuffer(8));" +
            "ta[0]=1; const oldX = Atomics.exchange(ta,0,9);" +     // old 1, now 9
            "const ceHit = Atomics.compareExchange(ta,0,9,5);" +    // matches → old 9, now 5
            "const after = ta[0];" +                                // 5
            "ta[1]=9; const ceMiss = Atomics.compareExchange(ta,1,3,7); const after1 = ta[1];" + // no match → old 9, stays 9
            "oldX + ',' + ceHit + ',' + ceMiss + ',' + after1 + ',' + after1 + ',' + after"));

    [Fact]
    public void AtomicsLoadAndStore()
        => Assert.Equal("42,42", Eval(
            "const ta = new Int32Array(new SharedArrayBuffer(8));" +
            "const ret = Atomics.store(ta, 0, 42);" +       // store returns the value
            "ret + ',' + Atomics.load(ta, 0)"));

    [Fact]
    public void AtomicsStoreReturnsIntegerNotTruncated()
        => Assert.Equal("257,1", Eval(
            "const ta = new Uint8Array(new SharedArrayBuffer(4));" +
            "const ret = Atomics.store(ta, 0, 257);" +      // returns 257, stores 257 & 0xFF = 1
            "ret + ',' + ta[0]"));

    [Fact]
    public void AtomicsBigIntOperations()
        => Assert.Equal("0,5", Eval(
            "const ta = new BigInt64Array(new SharedArrayBuffer(16));" +
            "const old = Atomics.add(ta, 0, 5n);" +
            "old.toString() + ',' + ta[0].toString()"));

    [Fact]
    public void AtomicsIsLockFree()
        => Assert.Equal("true,true,true,true,false", Eval(
            "[1,2,4,8,3].map(n => Atomics.isLockFree(n)).join(',')"));

    [Fact]
    public void AtomicsRejectsNonIntegerTypedArray()
        => Assert.Equal("TypeError", Eval(
            "let t; const ta = new Float64Array(4);" +
            "try { Atomics.add(ta, 0, 1); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void AtomicsValidatesArrayTypeBeforeIndexCoercion()
        => Assert.Equal("TypeError,false", Eval(
            "let t, indexCoerced = false;" +
            "const ta = new Float64Array(4);" +
            "const idx = { valueOf() { indexCoerced = true; return 0; } };" +
            "try { Atomics.add(ta, idx, 1); } catch (e) { t = e.constructor.name; }" +
            "t + ',' + indexCoerced"));

    [Fact]
    public void AtomicsOutOfRangeIndexThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "let t; const ta = new Int32Array(new SharedArrayBuffer(8));" +
            "try { Atomics.add(ta, 5, 1); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void AtomicsWaitOnNonSharedThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "let t; const ta = new Int32Array(4);" +        // non-shared
            "try { Atomics.wait(ta, 0, 0); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void AtomicsWaitReturnsNotEqualOnMismatch()
        => Assert.Equal("not-equal", Eval(
            "const ta = new Int32Array(new SharedArrayBuffer(8)); ta[0] = 1;" +
            "Atomics.wait(ta, 0, 0, 0)"));      // current 1 != expected 0

    [Fact]
    public void AtomicsNotifyReturnsZero()
        => Assert.Equal("0", Eval(
            "const ta = new Int32Array(new SharedArrayBuffer(8));" +
            "'' + Atomics.notify(ta, 0, 1)"));

    // ---- Problems 1, 5 & 9: Temporal.Duration ----

    [Fact]
    public void TemporalNamespaceExists()
        => Assert.Equal("object,function,function", Eval(
            "typeof Temporal + ',' + typeof Temporal.Duration + ',' + typeof Temporal.Instant"));

    [Fact]
    public void DurationConstructorAndAccessors()
        => Assert.Equal("1,2,3,4", Eval(
            "const d = new Temporal.Duration(1, 2, 3, 4);" +
            "d.years + ',' + d.months + ',' + d.weeks + ',' + d.days"));

    [Fact]
    public void DurationToString()
        => Assert.Equal("P1Y2M3DT4H5M6S", Eval(
            "new Temporal.Duration(1, 2, 0, 3, 4, 5, 6).toString()"));

    [Fact]
    public void DurationZeroToString()
        => Assert.Equal("PT0S", Eval("new Temporal.Duration().toString()"));

    [Fact]
    public void DurationFromString()
        => Assert.Equal("1,2,3,4,5,6", Eval(
            "const d = Temporal.Duration.from('P1Y2M3DT4H5M6S');" +
            "d.years + ',' + d.months + ',' + d.days + ',' + d.hours + ',' + d.minutes + ',' + d.seconds"));

    [Fact]
    public void DurationFromObject()
        => Assert.Equal("5,30", Eval(
            "const d = Temporal.Duration.from({ hours: 5, minutes: 30 });" +
            "d.hours + ',' + d.minutes"));

    [Fact]
    public void DurationFromFractionalHoursCascades()
        => Assert.Equal("1,30,0", Eval(
            "const d = Temporal.Duration.from('PT1.5H');" +
            "d.hours + ',' + d.minutes + ',' + d.seconds"));

    [Fact]
    public void DurationFractionalSecondsToString()
        => Assert.Equal("PT1.5S", Eval(
            "new Temporal.Duration(0,0,0,0,0,0,1,500).toString()"));

    [Fact]
    public void DurationSignAndBlank()
        => Assert.Equal("0,true,-1,false", Eval(
            "const z = new Temporal.Duration(); const n = new Temporal.Duration(0,0,0,0,-1);" +
            "z.sign + ',' + z.blank + ',' + n.sign + ',' + n.blank"));

    [Fact]
    public void DurationNegatedAndAbs()
        => Assert.Equal("-1,1", Eval(
            "new Temporal.Duration(1).negated().years + ',' + new Temporal.Duration(-1).abs().years"));

    [Fact]
    public void DurationWith()
        => Assert.Equal("5,2", Eval(
            "const d = new Temporal.Duration(1, 2); const d2 = d.with({ years: 5 });" +
            "d2.years + ',' + d2.months"));

    [Fact]
    public void DurationValueOfThrows()
        => Assert.Equal("TypeError", Eval(
            "let t; try { new Temporal.Duration(1).valueOf(); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void DurationNonIntegerThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "let t; try { new Temporal.Duration(1.5); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void DurationInfinityThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "let t; try { new Temporal.Duration(Infinity); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void DurationCompareTimeUnits()
        => Assert.Equal("-1,1,0", Eval(
            "const c = Temporal.Duration.compare;" +
            "c({hours:1},{hours:2}) + ',' + c({hours:2},{hours:1}) + ',' + c({hours:1},{minutes:60})"));

    [Fact]
    public void DurationCompareCalendarUnitsRequiresRelativeTo()
        => Assert.Equal("RangeError", Eval(
            "let t; try { Temporal.Duration.compare({months:1},{months:2}); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void DurationMethodIsNotAConstructor()
        => Assert.Equal("TypeError", Eval(
            "let t; try { new Temporal.Duration.prototype.abs(); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void DurationToStringTag()
        => Assert.Equal("[object Temporal.Duration]", Eval(
            "Object.prototype.toString.call(new Temporal.Duration())"));

    // ---- Problems 1, 5 & 9: Temporal.Instant ----

    [Fact]
    public void InstantConstructorAndEpochAccessors()
        => Assert.Equal("1000,1000000000", Eval(
            "const i = new Temporal.Instant(1000000000n);" +
            "i.epochMilliseconds + ',' + i.epochNanoseconds.toString()"));

    [Fact]
    public void InstantFromEpochMilliseconds()
        => Assert.Equal("1500,1500000000", Eval(
            "const i = Temporal.Instant.fromEpochMilliseconds(1500);" +
            "i.epochMilliseconds + ',' + i.epochNanoseconds.toString()"));

    [Fact]
    public void InstantFromEpochNanoseconds()
        => Assert.Equal("5", Eval(
            "Temporal.Instant.fromEpochNanoseconds(5n).epochNanoseconds.toString()"));

    [Fact]
    public void InstantOutOfRangeThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "let t; try { new Temporal.Instant(8640000000000000000001n); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void InstantToString()
        => Assert.Equal("1970-01-01T00:00:00Z", Eval("new Temporal.Instant(0n).toString()"));

    [Fact]
    public void InstantToStringWithFraction()
        => Assert.Equal("1970-01-01T00:00:01.5Z", Eval(
            "new Temporal.Instant(1500000000n).toString()"));

    [Fact]
    public void InstantAddTimeDuration()
        => Assert.Equal("3600000000000", Eval(
            "new Temporal.Instant(0n).add({ hours: 1 }).epochNanoseconds.toString()"));

    [Fact]
    public void InstantAddCalendarUnitThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "let t; try { new Temporal.Instant(0n).add({ days: 1 }); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void InstantCompareAndEquals()
        => Assert.Equal("-1,true", Eval(
            "const a = new Temporal.Instant(1n), b = new Temporal.Instant(2n);" +
            "Temporal.Instant.compare(a, b) + ',' + a.equals(new Temporal.Instant(1n))"));

    [Fact]
    public void InstantFromString()
        => Assert.Equal("1000000000", Eval(
            "Temporal.Instant.from('1970-01-01T00:00:01Z').epochNanoseconds.toString()"));

    [Fact]
    public void InstantFromStringWithOffset()
        => Assert.Equal("0", Eval(
            "Temporal.Instant.from('1970-01-01T01:00:00+01:00').epochNanoseconds.toString()"));

    [Fact]
    public void InstantValueOfThrows()
        => Assert.Equal("TypeError", Eval(
            "let t; try { new Temporal.Instant(0n).valueOf(); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void InstantToStringTag()
        => Assert.Equal("[object Temporal.Instant]", Eval(
            "Object.prototype.toString.call(new Temporal.Instant(0n))"));

    [Fact]
    public void InstantRound()
        => Assert.Equal("2000000000", Eval(
            "new Temporal.Instant(1600000000n).round('second').epochNanoseconds.toString()"));

    // ---- Remaining Temporal surface exists as constructors ----
    // (PlainDateTime/PlainYearMonth/PlainMonthDay/ZonedDateTime are now implemented; see
    //  Issue769Tests for their behavior.)

    [Fact]
    public void TemporalStubTypesExistAsConstructors()
        => Assert.Equal("function,function,function,function", Eval(
            "['PlainDateTime','PlainYearMonth','PlainMonthDay','ZonedDateTime']" +
            ".map(n => typeof Temporal[n]).join(',')"));

    [Fact]
    public void TemporalNowIsAStubNamespace()
        => Assert.Equal("object,function", Eval(
            "typeof Temporal.Now + ',' + typeof Temporal.Now.instant"));

    // Temporal.Now.instant() now returns a real instant (see Issue769Tests).
    [Fact]
    public void TemporalNowInstantReturnsInstant()
        => Assert.Equal("[object Temporal.Instant]", Eval(
            "Object.prototype.toString.call(Temporal.Now.instant())"));

    // Duration's add/subtract/round/total now work for the calendar-independent case
    // (see Issue769Tests); the methods still exist on the prototype.
    [Fact]
    public void DurationArithmeticMethodsExist()
        => Assert.Equal("function,function,function,function,PT1H", Eval(
            "const p = Temporal.Duration.prototype;" +
            "const r = new Temporal.Duration(0,0,0,0,1).round('hours').toString();" +
            "typeof p.add + ',' + typeof p.subtract + ',' + typeof p.round + ',' + typeof p.total + ',' + r"));

    // ---- Newly implemented stub: Temporal.PlainTime ----

    [Fact]
    public void PlainTimeConstructorAndAccessors()
        => Assert.Equal("12,34,56,789", Eval(
            "const t = new Temporal.PlainTime(12, 34, 56, 789);" +
            "t.hour + ',' + t.minute + ',' + t.second + ',' + t.millisecond"));

    [Fact]
    public void PlainTimeDefaultsToMidnight()
        => Assert.Equal("0,0,0", Eval(
            "const t = new Temporal.PlainTime();" +
            "t.hour + ',' + t.minute + ',' + t.second"));

    [Fact]
    public void PlainTimeOutOfRangeThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "let t; try { new Temporal.PlainTime(24); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void PlainTimeToString()
        => Assert.Equal("09:05:00,12:30:45.5", Eval(
            "new Temporal.PlainTime(9, 5).toString() + ',' + new Temporal.PlainTime(12, 30, 45, 500).toString()"));

    [Fact]
    public void PlainTimeFromString()
        => Assert.Equal("15:23:30.123", Eval(
            "Temporal.PlainTime.from('15:23:30.123').toString()"));

    [Fact]
    public void PlainTimeFromDateTimeString()
        => Assert.Equal("15:23:30", Eval(
            "Temporal.PlainTime.from('1976-11-18T15:23:30').toString()"));

    [Fact]
    public void PlainTimeFromObject()
        => Assert.Equal("08:15:00", Eval(
            "Temporal.PlainTime.from({ hour: 8, minute: 15 }).toString()"));

    [Fact]
    public void PlainTimeFromConstrainsByDefault()
        => Assert.Equal("23:59:59", Eval(
            "Temporal.PlainTime.from({ hour: 99, minute: 99, second: 99 }).toString()"));

    [Fact]
    public void PlainTimeFromRejectThrows()
        => Assert.Equal("RangeError", Eval(
            "let t; try { Temporal.PlainTime.from({ hour: 99 }, { overflow: 'reject' }); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void PlainTimeWith()
        => Assert.Equal("12:45:30", Eval(
            "new Temporal.PlainTime(12, 30, 30).with({ minute: 45 }).toString()"));

    [Fact]
    public void PlainTimeAddWrapsAroundMidnight()
        => Assert.Equal("01:30:00", Eval(
            "new Temporal.PlainTime(23, 0).add({ hours: 2, minutes: 30 }).toString()"));

    [Fact]
    public void PlainTimeSubtractWrapsAroundMidnight()
        => Assert.Equal("23:30:00", Eval(
            "new Temporal.PlainTime(0, 30).subtract({ hours: 1 }).toString()"));

    [Fact]
    public void PlainTimeUntil()
        => Assert.Equal("2,30,0", Eval(
            "const d = new Temporal.PlainTime(10, 0).until(new Temporal.PlainTime(12, 30));" +
            "d.hours + ',' + d.minutes + ',' + d.seconds"));

    [Fact]
    public void PlainTimeRound()
        => Assert.Equal("12,0,0", Eval(
            "const t = new Temporal.PlainTime(11, 45).round('hour');" +
            "t.hour + ',' + t.minute + ',' + t.second"));

    [Fact]
    public void PlainTimeCompareAndEquals()
        => Assert.Equal("-1,true", Eval(
            "const a = new Temporal.PlainTime(9), b = new Temporal.PlainTime(10);" +
            "Temporal.PlainTime.compare(a, b) + ',' + a.equals(new Temporal.PlainTime(9))"));

    [Fact]
    public void PlainTimeValueOfThrows()
        => Assert.Equal("TypeError", Eval(
            "let t; try { new Temporal.PlainTime(1).valueOf(); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void PlainTimeToStringTag()
        => Assert.Equal("[object Temporal.PlainTime]", Eval(
            "Object.prototype.toString.call(new Temporal.PlainTime())"));

    // ---- Newly implemented stub: Temporal.PlainDate (ISO 8601 calendar) ----

    [Fact]
    public void PlainDateConstructorAndAccessors()
        => Assert.Equal("2024,3,M03,15,iso8601", Eval(
            "const d = new Temporal.PlainDate(2024, 3, 15);" +
            "d.year + ',' + d.month + ',' + d.monthCode + ',' + d.day + ',' + d.calendarId"));

    [Fact]
    public void PlainDateInvalidThrowsRangeError()
        => Assert.Equal("RangeError,RangeError", Eval(
            "function tryNew(y, m, day) { try { new Temporal.PlainDate(y, m, day); return 'ok'; } catch (e) { return e.constructor.name; } }" +
            "tryNew(2024, 13, 1) + ',' + tryNew(2023, 2, 29)"));

    [Fact]
    public void PlainDateLeapDayValid()
        => Assert.Equal("2024-02-29", Eval("new Temporal.PlainDate(2024, 2, 29).toString()"));

    [Fact]
    public void PlainDateToStringExpandedYear()
        => Assert.Equal("-002000-01-01,+010000-06-15", Eval(
            "new Temporal.PlainDate(-2000, 1, 1).toString() + ',' + new Temporal.PlainDate(10000, 6, 15).toString()"));

    [Fact]
    public void PlainDateDerivedFields()
        => Assert.Equal("5,75,31,366,true", Eval(
            "const d = new Temporal.PlainDate(2024, 3, 15);" +    // Friday, day-of-year 75, leap year
            "d.dayOfWeek + ',' + d.dayOfYear + ',' + d.daysInMonth + ',' + d.daysInYear + ',' + d.inLeapYear"));

    [Fact]
    public void PlainDateWeekOfYear()
        => Assert.Equal("1,2021", Eval(
            "const d = new Temporal.PlainDate(2021, 1, 4);" +     // ISO week 1 of 2021
            "d.weekOfYear + ',' + d.yearOfWeek"));

    [Fact]
    public void PlainDateFromString()
        => Assert.Equal("1976-11-18", Eval("Temporal.PlainDate.from('1976-11-18').toString()"));

    [Fact]
    public void PlainDateFromDateTimeString()
        => Assert.Equal("2020-01-15", Eval("Temporal.PlainDate.from('2020-01-15T12:30:00').toString()"));

    [Fact]
    public void PlainDateFromObjectWithMonthCode()
        => Assert.Equal("2024-07-04", Eval(
            "Temporal.PlainDate.from({ year: 2024, monthCode: 'M07', day: 4 }).toString()"));

    [Fact]
    public void PlainDateFromConstrainsByDefault()
        => Assert.Equal("2023-02-28", Eval(
            "Temporal.PlainDate.from({ year: 2023, month: 2, day: 31 }).toString()"));

    [Fact]
    public void PlainDateFromRejectThrows()
        => Assert.Equal("RangeError", Eval(
            "let t; try { Temporal.PlainDate.from({ year: 2023, month: 2, day: 31 }, { overflow: 'reject' }); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void PlainDateAddMonthsConstrainsDay()
        => Assert.Equal("2024-02-29", Eval(
            "new Temporal.PlainDate(2024, 1, 31).add({ months: 1 }).toString()"));

    [Fact]
    public void PlainDateAddDaysRollsOver()
        => Assert.Equal("2025-01-05", Eval(
            "new Temporal.PlainDate(2024, 12, 31).add({ days: 5 }).toString()"));

    [Fact]
    public void PlainDateSubtractWeeks()
        => Assert.Equal("2024-03-01", Eval(
            "new Temporal.PlainDate(2024, 3, 15).subtract({ weeks: 2 }).toString()"));

    [Fact]
    public void PlainDateUntilDays()
        => Assert.Equal("0,0,0,16", Eval(
            "const d = new Temporal.PlainDate(2024, 3, 1).until(new Temporal.PlainDate(2024, 3, 17));" +
            "d.years + ',' + d.months + ',' + d.weeks + ',' + d.days"));

    [Fact]
    public void PlainDateUntilYearsMonthsDays()
        => Assert.Equal("2,3,10", Eval(
            "const d = new Temporal.PlainDate(2020, 1, 5).until(new Temporal.PlainDate(2022, 4, 15), { largestUnit: 'year' });" +
            "d.years + ',' + d.months + ',' + d.days"));

    [Fact]
    public void PlainDateUntilLargestUnitMonth()
        => Assert.Equal("27,10", Eval(
            "const d = new Temporal.PlainDate(2020, 1, 5).until(new Temporal.PlainDate(2022, 4, 15), { largestUnit: 'month' });" +
            "d.months + ',' + d.days"));

    [Fact]
    public void PlainDateCompareAndEquals()
        => Assert.Equal("-1,0,1,true", Eval(
            "const c = Temporal.PlainDate.compare;" +
            "const a = new Temporal.PlainDate(2024, 1, 1), b = new Temporal.PlainDate(2024, 6, 1);" +
            "c(a, b) + ',' + c(a, a) + ',' + c(b, a) + ',' + a.equals(new Temporal.PlainDate(2024, 1, 1))"));

    [Fact]
    public void PlainDateWith()
        => Assert.Equal("2024-12-15", Eval(
            "new Temporal.PlainDate(2024, 3, 15).with({ month: 12 }).toString()"));

    [Fact]
    public void PlainDateValueOfThrows()
        => Assert.Equal("TypeError", Eval(
            "let t; try { new Temporal.PlainDate(2024, 1, 1).valueOf(); } catch (e) { t = e.constructor.name; } t"));

    [Fact] // the arithmetic + lunisolar (#773), persian / ethioaa / islamic-umalqura (#775) and
           // indian (#777) calendars are supported; the sighting-based islamic-rgsa is not.
    public void PlainDateUnsupportedCalendarThrows()
        => Assert.Equal("RangeError", Eval(
            "let t; try { new Temporal.PlainDate(2024, 1, 1, 'islamic-rgsa'); } catch (e) { t = e.constructor.name; } t"));

    [Fact]
    public void PlainDateToStringTag()
        => Assert.Equal("[object Temporal.PlainDate]", Eval(
            "Object.prototype.toString.call(new Temporal.PlainDate(2024, 1, 1))"));
}
