
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
func foo() {
    // And this is a comment inside
}
";
        var sourceText = SourceText.FromString("foo.fresh", source);
        var tokens = Lexer.Lex(sourceText);
        var syntaxTokens = SyntaxTokenStream.Process(tokens);
        var tree = Parser.Parse(syntaxTokens);
        Console.WriteLine(tree.ToDebugString());
        Console.WriteLine(tree.ToSourceText());
    }

    private static string PrintNode(SyntaxNode n) => n switch
    {
        FileDeclarationSyntax s => $"{string.Join("", s.Declarations.Select(PrintNode))}{PrintToken(s.End)}",
        FunctionDeclarationSyntax f => $"{PrintToken(f.FuncKeyword)}{PrintToken(f.Name)}{PrintNode(f.ParameterList)}{PrintNode(f.Body)}",
        ParameterListSyntax p => $"{PrintToken(p.OpenParenthesis)}{PrintToken(p.CloseParenthesis)}",
        BlockExpressionSyntax b => $"{PrintToken(b.OpenBrace)}{PrintToken(b.CloseBrace)}",
    };

    private static string PrintToken(SyntaxToken t) =>
        $"{string.Join("", t.LeadingTrivia.Select(t => t.Text))}{t.Token.Text}{string.Join("", t.TrailingTrivia.Select(t => t.Text))}";
}
