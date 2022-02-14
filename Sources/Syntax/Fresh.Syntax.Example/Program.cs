
using System;
using System.Linq;
using Fresh.Syntax;

internal class Program
{
    public static void Main(string[] args)
    {
        var source = @"
func foo() {
    // hello func foo
    var a 0;
    @
}
";
        var sourceText = SourceText.FromString("foo.fresh", source);
        var tokens = Lexer.Lex(sourceText).Select(StringifyToken);
        Console.WriteLine(string.Join("\n", tokens));
    }

    private static string StringifyToken(Token token) =>
        $"{token.Type} - '{token.Text}' ({StringifyPosition(token.Location.Range.Start)})-({StringifyPosition(token.Location.Range.End)})";

    private static string StringifyPosition(Position position) =>
        $"line: {position.Line} column: {position.Column}";
}
