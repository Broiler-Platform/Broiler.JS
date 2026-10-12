# Adapter guide: `<Library>` — `<area>`

> **This is the template.** Copy it to `Broiler.ECMAScript/<Library>/docs/adapter-guide-<area>.md`
> when an area milestone moves its first operation, and fill in one section per operation. An
> engine's adapter is written against this guide, not against the library's source: if an adapter
> needs something this guide does not say, the guide is incomplete and is corrected first.
>
> The obligation is the programme's: [`governance.md` section 10](../../../Broiler.VM/docs/javascript-shared-libraries/governance.md#10-documentation-obligations-per-library)
> and [`architecture.md` section 5](../../../Broiler.VM/docs/javascript-shared-libraries/architecture.md#5-the-adapter-pattern).
> The conventions every operation follows are in [`conventions.md`](conventions.md).

- **Library and version:** `<PackageId>` `<version>`, API baseline `<file>`.
- **Specification pins:** ECMA-262 `<edition and commit>`; others `<ECMA-402, Temporal, Unicode, RFC>`.
- **Data versions:** `<none | the public constants and their values>`.
- **Consumers:** Broiler.JS `<adapter type and file>`; Broiler.VM `<adapter type and file>`.
- **Vectors:** `conformance/vectors/<area>/`, corpus revision `<n>` or later.

## `<Operation>` — `<clause and title, for example ECMA-262 §21.1.3.3 Number.prototype.toFixed>`

### Signature

```csharp
// The public member exactly as the API baseline lists it.
```

### What the caller must have done first

The steps of the specification the library does **not** perform (convention 2), in the order the
specification performs them. Each is the adapter's.

| Step | Clause | What the adapter does |
|---|---|---|
| 1 | `<§ x.y.z step n>` | `<for example: ToIntegerOrInfinity(fractionDigits); an abrupt completion propagates before the library is called>` |

### Preconditions

What the library assumes and throws on when it does not hold (convention 3). An adapter that
establishes every one of these never sees a library exception.

- `<for example: fractionDigits is in 0..100; the adapter has already thrown the RangeError>`

### Work and limits

- **Meter:** charged `<units>` per `<block>`, before the work (convention 4). `null` charges nothing.
- **Limits:** `<LengthLimit | DepthLimit | other>`, and what each bounds (convention 5). The
  library's own constant ceilings: `<value and why it is not lower than either engine needs>`.

### What the caller does with each status

| Status | Meaning | The specification's error | Broiler.JS | Broiler.VM |
|---|---|---|---|---|
| `Completed` | `<the result is valid>` | — | `<what the adapter returns>` | `<what the adapter returns>` |
| `Malformed` | `<which inputs>` | `<SyntaxError, RangeError, or none: for example parseFloat returns NaN>` | `<rendering of the message identifier>` | `<rendering>` |
| `LimitExceeded` | `<which limit>` | `<RangeError, or the engine's resource abort>` | `<…>` | `<…>` |

### Message identifiers

| Identifier | When | Arguments |
|---|---|---|
| `<area.operation-defect>` | `<…>` | `<…>` |

### Engine-specific behaviour

Behaviour one engine needs and the other does not is a parameter of the operation, never a fork
(governance section 6, step 7). List each parameter, the value each engine passes, and why.

| Parameter | Broiler.JS passes | Broiler.VM passes | Why they differ |
|---|---|---|---|

---

*Revisions of a filled-in guide are dated in place, quoting the text they replace.*
