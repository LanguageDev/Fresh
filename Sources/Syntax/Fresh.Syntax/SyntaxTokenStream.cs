// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fresh.Common;

namespace Fresh.Syntax;

/// <summary>
/// Converts a stream of <see cref="Token"/>s into a stream of <see cref="SyntaxToken"/>s,
/// which is essentially tokens with trivia attacked to them.
/// </summary>
public sealed class SyntaxTokenStream
{
    private readonly IEnumerator<Token> tokens;
    private readonly RingBuffer<Token> peekBuffer = new();

    private Token Take()
    {
        if (!this.TryPeek(0, out _)) throw new InvalidOperationException($"Could nod take a token");
        return this.peekBuffer.RemoveFront();
    }

    private Sequence<Token> Take(int length)
    {
        if (length == 0) return Sequence<Token>.Empty;
        if (!this.TryPeek(length - 1, out _)) throw new InvalidOperationException($"Could nod take {length} amount");
        var result = new Token[length];
        for (var i = 0; i < length; ++i) result[i] = this.peekBuffer.RemoveFront();
        return new(result);
    }

    private bool TryPeek(int offset, [MaybeNullWhen(false)] out Token token)
    {
        // Read as long as there aren't enough tokens in the peek buffer
        while (this.peekBuffer.Count <= offset)
        {
            if (!this.tokens.MoveNext())
            {
                // No more to read
                token = default;
                return false;
            }
            // This token was read successfully
            this.peekBuffer.AddBack(this.tokens.Current);
        }
        // We have enough tokens in the buffer
        token = this.peekBuffer[offset];
        return true;
    }
}
