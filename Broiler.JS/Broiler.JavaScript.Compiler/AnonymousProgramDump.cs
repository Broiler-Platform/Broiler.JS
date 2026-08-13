using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Writes out the source of programs that have no script of their own — <c>eval</c>, and the
/// <c>Function</c> constructor's body — so a stack frame naming one can be read against its code.
/// </summary>
/// <remarks>
/// <para>
/// Numbering those programs made a trace through a module loader attributable: frames say
/// <c>vm16.js</c> now, rather than every one of them saying <c>vm.js</c>. But a name is only half
/// of it. Knowing a <c>b is not defined</c> came from <c>vm16.js:1,14</c> still does not say what
/// <c>vm16.js</c> *is*, and a payload a loader evaluated exists nowhere on disk to go and look at.
/// This closes that gap: each program is written to a file named exactly as the frames name it.
/// </para>
/// <para>
/// Off unless <c>BROILER_JS_DUMP_PROGRAMS</c> names a directory, because page script is page
/// content: dumping it by default would write whatever a page evaluates — including anything
/// personal a response embedded — to disk on every render. Opt-in keeps that a deliberate act.
/// A failure to write is swallowed, because a diagnostic must never be able to break the
/// execution it is observing.
/// </para>
/// </remarks>
public static class AnonymousProgramDump
{
    /// <summary>Names the directory to write programs to. Unset disables the dump.</summary>
    public const string EnvironmentVariable = "BROILER_JS_DUMP_PROGRAMS";

    private static string directory = Environment.GetEnvironmentVariable(EnvironmentVariable) ?? string.Empty;

    /// <summary>
    /// Directory the programs are written to; empty disables the dump. Defaults from
    /// <see cref="EnvironmentVariable"/> and is settable so a test can drive it without the
    /// environment, as the other compiler switches are.
    /// </summary>
    public static string Directory
    {
        get => Volatile.Read(ref directory);
        set => Volatile.Write(ref directory, value ?? string.Empty);
    }

    /// <summary>Whether a dump directory is configured.</summary>
    public static bool Enabled => Directory.Length != 0;

    /// <summary>
    /// Writes <paramref name="source"/> to <c>&lt;directory&gt;/&lt;name&gt;</c>.
    /// <paramref name="name"/> is the compiler-assigned program name, so the file is called
    /// exactly what the stack frames call it.
    /// </summary>
    public static void Write(string name, string source)
    {
        var target = Directory;
        if (target.Length == 0 || string.IsNullOrEmpty(name))
            return;

        try
        {
            System.IO.Directory.CreateDirectory(target);
            // The name is generated, but a path separator reaching Path.Combine would escape the
            // dump directory, and a diagnostic has no business writing outside the one it was given.
            var safe = name.Replace('/', '_').Replace('\\', '_');
            File.WriteAllText(Path.Combine(target, safe), source ?? string.Empty, Encoding.UTF8);
        }
        catch (Exception)
        {
            // Deliberately swallowed - see the remarks above.
        }
    }
}
