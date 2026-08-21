using System;
using System.Text;
using System.Threading;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// The source text of the property access a <c>null</c>/<c>undefined</c> failure came from, so
/// the TypeError can name the expression instead of only the property.
/// </summary>
/// <remarks>
/// <para>
/// <b>What the message was missing.</b> <c>Cannot get property enabled of undefined</c> says which
/// property was asked for and says nothing about <em>what</em> was undefined — and that is the
/// half the reader needs. In <c>config.server.tls.enabled</c> the undefined value is
/// <c>config.server.tls</c>, and the message, the stack and the property name together still do
/// not say so: the frame points at the statement, and a statement can hold several accesses that
/// all end in <c>.enabled</c>. So the reader is left to re-derive the base by hand, which on a
/// bundle is a search through a line thousands of characters wide. JavaScriptCore reports
/// <c>undefined is not an object (evaluating 'config.server.tls.enabled')</c> and SpiderMonkey
/// reports <c>config.server.tls is undefined</c>; both name the expression, and this engine did
/// not. The <c>(evaluating '…')</c> clause here is JavaScriptCore's wording, appended to the
/// existing message rather than replacing it, so the property name and the receiver's type are
/// still where a reader — or a log scraper — already looks for them.
/// </para>
/// <para>
/// <b>Why it costs a successful read nothing.</b> The text is not carried by the running program.
/// The compiler already embeds one integer per emitted constant-key access — the inline-cache
/// site index — and that integer is enough to find anything recorded about the access at compile
/// time. So the emitted code is byte-for-byte what it was; what is new is a side table the site
/// index addresses, written once while compiling and read only after a receiver has turned out to
/// be nullish. The nullish test itself sits on the cache's MISS path, which a nullish receiver
/// always takes — it is not a <see cref="JSObject"/>, so it can never hit — meaning the hit path,
/// the one that matters, does not test anything new at all.
/// </para>
/// <para>
/// <b>Why the description is verified before it is used.</b> Site indices are handed out from a
/// process-wide counter, and a tier-2 recompile addresses a tier-1 site by its ordinal position in
/// the function's emission — a mapping two threads compiling at once can slide, which
/// <see cref="SpecializedPropertyRead"/> already documents and guards for the same reason. A slid
/// mapping here would put someone else's expression in the message, which is worse than saying
/// nothing. Each description therefore records the key its access was compiled for, and the clause
/// is emitted only when that key is the one that actually failed.
/// </para>
/// <para>
/// <b>What is not covered, and why each one is not.</b> A description reaches a message only
/// through the constant-key read and store caches, which is <c>a.b.c</c>, <c>a.b.c = v</c>,
/// <c>a.b.c op= v</c>, <c>a.b.c++</c>, and the receiver read of <c>a.b.c()</c>. Three separate
/// things fall outside that, for three different reasons:
/// <list type="bullet">
/// <item>
/// A <c>super</c> access, an optional-chain link and a computed access whose key is not a
/// compile-time constant emit <em>no site</em>, so there is nothing to record against. Giving
/// them one means changing what the emitted code looks like on paths that are hotter than this
/// is worth, which is its own change rather than something to smuggle in here.
/// </item>
/// <item>
/// A computed access that DOES take a site (<c>a.b["x"]</c>) has one but declines to use it:
/// its source span cannot be closed, because the access's last token is the key rather than the
/// bracket after it. <c>AstExpressionExtensions.AccessCode</c> holds that reasoning; so does a
/// parenthesized base, whose opening parenthesis no token accounts for.
/// </item>
/// <item>
/// "undefined is not a function" — <c>a.b.c()</c> where <c>a.b.c</c> read back undefined — is
/// raised at the INVOCATION, not at a property access, and a call site carries no site index.
/// The receiver read one link earlier is described, so <c>a.b()</c> on a nullish <c>a</c> is
/// named and <c>a.b()</c> on a missing <c>b</c> is not.
/// </item>
/// </list>
/// </para>
/// </remarks>
public static class NullishAccess
{
    /// <summary>
    /// The widest expression quoted in a message. Longer text is cut and marked with an ellipsis.
    /// </summary>
    /// <remarks>
    /// An access is normally short, but nothing bounds it — <c>fetch(url, opts).body.locked</c>
    /// contains a whole call, and a minifier can put an arbitrary expression in the base. A
    /// message is read in a log line and in a browser console, so it is bounded here rather than
    /// left to whatever the source happens to hold.
    /// </remarks>
    private const int MaxQuotedLength = 96;

    /// <summary>
    /// Mirrors the inline-cache table's own ceiling. A site past it is never recorded, exactly as
    /// it is never cached, and the message for one is what it was before this existed.
    /// </summary>
    private const int MaxSites = 65_536;

    /// <summary>
    /// Whether accesses are described while compiling. On by default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A description is one entry per emitted constant-key access, and the text is a <em>span</em>
    /// into the source rather than a copy — so it costs no string, and in exchange it keeps that
    /// source alive. For a host that compiles a fixed program that is free: the same string is
    /// already held for stack traces. For one that evaluates a stream of unrelated sources — a
    /// REPL, a server running user snippets — it is not, because a source stays reachable through
    /// this table after the code compiled from it is gone. Such a host should call
    /// <see cref="Reset"/> between programs, or clear this and get the messages it had before.
    /// </para>
    /// <para>
    /// Read while compiling, never while running: clearing it stops new descriptions being
    /// recorded and does not retract the ones already there. <see cref="Reset"/> does that.
    /// </para>
    /// </remarks>
    public static bool Enabled = true;

    /// <summary>One emitted access: the text to quote, and the key it was compiled for.</summary>
    /// <remarks>
    /// A class rather than a struct so that recording one is a single reference store. A reader is
    /// a failing access on another thread, and it must see a whole description or none — never
    /// this one's text against that one's key, which is exactly what the key check exists to
    /// prevent and what a torn multi-word write would slip past it.
    /// </remarks>
    private sealed class Description(in StringSpan access, uint key)
    {
        internal readonly StringSpan Access = access;

        /// <summary>The interned key of the property this access reads or writes.</summary>
        internal readonly uint Key = key;
    }

    private static readonly object growthLock = new();
    private static Description[] reads = [];
    private static Description[] stores = [];

    /// <summary>
    /// The access whose receiver has just turned out to be nullish, as one plus its index into
    /// <see cref="reads"/> or <see cref="stores"/> — so that zero, which is what an untouched
    /// thread-static reads back as, means "no access is being described" and does not collide
    /// with site zero.
    /// </summary>
    /// <remarks>
    /// Written only after the failure is certain: the read and store helpers below are entered
    /// when the receiver is already known to be <c>null</c> or <c>undefined</c>, so an access that
    /// succeeds never touches this. It is restored on the way out rather than merely cleared,
    /// because the operation it wraps throws through frames that may themselves be describing an
    /// access.
    /// </remarks>
    [ThreadStatic]
    private static int pendingSitePlusOne;

    [ThreadStatic]
    private static bool pendingIsStore;

    /// <summary>Records the source text of one emitted constant-key READ site.</summary>
    /// <param name="site">The inline-cache read-site index the compiler embedded.</param>
    /// <param name="access">The whole access, e.g. <c>config.server.tls.enabled</c>.</param>
    /// <param name="property">
    /// The name of the property <paramref name="access"/> reads — <c>enabled</c>. Interned here
    /// rather than by the caller so that a host with descriptions turned off does no work at all.
    /// </param>
    public static void DescribeRead(int site, in StringSpan access, in StringSpan property)
        => Describe(ref reads, site, in access, in property);

    /// <summary>Records the source text of one emitted constant-key STORE site.</summary>
    public static void DescribeStore(int site, in StringSpan access, in StringSpan property)
        => Describe(ref stores, site, in access, in property);

    private static void Describe(ref Description[] table, int site, in StringSpan access, in StringSpan property)
    {
        if (!Enabled
            || (uint)site >= MaxSites
            || access.Source == null
            || access.Length == 0
            || property.Length == 0)
        {
            return;
        }

        var key = KeyStrings.GetOrCreate(property).Key;
        if (key == 0)
            return;

        var current = Volatile.Read(ref table);
        if (site >= current.Length)
            current = Grow(ref table, site);

        // Deliberately outside the lock. Compilation already serializes once per emitted site on
        // the inline-cache table's own allocation lock, and taking a second lock for every access
        // in the program would double a bottleneck that a diagnostic has no business being on.
        // The store is a single reference write, and the only race it can lose is against a
        // CONCURRENT growth that has already replaced the array this one is holding — in which
        // case the description is dropped and the message is the one it would have been before
        // this existed. A lost clause; never a wrong one.
        Volatile.Write(ref current[site], new Description(in access, key));
    }

    /// <summary>Replaces a table with one large enough to hold <paramref name="site"/>.</summary>
    private static Description[] Grow(ref Description[] table, int site)
    {
        lock (growthLock)
        {
            var current = table;
            if (site < current.Length)
                return current;

            var length = Math.Max(64, current.Length);
            while (length <= site)
                length = Math.Min(MaxSites, length * 2);

            var replacement = new Description[length];
            Array.Copy(current, replacement, current.Length);
            Volatile.Write(ref table, replacement);
            return replacement;
        }
    }

    /// <summary>
    /// Performs a read whose receiver is already known to be nullish, with the site's description
    /// in scope so the TypeError it raises can quote the expression.
    /// </summary>
    /// <remarks>
    /// Never returns: both <see cref="JSUndefined"/> and the null singleton throw from this
    /// indexer. It is written as a returning method anyway so the caller keeps the shape of an
    /// ordinary read and the compiler is not asked to prove otherwise.
    /// </remarks>
    internal static JSValue ReadNullish(int site, JSValue target, in KeyString key)
    {
        var previousSite = pendingSitePlusOne;
        var previousIsStore = pendingIsStore;
        pendingSitePlusOne = site + 1;
        pendingIsStore = false;
        try
        {
            return target[key];
        }
        finally
        {
            pendingSitePlusOne = previousSite;
            pendingIsStore = previousIsStore;
        }
    }

    /// <summary>The store twin of <see cref="ReadNullish"/>.</summary>
    internal static void WriteNullish(int site, JSValue target, in KeyString key, JSValue value)
    {
        var previousSite = pendingSitePlusOne;
        var previousIsStore = pendingIsStore;
        pendingSitePlusOne = site + 1;
        pendingIsStore = true;
        try
        {
            target[key] = value;
        }
        finally
        {
            pendingSitePlusOne = previousSite;
            pendingIsStore = previousIsStore;
        }
    }

    /// <summary>
    /// The <c>(evaluating '…')</c> clause for the access currently failing on
    /// <paramref name="key"/>, or the empty string when this access carries no description.
    /// </summary>
    /// <remarks>
    /// Consumes the pending description as it renders it. Building the error is not guaranteed to
    /// be inert — it allocates an object and runs the engine's error factory — so leaving the
    /// description in place would let a later, unrelated failure inherit it. A description
    /// describes exactly one message.
    /// </remarks>
    public static string Evaluating(in KeyString key)
    {
        var sitePlusOne = pendingSitePlusOne;
        if (sitePlusOne == 0)
            return string.Empty;

        var table = pendingIsStore ? Volatile.Read(ref stores) : Volatile.Read(ref reads);
        pendingSitePlusOne = 0;
        pendingIsStore = false;

        var site = sitePlusOne - 1;
        if ((uint)site >= (uint)table.Length)
            return string.Empty;

        var description = Volatile.Read(ref table[site]);

        // Null means nothing was recorded for this site, and a key mismatch means the site index
        // no longer names the access it was recorded for; see the class remarks. Either way the
        // honest answer is the message without a clause.
        if (description == null || description.Key != key.Key)
            return string.Empty;

        return Quote(description.Access);
    }

    private const string ClauseOpening = " (evaluating '";

    /// <summary>
    /// Renders one access as <c>&#32;(evaluating '…')</c>: bounded, on one line, and with any
    /// quote in the source escaped so the clause cannot be misread as ending early.
    /// </summary>
    private static string Quote(in StringSpan access)
    {
        var source = access.Source;
        if (source == null || access.Length == 0)
            return string.Empty;

        var length = Math.Min(access.Length, MaxQuotedLength);
        var builder = new StringBuilder(ClauseOpening.Length + length + 3);
        builder.Append(ClauseOpening);

        // A member access is routinely written across lines — `client\n  .config\n  .retries` is
        // ordinary style — and the indentation that leaves behind is not part of what the reader
        // is looking for. Runs of whitespace collapse to at most one space so the clause stays a
        // single line, and to NONE where the space only separates punctuation from its operand:
        // a chain broken over four lines has to read back as `client.config.retries`, the thing
        // that can be searched for, rather than as `client .config .retries`, which cannot. An
        // argument list keeps its spaces, because there the space is how the source reads.
        var pendingSpace = false;
        var previous = '\0';
        for (var i = 0; i < length; i++)
        {
            var c = source[access.Offset + i];
            if (char.IsWhiteSpace(c))
            {
                pendingSpace = builder.Length > ClauseOpening.Length;
                continue;
            }

            if (pendingSpace)
            {
                if (!IsPunctuation(c) && !IsPunctuation(previous))
                    builder.Append(' ');

                pendingSpace = false;
            }

            // The clause is delimited by single quotes, so a quote inside the source would end it
            // early and leave the tail reading as prose. Escaped rather than dropped: the text is
            // meant to be searched for in the file it came from.
            if (c == '\'' || c == '\\')
                builder.Append('\\');

            builder.Append(c);
            previous = c;
        }

        if (builder.Length == ClauseOpening.Length)
            return string.Empty;

        if (access.Length > length)
            builder.Append('…');

        builder.Append("')");
        return builder.ToString();
    }

    /// <summary>
    /// Whether a space next to this character carries nothing — the punctuation that structures an
    /// access, never the characters of a name or a literal.
    /// </summary>
    private static bool IsPunctuation(char c)
        => c is '.' or '(' or ')' or '[' or ']' or ',' or '?' or ';' or ':';

    /// <summary>
    /// Drops every recorded description. For a host that reuses one process across unrelated
    /// scripts, and for tests that assert on the tables' own behaviour.
    /// </summary>
    /// <remarks>
    /// The in-flight description is per-thread, so this clears the caller's and no one else's.
    /// That is the only sense in which it can be cleared at all — another thread's is a local of a
    /// frame that is mid-throw — and it is not a gap: a description is consumed by the message it
    /// describes, and restored by the frame that set it, whether or not this is ever called.
    /// </remarks>
    public static void Reset()
    {
        lock (growthLock)
        {
            Volatile.Write(ref reads, []);
            Volatile.Write(ref stores, []);
        }

        pendingSitePlusOne = 0;
        pendingIsStore = false;
    }
}
