// Test parallelization is ON for this assembly — multithreading roadmap item #21.
//
// It was off, with `[assembly: CollectionBehavior(DisableTestParallelization = true)]`, and the
// roadmap's guess about why was right: the engine used to dispatch promise continuations, async
// function resumptions and generator steps onto `ThreadPool` threads whenever no synchronization
// context was installed, which is exactly the situation in a unit test. Two tests running at once
// could then have their continuations interleaved onto each other's ambient context. Item #15
// removed that dispatch — continuations pump a single-threaded event loop now — so the reason the
// attribute existed is gone.
//
// What makes it safe rather than merely no-longer-obviously-unsafe is that a context is reachable
// from exactly one thread. `JSEngine.Current` is `[ThreadStatic]` and the async-local mirror that
// restores it across await points is `AsyncLocal`, so two xUnit test threads each running
// `using var ctx = new JSContext()` cannot see each other's context at all. What they do share is
// process-wide: the interned key strings, the built-in registry's static constructors (which the
// CLR already serialises), and `DictionaryCodeCache.Current`, which is a `ConcurrentDictionary`
// whose per-key compilation is serialised by `Lazy<T>` — the code cache is documented as
// process-shared and concurrent, and item #16 is built on that being true.
//
// If this file ever needs to go back to serial, the thing to look for is a *new* piece of
// process-wide mutable state, not a new test.

using Xunit;
