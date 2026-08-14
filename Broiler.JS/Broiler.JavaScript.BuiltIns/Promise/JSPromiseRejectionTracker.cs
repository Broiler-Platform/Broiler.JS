using Broiler.JavaScript.Runtime;
using System;
using System.Collections.Generic;

namespace Broiler.JavaScript.BuiltIns.Promise;

/// <summary>One promise that was rejected and, so far, has nobody to tell.</summary>
public readonly struct UnhandledRejection
{
    internal UnhandledRejection(JSValue reason)
    {
        Reason = reason;
    }

    /// <summary>The rejection reason — an <c>Error</c> object, or whatever else was thrown.</summary>
    public JSValue Reason { get; }

    /// <summary>
    /// The reason rendered the way the console would render it: an <c>Error</c>'s message when it
    /// has one, otherwise the value's own string form.
    /// </summary>
    public string Describe()
    {
        try
        {
            if (Reason is JSObject error && error[KeyStrings.message] is { } message && !message.IsUndefined)
            {
                var name = error[KeyStrings.name];
                return name.IsUndefined ? message.ToString() : name + ": " + message;
            }

            return Reason?.ToString() ?? "undefined";
        }
        catch (Exception)
        {
            // Describing a rejection must never replace it: the reason can be a hostile object
            // whose toString throws, and this is called while reporting a failure.
            return "<rejection reason could not be described>";
        }
    }
}

/// <summary>
/// Tracks promises that were rejected with no handler attached — what a browser reports as
/// <c>Uncaught (in promise)</c>, and what this engine had no notion of at all.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why the report is deferred rather than raised at the rejection.</b> <c>Promise.reject(x)</c>
/// is rejected before <c>.catch(…)</c> on the next line has run, and an <c>await</c> attaches its
/// handler a microtask after the promise settles. Reporting from inside <see cref="JSPromise.Reject"/>
/// would therefore call almost every correctly-handled rejection unhandled. The spec's answer, and
/// this one, is to collect at the rejection, withdraw when a handler arrives, and let the host ask
/// what is left once its microtask checkpoint has drained.
/// </para>
/// <para>
/// <b>Opt-in.</b> Off by default, so no existing run changes shape and a promise-heavy page pays
/// nothing: <see cref="Rejected"/> and <see cref="Handled"/> read one static <see langword="bool"/>
/// and return. A host that wants the reports sets <see cref="Enabled"/> and drains
/// <see cref="TakePending"/>. Reporting them through <c>window.onunhandledrejection</c> — which
/// would make this always-on — is the natural next step and is deliberately not done here.
/// </para>
/// <para>
/// <b>Identity, not equality.</b> The pending set is keyed on the promise instance by reference,
/// because two distinct promises rejected with the same reason are two rejections, and <c>JSValue</c>
/// equality is a JavaScript question this must not ask.
/// </para>
/// </remarks>
public static class JSPromiseRejectionTracker
{
    private static readonly Dictionary<object, JSValue> _pending = new(ReferenceEqualityComparer.Instance);
    private static readonly object _sync = new();
    private static bool _enabled;

    /// <summary>
    /// Whether rejections are tracked. Off by default. Setting it to <see langword="false"/>
    /// discards anything already collected, so a host cannot pick up another host's rejections.
    /// </summary>
    public static bool Enabled
    {
        get { lock (_sync) return _enabled; }
        set
        {
            lock (_sync)
            {
                _enabled = value;
                if (!value)
                    _pending.Clear();
            }
        }
    }

    /// <summary>
    /// A promise was rejected while nothing was waiting on it. Called from
    /// <see cref="JSPromise.Reject"/>.
    /// </summary>
    internal static void Rejected(object promise, JSValue reason)
    {
        lock (_sync)
        {
            if (!_enabled)
                return;

            _pending[promise] = reason;
        }
    }

    /// <summary>
    /// A handler was attached to <paramref name="promise"/>, so its rejection — whether it has
    /// happened yet or not — is somebody's business now. Called from <see cref="JSPromise.Then"/>.
    /// </summary>
    internal static void Handled(object promise)
    {
        lock (_sync)
        {
            if (!_enabled)
                return;

            _pending.Remove(promise);
        }
    }

    /// <summary>
    /// The rejections still unclaimed, in rejection order, and clears them. A host calls this after
    /// draining its microtask checkpoint: anything left has had every chance to be handled.
    /// </summary>
    public static IReadOnlyList<UnhandledRejection> TakePending()
    {
        lock (_sync)
        {
            // A collection expression rather than Array.Empty: from inside this namespace the
            // sibling Broiler.JavaScript.BuiltIns.Array shadows System.Array by simple-name lookup.
            if (_pending.Count == 0)
                return [];

            var taken = new List<UnhandledRejection>(_pending.Count);
            foreach (var reason in _pending.Values)
                taken.Add(new UnhandledRejection(reason));

            _pending.Clear();
            return taken;
        }
    }
}
