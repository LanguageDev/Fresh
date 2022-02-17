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
        new object[]
        {
            "   ",
            MakeToken(TokenType.Whitespace, "   ", "0:0:0-0:3:3"),
            MakeToken(TokenType.End, "", "0:3:3-0:3:3"),
        },
        new object[]
        {
            "foo \nbar  \rbaz \t \r\n ",
            MakeToken(TokenType.Identifier, "foo", "0:0:0-0:3:3"),
            MakeToken(TokenType.Whitespace, " ", "0:3:3-0:4:4"),
            MakeToken(TokenType.Newline, "\n", "0:4:4-1:0:5"),
            MakeToken(TokenType.Identifier, "bar", "1:0:5-1:3:8"),
            MakeToken(TokenType.Whitespace, "  ", "1:3:8-1:5:10"),
            MakeToken(TokenType.Newline, "\r", "1:5:10-2:0:11"),
            MakeToken(TokenType.Identifier, "baz", "2:0:11-2:3:14"),
            MakeToken(TokenType.Whitespace, " \t ", "2:3:14-2:6:17"),
            MakeToken(TokenType.Newline, "\r\n", "2:6:17-3:0:19"),
            MakeToken(TokenType.Whitespace, " ", "3:0:19-3:1:20"),
            MakeToken(TokenType.End, "", "3:1:20-3:1:20"),
        },
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
    private static void AssertTokenEquals(Token expected, Token got)
    {
        Assert.Equal(expected.Location.Range, got.Location.Range);
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
