using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Collections.Generic;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.Engine;

public class CallStackItem
{
    private static readonly StringSpan Inline = "inline";

    internal CallStackItem(string fileName, in StringSpan function, int line, int column)
    {
        FileName = fileName;
        Function = function;
        Line = line;
        Column = column;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public CallStackItem(IJSExecutionContext context, ScriptInfo scriptInfo, int nameOffset, int nameLength, int line, int column)
    {
        context = context ?? JSEngine.Current as IJSExecutionContext;
        context.EnsureSufficientExecutionStack();
        this.context = context;
        var ctx = context.CurrentNewTarget;

        if (ctx != null)
        {
            NewTarget = ctx;
            context.CurrentNewTarget = null;
        }

        FileName = scriptInfo.FileName;
        Function = IsValidFunctionSpan(scriptInfo?.Code, nameOffset, nameLength)
            ? new StringSpan(scriptInfo.Code, nameOffset, nameLength)
            : Inline;
        Line = line;
        Column = column;
        Parent = context.Top;
        context.Top = this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public CallStackItem(IJSExecutionContext context, string fileName, in StringSpan function, int line, int column)
    {
        context = context ?? JSEngine.Current as IJSExecutionContext;
        context.EnsureSufficientExecutionStack();
        this.context = context;
        FileName = fileName;
        Function = function;
        Line = line;
        Column = column;
        Parent = context.Top;
        context.Top = this;
    }

    public CallStackItem Parent;
    public JSValue NewTarget;
    public StringSpan Function;
    public int Line;
    public int Column;
    private readonly IJSExecutionContext context;
    public string FileName;
    private Dictionary<uint, JSVariable> directEvalBindings;

    internal bool TryGetDirectEvalBinding(in KeyString name, out JSVariable variable)
    {
        if (directEvalBindings?.TryGetValue(name.Key, out variable) == true)
            return true;

        variable = null;
        return false;
    }

    internal void RegisterDirectEvalBinding(JSVariable variable)
    {
        if (variable == null || variable.Name.IsEmpty)
            return;

        directEvalBindings ??= [];
        var key = KeyStrings.GetOrCreate(variable.Name);
        directEvalBindings[key.Key] = variable;
    }

    internal bool DeleteDirectEvalBinding(in KeyString name)
        => directEvalBindings?.Remove(name.Key) == true;

    public void Update() => System.Diagnostics.Debug.WriteLine($"{Function} at {Line}, {Column}");

    public void Step(int line, int column)
    {
        context.Top = this;
        Line = line;
        Column = column;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Pop(IJSExecutionContext context)
    {
        context = context ?? JSEngine.Current as IJSExecutionContext;
        directEvalBindings = null;
        context.Top = Parent;
        Parent = null;
    }

    public override string ToString() => $"{Function} at {FileName} - {Line},{Column}";

    private static bool IsValidFunctionSpan(string code, int offset, int length)
        => code != null
            && offset >= 0
            && length > 0
            && offset <= code.Length
            && length <= code.Length - offset;
}
