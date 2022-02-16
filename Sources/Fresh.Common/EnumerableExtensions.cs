// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.Common;

/// <summary>
/// Extensions on enumerables.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Turns an enumerable into a <see cref="Sequence{T}"/>.
    /// </summary>
    /// <typeparam name="T">The sequence element type.</typeparam>
    /// <param name="enumerable">The enumerable to convert.</param>
    /// <returns>The elements of <paramref name="enumerable"/> in a <see cref="Sequence{T}"/>.</returns>
    public static Sequence<T> ToSequence<T>(this IEnumerable<T> enumerable) =>
        new(enumerable.ToImmutableArray());
}
