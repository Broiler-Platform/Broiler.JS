using System;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.BuiltIns.RegExp;

public partial class JSRegExp
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetObservableLastIndex()
    {
        var observableLastIndex = this[KeyStrings.lastIndex].DoubleValue;
        if (double.IsNaN(observableLastIndex) || observableLastIndex <= 0)
            return 0;

        if (observableLastIndex >= int.MaxValue)
            return int.MaxValue;

        return (int)observableLastIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetObservableLastIndex(int value)
    {
        this[KeyStrings.lastIndex] = JSValue.CreateNumber(value);
        lastIndex = value;
    }

    [JSExport("compile", Length = 2)]
    public JSValue Compile(in Arguments a)
    {
        var patternValue = a.Get1();
        var flagsValue = a.TryGetAt(1, out var second) ? second : JSValue.UndefinedValue;

        if (!ReferenceEquals(GetPrototypeOf(), GetCurrentPrototype()))
            throw JSEngine.NewTypeError("RegExp.prototype.compile called on incompatible receiver");

        string nextPattern;
        string nextFlags;

        if (patternValue is JSRegExp regExp)
        {
            if (!flagsValue.IsUndefined)
                throw JSEngine.NewTypeError("Cannot supply flags when constructing one RegExp from another");

            nextPattern = regExp.pattern;
            nextFlags = regExp.flags;
        }
        else
        {
            nextPattern = patternValue.IsUndefined ? string.Empty : patternValue.StringValue;
            nextFlags = flagsValue.IsUndefined ? string.Empty : flagsValue.StringValue;
        }

        pattern = nextPattern;
        (value, globalSearch, ignoreCase, multiline, hasIndices, sticky, unicode, unicodeSets, flags) = CreateRegex(nextPattern, nextFlags);
        this[KeyStrings.lastIndex] = JSValue.NumberZero;
        return this;
    }

    [JSExport]
    public int LastIndex
    {
        get => lastIndex; set => lastIndex = value;
    }

    [JSExport("test")]
    public JSValue Test(in Arguments a)
    {
        var text = a.Get1().StringValue;
        var startPosition = CalculateStartPosition(text);
        var match = value.Match(text, startPosition);

        if (sticky && (!match.Success || match.Index != startPosition))
            match = System.Text.RegularExpressions.Match.Empty;

        if (match.Success)
        {
            if (globalSearch || sticky)
                SetObservableLastIndex(match.Index + match.Length);

            return JSValue.BooleanTrue;
        }

        if (globalSearch || sticky)
            SetObservableLastIndex(0);

        return JSValue.BooleanFalse;
    }

    [JSExport("exec")]
    public JSValue Exec(in Arguments a)
    {
        var input = a.Get1().StringValue;
        // Perform the regular expression matching.
        var startPosition = CalculateStartPosition(input);
        var match = value.Match(input, startPosition);

        if (sticky && (!match.Success || match.Index != startPosition))
            match = System.Text.RegularExpressions.Match.Empty;

        // Return null if no match was found.
        if (match.Success == false)
        {
            // Reset the lastIndex property.
            if (globalSearch || sticky)
                SetObservableLastIndex(0);

            return JSValue.NullValue;
        }

        if (globalSearch || sticky)
            SetObservableLastIndex(match.Index + match.Length);

        var groups = match.Groups;
        var c = groups.Count;
        var result = JSValue.CreateArray((uint)c);

        for (int i = 0; i < c; i++)
        {
            var group = groups[i];
            if (group.Captures.Count == 0)
            {
                result[(uint)i] = JSUndefined.Value;
            } 
            else
            {
                result[(uint)i] = JSValue.CreateString(group.Value);
            }
        }

        result[KeyStrings.index] = JSValue.CreateNumber(match.Index);
        result[KeyStrings.input] = a.Get1();

        // Populate named groups (§2.7 — duplicate named capture groups support).
        var groupNames = value.GetGroupNames();
        if (groupNames.Length > 1) // First name is always "0" (the whole match)
        {
            var namedGroups = new JSObject();
            bool hasNamedGroups = false;
            
            for (int i = 0; i < groupNames.Length; i++)
            {
                var name = groupNames[i];
                // Skip numeric group names (they're positional, not named)
                if (name.Length > 0 && (name[0] < '0' || name[0] > '9'))
                {
                    hasNamedGroups = true;
                    var g = match.Groups[name];
                    namedGroups[name] = g.Success
                        ? JSValue.CreateString(g.Value)
                        : JSUndefined.Value;
                }
            }
            
            if (hasNamedGroups)
                result[KeyStrings.GetOrCreate("groups")] = namedGroups;
        }

        return result;
    }

    [JSPrototypeMethod]
    [JSExport("toString")]
    public static JSValue ToString(in Arguments a)
    {
        if (a.This is not JSRegExp regExp)
            throw JSEngine.NewTypeError("RegExp.prototype.toString called on incompatible receiver");

        return JSValue.CreateString($"/{regExp.pattern}/{regExp.flags}");
    }

    /// <summary>
    /// Calculates the position to start searching.
    /// </summary>
    /// <param name="input"> The string on which to perform the search. </param>
    /// <returns> The character position to start searching. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CalculateStartPosition(string input)
    {
        if (!globalSearch && !sticky)
            return 0;

        var maxIndex = GetObservableLastIndex();
        var minIndex = maxIndex < input.Length ? maxIndex : input.Length;

        return minIndex;
    }
}
