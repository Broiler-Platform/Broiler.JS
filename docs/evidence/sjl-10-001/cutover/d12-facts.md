# Decision D12 - facts for the records (SJL-10.5, 2026-10-19)

**Decision D12** (the owner's, 2026-10-19, under the direction of 2026-10-18 "do all recommended decisions"):
decision D11's rule reaches `NudgeToZonedTime`'s assertion on the day span's sign; where an input breaks it, the answer
is a `RangeError` (the library answers `TemporalStatus.OutOfRange`).

## The step

`tc39/proposal-temporal@e8cc03fc`, spec/duration.html, `NudgeToZonedTime` (s7.5.35, `#sec-temporal-nudgetozonedtime`),
**step 8: "Assert: TimeDurationSign(_daySpan_) = _sign_."** Steps 1 to 7: _start_ = CalendarDateAdd(origin date,
duration's date part, constrain); _startDateTime_ and _endDateTime_ = _start_ and AddDaysToISODate(_start_, _sign_) at the
origin's time; both through GetEpochNanosecondsFor(..., compatible); _daySpan_ = endEpochNs - startEpochNs. (Copies:
sjl101-syntax/spec-e8cc03fc/duration.html and sjl102v/spec-temporal/duration.html, identical.) The polyfill at the same
revision tests the same thing as code: lib/ecmascript.mjs line 3388, `if (daySpan.sign() !== sign) throw new
RangeErrorCtor('time zone returned inconsistent Instants')`.

## The input

a = `2011-12-31T16:15+14:00[Pacific/Apia]`, b = `2011-12-31T00:00+14:00[Pacific/Apia]`. Pacific/Apia skipped
2011-12-30. Back from a (sign -1), the nudge's start is 2011-12-31 and its end 2011-12-30T16:15, which `compatible` moves
24 hours on, onto the start: the day span is zero, not -1's sign. Reached by `since`/`until` from a to b and by
`Duration.round` relative to a whenever the smallest unit is a time unit (other than a nanosecond with increment 1, which
returns before rounding) and the largest unit a date unit. Forward from b the span is the next day, 24 hours: unaffected.

## The engines (probe sjl105-final/probes/d12z.js; every run TZ=UTC)

- (a) base Broiler.JS shell, main `1c411b68`: sjl105-base/shell/BroilerJS.dll (sha256 6071d324f2b23f60...), out
  probes/d12z.base.txt.
- (b) this build: branch `sjl-10-5-js` at `163d43aa` (the library code of `6dbff805`), Broiler.JavaScript Release, copied to sjl105-final/shell-after
  (BroilerJS.dll sha256 d65b78b15f038d53..., Broiler.ECMAScript.Algorithms.dll e0706569d11ec885...), out
  probes/d12z.after.txt.
- (c) the reference polyfill at `e8cc03fc` (sjl101-core/polyfill, lib/shim.mjs under Node v22.22.0, as
  sjl105-review/unlisted/run-ref.sh runs it), out probes/d12z.ref.txt (its two banner lines are not answers).
- (d) Broiler.VM before its cut-over, `14072b0` (/home/user/vm-at-14072b0, its existing Release build of
  Broiler.VM.Composition.JavaScript.Conformance, run as a raw test262 file as the correspondence ran it): the D11 line
  ends the whole invocation (ProfileFault/ProfileContractViolation, no payload: probes/d12z.vm-before.txt.raw), so the
  D12 lines were run without it, probes/d12z-nod11.js -> probes/d12z-nod11.vm-before.txt.
- (d) Broiler.VM now: `83fef1a` (SJL-10.4) with the published Broiler.ECMAScript.Algorithms 0.1.0-preview.6
  (`0.1.0-preview.6+fc829f8a...`, the build in repin6/vectors-bin from the re-pin's verification; Broiler.VM main
  `e42cd5d` differs from it only by the feed pin `8b6cc96`), out probes/d12z.vm-now.txt.

The runner is sjl105-final/run-probe.sh.

## Summary

| | the 36 D12 lines (7 operations x 5 modes, and `day/nanosecond x2`) | the 9 other lines |
|---|---|---|
| (a) base Broiler.JS | `RangeError` "Temporal.Duration: time zone returned inconsistent Instants" (its own zoned code) | as below |
| (b) this build | `RangeError` "Temporal.ZonedDateTime: / Temporal.Duration.prototype.round: result is outside the representable range" | equal to (a) |
| (c) polyfill | `RangeError` "time zone returned inconsistent Instants" | equal to (a) |
| (d) Broiler.VM `14072b0` | a duration from outside the text's domain: `P1DT16H`, `-P1DT17H`, `P1DT16H30M`, ... (its NudgeToZonedTime has no sign test) | equal to (a) |
| (d) Broiler.VM now (`0.1.0-preview.6`) | the same durations as `14072b0`, line for line (the library before D12 = Broiler.VM's code) | equal to (a) |

- With decision D12, this build answers as the base and the polyfill do (a `RangeError`; only the message differs from
  the base's). Without it, the cut-over would have moved these 36 lines from a `RangeError` to a duration; part C's
  shell at `41eaa90f` (sjl105-c/shell-final) answered the durations (sjl105-c/probes/d12-ext.final.txt).
- part C's probe d12-ext.js on this build: 30 of its 106 lines change from part C's shell (exactly the D12 lines:
  `round ... largest day/month smallest hour/minute x30` and `a.since/until(b) day/hour`, `day/minute x30`), every one to a
  `RangeError`, the base's answer kind; the other 76, the D11 lines among them, are unchanged. d12.js: unchanged.
- Broiler.VM keeps the durations until it takes a version of the library with this decision.
- No test262 path, retained dataset line or corpus vector reaches the step: the library-level runner still answers
  10,851 of revision 25's 10,878 Temporal vectors, every one matching, and its probe report is byte for byte the one
  recorded at SJL-10.3 (sha256 3723061191...).
- D11's line, for context: base `PT0S`; this build `RangeError`; polyfill a plain `Error` "assertion failure: day was 0
  days long"; Broiler.VM `14072b0` ends the invocation; Broiler.VM now `RangeError` (decision D11).

## Every line
| Probe line | (a) base Broiler.JS | (b) this build | (c) polyfill `e8cc03fc` | (d) Broiler.VM `14072b0` | (d) Broiler.VM now (`0.1.0-preview.6`) |
|---|---|---|---|---|---|
| `D11 a.since(b) day/day halfExpand` | PT0S | RangeError: result is outside the representable range | Error: assertion failure: day was 0 days long | ProfileFault: the invocation ends (no payload), see below | RangeError: Temporal: the date-time is outside the representable range |
| `a.since(b) day/hour trunc` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | P1DT16H | P1DT16H |
| `a.until(b) day/hour trunc` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT16H | -P1DT16H |
| `a.since(b) month/hour trunc` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | P1DT16H | P1DT16H |
| `a.since(b) day/minute x30 trunc` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | P1DT16H | P1DT16H |
| `-PT16H15M round largest day smallest hour trunc` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT16H | -P1DT16H |
| `-PT16H15M round largest month smallest hour trunc` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT16H | -P1DT16H |
| `-PT16H15M round largest day smallest minute x30 trunc` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT16H | -P1DT16H |
| `b.until(a) day/hour trunc` | PT16H | PT16H | PT16H | PT16H | PT16H |
| `a.since(b) day/hour floor` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | P1DT16H | P1DT16H |
| `a.until(b) day/hour floor` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT17H | -P1DT17H |
| `a.since(b) month/hour floor` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | P1DT16H | P1DT16H |
| `a.since(b) day/minute x30 floor` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | P1DT16H | P1DT16H |
| `-PT16H15M round largest day smallest hour floor` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT17H | -P1DT17H |
| `-PT16H15M round largest month smallest hour floor` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT17H | -P1DT17H |
| `-PT16H15M round largest day smallest minute x30 floor` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT16H30M | -P1DT16H30M |
| `b.until(a) day/hour floor` | PT16H | PT16H | PT16H | PT16H | PT16H |
| `a.since(b) day/hour ceil` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | P1DT17H | P1DT17H |
| `a.until(b) day/hour ceil` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT16H | -P1DT16H |
| `a.since(b) month/hour ceil` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | P1DT17H | P1DT17H |
| `a.since(b) day/minute x30 ceil` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | P1DT16H30M | P1DT16H30M |
| `-PT16H15M round largest day smallest hour ceil` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT16H | -P1DT16H |
| `-PT16H15M round largest month smallest hour ceil` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT16H | -P1DT16H |
| `-PT16H15M round largest day smallest minute x30 ceil` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT16H | -P1DT16H |
| `b.until(a) day/hour ceil` | PT17H | PT17H | PT17H | PT17H | PT17H |
| `a.since(b) day/hour expand` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | P1DT17H | P1DT17H |
| `a.until(b) day/hour expand` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT17H | -P1DT17H |
| `a.since(b) month/hour expand` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | P1DT17H | P1DT17H |
| `a.since(b) day/minute x30 expand` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | P1DT16H30M | P1DT16H30M |
| `-PT16H15M round largest day smallest hour expand` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT17H | -P1DT17H |
| `-PT16H15M round largest month smallest hour expand` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT17H | -P1DT17H |
| `-PT16H15M round largest day smallest minute x30 expand` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT16H30M | -P1DT16H30M |
| `b.until(a) day/hour expand` | PT17H | PT17H | PT17H | PT17H | PT17H |
| `a.since(b) day/hour halfExpand` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | P1DT16H | P1DT16H |
| `a.until(b) day/hour halfExpand` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT16H | -P1DT16H |
| `a.since(b) month/hour halfExpand` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | P1DT16H | P1DT16H |
| `a.since(b) day/minute x30 halfExpand` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | P1DT16H30M | P1DT16H30M |
| `-PT16H15M round largest day smallest hour halfExpand` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT16H | -P1DT16H |
| `-PT16H15M round largest month smallest hour halfExpand` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT16H | -P1DT16H |
| `-PT16H15M round largest day smallest minute x30 halfExpand` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | -P1DT16H30M | -P1DT16H30M |
| `b.until(a) day/hour halfExpand` | PT16H | PT16H | PT16H | PT16H | PT16H |
| `a.since(b) day/nanosecond x2` | RangeError: time zone returned inconsistent Instants | RangeError: result is outside the representable range | RangeError: time zone returned inconsistent Instants | P1DT16H15M | P1DT16H15M |
| `a.since(b) day (no smallest)` | PT16H15M | PT16H15M | PT16H15M | PT16H15M | PT16H15M |
| `a.since(b) hour` | PT16H15M | PT16H15M | PT16H15M | PT16H15M | PT16H15M |
| `-PT16H15M round largest day (no smallest)` | -PT16H15M | -PT16H15M | -PT16H15M | -PT16H15M | -PT16H15M |
| `-PT16H15M total hour` | -16.25 | -16.25 | -16.25 | -16.25 | -16.25 |
