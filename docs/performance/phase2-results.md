# Phase 2 collections, keys, and element layout

Phase 2 implements the seven delivery items in the performance and IL roadmap. The
generic ECMAScript paths remain available for Proxy, exotic, custom-descriptor, and
prototype-sensitive cases.

## Implementation matrix

| Delivery item | Implementation |
| --- | --- |
| SameValueZero Map/Set storage | `Dictionary<JSValue,int>` uses a dedicated comparer: zero signs and NaNs normalize, strings and BigInts compare by value, and objects/Symbols use reference identity. Ordered vectors retain tombstones for live iteration and delete/re-add order. |
| Direct WeakMap/WeakSet ephemerons | WeakMap is a direct `ConditionalWeakTable<JSValue,WeakMapValueBox>`; WeakSet uses the same table with a shared sentinel. String indexes, finalizable wrappers, weak-reference side indexes, and explicit locks were removed. |
| Immutable key metadata | Intern misses publish private-name, array-index, canonical-numeric-index, and stable ordinal-hash metadata. Intern hits use `ConcurrentDictionary`; ID metadata/name reads use a lock-free `Volatile.Read` snapshot. |
| Property order and introspection | `PropertySequence` nodes have previous/next handles, making deletion O(1). Delete/re-add appends at the tail. Ordinary exact objects use internal struct enumeration and enumerable flags; Proxy/exotic objects retain virtual key and descriptor traps. |
| Measured sparse storage | The benchmark matrix covers 0/1/4/16/100/10k entries for radix, hash, inline, segmented, and ordered candidates. The measured production choice is contiguous dense storage plus `Dictionary<uint,JSProperty>` and a `SortedSet<uint>` live-key index in sparse mode. |
| Packed/holey/dictionary arrays | `ElementArray` now has explicit element kinds. Gaps/deletes create holey storage; sparse indices and custom descriptors select dictionary mode. Dense-to-sparse conversion uses index and density thresholds and does not automatically convert back. |
| Guarded dense built-ins | `copyWithin`, `fill`, and `reverse` use array bulk operations only for ordinary dense arrays with default descriptors, extensibility, and a clean indexed prototype chain. A global indexed-prototype version invalidates cached assumptions. |

## Local storage measurement

The `--sparse-metrics` developer measurement on Windows x64/.NET 10 records
construction allocation and time after warm-up. It is directional evidence, not the
two-run release baseline.

| Entries | Radix bytes | Dictionary bytes | Sorted dictionary bytes |
| ---: | ---: | ---: | ---: |
| 1 | 344 | 192 | 160 |
| 4 | 344 | 272 | 304 |
| 16 | 848 | 472 | 880 |
| 100 | 11,184 | 2,272 | 4,912 |
| 10,000 | 1,312,952 | 202,192 | 480,112 |

At 10,000 entries the hash table used about 15% of the radix allocation and built
about 15x faster in the local sample. A sorted live-key structure is retained beside
the hash table only for dictionary elements, where ECMAScript requires ascending
numeric enumeration. Dense element storage measured about 105 bytes per live entry at
10,000 entries, sparse ordered element storage about 215 bytes, and named property
storage about 368 bytes per entry. These
figures include backing-array growth and are recorded so later shape/slot work can be
compared against the same harness.

## Verification

- Phase 0/1/2 architecture and configuration checks: 15/15 passed.
- Storage metadata/order/element-kind tests: 11/11 passed.
- Phase 2 SameValueZero, WeakMap-cycle GC, property-order, hole, and indexed-prototype tests: 7/7 passed.
- Compiler tests: 237/237 passed, including sparse, string, and Symbol key-snapshot
  mutation cases for object spread/rest.
- Benchmark project Release build: zero warnings and zero errors.
- The full built-ins project completed 1,899/1,900 tests. The sole failure is the
  independently reproducible CLDR alias expectation
  `ru-Armn-AM` versus current data result `ru-Armn-RU`; the focused Phase 2 matrix is clean.
- A broad solution run also exposed eight environment/data/path-sensitive integration
  failures in this checkout (timezone/locale-data expectations and fixed-path fixtures).
  One additional async-resource test failed in that combined run but passed immediately
  in isolation. None intersects the focused Phase 2 suites; they remain separate release
  baseline work rather than being hidden by the Phase 2 result.

The release exit gate still requires the controlled two-run packed/sparse benchmark
baseline and the pinned test262 Map/Set/WeakMap/WeakSet/Array/Proxy/species subsets.
The repository workflow now runs the architecture checks and focused Phase 2 tests;
full external compliance remains separately gated by the compliance process.
