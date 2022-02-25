
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

// This is a function
// With doc comment
func foo(x: int, y: bool): int {
    // And this is a comment inside
}
";
        var sourceText = SourceText.FromString("foo.fresh", source);
        var tokens = Lexer.Lex(sourceText);
        var syntaxTokens = SyntaxTokenStream.Process(tokens);
        var tree = Parser.Parse(syntaxTokens);
        Console.WriteLine(tree.ToDebugString());
        Console.WriteLine("\n==========\n");
        Console.WriteLine(tree.ToSourceText());
    }
}
