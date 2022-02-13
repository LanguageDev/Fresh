using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fresh.Syntax.Tests;

public sealed class SourceTextTests
{
    [InlineData("", new string[]{ "" })]
    [InlineData("abc", new string[]{ "abc" })]
    [InlineData("abc\ndef", new string[]{ "abc\n", "def" })]
    [InlineData("abc\ndef\n", new string[]{ "abc\n", "def\n" })]
    [InlineData("abc\ndef\nghi", new string[]{ "abc\n", "def\n", "ghi" })]
    [InlineData("abc\ndef\nghi\n", new string[]{ "abc\n", "def\n", "ghi\n" })]
    [InlineData("abc\r\ndef\rghi\n", new string[] { "abc\r\n", "def\r", "ghi\n" })]
    [InlineData("abc\ndef\r\nghi\r", new string[] { "abc\n", "def\r\n", "ghi\r" })]
    [Theory]
    public void TestLines(string text, string[] lines)
    {
        var source = SourceText.FromString("file", text);
        var gotLines = source.Lines.Select(l => l.ToString()).ToList();
        Assert.True(lines.SequenceEqual(gotLines));
    }
}
