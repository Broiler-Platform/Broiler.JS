# Bundle SJL-1-001's decision rule: every difference between a vector and Broiler.JS is classified

**Written:** 2026-10-12, at milestone SJL-0 (work package SJL-0.9) of the shared JavaScript libraries
programme, before the vector schema, the validator, the corpus or this engine's engine-level runner
exists. **Milestone judged:** SJL-1, `Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-1-shared-conformance-vectors.md`:
its home clauses 1 to 3, and the Broiler.JS half of clause 6. **Owner:** the Broiler.JS maintainer.
**Reviewer:** none. The owner, the JavaScript profile owner who judges Broiler.VM's runner, and every
other role this rule names are one person (the programme records this as Broiler.VM's exception
EX-30), and this rule does not claim that anyone independent has read it.

**What this file is.** The one predeclared decision of this repository's bundle `sjl-1-001`, in the
template of the programme's validation document, `Broiler.VM/docs/javascript-shared-libraries/validation.md`
section 4 (T3, read for classification). Broiler.VM commits its own rule at the same path in its own
repository, judging its own engine; neither rule reads the other's runs. This rule is the Broiler.JS
record [`SharedLibraries.md`](../../roadmap/SharedLibraries.md)'s SJL-1 item closes against. **It
holds no figure.**

```rule
evidence class: conformance (vector classification), not a measurement
population: every vector of the corpus at the corpus revision bundle sjl-1-001 names (a commit of this repository, the corpus in its home directory, conformance/vectors/ or the name SJL-1.2 gives it), whatever its area; the negative-control files the validator and the runner are given are not in the population
configuration: this engine's engine-level runner, a test project of this repository that reads the corpus from its home directory at the named revision and no other copy, built in Release; each vector's script form is evaluated in a fresh JSContext of the default composition (Broiler.JavaScript.All) with the feature flags the shell BroilerJS enables by default, named in the bundle's manifest; each result is rendered under the corpus's one rendering rule, the same rule Broiler.VM's runner uses
comparison: per vector, the rendered result against the vector's expected outcome: for a normative vector, its expected value; for an implementation-defined vector, the per-engine expectation it names for Broiler.JS; for a divergence vector, its expected value, the divergence being the classification of the difference
admitted difference classes:
  (d) the vector is classed divergence, names the engine broiler-js, and gives a reason linking a plan of this repository; it is listed by identifier
  (i) the vector is classed implementation-defined and this engine's result equals the per-engine expectation it names; this is a match, listed by identifier as implementation-defined
every other difference is unclassified, including a difference on a normative vector, an implementation-defined vector naming no expectation for Broiler.JS, a divergence vector naming the other engine only, and a runner refusal or harness fault on any vector
validator: the validator accepted the named revision, refused its malformed negative-control file, and refused a vector whose expected value has no source independent of both engines (provenance engine-observed only)
provenance: no vector in the revision takes its expected value from either engine; a vector this engine fails is never deleted or reclassified to make it pass, except by a dated corpus entry in a later revision
negative control: one vector's expected value is mutated in a copy of the corpus; the runner, given that copy, reports the vector as a difference and does not classify it; the copy is then removed, and the run on the named revision is retained
MET if and only if the validator conditions hold, every vector of the population has a result, every difference is in class (d) and is listed, every match in class (i) is listed, no vector's expected value is engine-observed only, and the negative control was reported unclassified.
NOT MET otherwise.
```

**How it is read.** The bundle's README reads the runner's retained output against the block and
states the decision. The runner computes nothing beyond the per-vector comparison: it prints each
vector's identifier, its class, the expected outcome and this engine's rendered result, so the
classification can be checked line by line. A run that could not read the corpus, or that stopped
before the last vector, is void and is not judged. Work package SJL-1.5's three outcomes map onto the
block: an **engine defect** becomes a divergence vector naming this engine (class (d)); a **vector
defect** is fixed in a new corpus revision and the run is repeated on it; an
**implementation-defined** difference moves into the vector and is then a class (i) match.

**Revisions.** A rule revised after any run it judges has been seen is a new dated file in this
directory, quoting this one whole and saying it was written afterwards. Its decision is then weaker
evidence.

## What it does not decide

- **Whether this engine is correct.** A divergence is a classified difference, not an excuse: it names
  the plan that answers it, and the cut-over that corrects it lists it in the changelog as a
  correction (decision SJL-D-16).
- **Anything about Broiler.VM.** Broiler.VM's runner is judged by its own rule in its own repository.
  The two engines are compared with the vectors, never with each other.
- **The library-level runner.** It has nothing to call until SJL-3, where the reference-arm rule and
  the vectors judge it.
- **Anything about test262.** Vector agreement meets no test262 clause.
- **Any performance.** The runner takes no timing, and none is read.
