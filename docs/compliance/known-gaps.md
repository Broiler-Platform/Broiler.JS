# Known compliance gaps

This file groups current gaps without duplicating issue-specific investigation history.
Exact failing paths live in `scripts/compliance/test262-failures.txt`; update this summary
when that manifest changes materially.

## Active semantic clusters

The current failure manifest includes work in these areas:

- RegExp literal parsing, Annex B escape handling, and Unicode ignore-case behavior;
- Array `slice`, `unshift`, `toReversed`, `reduceRight`, and near-maximum length
  semantics, including Proxy-created results;
- comment and regular-expression literal lexical edge cases;
- labeled/unlabeled `continue` and block-scoped loop bindings;
- direct and indirect `eval`, including lexical environments and `new.target`;
- Annex B block-scoped function and catch/`var` behavior; and
- string/array legacy edge cases represented by the staging tests.

Older triage also identified `Intl.DateTimeFormat` range/parts behavior and
SameValue/Proxy ordering cases. Keep them here only while a current reproduction or
linked issue remains; do not rely on deleted issue snapshots as evidence.

## Host-coverage gaps

- `$262` host-harness helpers are incomplete.
- `module` tests need a module-host mode.
- `raw` tests need raw-harness semantics.
- Negative-metadata support exists but must be enabled and reported by release runs.

## Gap lifecycle

For every gap:

1. record an upstream path and pinned suite revision;
2. add a minimal test in the owning repository project;
3. implement the fix in the narrowest parser/compiler/runtime/built-in layer;
4. rerun the focused path and affected full shard;
5. update `test262-failures.txt` and `dashboard.md`.

The active execution order and exit gates are in
[the repository roadmap](../roadmap.md).
