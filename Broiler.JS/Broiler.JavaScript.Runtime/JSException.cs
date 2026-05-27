using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

public class JSException : Exception
{
    // Factory delegates for creating JSError instances, wired by BuiltInsAssemblyInitializer.
    internal static Func<JSException, string, JSValue> CreateJSError;
    internal static Func<JSException, JSObject, JSValue> CreateJSErrorWithPrototype;
    internal static Func<Exception, JSValue> JSErrorFrom;

    // ── Delegates for engine-level functionality (wired by Core's initializer) ──
    // These replace the direct JSEngine references that existed when JSException
    // lived inside the Core assembly.
    internal static Func<string, JSException> NewSyntaxErrorFactory;
    internal static Func<string, JSException> NewTypeErrorFactory;
    internal static Action<StringBuilder, List<(StringSpan target, string file, int line, int column)>> AppendStackTraceHelper;

    // Error message constants (moved from JSError for Core accessibility)
    public const string Cannot_convert_undefined_or_null_to_object = "Cannot convert undefined or null to object";
    public const string Parameter_is_not_an_object = "Parameter is not an object";

    // Helper methods (moved from JSTypeError for Core accessibility)
    public static string NotIterable(object name) => $"{name} is not iterable";
    public static string NotEntry(object name) => $"Iterator value {name} is an entry object";

    public override string Message
    {
        get
        {
            if (Error is IJSError)
                return Error[KeyStrings.message].ToString();

            return Error.ToString();
        }
    }

    public JSValue Error { get; internal set; }
    internal string RawMessage { get; }

    internal protected JSException With(JSValue error)
    {
        Error = error;
        return this;
    }

    private readonly List<(StringSpan target, string file, int line, int column)> trace = [];

    public JSException(JSValue message, [CallerMemberName] string function = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int line = 0) : base()
    {
        if (function != null)
            trace.Add((function, filePath ?? "Unknown", line, 1));

        RawMessage = message?.ToString();
        Error = message;
    }

    public JSException(string message, [CallerMemberName] string function = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int line = 0) : base(message)
    {
        if (function != null)
            trace.Add((function, filePath ?? "Unknown", line, 1));

        RawMessage = message;
        Error = (CreateJSError ?? throw new InvalidOperationException("JSException.CreateJSError delegate is not initialized. Ensure the BuiltIns assembly module initializer has run."))
            (this, message);
    }

    public JSException(string message, JSObject prototype, [CallerMemberName] string function = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int line = 0) : base(message)
    {
        if (function != null)
            trace.Add((function, filePath ?? "Unknown", line, 1));

        RawMessage = message;
        Error = (CreateJSErrorWithPrototype ?? throw new InvalidOperationException("JSException.CreateJSErrorWithPrototype delegate is not initialized. Ensure the BuiltIns assembly module initializer has run."))
            (this, prototype);
    }

    public JSValue JSStackTrace
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendLine(Message);

            if (trace.Count > 0)
            {
                var f = trace[0];
                sb.AppendLine($"    at {f.target}:{f.file}:{f.line},{f.column}");
            }

            AppendStackTraceHelper?.Invoke(sb, trace);

            return JSValue.CreateString(sb.ToString());
        }
    }

    internal static void Throw(JSValue value, [CallerMemberName] string function = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int line = 0)
    {
#if DEBUG
        var st = new System.Diagnostics.StackTrace(true);
        Console.Error.WriteLine($"[JSException.Throw] {value}");
        Console.Error.WriteLine($"  Function: {function}, File: {filePath}, Line: {line}");
        Console.Error.WriteLine(st.ToString());
#endif
        if (value is IJSError error && TryGetJSException(error.Exception, out var jsException))
            throw jsException.With(value);

        throw new JSException(value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSValue ThrowSyntaxError(string value) =>
        throw (NewSyntaxErrorFactory ?? throw new InvalidOperationException("JSException.NewSyntaxErrorFactory delegate is not initialized. Ensure the Core assembly module initializer has run."))
            (value);

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ThrowTypeError<T>(string value) =>
        throw (NewTypeErrorFactory ?? throw new InvalidOperationException("JSException.NewTypeErrorFactory delegate is not initialized. Ensure the Core assembly module initializer has run."))
            (value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static JSValue ThrowNotFunction(JSValue value) =>
        throw (NewTypeErrorFactory ?? throw new InvalidOperationException("JSException.NewTypeErrorFactory delegate is not initialized. Ensure the Core assembly module initializer has run."))
            ($"{value} is not a function");

    public static JSException FromValue(JSValue value)
    {
        if (value is IJSError error && TryGetJSException(error.Exception, out var jsException))
            return jsException.With(value);

        var ex = new JSException(value);
        return ex;
    }

    public static JSException From(Exception exception)
    {
        if (TryGetJSException(exception, out var jsException))
            return jsException;

        var error = new JSException(exception.InnerException?.ToString() ?? exception.ToString());
        return error;
    }

    public static JSValue ErrorFrom(Exception exception)
    {
        if (TryGetJSException(exception, out var jsException))
            return jsException.Error;

        var error = new JSException(exception.InnerException?.Message ?? exception.Message);
        return error.Error;
    }

    private static bool TryGetJSException(Exception exception, out JSException jsException)
    {
        jsException = null;

        switch (exception)
        {
            case JSException direct:
                jsException = direct;
                return true;
            case FastParseException parseException:
                jsException = (NewSyntaxErrorFactory ?? throw new InvalidOperationException("JSException.NewSyntaxErrorFactory delegate is not initialized."))
                    (parseException.Message);
                return true;
            case AggregateException aggregateException:
                foreach (var innerException in aggregateException.Flatten().InnerExceptions)
                {
                    if (TryGetJSException(innerException, out jsException))
                        return true;
                }
 
                break;
        }

        var inner = exception?.InnerException;
        if (inner != null && !ReferenceEquals(inner, exception) && TryGetJSException(inner, out jsException))
            return true;

        return false;
    }

    public override string StackTrace
    {
        get
        {
            var sb = new StringBuilder();
            foreach (var (target, file, line, _) in trace)
            {
                sb.Append("at ");
                sb.Append(target);
                sb.Append(" in ");
                sb.Append(file);
                sb.Append(":line ");
                sb.Append(line);
                sb.AppendLine();
            }

            // add internal stack..
            if (Error is IJSError error)
                sb.AppendLine(error.Stack);

            return sb.ToString();
        }
    }
}
