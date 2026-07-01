# Human review: Broiler.JS

> **Status: PENDING HUMAN REVIEW - first preview may be used only with the safety warning below.**

Broiler.JS contains substantial AI-assisted implementation and is still pending final
human review. This document records the current first-preview position: the component is
available for preview testing, but this availability is **not** a full safety approval and
must not be read as a guarantee that the code is free of defects, vulnerabilities, or
security-sensitive behavior.

During development, the implementation was cross-checked repeatedly. However, a large
number of changes were made in a short period of time, so it cannot be guaranteed with
100% certainty that no issue remains. No suspected malicious, intentionally dangerous, or
obviously unsafe code is currently known to be present, but residual risk remains.

## Safety warning

Broiler.JS can parse, compile, and execute JavaScript code and can interact with .NET host
capabilities. This makes it inherently security-sensitive. It is **not a security
sandbox**, and it must not be used to execute untrusted JavaScript unless the embedding
application provides appropriate isolation and restrictions.

For the first preview, execution in a disposable virtual machine or an equivalent isolated
environment is strongly recommended. This is especially important when testing third-party,
unknown, or generated scripts, or when enabling host integration, module loading, file
access, networking, storage, debugging, or CLR interop features.

## Review target

- **Component:** Broiler.JS
- **Scope:** The JavaScript parser, compiler, runtime, built-ins, CLR integration,
  modules, debugging, storage, networking, and integration assemblies.
- **Release:** First preview
- **Commit:** `f1d12d93fe6bc33746ac5ecfb8a0324356b112c1`
- **Reviewer:** Maik Ratzmer (`MaiRat`)
- **Reviewer contact or profile:** `MaiRat`
- **Review date:** 2026-07-01
- **Intended preview use:** Early preview testing and evaluation by developers who
  understand that Broiler.JS is review-pending, unstable, and not a sandbox.

Any source change after the reviewed commit invalidates this record until the changed
revision is reviewed again.

## Review status

The first preview is released with conditions while final human review remains pending.
This means:

- Automated and manual checks performed during development are considered sufficient for
  an initial preview release.
- The component is not yet approved as fully human-reviewed.
- Security-sensitive behavior may still exist because Broiler.JS directly executes code
  and exposes host integration surfaces.
- Users must treat the preview as experimental software and run it in an isolated
  environment when possible.

## Required evidence for final human approval

Before this file may be changed from pending status to a final human approval, the human
reviewer should record links, logs, or concise findings for every item:

- [ ] Build and automated tests completed; minimum expected commands:
      `dotnet test Broiler.JS.slnx` plus the documented test262 workflow.
- [ ] Security-sensitive inputs, trust boundaries, file/network access, native interop,
      and code-execution paths were inspected where applicable.
- [ ] Dependency and license notices were checked, including inherited upstream code.
- [ ] AI-generated or AI-modified code received source-level review; no AI summary was
      accepted as a substitute for reading the relevant code.
- [ ] Public APIs, failure behavior, known limitations, and preview compatibility risks
      were assessed.
- [ ] Static analysis, dependency/vulnerability scanning, or an explicit reason for
      omitting each was recorded.
- [ ] Open findings and residual risks are listed below.

### Evidence and commands

Development-time checks and repeated cross-checks were performed, but final review
evidence has not yet been completed in this document.

### Findings and residual risks

- Final human review is pending.
- The first preview was produced after many rapid changes, so undiscovered issues may
  remain.
- Code execution paths are security-sensitive by design.
- Host integration, CLR interop, module loading, file/network access, storage, and
  debugging features require careful embedding restrictions.
- Broiler.JS is not a sandbox and must not be treated as one.
- Use of a disposable VM or equivalent isolation is strongly recommended for preview
  testing.

## Preview decision

- [x] **AVAILABLE FOR FIRST PREVIEW WITH SAFETY WARNING** within the intended-use scope
      above.
- [ ] **FULLY HUMAN-REVIEWED AND APPROVED** for preview use.
- [ ] **NOT APPROVED** for preview use.

**Conditions:** Use only as experimental first-preview software. Prefer a disposable VM
or equivalent isolation. Do not execute untrusted scripts unless the embedding application
provides appropriate sandboxing, capability restrictions, and operational controls.

## Human attestation

Final human attestation remains pending. The reviewer of record is Maik Ratzmer
(`MaiRat`), but this document does not claim that a complete final human review has been
finished.

- **Name:** Maik Ratzmer (`MaiRat`)
- **Signature or attributable commit:** Pending final attestation
- **Date:** 2026-07-01

AI tools may help assemble evidence, but must not independently claim final human
approval, sign the attestation, or remove the pending-review status.
