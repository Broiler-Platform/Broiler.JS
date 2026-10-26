# Negative controls

Every file here is malformed on purpose, in one way each, and **the validator must refuse it**. The
files are not part of the corpus: a reader of the corpus skips this directory, and Broiler.VM's
archive does not carry it. The validator's tests assert that each file is refused, and for the reason
its name gives.

| File | Defect |
|---|---|
| `missing-field.vectors` | A record without `script` |
| `unknown-key.vectors` | A record with a key the schema does not define |
| `duplicate-key.vectors` | A record with `out` twice |
| `duplicate-id.vectors` | Two records with one `id` |
| `bad-double.vectors` | A double written in lowercase hexadecimal |
| `bad-string.vectors` | A string written with an escape that is not the canonical one |
| `bad-class.vectors` | A class the schema does not define |
| `impl-defined-without-expectation.vectors` | An `impl-defined` vector with no `per-engine` field |
| `divergence-without-engine.vectors` | A `divergence` vector naming no engine |
| `future-revision.vectors` | A `since` beyond the corpus's revision |
| `no-schema.vectors` | No `schema: 1` line |
| `engine-observed.vectors` | A vector whose only provenance is `observed:broiler-js` |
| `no-source.vectors` | A vector whose only provenance is an `overruled:` entry, an answer the vector does not take *(added 2026-10-18, at corpus revision 24)* |
| `bad-provenance.vectors` | A `spec:ecma402@` entry that names ECMA-262's pinned revision, not sources/ecma402.pin's *(added 2026-10-26, at corpus revision 27)* |
