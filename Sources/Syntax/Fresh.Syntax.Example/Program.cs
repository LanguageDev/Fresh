
using System;
using System.Linq;
using Fresh.Syntax;

internal class Program
{
    public static void Main(string[] args)
    {
        var source = @"
// This is sticky
// To func
func foo() {

    // hello func foo
    var a 0; // This goes to semicolon
    @
}
// This goes to the close brace
";
        var sourceText = SourceText.FromString("foo.fresh", source);
        var tokens = Lexer.Lex(sourceText);
        var syntaxTokens = SyntaxTokenStream.Process(tokens);
        foreach (var t in syntaxTokens)
        {
            Console.WriteLine("{");
            Console.WriteLine("  Leading trivia:");
            foreach (var l in t.LeadingTrivia) Console.WriteLine($"    {StringifyToken(l)}");
            Console.WriteLine($"  Token: {StringifyToken(t.Token)}");
            Console.WriteLine("  Trailing trivia:");
            foreach (var l in t.TrailingTrivia) Console.WriteLine($"    {StringifyToken(l)}");
            Console.WriteLine("}");
        }
    }

    private static string StringifyToken(Token token) =>
        $"{token.Type} - '{token.Text}' ({StringifyPosition(token.Location.Range.Start)})-({StringifyPosition(token.Location.Range.End)})";

    private static string StringifyPosition(Position position) =>
        $"line: {position.Line} column: {position.Column}";
}
