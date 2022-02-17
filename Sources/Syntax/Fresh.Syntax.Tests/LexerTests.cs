// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fresh.Syntax.Tests;

public sealed class LexerTests
{
    public static IEnumerable<object[]> LexedTokens => new List<object[]>
    {
        new object[] { "", MakeToken(TokenType.End, "", "0:0:0-0:0:0") },
    };

    private static SourceText emptySource = SourceText.FromString(string.Empty, string.Empty);

    private static Token MakeToken(TokenType tt, string text, string pos)
    {
        var startAndEnd = pos.Split('-');
        var range = new Range(MakePosition(startAndEnd[0]), MakePosition(startAndEnd[1]));
        var loc = new Location(emptySource, range);
        return new(loc, tt, text);
    }

    private static Position MakePosition(string pos)
    {
        var parts = pos.Split(':');
        return new(line: int.Parse(parts[0]), column: int.Parse(parts[1]), index: int.Parse(parts[2]));
    }

    // NOTE: We ignore location
    private static bool TokenEquals(Token t1, Token t2) =>
           t1.Location.Range == t2.Location.Range
        && t1.Text == t2.Text
        && t1.Type == t2.Type;

    [MemberData(nameof(LexedTokens))]
    [Theory]
    public void TestLexedTokens(string input, params Token[] expected)
    {
        var got = Lexer.Lex(SourceText.FromString("", input)).ToList();
        Assert.Equal(expected.Length, got.Count);
        for (var i = 0; i < got.Count; ++i) Assert.True(TokenEquals(expected[i], got[i]));
    }
}
