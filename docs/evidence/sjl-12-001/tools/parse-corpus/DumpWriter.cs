using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ParseCorpus;

/// <summary>
/// Where a canonical dump is written: its UTF-8 bytes go straight into a SHA-256 and, when the run
/// keeps the text (<c>--text</c>), into a copy. Nothing holds the whole text, so a dump's size is
/// bounded by nothing but the tree's. One writer serves one thread, one dump at a time.
/// </summary>
internal sealed class DumpWriter : IDisposable
{
    private readonly IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private readonly char[] chars = new char[16 * 1024];
    private readonly byte[] bytes;
    private readonly Encoder encoder = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetEncoder();
    private Stream? copy;
    private int count;
    private char last = '\n';

    internal DumpWriter() => bytes = new byte[Encoding.UTF8.GetMaxByteCount(chars.Length)];

    /// <summary>Starts a dump, copied to <paramref name="copyTo"/> as well when it is given.</summary>
    internal void Start(Stream? copyTo)
    {
        copy = copyTo;
        count = 0;
        last = '\n';
        encoder.Reset();
        hash.GetHashAndReset();
    }

    /// <summary>Whether the text written so far ends a line (or is empty).</summary>
    internal bool AtLineStart => last == '\n';

    /// <summary>A line's head: its depth as a number, a space, and its label and a colon.</summary>
    internal DumpWriter Head(int depth, string label) => Append(depth).Append(' ').Append(label).Append(':');

    internal DumpWriter Append(char character)
    {
        if (count == chars.Length)
            Drain(flush: false);

        chars[count++] = character;
        last = character;
        return this;
    }

    internal DumpWriter Append(string text)
    {
        var from = 0;
        while (from < text.Length)
        {
            if (count == chars.Length)
                Drain(flush: false);

            var length = Math.Min(chars.Length - count, text.Length - from);
            text.CopyTo(from, chars, count, length);
            count += length;
            from += length;
        }

        if (text.Length > 0)
            last = text[^1];
        return this;
    }

    internal DumpWriter Append(int number) => Append(number.ToString(CultureInfo.InvariantCulture));

    /// <summary>Ends the dump: the SHA-256 of every byte written since <see cref="Start"/>, in lower-case hex.</summary>
    internal string Finish()
    {
        Drain(flush: true);
        copy = null;
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private void Drain(bool flush)
    {
        var length = encoder.GetBytes(chars, 0, count, bytes, 0, flush);
        hash.AppendData(bytes, 0, length);
        copy?.Write(bytes, 0, length);
        count = 0;
    }

    public void Dispose() => hash.Dispose();
}
