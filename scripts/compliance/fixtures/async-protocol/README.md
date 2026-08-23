# Async completion-protocol fixtures

test262-shaped tests whose subject is the **runner**, not the engine: each one completes,
fails, never settles, completes twice, or never returns, and `expected.json` records the
verdict `run_test262.py` has to reach for it.

Run them against a built engine:

```sh
python scripts/compliance/run_test262.py --self-check \
    --broiler-dll Broiler.JS/Broiler.JavaScript/bin/Debug/net10.0/BroilerJS.dll
```

They exist because an async test could not fail. `$DONE` used to settle a promise the
assembled script ended in; `--script-host` evaluates and discards a script's completion
value and reports no unhandled rejection, so `$DONE(error)` and a `$DONE` that was never
called both exited 0 and were counted as passes. Every fixture here except `completes.js`
passed under that protocol, which is what makes them the regression: they are the shapes an
`flags: [async]` result has to be able to take.

Only `test/` lives here. The harness comes from the pinned suite (`--suite-ref`), so the
fixtures run against the same `assert.js`, `sta.js`, and `doneprintHandle.js` as the corpus
rather than against a copy that could drift from it.
