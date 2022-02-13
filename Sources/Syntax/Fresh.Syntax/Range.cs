// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.Syntax;

/// <summary>
/// Represents a range in some source text.
/// </summary>
public readonly record struct Range
{
    /// <summary>
    /// The inclusive start of this range.
    /// </summary>
    public readonly Position Start;

    /// <summary>
    /// The exclusive end of this range.
    /// </summary>
    public readonly Position End;

    public Range(Position start, Position end)
    {
        if (end < start) throw new ArgumentOutOfRangeException(nameof(end), "The end position can not come before the start.");
        this.Start = start;
        this.End = end;
    }
}
