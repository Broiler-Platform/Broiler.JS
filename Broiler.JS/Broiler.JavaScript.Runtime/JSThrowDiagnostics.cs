using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Reports a JavaScript throw to the host at the point it is RAISED, before any
/// <c>try</c>/<c>catch</c> in page script can swallow it.
/// </summary>
/// <remarks>
/// <para>
/// A page that catches its own TypeError leaves no trace. <c>javascript-errors.log</c> records
/// what escaped to the host, and a caught throw never gets there — which is the right default,
/// because feature detection reads properties off <c>undefined</c> constantly and an engine that
/// treated every one as a failure would report thousands per page.
/// </para>
/// <para>
/// It is the wrong default when the question is "does this throw still happen?". A debugger
/// answers that by breaking on first-chance exceptions, and until now there was no equivalent
/// without one: <see cref="JSException.LogThrows"/> is compiled out of Release, and the
/// property-access TypeErrors never reach <see cref="JSException.Throw"/> in any configuration
/// anyway — they are raised straight from <see cref="JSValue"/>, so the one thing that flag's
/// documentation promises to cover was the one thing it could not. This is that equivalent:
/// opt-in, present in every configuration, and reported the moment the throw is raised.
/// </para>
/// <para>
/// What it carries is the JavaScript stack, not the CLR one. The CLR frames are the same handful
/// of engine methods for every throw, while the script frames name the line that actually did the
/// read — and they are gone by the time the exception reaches a handler, so they have to be
/// rendered here.
/// </para>
/// <para>
/// Costs nothing when no sink is installed: one static read on a path that is already throwing.
/// </para>
/// </remarks>
public static class JSThrowDiagnostics
{
    /// <summary>Reading a property of <c>null</c> or <c>undefined</c>.</summary>
    public const string PropertyRead = "read";

    /// <summary>Writing a property of <c>null</c> or <c>undefined</c>.</summary>
    public const string PropertyWrite = "write";

    /// <summary>
    /// Names the environment variable that installs the default sink. <c>1</c> (or <c>stderr</c>)
    /// writes to standard error; any other value is taken as a file path and appended to. A host
    /// that wants the records somewhere else assigns <see cref="Sink"/> instead, replacing this.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The file form exists for a windowed host. Broiler.App has no console to write to, and its
    /// most useful case is precisely the one a console cannot serve: a throw that only reproduces
    /// outside the debugger, where the evidence has to outlive the process.
    /// </para>
    /// <para>
    /// Deliberately not the older <c>BROILER_LOG_THROWS</c>: that one dumps every throw with a
    /// full CLR stack in Debug builds and is a different, much noisier instrument. Both can be on.
    /// </para>
    /// </remarks>
    public const string EnvironmentVariable = "BROILER_LOG_JS_THROWS";

    private static Action<JSThrowInfo> sink = CreateDefaultSink();

    // Rendering a stack, and whatever the host's sink does with it, is ordinary managed code that
    // can itself read a property off undefined. Without this the first such read would report,
    // and that report would report, until the stack ran out.
    [ThreadStatic]
    private static bool reporting;

    /// <summary>
    /// Where records go. <c>null</c> — the default unless <see cref="EnvironmentVariable"/> is
    /// set — disables the diagnostic entirely. Set before scripts run; a sink installed midway
    /// only sees throws raised after it.
    /// </summary>
    public static Action<JSThrowInfo> Sink
    {
        get => Volatile.Read(ref sink);
        set => Volatile.Write(ref sink, value);
    }

    /// <summary>Whether a sink is installed, and so whether <see cref="Report"/> will do work.</summary>
    public static bool Enabled => Volatile.Read(ref sink) != null;

    /// <summary>
    /// Hands one throw to the sink. Never throws: a diagnostic that can break the execution it is
    /// observing is worse than no diagnostic.
    /// </summary>
    internal static void Report(string kind, string message)
    {
        var current = Volatile.Read(ref sink);
        if (current == null || reporting)
            return;

        reporting = true;
        try
        {
            current(new JSThrowInfo(kind, message, CaptureJavaScriptStack()));
        }
        catch (Exception)
        {
            // Deliberately swallowed — see the summary.
        }
        finally
        {
            reporting = false;
        }
    }

    /// <summary>
    /// Reports <paramref name="message"/> and hands it straight back, so a throw site reports and
    /// raises in one expression:
    /// <c>throw NewTypeError(JSThrowDiagnostics.Reported(PropertyRead, $"…"))</c>.
    /// </summary>
    internal static string Reported(string kind, string message)
    {
        Report(kind, message);
        return message;
    }

    private static string CaptureJavaScriptStack()
    {
        try
        {
            var sb = new StringBuilder();
            JSException.AppendStackTraceHelper?.Invoke(sb, []);
            return sb.ToString();
        }
        catch (Exception)
        {
            // A stack is worth having but never worth failing for.
            return string.Empty;
        }
    }

    private static Action<JSThrowInfo> CreateDefaultSink()
    {
        string setting;
        try
        {
            setting = Environment.GetEnvironmentVariable(EnvironmentVariable);
        }
        catch (Exception)
        {
            // A restricted host can refuse the read; that is not a reason to fail type init.
            return null;
        }

        if (string.IsNullOrWhiteSpace(setting) || setting == "0")
            return null;

        return setting == "1" || string.Equals(setting, "stderr", StringComparison.OrdinalIgnoreCase)
            ? WriteToStandardError
            : CreateFileSink(setting);
    }

    private static void WriteToStandardError(JSThrowInfo info)
        => Console.Error.WriteLine(info.ToString());

    /// <summary>
    /// Appends each record to <paramref name="path"/> as it happens, rather than buffering, so a
    /// run that ends by crashing still leaves the throws that led up to it on disk.
    /// </summary>
    public static Action<JSThrowInfo> CreateFileSink(string path)
    {
        var gate = new object();
        return info =>
        {
            try
            {
                lock (gate)
                    File.AppendAllText(path, info.ToString() + Environment.NewLine);
            }
            catch (Exception)
            {
                // Deliberately swallowed — see Report.
            }
        };
    }
}

/// <summary>One reported throw: what it said, and where in the script it came from.</summary>
public readonly struct JSThrowInfo
{
    public JSThrowInfo(string kind, string message, string javaScriptStack)
    {
        Kind = kind;
        Message = message;
        JavaScriptStack = javaScriptStack;
    }

    /// <summary><see cref="JSThrowDiagnostics.PropertyRead"/> or <see cref="JSThrowDiagnostics.PropertyWrite"/>.</summary>
    public string Kind { get; }

    /// <summary>The message the TypeError carries, including the property name where one is known.</summary>
    public string Message { get; }

    /// <summary>
    /// The JavaScript frames at the point of the throw, innermost first, one
    /// <c>    at function:file:line,column</c> per line. Empty when no script frames were live.
    /// </summary>
    public string JavaScriptStack { get; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("[js-throw] ").Append(Kind).Append(": ").Append(Message);

        if (!string.IsNullOrEmpty(JavaScriptStack))
            sb.AppendLine().Append(JavaScriptStack.TrimEnd());

        return sb.ToString();
    }
}
