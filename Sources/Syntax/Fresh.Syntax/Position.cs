using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.Syntax;

/// <summary>
/// Represents a position in a source text.
/// </summary>
public readonly record struct Position : IComparable<Position>
{
    /// <summary>
    /// The 0-based index the position represents.
    /// </summary>
    public readonly int Index;

    /// <summary>
    /// The 0-based line index the position represents.
    /// </summary>
    public readonly int Line;

    /// <summary>
    /// The 0-based column index the position represents.
    /// </summary>
    public readonly int Column;

    public Position(int index, int line, int column)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        if (line < 0) throw new ArgumentOutOfRangeException(nameof(line));
        if (column < 0) throw new ArgumentOutOfRangeException(nameof(column));

        this.Index = index;
        this.Line = line;
        this.Column = column;
    }

    public int CompareTo(Position other) => this.Index.CompareTo(other.Index);

    public static bool operator <(Position left, Position right) => left.CompareTo(right) < 0;

    public static bool operator <=(Position left, Position right) => left.CompareTo(right) <= 0;

    public static bool operator >(Position left, Position right) => left.CompareTo(right) > 0;

    public static bool operator >=(Position left, Position right) => left.CompareTo(right) >= 0;
}
