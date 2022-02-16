// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
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
    private readonly IEnumerator<Token> tokenSource;
    // Holds raw peeked tokens
    private readonly RingBuffer<Token> tokenPeekBuffer = new();
    // Holds peeked syntax tokens that have their trivia attached
    private readonly RingBuffer<SyntaxToken> syntaxTokenPeekBuffer = new();

    private Parser(IEnumerator<Token> tokenSource)
    {
        this.tokenSource = tokenSource;
    }
}
