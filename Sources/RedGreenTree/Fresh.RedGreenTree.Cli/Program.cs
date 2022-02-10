using System;
using System.IO;
using Fresh.RedGreenTree.Cli.Model;

namespace Fresh.RedGreenTree.Cli;

internal static class Program
{
    private static readonly string testXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Tree Root=""SyntaxNode"" Namespace=""Fresh.Test"" Factory=""SyntaxFactory"">
    <Using Namespace=""System"" />
    <Using Namespace=""System.Collections.Generic"" />

    <Primitive Name=""Token"" />
    <Primitive Name=""List[T]"" />

    <Node Name=""SyntaxNode"" Abstract=""true"" />

    <Node Name=""StatementSyntax"" Base=""SyntaxNode"" IsAbstract=""true"" />
    <Node Name=""DeclarationSyntax"" Base=""StatementSyntax"" IsAbstract=""true"" />
    <Node Name=""ExpressionSyntax"" Base=""SyntaxNode"" IsAbstract=""true"" />

    <Node Name=""LiteralSyntax"" Base=""ExpressionSyntax"">
        <Attribute Name=""Value"" Type=""Token"" />
    </Node>

    <Node Name=""IdentifierSyntax"" Base=""ExpressionSyntax"">
        <Attribute Name=""Name"" Type=""Token"" />
    </Node>

    <Node Name=""ProgramSyntax"" Base=""StatementSyntax"">
        <Attribute Name=""Declarations"" Type=""List[DeclarationSyntax]"" />
    </Node>

    <Node Name=""FunctionDefinitionSyntax"" Base=""DeclarationSyntax"">
        <Attribute Name=""FuncKeyword"" Type=""Token"" />
        <Attribute Name=""Name"" Type=""Token"" />
        <Attribute Name=""OpenParen"" Type=""Token"" />
        <Attribute Name=""CloseParen"" Type=""Token"" />
        <Attribute Name=""Body"" Type=""StatementSyntax"" />
    </Node>
</Tree>
";

    internal static void Main(string[] args)
    {
        var tree = XmlConverter.Convert(testXml);
        var code = CodeGenerator.Generate(tree);
        Console.WriteLine(code);
    }
}
