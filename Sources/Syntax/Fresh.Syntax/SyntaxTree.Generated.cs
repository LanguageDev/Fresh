using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fresh.Common;

#nullable enable

namespace Fresh.Syntax;

/// <summary>
/// The base for all statement syntax nodes.
/// </summary>
public abstract partial class StatementSyntax : SyntaxNode
{
    /// <inheritdoc/>
    public override Sequence<Token> LeadingTrivia { get; }

    /// <inheritdoc/>
    public override Sequence<Token> TrailingTrivia { get; }
}

/// <summary>
/// The base for all expression syntax nodes.
/// </summary>
public abstract partial class ExpressionSyntax : SyntaxNode
{
    /// <inheritdoc/>
    public override Sequence<Token> LeadingTrivia { get; }

    /// <inheritdoc/>
    public override Sequence<Token> TrailingTrivia { get; }
}

/// <summary>
/// The base for all declaration syntax nodes.
/// </summary>
public abstract partial class DeclarationSyntax : StatementSyntax
{
    /// <inheritdoc/>
    public override Sequence<Token> LeadingTrivia { get; }

    /// <inheritdoc/>
    public override Sequence<Token> TrailingTrivia { get; }
}

/// <summary>
/// A full, parsed file containing all of its declarations.
/// </summary>
public sealed partial class FileDeclarationSyntax : DeclarationSyntax
{
    /// <inheritdoc/>
    public override Sequence<Token> LeadingTrivia { get; }

    /// <summary>
    /// The declarations contained in the file.
    /// </summary>
    public Sequence<DeclarationSyntax> Declarations { get; }

    /// <summary>
    /// The end of file token.
    /// </summary>
    public SyntaxToken End { get; }

    /// <inheritdoc/>
    public override Sequence<Token> TrailingTrivia { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDeclarationSyntax"/> class.
    /// </summary>
    /// <param name="leadingTrivia">The leading trivia for this node.</param>
    /// <param name="declarations">The declarations contained in the file.</param>
    /// <param name="end">The end of file token.</param>
    /// <param name="trailingTrivia">The trailing trivia for this node.</param>
    internal FileDeclarationSyntax(
        Sequence<Token> leadingTrivia,
        Sequence<DeclarationSyntax> declarations,
        SyntaxToken end,
        Sequence<Token> trailingTrivia)
    {
        this.LeadingTrivia = leadingTrivia;
        this.Declarations = declarations;
        this.End = end;
        this.TrailingTrivia = trailingTrivia;
    }

    /// <inheritdoc/>
    public override bool Equals(SyntaxNode? other) =>
           other is FileDeclarationSyntax o
        && this.LeadingTrivia.Equals(o.LeadingTrivia)
        && this.Declarations.Equals(o.Declarations)
        && this.End.Equals(o.End)
        && this.TrailingTrivia.Equals(o.TrailingTrivia);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(
        this.LeadingTrivia,
        this.Declarations,
        this.End,
        this.TrailingTrivia);

    /// <inheritdoc/>
    public override IEnumerable<KeyValuePair<string, object?>> Children
    {
        get
        {
            yield return new("LeadingTrivia", this.LeadingTrivia);
            yield return new("Declarations", this.Declarations);
            yield return new("End", this.End);
            yield return new("TrailingTrivia", this.TrailingTrivia);
        }
    }
}

/// <summary>
/// A function declaration.
/// </summary>
public sealed partial class FunctionDeclarationSyntax : DeclarationSyntax
{
    /// <inheritdoc/>
    public override Sequence<Token> LeadingTrivia { get; }

    /// <summary>
    /// The function keyword.
    /// </summary>
    public SyntaxToken FuncKeyword { get; }

    /// <summary>
    /// The name of the function.
    /// </summary>
    public SyntaxToken Name { get; }

    /// <summary>
    /// The parameters of the function.
    /// </summary>
    public ParameterListSyntax ParameterList { get; }

    /// <summary>
    /// The body of the function.
    /// </summary>
    public ExpressionSyntax Body { get; }

    /// <inheritdoc/>
    public override Sequence<Token> TrailingTrivia { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionDeclarationSyntax"/> class.
    /// </summary>
    /// <param name="leadingTrivia">The leading trivia for this node.</param>
    /// <param name="funcKeyword">The function keyword.</param>
    /// <param name="name">The name of the function.</param>
    /// <param name="parameterList">The parameters of the function.</param>
    /// <param name="body">The body of the function.</param>
    /// <param name="trailingTrivia">The trailing trivia for this node.</param>
    internal FunctionDeclarationSyntax(
        Sequence<Token> leadingTrivia,
        SyntaxToken funcKeyword,
        SyntaxToken name,
        ParameterListSyntax parameterList,
        ExpressionSyntax body,
        Sequence<Token> trailingTrivia)
    {
        this.LeadingTrivia = leadingTrivia;
        this.FuncKeyword = funcKeyword;
        this.Name = name;
        this.ParameterList = parameterList;
        this.Body = body;
        this.TrailingTrivia = trailingTrivia;
    }

    /// <inheritdoc/>
    public override bool Equals(SyntaxNode? other) =>
           other is FunctionDeclarationSyntax o
        && this.LeadingTrivia.Equals(o.LeadingTrivia)
        && this.FuncKeyword.Equals(o.FuncKeyword)
        && this.Name.Equals(o.Name)
        && this.ParameterList.Equals(o.ParameterList)
        && this.Body.Equals(o.Body)
        && this.TrailingTrivia.Equals(o.TrailingTrivia);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(
        this.LeadingTrivia,
        this.FuncKeyword,
        this.Name,
        this.ParameterList,
        this.Body,
        this.TrailingTrivia);

    /// <inheritdoc/>
    public override IEnumerable<KeyValuePair<string, object?>> Children
    {
        get
        {
            yield return new("LeadingTrivia", this.LeadingTrivia);
            yield return new("FuncKeyword", this.FuncKeyword);
            yield return new("Name", this.Name);
            yield return new("ParameterList", this.ParameterList);
            yield return new("Body", this.Body);
            yield return new("TrailingTrivia", this.TrailingTrivia);
        }
    }
}

/// <summary>
/// A list of parameters for a function declaration.
/// </summary>
public sealed partial class ParameterListSyntax : SyntaxNode
{
    /// <inheritdoc/>
    public override Sequence<Token> LeadingTrivia { get; }

    /// <summary>
    /// The open parenthesis token.
    /// </summary>
    public SyntaxToken OpenParenthesis { get; }

    /// <summary>
    /// The close parenthesis token.
    /// </summary>
    public SyntaxToken CloseParenthesis { get; }

    /// <inheritdoc/>
    public override Sequence<Token> TrailingTrivia { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterListSyntax"/> class.
    /// </summary>
    /// <param name="leadingTrivia">The leading trivia for this node.</param>
    /// <param name="openParenthesis">The open parenthesis token.</param>
    /// <param name="closeParenthesis">The close parenthesis token.</param>
    /// <param name="trailingTrivia">The trailing trivia for this node.</param>
    internal ParameterListSyntax(
        Sequence<Token> leadingTrivia,
        SyntaxToken openParenthesis,
        SyntaxToken closeParenthesis,
        Sequence<Token> trailingTrivia)
    {
        this.LeadingTrivia = leadingTrivia;
        this.OpenParenthesis = openParenthesis;
        this.CloseParenthesis = closeParenthesis;
        this.TrailingTrivia = trailingTrivia;
    }

    /// <inheritdoc/>
    public override bool Equals(SyntaxNode? other) =>
           other is ParameterListSyntax o
        && this.LeadingTrivia.Equals(o.LeadingTrivia)
        && this.OpenParenthesis.Equals(o.OpenParenthesis)
        && this.CloseParenthesis.Equals(o.CloseParenthesis)
        && this.TrailingTrivia.Equals(o.TrailingTrivia);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(
        this.LeadingTrivia,
        this.OpenParenthesis,
        this.CloseParenthesis,
        this.TrailingTrivia);

    /// <inheritdoc/>
    public override IEnumerable<KeyValuePair<string, object?>> Children
    {
        get
        {
            yield return new("LeadingTrivia", this.LeadingTrivia);
            yield return new("OpenParenthesis", this.OpenParenthesis);
            yield return new("CloseParenthesis", this.CloseParenthesis);
            yield return new("TrailingTrivia", this.TrailingTrivia);
        }
    }
}

/// <summary>
/// A code block expression.
/// </summary>
public sealed partial class BlockExpressionSyntax : ExpressionSyntax
{
    /// <inheritdoc/>
    public override Sequence<Token> LeadingTrivia { get; }

    /// <summary>
    /// The open brace token.
    /// </summary>
    public SyntaxToken OpenBrace { get; }

    /// <summary>
    /// The close brace token.
    /// </summary>
    public SyntaxToken CloseBrace { get; }

    /// <inheritdoc/>
    public override Sequence<Token> TrailingTrivia { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockExpressionSyntax"/> class.
    /// </summary>
    /// <param name="leadingTrivia">The leading trivia for this node.</param>
    /// <param name="openBrace">The open brace token.</param>
    /// <param name="closeBrace">The close brace token.</param>
    /// <param name="trailingTrivia">The trailing trivia for this node.</param>
    internal BlockExpressionSyntax(
        Sequence<Token> leadingTrivia,
        SyntaxToken openBrace,
        SyntaxToken closeBrace,
        Sequence<Token> trailingTrivia)
    {
        this.LeadingTrivia = leadingTrivia;
        this.OpenBrace = openBrace;
        this.CloseBrace = closeBrace;
        this.TrailingTrivia = trailingTrivia;
    }

    /// <inheritdoc/>
    public override bool Equals(SyntaxNode? other) =>
           other is BlockExpressionSyntax o
        && this.LeadingTrivia.Equals(o.LeadingTrivia)
        && this.OpenBrace.Equals(o.OpenBrace)
        && this.CloseBrace.Equals(o.CloseBrace)
        && this.TrailingTrivia.Equals(o.TrailingTrivia);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(
        this.LeadingTrivia,
        this.OpenBrace,
        this.CloseBrace,
        this.TrailingTrivia);

    /// <inheritdoc/>
    public override IEnumerable<KeyValuePair<string, object?>> Children
    {
        get
        {
            yield return new("LeadingTrivia", this.LeadingTrivia);
            yield return new("OpenBrace", this.OpenBrace);
            yield return new("CloseBrace", this.CloseBrace);
            yield return new("TrailingTrivia", this.TrailingTrivia);
        }
    }
}

/// <summary>
/// Provides factory methods for the syntax nodes.
/// </summary>
public static partial class SyntaxFactory
{
    /// <summary>
    /// Constructs a <see cref="FileDeclarationSyntax"/> from the given arguments.
    /// </summary>
    /// <param name="declarations">The declarations contained in the file.</param>
    /// <param name="end">The end of file token.</param>
    public static FileDeclarationSyntax FileDeclaration(
        Sequence<DeclarationSyntax> declarations,
        SyntaxToken end) => new FileDeclarationSyntax(
        Sequence<Token>.Empty,
        declarations,
        end,
        Sequence<Token>.Empty);

    /// <summary>
    /// Constructs a <see cref="FunctionDeclarationSyntax"/> from the given arguments.
    /// </summary>
    /// <param name="funcKeyword">The function keyword.</param>
    /// <param name="name">The name of the function.</param>
    /// <param name="parameterList">The parameters of the function.</param>
    /// <param name="body">The body of the function.</param>
    public static FunctionDeclarationSyntax FunctionDeclaration(
        SyntaxToken funcKeyword,
        SyntaxToken name,
        ParameterListSyntax parameterList,
        ExpressionSyntax body) => new FunctionDeclarationSyntax(
        Sequence<Token>.Empty,
        funcKeyword,
        name,
        parameterList,
        body,
        Sequence<Token>.Empty);

    /// <summary>
    /// Constructs a <see cref="ParameterListSyntax"/> from the given arguments.
    /// </summary>
    /// <param name="openParenthesis">The open parenthesis token.</param>
    /// <param name="closeParenthesis">The close parenthesis token.</param>
    public static ParameterListSyntax ParameterList(
        SyntaxToken openParenthesis,
        SyntaxToken closeParenthesis) => new ParameterListSyntax(
        Sequence<Token>.Empty,
        openParenthesis,
        closeParenthesis,
        Sequence<Token>.Empty);

    /// <summary>
    /// Constructs a <see cref="BlockExpressionSyntax"/> from the given arguments.
    /// </summary>
    /// <param name="openBrace">The open brace token.</param>
    /// <param name="closeBrace">The close brace token.</param>
    public static BlockExpressionSyntax BlockExpression(
        SyntaxToken openBrace,
        SyntaxToken closeBrace) => new BlockExpressionSyntax(
        Sequence<Token>.Empty,
        openBrace,
        closeBrace,
        Sequence<Token>.Empty);
}
/// <summary>
/// Provides extension methods for the syntax nodes.
/// </summary>
public static partial class SyntaxNodeExtensions
{
}

#nullable restore
