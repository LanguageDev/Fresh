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
}

/// <summary>
/// The base for all expression syntax nodes.
/// </summary>
public abstract partial class ExpressionSyntax : SyntaxNode
{
}

/// <summary>
/// The base for all declaration syntax nodes.
/// </summary>
public abstract partial class DeclarationSyntax : StatementSyntax
{
}

/// <summary>
/// A full, parsed file containing all of its declarations.
/// </summary>
public sealed partial class FileDeclarationSyntax : DeclarationSyntax
{
    /// <summary>
    /// The declarations contained in the file.
    /// </summary>
    public Sequence<DeclarationSyntax> Declarations { get; }

    /// <summary>
    /// The end of file token.
    /// </summary>
    public SyntaxToken End { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDeclarationSyntax"/> class.
    /// </summary>
    /// <param name="declarations">The declarations contained in the file.</param>
    /// <param name="end">The end of file token.</param>
    internal FileDeclarationSyntax(
        Sequence<DeclarationSyntax> declarations,
        SyntaxToken end)
    {
        this.Declarations = declarations;
        this.End = end;
    }

    /// <inheritdoc/>
    public override bool Equals(SyntaxNode? other) =>
           other is FileDeclarationSyntax o
        && this.Declarations.Equals(o.Declarations)
        && this.End.Equals(o.End);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(
        this.Declarations,
        this.End);

    /// <inheritdoc/>
    public override IEnumerable<KeyValuePair<string, object?>> Children
    {
        get
        {
            yield return new("Declarations", this.Declarations);
            yield return new("End", this.End);
        }
    }
}

/// <summary>
/// A function declaration.
/// </summary>
public sealed partial class FunctionDeclarationSyntax : DeclarationSyntax
{
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

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionDeclarationSyntax"/> class.
    /// </summary>
    /// <param name="funcKeyword">The function keyword.</param>
    /// <param name="name">The name of the function.</param>
    /// <param name="parameterList">The parameters of the function.</param>
    /// <param name="body">The body of the function.</param>
    internal FunctionDeclarationSyntax(
        SyntaxToken funcKeyword,
        SyntaxToken name,
        ParameterListSyntax parameterList,
        ExpressionSyntax body)
    {
        this.FuncKeyword = funcKeyword;
        this.Name = name;
        this.ParameterList = parameterList;
        this.Body = body;
    }

    /// <inheritdoc/>
    public override bool Equals(SyntaxNode? other) =>
           other is FunctionDeclarationSyntax o
        && this.FuncKeyword.Equals(o.FuncKeyword)
        && this.Name.Equals(o.Name)
        && this.ParameterList.Equals(o.ParameterList)
        && this.Body.Equals(o.Body);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(
        this.FuncKeyword,
        this.Name,
        this.ParameterList,
        this.Body);

    /// <inheritdoc/>
    public override IEnumerable<KeyValuePair<string, object?>> Children
    {
        get
        {
            yield return new("FuncKeyword", this.FuncKeyword);
            yield return new("Name", this.Name);
            yield return new("ParameterList", this.ParameterList);
            yield return new("Body", this.Body);
        }
    }
}

/// <summary>
/// A list of parameters for a function declaration.
/// </summary>
public sealed partial class ParameterListSyntax : SyntaxNode
{
    /// <summary>
    /// The open parenthesis token.
    /// </summary>
    public SyntaxToken OpenParenthesis { get; }

    /// <summary>
    /// The close parenthesis token.
    /// </summary>
    public SyntaxToken CloseParenthesis { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterListSyntax"/> class.
    /// </summary>
    /// <param name="openParenthesis">The open parenthesis token.</param>
    /// <param name="closeParenthesis">The close parenthesis token.</param>
    internal ParameterListSyntax(
        SyntaxToken openParenthesis,
        SyntaxToken closeParenthesis)
    {
        this.OpenParenthesis = openParenthesis;
        this.CloseParenthesis = closeParenthesis;
    }

    /// <inheritdoc/>
    public override bool Equals(SyntaxNode? other) =>
           other is ParameterListSyntax o
        && this.OpenParenthesis.Equals(o.OpenParenthesis)
        && this.CloseParenthesis.Equals(o.CloseParenthesis);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(
        this.OpenParenthesis,
        this.CloseParenthesis);

    /// <inheritdoc/>
    public override IEnumerable<KeyValuePair<string, object?>> Children
    {
        get
        {
            yield return new("OpenParenthesis", this.OpenParenthesis);
            yield return new("CloseParenthesis", this.CloseParenthesis);
        }
    }
}

/// <summary>
/// A code block expression.
/// </summary>
public sealed partial class BlockExpressionSyntax : ExpressionSyntax
{
    /// <summary>
    /// The open brace token.
    /// </summary>
    public SyntaxToken OpenBrace { get; }

    /// <summary>
    /// The close brace token.
    /// </summary>
    public SyntaxToken CloseBrace { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockExpressionSyntax"/> class.
    /// </summary>
    /// <param name="openBrace">The open brace token.</param>
    /// <param name="closeBrace">The close brace token.</param>
    internal BlockExpressionSyntax(
        SyntaxToken openBrace,
        SyntaxToken closeBrace)
    {
        this.OpenBrace = openBrace;
        this.CloseBrace = closeBrace;
    }

    /// <inheritdoc/>
    public override bool Equals(SyntaxNode? other) =>
           other is BlockExpressionSyntax o
        && this.OpenBrace.Equals(o.OpenBrace)
        && this.CloseBrace.Equals(o.CloseBrace);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(
        this.OpenBrace,
        this.CloseBrace);

    /// <inheritdoc/>
    public override IEnumerable<KeyValuePair<string, object?>> Children
    {
        get
        {
            yield return new("OpenBrace", this.OpenBrace);
            yield return new("CloseBrace", this.CloseBrace);
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
        declarations,
        end);

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
        funcKeyword,
        name,
        parameterList,
        body);

    /// <summary>
    /// Constructs a <see cref="ParameterListSyntax"/> from the given arguments.
    /// </summary>
    /// <param name="openParenthesis">The open parenthesis token.</param>
    /// <param name="closeParenthesis">The close parenthesis token.</param>
    public static ParameterListSyntax ParameterList(
        SyntaxToken openParenthesis,
        SyntaxToken closeParenthesis) => new ParameterListSyntax(
        openParenthesis,
        closeParenthesis);

    /// <summary>
    /// Constructs a <see cref="BlockExpressionSyntax"/> from the given arguments.
    /// </summary>
    /// <param name="openBrace">The open brace token.</param>
    /// <param name="closeBrace">The close brace token.</param>
    public static BlockExpressionSyntax BlockExpression(
        SyntaxToken openBrace,
        SyntaxToken closeBrace) => new BlockExpressionSyntax(
        openBrace,
        closeBrace);
}
/// <summary>
/// Provides extension methods for the syntax nodes.
/// </summary>
public static partial class SyntaxNodeExtensions
{
}

#nullable restore
