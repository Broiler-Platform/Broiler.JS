using System.Globalization;

#nullable enable

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public readonly struct Position(int line, int column)
{
    public readonly int Line = line;
    public readonly int Column = column;

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Line}, {Column}");
}
