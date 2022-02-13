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
/// A simple utility that tracks line and column info by feeding in characters.
/// </summary>
public struct Cursor
{
    /// <summary>
    /// The current character index.
    /// </summary>
    public int Index { get; private set; }

    /// <summary>
    /// The line the cursor is at.
    /// </summary>
    public int Line { get; private set; }

    /// <summary>
    /// The column the cursor is at.
    /// </summary>
    public int Column { get; private set; }

    /// <summary>
    /// The current position the cursor is at.
    /// </summary>
    public Position Position => new(index: this.Index, line: this.Line, column: this.Column);

    private bool lastCr;

    /// <summary>
    /// Appends a character to the cursor, stepping it.
    /// </summary>
    /// <param name="ch">The character to append.</param>
    public void Append(char ch)
    {
        ++this.Index;
        if (ch == '\r')
        {
            // OS-X 9 newline (or the first part of Windows newline)
            ++this.Line;
            this.Column = 0;
            this.lastCr = true;
        }
        else if (ch == '\n')
        {
            // UNIX newline (or the second part of Windows newline)
            if (this.lastCr)
            {
                // It was a Windows newline, we do nothing, already stepped on '\r'
                this.lastCr = false;
            }
            else
            {
                // It's a UNIX newline
                ++this.Line;
                this.Column = 0;
            }
        }
        else if (ch == '\t' || !char.IsControl(ch))
        {
            // Some non-control character
            ++this.Column;
        }
    }
}
