namespace Broiler.Regex.Matching;

/// <summary>
/// A shared, per-run step budget guarding against catastrophic backtracking.
/// Carried by reference through every <see cref="MatchState"/> derived from one
/// match attempt, so the whole search tree shares a single counter (and distinct
/// attempts — including on different threads — get distinct budgets).
/// </summary>
internal sealed class Budget
{
    private long _steps;
    private readonly long _limit;

    public Budget(long limit) => _limit = limit;

    /// <summary>Consumes one step; returns false once the limit is exhausted.</summary>
    public bool Step() => ++_steps <= _limit;
}

/// <summary>
/// The ECMAScript matcher "State" (§22.2.2.1): an end index plus the list of
/// capture spans. Immutable with copy-on-write captures so backtracking simply
/// discards derived states. The whole-match span is captures[0].
/// </summary>
internal sealed class MatchState
{
    public readonly string Input;
    public readonly int Position;

    /// <summary>
    /// Capture spans, length <c>2 * (captureCount + 1)</c>. For group <c>i</c>,
    /// <c>[2i]</c> is the start and <c>[2i+1]</c> the end; <c>-1</c> means unset.
    /// </summary>
    public readonly int[] Captures;

    public readonly Budget Budget;

    public MatchState(string input, int position, int[] captures, Budget budget)
    {
        Input = input;
        Position = position;
        Captures = captures;
        Budget = budget;
    }

    public MatchState WithPosition(int position)
        => new(Input, position, Captures, Budget);

    public MatchState WithCapture(int index, int start, int end)
    {
        var captures = (int[])Captures.Clone();
        captures[2 * index] = start;
        captures[2 * index + 1] = end;
        return new MatchState(Input, Position, captures, Budget);
    }

    /// <summary>Returns a state with the given capture indices reset to unset.</summary>
    public MatchState WithResetCaptures(int[] indices)
    {
        if (indices.Length == 0)
            return this;
        var captures = (int[])Captures.Clone();
        foreach (var i in indices)
        {
            captures[2 * i] = -1;
            captures[2 * i + 1] = -1;
        }
        return new MatchState(Input, Position, captures, Budget);
    }

    public bool TryGetCapture(int index, out int start, out int end)
    {
        start = Captures[2 * index];
        end = Captures[2 * index + 1];
        return start >= 0 && end >= 0;
    }
}
