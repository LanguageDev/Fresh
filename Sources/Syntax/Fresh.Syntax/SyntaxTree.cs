// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Fresh.Common;

namespace Fresh.Syntax;

#pragma warning disable CS0282
#pragma warning disable CS0109

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
public partial struct SyntaxToken
{
    internal partial struct GreenNode
    {
        public TokenType Type => this.Token.Type;
    }

    /// <summary>
    /// The type of the token.
    /// </summary>
    public TokenType Type => this.Green.Type;

    /// <inheritdoc/>
    public CommentGroup? Documentation => null;

    /// <inheritdoc/>
    public IEnumerable<KeyValuePair<string, object?>> Children => this.Green.Children;

    /// <summary>
    /// The parent node of this token.
    /// </summary>
    public SyntaxNode? Parent { get; }

    /// <inheritdoc/>
    public bool Equals(SyntaxToken other) => this.Green.Equals(other.Green);
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

    private static SyntaxToken.GreenNode? GetFirstToken(object? value) => value switch
    {
        SyntaxToken.GreenNode syntaxToken => syntaxToken,
        SyntaxToken syntaxToken => syntaxToken.Green,
        ISyntaxElement syntaxElement => syntaxElement.Children
            .Select(c => GetFirstToken(c.Value))
            .First(t => t is not null)!.Value,
        IEnumerable enumerable => enumerable
            .Cast<object?>()
            .Select(GetFirstToken)
            .First(t => t is not null)!.Value,
        _ => throw new InvalidOperationException(),
    };

    private static SyntaxToken.GreenNode? GetLastToken(object? value) => value switch
    {
        SyntaxToken.GreenNode syntaxToken => syntaxToken,
        SyntaxToken syntaxToken => syntaxToken.Green,
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
            var type = syntaxElement.GetType();
            if (type.Name == "GreenNode") type = type.DeclaringType;
            builder.Append(type?.Name).AppendLine(" {");
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

    protected static IEnumerable<Token> TakeCommentGroup(IEnumerable<Token> tokens)
    {
        var wasNewline = false;
        foreach (var token in tokens)
        {
            if (token.Type == TokenType.Newline)
            {
                if (wasNewline) break;
                wasNewline = true;
            }
            else
            {
                wasNewline = false;
            }
            if (token.IsComment) yield return token;
        }
    }
}

public partial class ModuleDeclarationSyntax
{
    internal partial class GreenNode
    {
        /// <inheritdoc/>
        public override CommentGroup? Documentation
        {
            get
            {
                var comments = TakeCommentGroup(this.LeadingTrivia).ToList();
                if (comments.Count == 0) return null;
                return new(comments.ToSequence());
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
                var comments = TakeCommentGroup(this.LeadingTrivia.Reverse()).ToList();
                if (comments.Count == 0) return null;
                comments.Reverse();
                return new(comments.ToSequence());
            }
        }
    }
}

public partial class SyntaxFactory
{
    /// <summary>
    /// Creates a new <see cref="SyntaxToken"/>.
    /// </summary>
    /// <param name="token">The represented token.</param>
    /// <returns>The constructed <see cref="SyntaxToken"/>.</returns>
    public static SyntaxToken Token(Token token) => Token(Sequence<Token>.Empty, token, Sequence<Token>.Empty);
}

#pragma warning restore CS0109
#pragma warning restore CS0282
