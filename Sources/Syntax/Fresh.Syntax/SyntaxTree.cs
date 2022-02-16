// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fresh.Common;

namespace Fresh.Syntax;

/// <summary>
/// Represents comments that are right under each other without any blank lines.
/// </summary>
/// <param name="Comments">The sequence of comment tokens.</param>
public readonly record struct CommentGroup(Sequence<Token> Comments);

/// <summary>
/// Represents a single token as the part of the syntax tree.
/// </summary>
/// <param name="LeadingTrivia">The sequence of comment groups before this token.</param>
/// <param name="Token">The token that is part of the syntax tree.</param>
/// <param name="TrailingTrivia">The sequence of comment groups after this token.</param>
public readonly record struct SyntaxToken(
    Sequence<CommentGroup>? LeadingTrivia,
    Token Token,
    Sequence<CommentGroup>? TrailingTrivia)
{
    public SyntaxToken(Token Token)
        : this(null, Token, null)
    {
    }
}

/// <summary>
/// The base for all syntax tree nodes.
/// </summary>
public abstract record class SyntaxNode
{
    /// <summary>
    /// The documentation comment group on this syntax node.
    /// </summary>
    public virtual CommentGroup? Documentation => null;

    /// <summary>
    /// The leading trivia of this syntax node.
    /// </summary>
    public abstract Sequence<CommentGroup>? LeadingTrivia { get; }

    /// <summary>
    /// The trailing trivia of this syntax node.
    /// </summary>
    public abstract Sequence<CommentGroup>? TrailingTrivia { get; }

    /// <summary>
    /// Utility to read out a doc comment attached right above a token from the <see cref="SyntaxNode.LeadingTrivia"/>.
    /// </summary>
    /// <param name="token">The token to get the attached documentation for.</param>
    /// <returns>The comments that are right above <paramref name="token"/>, or null.</returns>
    protected CommentGroup? GetDocumentationFor(SyntaxToken token)
    {
        // Check if there is any leading trivial
        if (this.LeadingTrivia is null) return null;
        var leading = this.LeadingTrivia.Value;
        if (leading.Count == 0) return null;
        var lastLeading = leading[^1];
        // NOTE: Should not happen
        if (lastLeading.Comments.Count == 0) return null;

        // There is, check if the last token is just below that
        var lastComment = lastLeading.Comments[^1];
        if (lastComment.Location.Start.Line != token.Token.Location.End.Line - 1) return null;
        return lastLeading;
    }
}

/// <summary>
/// The base for all statement syntax nodes.
/// </summary>
public abstract record class StatementSyntax : SyntaxNode;

/// <summary>
/// The base for all declaration syntax nodes.
/// </summary>
public abstract record class DeclarationSyntax : StatementSyntax;

/// <summary>
/// A function declaration.
/// </summary>
/// <param name="FuncKeyword">The keyword starting the declaration.</param>
/// <param name="Name">The name of the function.</param>
/// <param name="ArgumentList">The argument list syntax.</param>
/// <param name="Body">The body of the function.</param>
public sealed record class FunctionDeclarationSyntax(
    SyntaxToken FuncKeyword,
    SyntaxToken Name,
    ArgumentListSyntax ArgumentList,
    BlockSyntax Body) : DeclarationSyntax
{
    /// <inheritdoc/>
    public override CommentGroup? Documentation => this.GetDocumentationFor(this.FuncKeyword);

    /// <inheritdoc/>
    public override Sequence<CommentGroup>? LeadingTrivia => this.FuncKeyword.LeadingTrivia;

    /// <inheritdoc/>
    public override Sequence<CommentGroup>? TrailingTrivia => this.Body.TrailingTrivia;
}

/// <summary>
/// An argument list for a function.
/// </summary>
/// <param name="OpenParenthesis">The open parenthesis.</param>
/// <param name="CloseParenthesis">The close parenthesis.</param>
public sealed record class ArgumentListSyntax(
    SyntaxToken OpenParenthesis,
    SyntaxToken CloseParenthesis) : SyntaxNode
{
    /// <inheritdoc/>
    public override Sequence<CommentGroup>? LeadingTrivia => this.OpenParenthesis.LeadingTrivia;

    /// <inheritdoc/>
    public override Sequence<CommentGroup>? TrailingTrivia => this.CloseParenthesis.TrailingTrivia;
}

/// <summary>
/// The base for all expression syntax nodes.
/// </summary>
public abstract record class ExpressionSyntax : SyntaxNode;

/// <summary>
/// A code block syntax node.
/// </summary>
/// <param name="OpenBrace">The open brace.</param>
/// <param name="CloseBrace">The close brace.</param>
public sealed record class BlockSyntax(
    SyntaxToken OpenBrace,
    SyntaxToken CloseBrace) : ExpressionSyntax
{
    /// <inheritdoc/>
    public override Sequence<CommentGroup>? LeadingTrivia => this.OpenBrace.LeadingTrivia;

    /// <inheritdoc/>
    public override Sequence<CommentGroup>? TrailingTrivia => this.CloseBrace.TrailingTrivia;
}
