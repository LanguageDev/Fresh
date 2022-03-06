// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.IO;
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

    /// <summary>
    /// Retrieves a location from the given offset.
    /// </summary>
    /// <param name="offset">The offset to get the location for.</param>
    /// <param name="width">The width of the location.</param>
    /// <returns>The location that is <paramref name="offset"/> characters away from the start of the source
    /// and spans <paramref name="width"/> characters.</returns>
    public Location GetLocation(int offset, int width) => new(this, this.GetRange(offset, width));

    /// <summary>
    /// Retrieves a range from the given offset.
    /// </summary>
    /// <param name="offset">The offset to get the range for.</param>
    /// <param name="width">The width of the range.</param>
    /// <returns>The range that is <paramref name="offset"/> characters away from the start of the source
    /// and spans <paramref name="width"/> characters.</returns>
    public Range GetRange(int offset, int width)
    {
        // TODO: Very inefficient but good enough for now.
        var cursor = default(Cursor);
        // Walk up to the start
        offset = Math.Min(offset, this.text.Length);
        for (var i = 0; i < offset && i < offset; ++i) cursor.Append(this.text[i]);
        // Save the start position
        var start = cursor.Position;
        // Walk to the end
        var endOffset = Math.Min(offset + width, this.text.Length);
        for (var i = offset; i < endOffset; ++i) cursor.Append(this.text[i]);
        // Save the end position
        var end = cursor.Position;
        // Construct location
        return new(start, end);
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
