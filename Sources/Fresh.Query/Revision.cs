// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;

namespace Fresh.Query;

/// <summary>
/// Represents a revision/version number in the system.
/// </summary>
/// <param name="Number"></param>
public readonly record struct Revision(int Number) : IComparable<Revision>
{
    /// <summary>
    /// An invalid revision number.
    /// </summary>
    public static readonly Revision Invalid = new(-1);

    /// <summary>
    /// The maximum possible version number.
    /// </summary>
    public static readonly Revision MaxValue = new(int.MaxValue);

    /// <inheritdoc/>
    public int CompareTo(Revision other) => this.Number - other.Number;

    public static bool operator <(Revision a, Revision b) => a.CompareTo(b) < 0;
    public static bool operator >(Revision a, Revision b) => a.CompareTo(b) > 0;
    public static bool operator <=(Revision a, Revision b) => a.CompareTo(b) <= 0;
    public static bool operator >=(Revision a, Revision b) => a.CompareTo(b) >= 0;
}
