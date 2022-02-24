// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Fresh.Common;

namespace Fresh.Syntax;

/// <summary>
/// Interface for any syntax element in the tree.
/// </summary>
public interface ISyntaxElement
{
    /// <summary>
    /// The leading trivia of the syntax element.
    /// </summary>
    public Sequence<Token> LeadingTrivia { get; }

    /// <summary>
    /// The trailing trivia of the syntax element.
    /// </summary>
    public Sequence<Token> TrailingTrivia { get; }

    /// <summary>
    /// The documentation comment group on this syntax element.
    /// </summary>
    public CommentGroup? Documentation { get; }

    /// <summary>
    /// The children as name and value pair inside this syntax node.
    /// </summary>
    public IEnumerable<KeyValuePair<string, object?>> Children { get; }
}

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
    Sequence<Token> TrailingTrivia) : ISyntaxElement
{
    /// <summary>
    /// The type of the token.
    /// </summary>
    public TokenType Type => this.Token.Type;

    /// <inheritdoc/>
    public CommentGroup? Documentation => null;

    /// <inheritdoc/>
    public IEnumerable<KeyValuePair<string, object?>> Children
    {
        get
        {
            yield return new(nameof(this.LeadingTrivia), this.LeadingTrivia);
            yield return new(nameof(this.Token), this.Token);
            yield return new(nameof(this.TrailingTrivia), this.TrailingTrivia);
        }
    }

    public SyntaxToken(Token Token)
        : this(Sequence<Token>.Empty, Token, Sequence<Token>.Empty)
    {
    }
}

/// <summary>
/// Represents a sequence of syntax elements.
/// </summary>
/// <typeparam name="TElement">The syntax element type.</typeparam>
public readonly record struct SyntaxSequence<TElement> : IReadOnlyList<TElement>
    where TElement : ISyntaxElement
{
    private readonly IReadOnlyList<SyntaxNode.GreenNode> greenNodes;
    private readonly Func<SyntaxNode.GreenNode, TElement> transformer;

    /// <inheritdoc/>
    public int Count => this.greenNodes.Count;

    /// <inheritdoc/>
    public TElement this[int index] => this.transformer(this.greenNodes[index]);

    internal SyntaxSequence(
        IReadOnlyList<SyntaxNode.GreenNode> greenNodes,
        Func<SyntaxNode.GreenNode, TElement> transformer)
    {
        this.greenNodes = greenNodes;
        this.transformer = transformer;
    }

    /// <inheritdoc/>
    public IEnumerator<TElement> GetEnumerator() => this.greenNodes.Select(this.transformer).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}

/// <summary>
/// The base for all syntax tree nodes.
/// </summary>
public abstract class SyntaxNode : ISyntaxElement, IEquatable<SyntaxNode>
{
    internal abstract class GreenNode : ISyntaxElement, IEquatable<GreenNode>
    {
        public Sequence<Token> LeadingTrivia => GetFirstToken(this)!.Value.LeadingTrivia;

        public Sequence<Token> TrailingTrivia => GetLastToken(this)!.Value.TrailingTrivia;

        public virtual CommentGroup? Documentation => null;

        public abstract IEnumerable<KeyValuePair<string, object?>> Children { get; }

        public override bool Equals(object? obj) => this.Equals(obj as GreenNode);

        public abstract bool Equals(GreenNode? other);

        public abstract override int GetHashCode();

        public abstract SyntaxNode ToRedNode(SyntaxNode? parent);
    }

    /// <inheritdoc/>
    public Sequence<Token> LeadingTrivia => this.Green.LeadingTrivia;

    /// <inheritdoc/>
    public Sequence<Token> TrailingTrivia => this.Green.TrailingTrivia;

    /// <inheritdoc/>
    public CommentGroup? Documentation => this.Green.Documentation;

    /// <summary>
    /// The parent node of this one.
    /// </summary>
    public SyntaxNode? Parent { get; protected set; } // NOTE: It's only being set in child constructors, simplifies codegen

    /// <inheritdoc/>
    public IEnumerable<KeyValuePair<string, object?>> Children => this.Green.Children
        .Select(kv => new KeyValuePair<string, object?>(kv.Key, this.ToRedObject(kv.Value)));

    private const int indentSize = 2;

    internal abstract GreenNode Green { get; }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => this.Equals(obj as SyntaxNode);

    /// <inheritdoc/>
    public bool Equals(SyntaxNode? other) => this.Green.Equals(other?.Green);

    /// <inheritdoc/>
    public override int GetHashCode() => this.Green.GetHashCode();

    /// <summary>
    /// Converts the syntax tree to source code.
    /// </summary>
    /// <returns>The source code that this syntax tree represents.</returns>
    public string ToSourceText()
    {
        var writer = new StringWriter();
        this.WriteSourceText(writer);
        return writer.ToString();
    }

    /// <summary>
    /// Writes the syntax tree as source code to a writer.
    /// </summary>
    /// <param name="writer">The writer to write the tree to.</param>
    public void WriteSourceText(TextWriter writer) => WriteSourceTextImpl(writer, this);

    /// <summary>
    /// Creates a debug representation of the syntax tree.
    /// </summary>
    /// <returns>The debug representation of the syntax tree.</returns>
    public string ToDebugString()
    {
        var builder = new StringBuilder();
        DebugPrint(builder, 0, null, this);
        return builder.ToString();
    }

    private object? ToRedObject(object? value) => value switch
    {
        GreenNode g => g.ToRedNode(this),
        IReadOnlyList<GreenNode> l => new SyntaxSequence<SyntaxNode>(l, n => n.ToRedNode(this)),
        _ => value,
    };

    private static SyntaxToken? GetFirstToken(object? value) => value switch
    {
        SyntaxToken syntaxToken => syntaxToken,
        ISyntaxElement syntaxElement => syntaxElement.Children
            .Select(c => GetFirstToken(c.Value))
            .First(t => t is not null)!.Value,
        IEnumerable enumerable => enumerable
            .Cast<object?>()
            .Select(GetFirstToken)
            .First(t => t is not null)!.Value,
        _ => throw new InvalidOperationException(),
    };

    private static SyntaxToken? GetLastToken(object? value) => value switch
    {
        SyntaxToken syntaxToken => syntaxToken,
        ISyntaxElement syntaxElement => syntaxElement.Children
            .Select(c => GetLastToken(c.Value))
            .Last(t => t is not null)!.Value,
        IEnumerable enumerable => enumerable
            .Cast<object?>()
            .Select(GetLastToken)
            .Last(t => t is not null)!.Value,
        _ => throw new InvalidOperationException(),
    };

    private static void WriteSourceTextImpl(TextWriter writer, object? value)
    {
        switch (value)
        {
        case ISyntaxElement syntaxElement:
            foreach (var (_, child) in syntaxElement.Children) WriteSourceTextImpl(writer, child);
            break;

        case IEnumerable enumerable:
            foreach (var item in enumerable) WriteSourceTextImpl(writer, item);
            break;

        case Token token:
            writer.Write(token.Text);
            break;

        default:
            writer.Write(value);
            break;
        }
    }

    private static void DebugPrint(StringBuilder builder, int indent, string? name, object? value)
    {
        // Handle null value
        if (value is null)
        {
            builder.Append("null");
            return;
        }

        // Otherwise depends on what the name or value is
        if (name == "LeadingTrivia" || name == "TrailingTrivia")
        {
            // Leading and trailing trivia is simple, we just list the strings in a debug string
            var seq = (Sequence<Token>)value;
            builder
                .Append('[')
                .AppendJoin(", ", seq.Select(t => ToDebugString(t.Text)))
                .Append(']');
            return;
        }

        // Sequences
        if (value is IEnumerable enumerable)
        {
            builder.AppendLine("[");
            foreach (var obj in enumerable)
            {
                builder.Append(' ', (indent + 1) * indentSize);
                DebugPrint(builder, indent + 1, null, obj);
                builder.AppendLine(",");
            }
            builder
                .Append(' ', indent * indentSize)
                .Append(']');
            return;
        }

        // Syntax nodes
        if (value is ISyntaxElement syntaxElement)
        {
            builder.Append(syntaxElement.GetType().Name).AppendLine(" {");
            if (syntaxElement.Documentation is not null)
            {
                // It has documentation
                builder
                    .Append(' ', (indent + 1) * indentSize)
                    .Append("Documentation: ");
                DebugPrint(builder, indent + 1, null, syntaxElement.Documentation.Value.Comments);
                builder.AppendLine(",");
            }
            foreach (var (fieldName, obj) in syntaxElement.Children)
            {
                builder
                    .Append(' ', (indent + 1) * indentSize)
                    .Append(fieldName)
                    .Append(": ");
                DebugPrint(builder, indent + 1, fieldName, obj);
                builder.AppendLine(",");
            }
            builder
                .Append(' ', indent * indentSize)
                .Append('}');
            return;
        }

        // Tokens
        if (value is Token token)
        {
            builder.Append(ToDebugString(token.Text));
            return;
        }

        // Best-effort
        builder.Append(value.ToString());
    }

    private static string ToDebugString(string text) => $"\"{EscapeString(text)}\"";

    private static string EscapeString(string text) => text
        .Replace("\a", @"\a")
        .Replace("\b", @"\b")
        .Replace("\f", @"\f")
        .Replace("\n", @"\n")
        .Replace("\r", @"\r")
        .Replace("\t", @"\t")
        .Replace("\v", @"\v")
        .Replace("\\", @"\")
        .Replace("\0", @"\0")
        .Replace("\"", @"\""");
}

public partial class FileDeclarationSyntax
{
    internal partial class GreenNode
    {
        /// <inheritdoc/>
        public override CommentGroup? Documentation
        {
            get
            {
                var trivia = this.LeadingTrivia;
                var maxAllowedLine = 0;
                var comments = new List<Token>();
                foreach (var comment in trivia.Where(t => t.IsComment))
                {
                    if (comment.Location.Start.Line > maxAllowedLine) break;
                    maxAllowedLine = comment.Location.Start.Line + 1;
                    comments.Add(comment);
                }
                return comments.Count > 0 ? new(comments.ToSequence()) : null;
            }
        }
    }
}

public partial class FunctionDeclarationSyntax
{
    internal partial class GreenNode
    {
        /// <inheritdoc/>
        public override CommentGroup? Documentation
        {
            get
            {
                var trivia = this.LeadingTrivia;
                var minAllowedLine = this.FuncKeyword.Token.Location.Start.Line - 1;
                var comments = new List<Token>();
                foreach (var comment in trivia.Where(t => t.IsComment).Reverse())
                {
                    if (comment.Location.Start.Line < minAllowedLine) break;
                    minAllowedLine = comment.Location.Start.Line - 1;
                    comments.Add(comment);
                }
                comments.Reverse();
                return comments.Count > 0 ? new(comments.ToSequence()) : null;
            }
        }
    }
}
