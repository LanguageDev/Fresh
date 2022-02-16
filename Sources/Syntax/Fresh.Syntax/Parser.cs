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
/// Produces a syntax tree from a sequence of tokens.
/// </summary>
public sealed class Parser
{
    private readonly IEnumerator<SyntaxToken> tokens;
    private readonly RingBuffer<SyntaxToken> peekBuffer = new();

    private Parser(IEnumerator<SyntaxToken> tokenSource)
    {
        this.tokens = tokenSource;
    }

    private SyntaxToken Take()
    {
        if (!this.TryPeek(0, out _)) throw new InvalidOperationException($"Could nod take a token");
        return this.peekBuffer.RemoveFront();
    }

    private bool TryPeek(int offset, [MaybeNullWhen(false)] out SyntaxToken token)
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
