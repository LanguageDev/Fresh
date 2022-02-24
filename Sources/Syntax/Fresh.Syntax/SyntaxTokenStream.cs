// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// <summary>
    /// Processes a sequence of raw <see cref="Token"/>s into a sequence of <see cref="SyntaxToken"/>s
    /// that contain trivia information.
    /// </summary>
    /// <param name="tokens">The sequence of tokens to process.</param>
    /// <returns>The sequence of syntax tokens made from <paramref name="tokens"/>.</returns>
    public static IEnumerable<SyntaxToken> Process(IEnumerable<Token> tokens)
    {
        var stream = new SyntaxTokenStream(tokens.GetEnumerator());
        while (true)
        {
            var t = stream.Next();
            yield return t;
            if (t.Token.Type == TokenType.End) break;
        }
    }

    private readonly IEnumerator<Token> tokens;
    private readonly RingBuffer<Token> peekBuffer = new();

    private SyntaxTokenStream(IEnumerator<Token> tokens)
    {
        this.tokens = tokens;
    }

    /// <summary>
    /// Retrieves the next <see cref="SyntaxToken"/>.
    /// </summary>
    /// <returns>The next <see cref="SyntaxToken"/> with leading and trailing trivia.</returns>
    public SyntaxToken Next()
    {
        // We collect trivia tokens as long as possible, this will be our leading trivial
        var offs = 0;
        for (; this.TryPeek(offs, out var t) && t.IsTrivia; ++offs) ;
        var leadingTrivia = offs > 0 ? this.Take(offs) : Sequence<Token>.Empty;
        // We now expect a token, which is not a trivia token
        // If we don't get a token here, it means something went terribly wrong
        // This is because all trivia tokens must have been attached to some token before EOF.
        var token = this.Take();
        Debug.Assert(!token.IsTrivia);
        // Now we start collecting trailing trivia as long as possible
        // If we find a token later that's not trivia, we only consider trailing trivia inline with the original token
        // We first consume trivia until the end of line
        offs = 0;
        var cursor = default(Cursor);
        for (; cursor.Line == 0
            && this.TryPeek(offs, out var t)
            && t.IsTrivia;
            cursor.Append(t.Text), ++offs) ;
        // Now we can save the end of line
        var endOfLineOffs = offs;
        // Now we continue until we either hit the EOF (all is trivia) or a non-trivia token (only until end of line)
        while (true)
        {
            if (!this.TryPeek(offs, out var t) || t.Type == TokenType.End)
            {
                // EOF hit, all is trivia
                var trailingTrivia = offs > 0 ? this.Take(offs) : Sequence<Token>.Empty;
                return new(leadingTrivia, token, trailingTrivia);
            }
            if (!t.IsTrivia)
            {
                // Not a trivia anymore, only consume until end of line
                var trailingTrivia = endOfLineOffs > 0 ? this.Take(endOfLineOffs) : Sequence<Token>.Empty;
                return new(leadingTrivia, token, trailingTrivia);
            }
            // Still trivia
            ++offs;
        }
    }

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
