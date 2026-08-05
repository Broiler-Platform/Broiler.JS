using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Broiler.JavaScript.ExpressionCompiler.Runtime;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Counts, per corpus, how many of a script's functions are ever invoked once the script has
/// been evaluated — the population that decides what is left of roadmap item 1-1.
/// </summary>
/// <remarks>
/// <para>
/// 1-1's emission half defers IL generation to first invocation, and its remaining half proposes
/// deferring the parse and the expression-tree construction as well. Both are worth exactly what
/// the *never-invoked* functions cost, and that share has never been counted: the roadmap's
/// ceiling table sizes 1-1 by stubbing every outermost function body, which for a corpus like
/// jQuery removes the program — its 532 functions live inside one IIFE, and that IIFE runs.
/// </para>
/// <para>
/// The counter used is the deferral's own. Item 1-1's registration happens once per syntactic
/// site and <c>Force</c> once per site that is invoked, so <c>registered - forced</c> is the
/// number of functions a front-end deferral would never have had to look at. Evaluating the
/// script and stopping is not an approximation of CodeLoad's shape — it *is* CodeLoad's shape:
/// the benchmark evaluates jQuery and the Closure base and calls nothing else.
/// </para>
/// <para>
/// A corpus that throws while evaluating (a browser global this host does not provide) still
/// reports its counts, with the failure named. A partial evaluation under-reports <c>forced</c>,
/// so it bounds the never-invoked share from above and is labelled rather than dropped.
/// </para>
/// </remarks>
internal static class DeferPopulationMetrics
{
    /// <summary>
    /// The globals each corpus is evaluated against, lifted from the harness that evaluates it.
    /// </summary>
    /// <remarks>
    /// A corpus that throws on its first line counts nothing, so these are not cosmetic: jQuery's
    /// payload ends <c>})(windowmock)</c> and Octane's <c>runJQuery</c> supplies that mock and
    /// the <c>MockElement</c> it holds, while the four benchmark files construct a
    /// <c>BenchmarkSuite</c> at top level. They are stubs of the harness, not of the corpus —
    /// nothing here changes how much of the corpus is compiled.
    /// </remarks>
    private const string Prologue = """
        function BenchmarkSuite() {}
        function Benchmark() {}
        function MockElement() {
          this.appendChild = function(a) {};
          this.createComment = function(a) {};
          this.createDocumentFragment = function() { return new MockElement(); };
          this.createElement = function(a) { return new MockElement(); };
          this.documentElement = this;
          this.getElementById = function(a) { return 0; };
          this.getElementsByTagName = function(a) { return [0]; };
          this.insertBefore = function(a, b) {};
          this.removeChild = function(a) {};
          this.setAttribute = function(a, b) {};
        }
        var jQuerySalt = 1;
        var googsalt = 1;
        var windowmock = {
          'document': new MockElement(),
          'location': { 'href': '' },
          'navigator': { 'userAgent': '' }
        };
        """;

    /// <summary>
    /// The call CodeLoad makes through each payload after evaluating it, verbatim from
    /// <c>runJQuery</c> and <c>runClosure</c>.
    /// </summary>
    private static readonly Dictionary<string, string> Epilogue = new(StringComparer.Ordinal)
    {
        ["codeload-jquery"] = "(function(){return windowmock.jQuery.grep([jQuerySalt], function(a,b){return true;})[0];})();",
        ["codeload-closure"] = "(function(){return goog.cloneObject(googsalt);})();",
    };

    public static void Write(string octaneDirectory, string only = null)
    {
        var corpora = CompileProfileMetrics.LoadCorpora(octaneDirectory);
        if (!string.IsNullOrEmpty(only))
            corpora = corpora.Where(c => c.Name == only).ToList();

        var rows = new List<object>(corpora.Count);

        // Item 1-1's remaining half needs a second number beside the never-invoked share, and it
        // has to be on before anything is compiled — a compile-time counter enabled among the
        // run-time censuses reads zero, which phase 3 paid for twice.
        Compiler.DeferrableFunctions.Counting = true;
        // Item 1-1's remaining half: check the source-derived capture layout against the one the
        // rewrite derives from the tree, on the same corpora and in the same run.
        ExpressionCompiler.DeferredCaptureLayout.Checking = true;
        // Item 1-1's remaining half, the mechanism: retain an enclosing scope per function and
        // re-compile every body from it afterwards, checking each against the eager tree.
        Compiler.DeferredTreeCompilation.Retaining = true;

        foreach (var corpus in corpora)
        {
            // One context per corpus, and the counters reset after it is built: creating a
            // context compiles built-in JavaScript of its own, and counting that as the
            // corpus's would inflate every row by the same fixed amount.
            using var context = BenchmarkContext.Create(new NoCodeCache());
            context.Eval(Prologue, "defer-population-prologue.js", context);
            DeferredMethodDiagnostics.Reset();
            ClosureRewriteDiagnostics.Reset();
            Compiler.CompilerSpecializationDiagnostics.Reset();
            ExpressionCompiler.DeferredCaptureLayout.Reset();
            Compiler.DeferredTreeCompilation.Reset();

            string failure = null;
            try
            {
                context.Eval(corpus.Source, corpus.Name + ".js", context);
                // CodeLoad does not stop at evaluating the payload: it calls one function
                // through it, which is what makes the run a load rather than a parse. Running
                // the same call is what keeps `forced` honest for the benchmark this item is
                // justified by.
                if (Epilogue.TryGetValue(corpus.Name, out var epilogue))
                    context.Eval(epilogue, corpus.Name + "-epilogue.js", context);
            }
            catch (Exception ex)
            {
                failure = ex.GetType().Name + ": " + ex.Message;
            }

            var registered = DeferredMethodDiagnostics.RegisteredSites;
            var forced = DeferredMethodDiagnostics.ForcedSites;
            var relaysRewritten = ClosureRewriteDiagnostics.RewroteRelays;
            var relaysSkipped = ClosureRewriteDiagnostics.SkippedRelays;
            var capturesInRepeat = ClosureRewriteDiagnostics.CapturesCreatedInRepeatWalk;

            var reentry = Compiler.DeferredTreeCompilation.Recompile();
            var specialization = Compiler.CompilerSpecializationDiagnostics.Snapshot();
            var captureFree = specialization.DeferralRefusals[(int)Compiler.DeferralRefusal.CaptureFree];
            var dynamicSites = specialization.DeferralRefusals[(int)Compiler.DeferralRefusal.Dynamic];
            var capturingSites = specialization
                .DeferralRefusals[(int)Compiler.DeferralRefusal.CapturesEnclosingBinding];
            var classified = captureFree + dynamicSites + capturingSites;

            rows.Add(new
            {
                corpus = corpus.Name,
                sourceBytes = corpus.Source.Length,
                registeredSites = registered,
                forcedSites = forced,
                neverForcedSites = registered - forced,
                neverForcedShare = registered > 0
                    ? Math.Round((registered - forced) / (double)registered, 4)
                    : 0d,
                // How many relayed sites needed their own closure rewrite and how many had
                // already had one from the walk that emitted their parent.
                relaysRewritten,
                relaysSkipped,
                // Non-zero only with BROILER_JS_RELAY_REWRITE_ONCE=0, which is the arm that still
                // runs the repeat: how many captures the repeat created that the first walk had
                // not. Zero is what makes the skip a removal of repeated work.
                capturesInRepeat,

                // Item 1-1's remaining half, classified. A waterfall over every function site the
                // corpus compiles: captureFree needs no Box[] and so no capture mechanism,
                // dynamicSites can reach bindings their text never names and no mechanism reaches
                // them, and capturingSites are exactly what the mechanism is for. The three sum to
                // classifiedSites.
                classifiedSites = classified,
                captureFreeSites = captureFree,
                dynamicSites,
                capturingSites,
                captureFreeShare = classified > 0
                    ? Math.Round(captureFree / (double)classified, 4)
                    : 0d,
                // The density behind the verdict: how many names a capturing site actually
                // captures, and how many of those belong to a function rather than the program
                // top level. A refusal count alone cannot tell "captures one name of forty" from
                // "captures everything", and the mechanism is worth different amounts in the two.
                freeNames = specialization.DeferralFreeNames,
                boundFreeNames = specialization.DeferralBoundFreeNames,
                functionOwnedFreeNames = specialization.DeferralFunctionOwnedFreeNames,
                // The one that decides the item: bound names held in a CLR local of an enclosing
                // lambda, and so needing a Box[] entry whose index only LambdaRewriter knows. A
                // bound name WITHOUT one — a script's top-level var or function declaration, which
                // is a property of the global object — costs a deferral nothing.
                cellBackedFreeNames = specialization.DeferralCellBackedFreeNames,

                // Non-null means evaluation stopped early, so `forced` is a floor and the share
                // above is a ceiling.
                // Item 1-1's capture layout, predicted from source against the rewrite's own
                // answer. missedSites is the go/no-go: over-approximating costs a box, missing is
                // a miscompile.
                layoutSites = ExpressionCompiler.DeferredCaptureLayout.Sites,
                layoutExact = ExpressionCompiler.DeferredCaptureLayout.ExactSites,
                layoutOver = ExpressionCompiler.DeferredCaptureLayout.OverApproximatedSites,
                layoutMissed = ExpressionCompiler.DeferredCaptureLayout.MissedSites,
                layoutPredictedNames = ExpressionCompiler.DeferredCaptureLayout.PredictedNames,
                layoutActualNames = ExpressionCompiler.DeferredCaptureLayout.ActualNames,
                layoutMissedNames = ExpressionCompiler.DeferredCaptureLayout.MissedNames,
                layoutOverNames = ExpressionCompiler.DeferredCaptureLayout.OverApproximatedNames,
                layoutSynthetic = ExpressionCompiler.DeferredCaptureLayout.SyntheticNames,
                // Checks against sites: a site relayed more than once is recognised rather than
                // counted again, and whether the repeats AGREE is what says the repeat is pure
                // duplication instead of the rewrite deciding differently on a later relay.
                layoutExcluded = ExpressionCompiler.DeferredCaptureLayout.ExcludedSites,
                layoutChecks = ExpressionCompiler.DeferredCaptureLayout.Checks,
                layoutRepeatChecks = ExpressionCompiler.DeferredCaptureLayout.RepeatChecks,
                layoutRepeatDisagreements = ExpressionCompiler.DeferredCaptureLayout.RepeatDisagreements,
                layoutMissedSamples = ExpressionCompiler.DeferredCaptureLayout.MissedNameSamples,

                // Re-entry: bodies compiled a SECOND time from the retained enclosing scope,
                // after this corpus's compilation finished, compared against the eager tree TWO
                // ways. `reproduced` is equality up to an order-preserving renaming of the
                // compiler's monotonic counters; `structural` erases those numbers instead of
                // mapping them, so it asks whether the trees agree in every token a counter did
                // not produce. The gap between them is numbering, and only what is outside
                // `structural` is a candidate difference in the tree itself.
                reentryTotal = reentry.Total,
                reentryReproduced = reentry.Reproduced,
                reentryStructural = reentry.Structural,
                reentryThrew = reentry.Threw,
                reentryFirstDifference = reentry.FirstDifference,
                reentryFirstStructuralDifference = reentry.FirstStructuralDifference,
                reentryFirstOrdinalDivergence = reentry.FirstOrdinalDivergence,
                reentryExhausted = reentry.Exhausted,
                reentryEagerReusedASite = reentry.EagerReusedASite,
                reentryOtherDivergence = reentry.OtherDivergence,
                reentrySitesBefore = reentry.SitesBefore,
                reentrySitesAfter = reentry.SitesAfter,

                evaluationFailure = failure,
            });

            Console.Error.WriteLine(
                $"{corpus.Name,-18} registered={registered,6} forced={forced,6} "
                + $"never={registered - forced,6} ({(registered > 0 ? (registered - forced) * 100.0 / registered : 0):F1}%) "
                + $"relayRewrote={relaysRewritten,6} relaySkipped={relaysSkipped,6} "
                + $"capturesInRepeat={capturesInRepeat,6}");
            Console.Error.WriteLine(
                $"{string.Empty,-18} sites={classified,6} captureFree={captureFree,6} "
                + $"({(classified > 0 ? captureFree * 100.0 / classified : 0):F1}%) "
                + $"capturing={capturingSites,6} dynamic={dynamicSites,6} "
                + $"free={specialization.DeferralFreeNames,6} bound={specialization.DeferralBoundFreeNames,6} "
                + $"fnOwned={specialization.DeferralFunctionOwnedFreeNames,6} "
                + $"cellBacked={specialization.DeferralCellBackedFreeNames,6}"
                + (failure == null ? string.Empty : $"  [{failure}]"));
        }

        Compiler.DeferrableFunctions.Counting = false;
        ExpressionCompiler.DeferredCaptureLayout.Checking = false;
        Compiler.DeferredTreeCompilation.Retaining = false;

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                metric = "defer-population",
                note = "registered = deferred sites created while compiling and running the "
                    + "script; forced = those whose IL was generated, i.e. that were invoked. "
                    + "neverForcedShare bounds what deferring the parse and the expression tree "
                    + "as well could remove.",
                rows,
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }
}
