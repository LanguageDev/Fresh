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
/// Represents a source text that can be accessed by index and by position.
/// </summary>
public sealed class SourceText
{
    /// <summary>
    /// Creates a new <see cref="SourceText"/> from a name and the contents.
    /// </summary>
    /// <param name="name">The name of the source to create.</param>
    /// <param name="text">The contents of the source.</param>
    /// <returns>A new <see cref="SourceText"/> with the name <paramref name="name"/> and contents <paramref name="text"/>.</returns>
    public static SourceText FromString(string name, string text) => new(name, text);

    /// <summary>
    /// The name of this source.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Retrieves a reader for the source contents.
    /// </summary>
    public TextReader Reader => new StringReader(this.text);

    private readonly string text;
    private List<ReadOnlyMemory<char>>? lines;

    /// <summary>
    /// The list of lines in this source.
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<char>> Lines
    {
        get
        {
            this.lines ??= this.ComputeLines();
            return this.lines;
        }
    }

    private SourceText(string name, string text)
    {
        this.Name = name;
        this.text = text;
    }

    private List<ReadOnlyMemory<char>> ComputeLines()
    {
        var result = new List<ReadOnlyMemory<char>>();
        var lastOffset = 0;
        var prevLine = 0;
        var cursor = default(Cursor);
        for (var i = 0; i < this.text.Length; ++i)
        {
            cursor.Append(this.text[i]);
            if (cursor.Line != prevLine && cursor.Column != 0)
            {
                // We are at the next line
                prevLine = cursor.Line;
                result.Add(this.text.AsMemory(lastOffset..i));
                lastOffset = i;
            }
        }
        // Add the last one
        result.Add(this.text.AsMemory(lastOffset..));
        return result;
    }
}