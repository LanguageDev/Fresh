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
        new object[] { "", MakeToken(TokenType.End, "") },
        new object[]
        {
            "   ",
            MakeToken(TokenType.Whitespace, "   "),
            MakeToken(TokenType.End, ""),
        },
        new object[]
        {
            "foo \nbar  \rbaz \t \r\n ",
            MakeToken(TokenType.Identifier, "foo"),
            MakeToken(TokenType.Whitespace, " "),
            MakeToken(TokenType.Newline, "\n"),
            MakeToken(TokenType.Identifier, "bar"),
            MakeToken(TokenType.Whitespace, "  "),
            MakeToken(TokenType.Newline, "\r"),
            MakeToken(TokenType.Identifier, "baz"),
            MakeToken(TokenType.Whitespace, " \t "),
            MakeToken(TokenType.Newline, "\r\n"),
            MakeToken(TokenType.Whitespace, " "),
            MakeToken(TokenType.End, ""),
        },
    };

    private static SourceText emptySource = SourceText.FromString(string.Empty, string.Empty);

    private static Token MakeToken(TokenType tt, string text) => new(emptySource, tt, text);

    private static Position MakePosition(string pos)
    {
        var parts = pos.Split(':');
        return new(line: int.Parse(parts[0]), column: int.Parse(parts[1]), index: int.Parse(parts[2]));
    }

    // NOTE: We ignore location
    private static void AssertTokenEquals(Token expected, Token got)
    {
        // Assert.Equal(expected.Location.Range, got.Location.Range);
        Assert.Equal(expected.Text, got.Text);
        Assert.Equal(expected.Type, got.Type);
    }

    [MemberData(nameof(LexedTokens))]
    [Theory]
    public void TestLexedTokens(string input, params Token[] expected)
    {
        var got = Lexer.Lex(SourceText.FromString("", input)).ToList();
        Assert.Equal(expected.Length, got.Count);
        for (var i = 0; i < got.Count; ++i) AssertTokenEquals(expected[i], got[i]);
    }
}
