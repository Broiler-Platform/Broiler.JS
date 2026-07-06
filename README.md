# Broiler.JS

**Broiler.JS** is a modular JavaScript engine for .NET, part of the **Broiler** project
(**B**rowser **I**nfrastructure in **I**ntermediate **L**anguage with **E**nhanced **R**eliability).

Broiler.JS compiles JavaScript to .NET IL at runtime, providing an embeddable ECMAScript
engine with strong interop to C# code. **It is not a security sandbox**: JavaScript host
capabilities and CLR integration must be restricted by the embedding application.

> **First-preview status:** The public API and behavior are unstable, breaking changes
> may occur without notice, and untrusted scripts must not be executed on the assumption
> that the engine provides isolation. This component is **not human-approved for preview
> use** while [HUMAN_REVIEW.md](HUMAN_REVIEW.md) remains `PENDING`.

---

## Origins

Broiler.JS began as a fork of [**Yantra JS**](https://github.com/yantrajs/yantra)
(Apache 2.0), an open-source JavaScript engine for .NET. The original Yantra JS architecture,
parser, and runtime design provided the foundation; Broiler.JS has since diverged
substantially to pursue the goal of near-complete ECMAScript conformance.

Broiler.JS is maintained independently. It is not an official Yantra JS version,
continuation, or release, and the Yantra JS authors have not reviewed or endorsed it.
The upstream name is used only for accurate provenance. See
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

---

## How this project is built

The implementation work in Broiler.JS is done primarily by **Claude Opus** (Anthropic's
coding AI). The project reached a point where that was viable only after a substantial
manual refactor — restructuring the inherited Yantra JS codebase into the modular,
IL-centric architecture that a coding AI can navigate and extend reliably.

From that point on, the split is:

- **Coding AI (Claude Opus)** — new features, bug fixes, spec-compliance work, and
  code-level implementation.
- **Maintainer (manual)** — directing the work, reviewing changes, running tests, driving
  refactors, managing AI costs, and keeping the overall project on track.

This means the bottleneck on progress is not coding hours but maintenance bandwidth
and the cost of AI inference. If you want to see faster progress, the most direct way
to help is a donation (see below).

---

## Human review

AI-generated or AI-modified implementation must receive source-level review from a real
developer before a release is described as reviewed. The per-release record must identify
the exact commit, evidence, findings, scope, and human decision in
[HUMAN_REVIEW.md](HUMAN_REVIEW.md). Test262 results are valuable conformance evidence,
but they do not by themselves establish security or safety.

---

## Compliance

Broiler.JS is validated against the **[test262](https://github.com/tc39/test262)**
conformance suite — the official ECMAScript test suite maintained by TC39. The engine
aims to pass nearly the full suite, and conformance is tracked continuously in CI.

| Resource | Description |
|---|---|
| [Compliance dashboard](docs/compliance/dashboard.md) | Current pass/fail status by feature area |
| [Known gaps](docs/compliance/known-gaps.md) | Features not yet implemented |
| [Roadmap to 100%](docs/compliance/roadmap-to-100-percent.md) | Planned work to close remaining gaps |
| [Compliance process](docs/compliance/process.md) | How test262 runs are managed in CI |

---

## Documentation

| Resource | Description |
|---|---|
| [Public API reference](docs/public-api.md) | Supported packages, entry points, module boundaries |
| [Architecture overview](docs/architecture/overview.md) | Engine layers and satellite assemblies |
| [Performance and IL roadmap](docs/architecture/performance-and-il-roadmap.md) | Runtime optimization plan and Roslyn/.NET IL platform direction |
| [Contributing built-ins](docs/architecture/contributing-builtins.md) | How to implement new built-in objects |
| [LogParser usage](logs/README.md) | Shard log summarizer and JSON export |
| [Contributing](CONTRIBUTING.md) | CI pipeline, test262 workflow, running tests locally |

---

## Building and testing

Restore and run the .NET test projects:

```bash
dotnet test Broiler.JS.slnx
```

Run test262 for a single assembly (e.g. the parser):

```bash
python scripts/compliance/list_test262_assemblies.py \
  --manifest scripts/compliance/test262-assemblies.json \
  --paths-for parser --output /tmp/parser-paths.txt

python scripts/compliance/run_test262.py \
  --suite-root <path-to-test262> \
  --broiler-dll <path-to-BroilerJS.dll> \
  --path-file /tmp/parser-paths.txt \
  --shard-count 1 --shard-index 0
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for the full incremental CI workflow.

---

## Support the project

Broiler.JS is free and open-source software. As described above, the coding is handled
by Claude Opus, but the surrounding work — testing, directing, refactoring, and AI
inference costs — falls on the maintainer personally.

A donation is genuinely appreciated. It directly offsets AI API costs and frees up
the time needed to keep test262 passing and the project moving forward.

**Sponsor or donate:** [github.com/sponsors/MaiRat](https://github.com/sponsors/MaiRat)

---

## License

Broiler.JS is licensed under the [Apache License 2.0](LICENSE).

This project is derived in part from [Yantra JS](https://github.com/yantrajs/yantra),
which is also licensed under the Apache License 2.0. Upstream attribution and independence
are documented in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). The license provides
the software on an “AS IS” basis, without warranties or conditions.
