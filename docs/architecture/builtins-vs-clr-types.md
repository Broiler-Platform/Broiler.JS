# Why the built-ins are not .NET types

An analysis of a question that gets asked of every managed JavaScript engine: **why does
`Broiler.JavaScript.BuiltIns` define `JSArray`, `JSString`, `JSMap` and forty more, instead
of using `System.Array`, `System.String` and `Dictionary<K,V>` — and why can they not simply
*derive* from them?**

The short answer is in three parts:

1. **They are not duplicates — they are wrappers.** The .NET type does the data; the `JS*`
   type does the semantics. `JSNumber` holds a `double`. `JSString` holds a `string`.
   `JSMap` holds a `Dictionary<JSValue,int>`.
2. **Deriving is impossible for the two cases the question usually means.** `class JSArray :
   System.Array` does not compile — CS0644, `Array` is a special class — and `System.String`
   is `sealed`. This is a C# language rule, not a design choice.
3. **Even where deriving compiles, it would be wrong**, because the JavaScript semantics
   differ from the .NET semantics at nearly every point where the two would have to agree —
   and C#'s single inheritance is already spent on `JSValue`, which is where those semantics
   live.

The rest of this document is the evidence.

---

## 1 · The premise: it is composition, not duplication

Read from the tree on 2026-08-07:

| JS type | What it actually holds |
|---|---|
| `JSNumber : JSPrimitive` | `internal readonly double value;` — **a .NET `double`** |
| `JSString : JSPrimitive` | a `string`/`StringSpan`, plus a rope node (`Left`, `Right`, `Length`) for concatenation |
| `JSMap : JSObject` | `Dictionary<JSValue,int> index` with a `SameValueZeroComparer` |
| `JSSet : JSObject` | the same |
| `JSArray : JSObject` | an `ElementArray` struct in `Broiler.JavaScript.Storage` |
| `JSRegExp : JSObject` | .NET's `System.Text.RegularExpressions` engine by default |

**Twenty-five files in `BuiltIns` use `System.Globalization`.** The engine leans on the BCL
wherever the BCL's behaviour *is* the specified behaviour. What it cannot do is let a BCL
type **be** a JavaScript value, because a JavaScript value has to answer a protocol no BCL
type implements.

That protocol is `JSValue` — an abstract class with **79 abstract or virtual members**,
implementing `IPropertyAccessor` and `IDynamicMetaObjectProvider`. Everything in the language
derives from it:

```
JSValue                          (abstract — the protocol)
├── JSNull, JSUndefined
├── JSPrimitive
│   ├── JSNumber (double), JSString (string), JSBoolean, JSBigInt, JSSymbol
└── JSObject                     (prototype + shape + own properties + elements)
    ├── JSArray, JSFunction, JSMap, JSSet, JSPromise, JSProxy, JSRegExp, JSError, …
    └── ClrProxy                 ← a real .NET object, wrapped
```

## 2 · Why deriving is impossible: four independent reasons

**Any one of these is sufficient.** All four hold at once.

### 2.1 · The C# compiler forbids it, for exactly the types the question names

| Attempt | Result |
|---|---|
| `class JSArray : System.Array` | **CS0644** — *cannot derive from special class `System.Array`* |
| `class JSString : System.String` | **CS0509** — `String` is `sealed` |
| `class JSFunction : System.Delegate` | **CS0644** — special class |
| `class JSArray : JSValue[]` | not a thing; array types are constructed by the runtime |

This is not a matter of taste or of the engine having chosen badly. **`System.Array` and
`System.Delegate` cannot be base classes in C# at all**, and `String` is sealed. The two most
obvious candidates for "just use the .NET type" are the two that are hardest blocked.

### 2.2 · Single inheritance is already spent — on the thing that matters

`JSObject` carries, **per instance**:

- the prototype (`currentPrototype`), mutable at run time via `Object.setPrototypeOf`;
- the object shape (`ObjectShape`) that phase 2's inline caches key on;
- the own-property radix trie (`PropertySequence`);
- the element storage (`ElementArray`);
- extensibility, sealed and frozen state;
- iterator flags, and the whole `[[Get]]`/`[[Set]]`/`[[DefineOwnProperty]]` protocol.

`JSArray : JSObject` inherits all of it. C# has single inheritance, so
`class JSArray : JSObject, List<JSValue>` **is not expressible**. The choice is not *"JSObject
or List<JSValue>"* at the margin — it is *"the JavaScript object model, or a .NET
collection"*, and an array that is not a JavaScript object is not a JavaScript array.

**And `List<T>` would not help even if you could.** Its indexer, `Count` and `Add` are
**non-virtual**, so a derived class cannot intercept `this[i]` to implement the semantics
below. You would inherit the storage and none of the behaviour.

### 2.3 · The semantics differ at nearly every contact point

This is the substantive reason, and `Array` is the clearest case. **Each row is a place where
`List<T>` or `T[]` gives the wrong answer:**

| ECMAScript requires | `List<T>` / `T[]` does |
|---|---|
| **Sparse.** `a[1_000_000_000] = 1` sets one property | allocates a billion slots, or throws |
| **`length` is writable and truncating.** `a.length = 0` empties it; `a.length = 5` extends with holes | `Count` is read-only and derived from contents |
| **An index is a *property key*.** `a[0]` and `a["0"]` are the same property; `a[-1]`, `a["01"]`, `a[1.5]` are ordinary **named** properties, not elements | an index is an `int`; `a["01"]` is a compile error |
| **Only canonical numeric strings below 2³²−1 are indices** — the array index test is a string-canonicalization rule | no such concept |
| **Out of range is `undefined`** | `IndexOutOfRangeException` / `ArgumentOutOfRangeException` |
| **Holes are observable and distinct from `undefined`.** `[,].hasOwnProperty(0) === false`, and `in`, `forEach`, `Object.keys` and `JSON.stringify` all distinguish them | no hole concept; a gap is `default(T)` |
| **Every element can carry a descriptor.** `Object.defineProperty(a, 0, {writable:false, enumerable:false})`, and getters/setters on an index | a slot is a slot |
| **Arrays carry named properties too.** `a.foo = 1` alongside `a[0]` | a `List<T>` has no property bag |
| **A miss consults the prototype chain**, which is mutable. `Object.setPrototypeOf(a, {0: 'x'})` changes what `a[0]` reads | no chain |
| **Extensible / sealed / frozen** are per-object states | none |
| **Symbol keys**, `Symbol.iterator`, `Symbol.species`, `Symbol.unscopables` | none |
| **Subclassable from script.** `class MyArray extends Array {}` must produce an object whose `[[Prototype]]` comes from `new.target` | .NET inheritance is fixed at compile time |
| **`Array.prototype.map.call(arrayLike)`** must work on anything with `length` and indices | typed inheritance cannot express structural array-likeness |

The same table exists for every other type. `JSString` cannot be `System.String` because JS
strings are UTF-16 **code-unit** indexed with observable lone surrogates, are objects when
boxed, carry a mutable prototype, and — after phase 2's item P2-4 — are **ropes**, so that
repeated concatenation is not quadratic. `JSFunction` cannot be a `Delegate` because a JS
function is an object with properties, a prototype, `length`, `name`, a `[[Construct]]`
behaviour, and Annex B `caller`/`arguments`.

### 2.4 · The engine's own performance machinery lives in `JSObject`

Phases 2 and 3 of the [performance roadmap](../roadmap/Roadmap.md) built object shapes,
inline caches, property maps and dense element storage **into `JSObject` and
`Broiler.JavaScript.Storage`**. A `List<JSValue>` cannot participate: it cannot expose a
shape for an inline cache to key on, cannot declare `SupportsShapeTracking`, cannot publish
`IndexedPrototypeVersion` when a prototype gains an indexed property.

Item P2-3's dense element storage — *a dense element is one reference instead of a 32-byte
descriptor* — is precisely the optimization that **recovers `T[]`-like density inside the JS
object model**, on the objects that qualify. Which is the next section.

## 3 · The proof is in `JSArray` itself

`JSArray.CanUseDenseElementFastPath()` is the engine stating, in code, exactly when a
JavaScript array may be treated like a .NET array:

```csharp
private bool CanUseDenseElementFastPath()
{
    if (GetType() != typeof(JSArray) || !IsExtensible() || IsSealedOrFrozen())
        return false;

    var currentVersion = JSObject.IndexedPrototypeVersion;
    if (indexedPrototypeVersion != currentVersion)
    {
        indexedPrototypeSafe = !HasIndexedPropertiesOnPrototypeChain();
        indexedPrototypeVersion = currentVersion;
    }

    ref var elements = ref GetElements(false);
    return indexedPrototypeSafe && elements.IsDense && elements.HasDefaultDescriptors;
}
```

**Six conditions**, and every one of them is a way JavaScript can differ from a .NET array:
not a subclass, still extensible, not sealed or frozen, no indexed property anywhere on the
prototype chain, no holes, no non-default descriptors.

**That is the answer in one method.** The .NET-array-shaped representation is the *fast path*,
guarded, and re-checked because script can leave the state at any moment — `Object.freeze(a)`,
`delete a[3]`, `Object.setPrototypeOf`, `Object.defineProperty` on an index. If `JSArray`
*were* a `List<JSValue>`, there would be nowhere for the slow path to go.

## 4 · The counterexample that confirms it

The engine does expose real .NET objects to JavaScript — that is
`Broiler.JavaScript.Clr`, and it works **by wrapping, in the same direction**:

```
ClrProxy : JSObject      — an arbitrary CLR instance, given a prototype and a property protocol
ClrType  : JSFunction    — a CLR type, given [[Construct]] and static members
```

**A .NET object entering JavaScript acquires a `JSObject`.** The reverse — a JavaScript
object *being* a .NET collection — is what has no expression, and `ClrProxy`'s existence is
the demonstration that the boundary is real and one-directional.

## 5 · What the alternative would actually cost

To make `System.Array` serve as a JavaScript array you would have to add, to a type you do
not own: a mutable prototype reference, a per-element descriptor table, a named-property bag,
symbol-keyed properties, hole tracking distinct from `null`, a writable truncating `length`,
extensibility and freeze state, and the string-canonicalization rule that decides whether
`"01"` is an index.

**That is the JavaScript object model.** You would have re-implemented `JSObject`, inside
`System.Array`, without being able to modify `System.Array`. The wrapper is not the expensive
option; it is the only one, and it is what every managed JavaScript engine does — Jint's
`JsValue`/`ArrayInstance`, ClearScript's marshalling layer, and Broiler.JS's `JSValue` are
the same shape for the same reason.

## 6 · Where the criticism *is* fair

Two parts of `BuiltIns` are genuinely re-implementations of things .NET has, and they are the
two largest directories in the assembly:

| | LOC | .NET has |
|---|---:|---|
| `Temporal/` | **12 597** | `DateTime`, `DateTimeOffset`, `TimeZoneInfo`, `System.Globalization.Calendar` |
| `Intl/` | **8 612** | `System.Globalization` — `CultureInfo`, `NumberFormatInfo`, ICU |

**They are 21 209 lines, a third of the assembly**, and `BuiltIns` reaches its own vendored
`Broiler.DateTime` and `UnicodeCldr.LocaleData` for them rather than the BCL. Three reasons
that is defensible, and one that is not:

- **Spec-exactness.** `Temporal` is a specification with its own arithmetic, calendars and
  ambiguity-resolution rules. `DateTime` is not a partial implementation of it; it is a
  different design, and test262 tests the difference.
- **Determinism.** `System.Globalization` is ICU-backed, so its behaviour is a function of
  the ICU version on the machine. A conformance suite that must give the same answer on
  Windows, Linux and macOS cannot be built on a moving dependency.
- **Native AOT.** `samples/Broiler.JavaScript.NativeAotSample` sets
  **`<InvariantGlobalization>true</InvariantGlobalization>`** — which is the normal thing to
  do when publishing AOT, because ICU is a large native dependency. **Under invariant
  globalization the BCL's culture support is gone**, so an `Intl` built on `CultureInfo`
  would not merely be slower on that configuration — it would not work.
- **And the part that is not defensible:** none of the above says *how much* of the 21 209
  lines is spec-driven and how much is re-invention. **That has never been measured**, and it
  should be, before anyone defends or attacks the number.

## 7 · What this means for the roadmap

- **The wrapper design is not on the table**, and no item proposes changing it. `JSValue` is
  the engine's value representation and phases 3 and 4 optimize *within* it — item 3-4's
  tagged value is a change to how a `JSValue` is represented, not a proposal to stop having
  one.
- **The size problem is real and is addressed by packaging, not by deletion.**
  [`Assemblies.md`](../roadmap/Assemblies.md)'s item A-2 splits `Temporal`, `Intl` and
  `RegExp` into satellite assemblies. That reduces what a host must ship; it does not reduce
  what the engine implements, and it should not be described as if it did.
- **The open question worth asking** is §6's last bullet — how much of `Temporal` and `Intl`
  is genuinely spec-driven. It is a scoping measurement, not an argument, and nobody has
  taken it.

## See also

- [Assembly boundaries and dependencies](dependencies.md) — where these types live and the
  layering rule
- [Contributing built-ins](contributing-builtins.md) — how to add one
- [`docs/roadmap/Assemblies.md`](../roadmap/Assemblies.md) — the `BuiltIns` split (item A-2)
- [`docs/roadmap/Roadmap.md`](../roadmap/Roadmap.md) — phases 2 and 3, which built the shape,
  cache and element machinery this document describes
