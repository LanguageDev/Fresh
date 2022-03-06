
using System;
using System.IO;
using System.Linq;
using Fresh.Syntax;

internal class Program
{
    public static void Main(string[] args)
    {
        var source =
@"// File documentation comment
// Starts from the first line comment
// And comments stick together!

    hello

// This is a function
// With doc comment
func foo(x: int, y: bool): int {
    // And this is a comment inside
    var x: int = 1 < 2 < 3;
}
";
        var sourceText = SourceText.FromString("foo.fresh", source);
        var tokens = Lexer.Lex(sourceText);
        var syntaxTokens = SyntaxTokenLexer.ParseSyntaxTokens(tokens);
        var tree = Parser.Parse(syntaxTokens);
        Console.WriteLine(tree.ToDebugString());
        Console.WriteLine("\n==========\n");
        Console.WriteLine(tree.ToSourceText());
        Console.WriteLine("\n==========\n");
        var errors = tree.CollectErrors();
        foreach (var err in errors)
        {
            Console.WriteLine($"Syntax error [{PrettyLocation(err.Location)}]: {err.Message}");
        }
    }

    private static string PrettyLocation(Location location)
    {
        var start = location.Start;
        var end = location.End;
        return $"{location.Source.Name}:{start.Line}:{start.Column}";
    }
}
