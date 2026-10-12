# Human Review: Broiler.ECMAScript

GENERATED - DO NOT EDIT MANUALLY. Regenerate with
`dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance generate --root Broiler.JS/Broiler.ECMAScript`, which rewrites this file,
`CODE-ASSURANCE.md`, `assurance.manifest.json` and every generated source header from the
product tree.

> **Status: PENDING.** Human-reviewed: 0 of 111 relevant units. `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.JS/Broiler.ECMAScript --release`
> fails while any relevant unit is without a decision bound to its current fingerprint.

## 1. How To Use This File

Read it; do not edit it. A decision about a code unit is the `// Broiler-Human:` line on that
unit's declaration, and every table below is read out of those lines. There is nothing here
to fill in and nothing here to leave blank.

## 2. How A Review Is Recorded

In one place: the `// Broiler-Human:` line of the assurance annotation that sits on the
declaration being read. Nothing in this file is edited by hand, no second document carries a
per-item checklist, and no list of permitted aliases exists to be added to.

```csharp
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=2; Fingerprint=4A3BFD
// Broiler-Falsified-If: a negative value reaches the running total
// Broiler-Human:        PENDING
```

The last line has four shapes. A human writes three of them; the generator writes the fourth
and may never invent an alias, which the check asserts in both directions.

| Line | Meaning |
|---|---|
| `PENDING` | Nobody has recorded a decision for this unit. The generator leaves it exactly as it stands. |
| `<alias>` | A human states their own alias and leaves the machine field to the generator, which fills it with the declaration's fingerprint at the next run. |
| `<alias>; Fingerprint=<six hex>` | A decision bound to one exact version of one declaration. |
| `STALE; Previous=<alias>@<fingerprint>` | Written by the generator when the code moved after a decision. Only a human clears it, by stating their alias again. |

A human may state their own `IP=`, `Security=` and `Resources=` assessment beside their alias,
which is how a reader disagrees with the machine assessment on the line above: an assessment is
a comment and moves no fingerprint, so there is nowhere else to say it.

**No branch, commit or tag is recorded in this file.** Each decision names the fingerprint of
the declaration it was made against, and the state machine compares that value with the
declaration as it now stands. A commit says a tree moved; a fingerprint says whether this unit
did, which is the narrower and the more useful of the two.

## 3. Summary

| Metric | Value |
|---|---:|
| Files scanned | 12 |
| Code units | 157 |
| Relevant | 111 |
| Exempt | 46 |
| Assessed | 111 of 111 (100%) |
| Human reviewed | 0 of 111 (0%) |
| Unverified | 111 |
| Aliases naming a decision | 0 |

## 4. Review States

One row per state of the machine that reads the two lines. The states are computed from the
annotations and the current fingerprints; nothing stores them.

| State | Units |
|---|---:|
| NEW | 0 |
| AI_ASSESSED | 0 |
| HUMAN_PENDING | 111 |
| HUMAN_APPROVED_PENDING_FINGERPRINT | 0 |
| VERIFIED | 0 |
| STALE | 0 |
| EXEMPT | 46 |

## 5. Aliases In The Tree

No alias appears on a human line anywhere in the product tree. Nobody has recorded a
decision about any unit of this component.

## 6. Coverage By File

One row per covered file, carrying that file's generated header. `Unverified` counts the
relevant units in a state that blocks a release.

| File | Units | Relevant | Exempt | Unverified | IP risk | Security risk | Criteria |
|---|---:|---:|---:|---:|---|---|---:|
| `Broiler.ECMAScript.Algorithms/Failure.cs` | 6 | 3 | 3 | 3 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Limits.cs` | 8 | 6 | 2 | 6 | Low | High | 2/2 |
| `Broiler.ECMAScript.Algorithms/ResultStatus.cs` | 4 | 1 | 3 | 1 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/WorkCharge.cs` | 1 | 1 | 0 | 1 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Conformance/Ascii.cs` | 15 | 15 | 0 | 15 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/LibraryRun.cs` | 10 | 8 | 2 | 8 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/Outcome.cs` | 11 | 7 | 4 | 7 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/TypedValue.cs` | 24 | 11 | 13 | 11 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/VectorCorpus.cs` | 16 | 11 | 5 | 11 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/VectorFile.cs` | 23 | 18 | 5 | 18 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/VectorRun.cs` | 17 | 12 | 5 | 12 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/VectorValidator.cs` | 22 | 18 | 4 | 18 | Low | Low | 0/0 |

## 7. Decisions Recorded

No unit in this component carries a decision on its human line. Every one of them reads
`PENDING`.

## 8. Decisions The Code Has Outrun

No unit carries a decision that the code has since moved past.

## 9. Where A Decision Is Required First

The units at the top of the security vocabulary, with the observation that would show each
one wrong and the human line it carries. The set is read from the assessments rather than
written out, so a unit that becomes `High` joins it at the next generation.

- `Broiler.ECMAScript.LengthLimit.Admits(long)` in `Broiler.ECMAScript.Algorithms/Limits.cs` - Security=High, Spec=SJL architecture s4, `CD14A4`, PENDING
  - Falsified if: Admits returns true for a length greater than Maximum, so an operation that consults it produces a result longer than its caller allowed
- `Broiler.ECMAScript.DepthLimit.Admits(int)` in `Broiler.ECMAScript.Algorithms/Limits.cs` - Security=High, Spec=SJL architecture s4, `F3198B`, PENDING
  - Falsified if: Admits returns true for a depth greater than Maximum, so an operation that consults it nests deeper than its caller allowed and can exhaust the stack

## 10. What This Record Does Not Say

It is not an approval of the component, and a full table above would not be one either. It
records which declarations somebody stated a decision about, and against which version of
each. It does not record what they read, how long they spent, or whether they were right.

A fingerprint is six hex characters of SHA-256 over a declaration's token texts. It answers
whether a unit changed since a decision was recorded against it. It is not a collision-free
identifier across units and it is not a cryptographic commitment, so it detects a change and
does not resist a forger with commit access.

An assessment is a comment, so changing one moves no fingerprint anywhere, and nothing
mechanical checks that it is right; the check holds its values to their vocabularies and no
further.

111 of the 111 assessed units declare `Origin=AI`. Reading a declaration is the only thing
that makes it read.
