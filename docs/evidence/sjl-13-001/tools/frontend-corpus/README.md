# Part A's corpus runner (bundle SJL-13-001)

The home rule's part A ([`../../decision-rule.md`](../../decision-rule.md)) names "the library's corpus runner
(Broiler.ECMAScript.Frontend.Tests' corpus mode, or docs/evidence/sjl-13-001/tools/frontend-corpus)". This is the second.
It was written at stage SJL-13.2f, after the rule and before any run of part A, and it is committed with part A's
comparison, [`../compare_corpus.py`](../compare_corpus.py), which reads its report.

**What it runs.** Every input of `conformance/parse/corpus.manifest`, under its goal and each of its variants, in
configurations V and J, and every eval-family input as eval code under E0 to E7 in configuration V, in the
classification's order, on one thread of `ParseLimits.StackBytes`, through `Broiler.ECMAScript.Frontend` as this
checkout builds it. The requests are the Native AOT probe's (`Broiler.ECMAScript.Probe/ParseCorpus.cs`, compiled in from
its file): V with no proposal, no lexer extension and Broiler.VM's default nesting, 64; J with Broiler.JS's admitted
proposals (decorators, auto-accessors and the import phases' call forms), the `m` extension and the library's supported
maximum nesting, 512; a `strict` variant with the manifest's prologue, a `module` variant under the Module goal and every
other variant, `json` among them, under the Script goal. The Unicode provider is the tables the owner's generator writes
from `data/unicode/pins/`. The regular-expression provider answers from the classification (decision D26): for an input,
variant and configuration the classification expects refused with a row among those it names first whose stage-table
basis is `provider`, it refuses the first literal the library asks about with the first such row's message identifier,
and it admits every other literal.

*(Noted 2026-11-02, at SJL-13.2f, under decision (a) of the rule's revision of that day,
[`../../decision-rule-revised-2026-11-02.md`](../../decision-rule-revised-2026-11-02.md), before any run it judges:)* with
`--provider-readings docs/evidence/sjl-13-001/readings/provider-readings.tsv`, the provider also refuses the first literal
the library asks about for every input, variant and configuration that list names, with the message identifier of the
row it names (EE0019's, `regexp.literal.invalid`), and the report's header names the list's SHA-256
(`#% provider-readings`) and its number of lines.

**What it writes.** One line per parse: `id`, `variant`, `config`, the outcome (`accepted`, `refused`, `crashed`, or
`absent` when the input's text is missing or differs from the manifest's SHA-256), and for a refusal the first
diagnostic's shared code, phase and span (`offset+length@line:column`), and for a refusal at either limit the depth the
library reports, its diagnostic's argument: the bound it refused past. Its header names the manifest's, the
classification's, the catalogue's and the stage table's SHA-256 and the commit given. It holds no expectation.

    dotnet build docs/evidence/sjl-13-001/tools/frontend-corpus -c Release
    TZ=UTC dotnet docs/evidence/sjl-13-001/tools/frontend-corpus/bin/Release/net10.0/frontend-corpus.dll \
      --corpus conformance/parse --test262 <test262 at ccaac100> \
      --classification docs/evidence/sjl-13-001/classification/classification.tsv.gz \
      --catalogue Broiler.ECMAScript/docs/frontend/early-errors.tsv \
      --stages Broiler.ECMAScript/docs/frontend/stages.tsv --out report.tsv --commit <commit>
