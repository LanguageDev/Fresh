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
    Sequence<Token> LeadingTrivia,
    Token Token,
    Sequence<Token> TrailingTrivia)
{
    /// <summary>
    /// The type of the token.
    /// </summary>
    public TokenType Type => this.Token.Type;

    public SyntaxToken(Token Token)
        : this(Sequence<Token>.Empty, Token, Sequence<Token>.Empty)
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
    public abstract Sequence<Token> LeadingTrivia { get; }

    /// <summary>
    /// The trailing trivia of this syntax node.
    /// </summary>
    public abstract Sequence<Token> TrailingTrivia { get; }

    /// <summary>
    /// Gets the leading trivia from a list of syntax nodes.
    /// </summary>
    /// <typeparam name="TSyntax">The type of the syntax nodes.</typeparam>
    /// <param name="nodes">The list of nodes.</param>
    /// <returns>The leading trivia of the first element in <paramref name="nodes"/>.</returns>
    protected static Sequence<Token> GetLeadingTrivia<TSyntax>(IReadOnlyList<TSyntax> nodes)
        where TSyntax : SyntaxNode => nodes.Count == 0
        ? Sequence<Token>.Empty
        : nodes[0].LeadingTrivia;

    /// <summary>
    /// Gets the trailing trivia from a list of syntax nodes.
    /// </summary>
    /// <typeparam name="TSyntax">The type of the syntax nodes.</typeparam>
    /// <param name="nodes">The list of nodes.</param>
    /// <returns>The trailing trivia of the last element in <paramref name="nodes"/>.</returns>
    protected static Sequence<Token> GetTrailingTrivia<TSyntax>(IReadOnlyList<TSyntax> nodes)
        where TSyntax : SyntaxNode => nodes.Count == 0
        ? Sequence<Token>.Empty
        : nodes[^1].TrailingTrivia;

    /// <summary>
    /// Utility to read out a doc comment attached right above a token from the <see cref="SyntaxNode.LeadingTrivia"/>.
    /// </summary>
    /// <param name="token">The token to get the attached documentation for.</param>
    /// <returns>The comments that are right above <paramref name="token"/>, or null.</returns>
    protected CommentGroup? GetDocumentationFor(SyntaxToken token)
    {
        // Check if there is any leading trivial
        if (this.LeadingTrivia.Count == 0) return null;
        // Take comments in reverse order
        var lastComments = this.LeadingTrivia
            .Where(t => t.IsComment)
            .Reverse();
        // Take comments while they are strictly stuck together
        var comments = new List<Token>();
        var minAllowedLine = token.Token.Location.Start.Line - 1;
        foreach (var comment in comments)
        {
            if (comment.Location.Start.Line < minAllowedLine) break;
            comments.Add(comment);
            minAllowedLine = comment.Location.Start.Line - 1;
        }
        if (comments.Count == 0) return null;
        comments.Reverse();
        return new(comments.ToSequence());
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

public sealed record class FileDeclarationSyntax(
    IReadOnlyList<DeclarationSyntax> Declarations) : DeclarationSyntax
{
    /// <inheritdoc/>
    public override CommentGroup? Documentation
    {
        get
        {
            var maxAllowedLine = 0;
            var comments = new List<Token>();
            foreach (var comment in this.LeadingTrivia.Where(t => t.IsComment))
            {
                if (comment.Location.Start.Line > maxAllowedLine) break;
                comments.Add(comment);
                maxAllowedLine = comment.Location.End.Line + 1;
            }
            return new(comments.ToSequence());
        }
    }

    /// <inheritdoc/>
    public override Sequence<Token> LeadingTrivia => GetLeadingTrivia(this.Declarations);

    /// <inheritdoc/>
    public override Sequence<Token> TrailingTrivia => GetTrailingTrivia(this.Declarations);
}

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
    ExpressionSyntax Body) : DeclarationSyntax
{
    /// <inheritdoc/>
    public override CommentGroup? Documentation => this.GetDocumentationFor(this.FuncKeyword);

    /// <inheritdoc/>
    public override Sequence<Token> LeadingTrivia => this.FuncKeyword.LeadingTrivia;

    /// <inheritdoc/>
    public override Sequence<Token> TrailingTrivia => this.Body.TrailingTrivia;
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
    public override Sequence<Token> LeadingTrivia => this.OpenParenthesis.LeadingTrivia;

    /// <inheritdoc/>
    public override Sequence<Token> TrailingTrivia => this.CloseParenthesis.TrailingTrivia;
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
    public override Sequence<Token> LeadingTrivia => this.OpenBrace.LeadingTrivia;

    /// <inheritdoc/>
    public override Sequence<Token> TrailingTrivia => this.CloseBrace.TrailingTrivia;
}
