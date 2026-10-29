#nullable enable

using System;
using Broiler.Unicode.Algorithms;

namespace Broiler.JavaScript.Runtime.Unicode;

/// <summary>
/// This engine's Unicode tables, as the owner's algorithms read them (decision SJL-D-6 (c), as
/// Broiler.VM's JSD-0074 applies it): the tables <c>Broiler.Unicode.Generation</c> writes from this
/// repository's own pin, <c>data/unicode/pins/unicode.pin</c>, embedded in this assembly as spans over
/// constant data.
/// </summary>
/// <remarks>
/// <para>
/// <c>JSUnicodeTableData.g.cs</c> beside this file is generated, and
/// <c>Broiler.JavaScript.Ast.Tests</c>' <c>UnicodeTableRegenerationTests</c> regenerates it from the
/// pinned archive and holds it equal byte for byte (the shared JavaScript libraries programme's rule SL7,
/// in its shape for this engine). A change belongs in the generator or the pin, never in the file.
/// </para>
/// <para>
/// The tables are held by <c>Broiler.JavaScript.Ast</c>, below the runtime and the parser, so that the
/// parser reads them without referencing the runtime (milestone SJL-12.5, Broiler.VM's JSD-0080,
/// decision D20); they moved here from <c>Broiler.JavaScript.Runtime</c> with their namespace. They are
/// the default provider of the parser's options: a <c>FastParserOptions</c> whose provider is null scans
/// over them.
/// </para>
/// <para>
/// The case-folding tables are included for Broiler.Regex, which consumes the engine's provider
/// for Unicode and non-Unicode case-insensitive matching.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0074 SJL-D-6 (c) table provider; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
// Broiler-Falsified-If: a table the owner's algorithms read answers bytes other than the generator's output from data/unicode/pins
// Broiler-Human:        PENDING
internal sealed class JSUnicodeTables : IUnicodeTables
{
    /// <summary>The Unicode version of the embedded tables: the pin's.</summary>
    internal const string Version = "17.0.0";

    private JSUnicodeTables()
    {
    }

    /// <summary>The one instance; it holds nothing, and every table it answers is constant data.</summary>
    internal static JSUnicodeTables Instance { get; } = new();

    /// <inheritdoc/>
    public string UnicodeVersion => Version;

    /// <inheritdoc/>
    public ReadOnlySpan<byte> Table(UnicodeTable table) => JSUnicodeTableData.Get(table);
}
