using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.Common;

/// <summary>
/// An immutable sequence compared and hashed by value (per elements) instead of by reference.
/// </summary>
/// <typeparam name="T">The sequence element type.</typeparam>
/// <param name="Elements">The backing sequence.</param>
public readonly record struct Sequence<T>(IReadOnlyList<T> Elements) : IReadOnlyList<T>
{
    public int Count => this.Elements.Count;

    public T this[int index] => this.Elements[index];

    public override string ToString() => $"[{string.Join(", ", this.Elements)}]";

    public bool Equals(Sequence<T> other)
    {
        if (this.Elements.Count != other.Elements.Count) return false;
        return this.Elements.SequenceEqual(other.Elements);
    }

    public override int GetHashCode()
    {
        var h = default(HashCode);
        foreach (var item in this.Elements) h.Add(item);
        return h.ToHashCode();
    }

    public IEnumerator<T> GetEnumerator() => this.Elements.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.Elements.GetEnumerator();
}
