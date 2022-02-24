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
    new internal abstract partial class GreenNode : SyntaxNode.GreenNode
    {
        public abstract override StatementSyntax ToRedNode(SyntaxNode? parent);
    }

    internal abstract override GreenNode Green { get; }
}

/// <summary>
/// The base for all expression syntax nodes.
/// </summary>
public abstract partial class ExpressionSyntax : SyntaxNode
{
    new internal abstract partial class GreenNode : SyntaxNode.GreenNode
    {
        public abstract override ExpressionSyntax ToRedNode(SyntaxNode? parent);
    }

    internal abstract override GreenNode Green { get; }
}

/// <summary>
/// The base for all declaration syntax nodes.
/// </summary>
public abstract partial class DeclarationSyntax : StatementSyntax
{
    new internal abstract partial class GreenNode : StatementSyntax.GreenNode
    {
        public abstract override DeclarationSyntax ToRedNode(SyntaxNode? parent);
    }

    internal abstract override GreenNode Green { get; }
}

/// <summary>
/// A full, parsed file containing all of its declarations.
/// </summary>
public sealed partial class FileDeclarationSyntax : DeclarationSyntax
{
    new internal sealed partial class GreenNode : DeclarationSyntax.GreenNode
    {
        public Sequence<DeclarationSyntax.GreenNode> Declarations { get; }

        public SyntaxToken End { get; }

        public GreenNode(
            Sequence<DeclarationSyntax.GreenNode> declarations,
            SyntaxToken end)
        {
            this.Declarations = declarations;
            this.End = end;
        }

        public override bool Equals(SyntaxNode.GreenNode? other) =>
               other is GreenNode o
            && this.Declarations.Equals(o.Declarations)
            && this.End.Equals(o.End);

        public override int GetHashCode() => HashCode.Combine(
            this.Declarations,
            this.End);

        public override IEnumerable<KeyValuePair<string, object?>> Children
        {
            get
            {
                yield return new("Declarations", this.Declarations);
                yield return new("End", this.End);
            }
        }

        public override FileDeclarationSyntax ToRedNode(SyntaxNode? parent) => new(parent, this);
    }

    /// <summary>
    /// The declarations contained in the file.
    /// </summary>
    public SyntaxSequence<DeclarationSyntax> Declarations => new(this.Green.Declarations, n => (DeclarationSyntax)n.ToRedNode(this));

    /// <summary>
    /// The end of file token.
    /// </summary>
    public SyntaxToken End => this.Green.End;

    internal override GreenNode Green { get; }

    internal FileDeclarationSyntax(SyntaxNode? parent, GreenNode green)
    {
        this.Parent = parent;
        this.Green = green;
    }
}

/// <summary>
/// A function declaration.
/// </summary>
public sealed partial class FunctionDeclarationSyntax : DeclarationSyntax
{
    new internal sealed partial class GreenNode : DeclarationSyntax.GreenNode
    {
        public SyntaxToken FuncKeyword { get; }

        public SyntaxToken Name { get; }

        public ParameterListSyntax.GreenNode ParameterList { get; }

        public ExpressionSyntax.GreenNode Body { get; }

        public GreenNode(
            SyntaxToken funcKeyword,
            SyntaxToken name,
            ParameterListSyntax.GreenNode parameterList,
            ExpressionSyntax.GreenNode body)
        {
            this.FuncKeyword = funcKeyword;
            this.Name = name;
            this.ParameterList = parameterList;
            this.Body = body;
        }

        public override bool Equals(SyntaxNode.GreenNode? other) =>
               other is GreenNode o
            && this.FuncKeyword.Equals(o.FuncKeyword)
            && this.Name.Equals(o.Name)
            && this.ParameterList.Equals(o.ParameterList)
            && this.Body.Equals(o.Body);

        public override int GetHashCode() => HashCode.Combine(
            this.FuncKeyword,
            this.Name,
            this.ParameterList,
            this.Body);

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

        public override FunctionDeclarationSyntax ToRedNode(SyntaxNode? parent) => new(parent, this);
    }

    /// <summary>
    /// The function keyword.
    /// </summary>
    public SyntaxToken FuncKeyword => this.Green.FuncKeyword;

    /// <summary>
    /// The name of the function.
    /// </summary>
    public SyntaxToken Name => this.Green.Name;

    /// <summary>
    /// The parameters of the function.
    /// </summary>
    public ParameterListSyntax ParameterList => this.Green.ParameterList.ToRedNode(this);

    /// <summary>
    /// The body of the function.
    /// </summary>
    public ExpressionSyntax Body => this.Green.Body.ToRedNode(this);

    internal override GreenNode Green { get; }

    internal FunctionDeclarationSyntax(SyntaxNode? parent, GreenNode green)
    {
        this.Parent = parent;
        this.Green = green;
    }
}

/// <summary>
/// A list of parameters for a function declaration.
/// </summary>
public sealed partial class ParameterListSyntax : SyntaxNode
{
    new internal sealed partial class GreenNode : SyntaxNode.GreenNode
    {
        public SyntaxToken OpenParenthesis { get; }

        public SyntaxToken CloseParenthesis { get; }

        public GreenNode(
            SyntaxToken openParenthesis,
            SyntaxToken closeParenthesis)
        {
            this.OpenParenthesis = openParenthesis;
            this.CloseParenthesis = closeParenthesis;
        }

        public override bool Equals(SyntaxNode.GreenNode? other) =>
               other is GreenNode o
            && this.OpenParenthesis.Equals(o.OpenParenthesis)
            && this.CloseParenthesis.Equals(o.CloseParenthesis);

        public override int GetHashCode() => HashCode.Combine(
            this.OpenParenthesis,
            this.CloseParenthesis);

        public override IEnumerable<KeyValuePair<string, object?>> Children
        {
            get
            {
                yield return new("OpenParenthesis", this.OpenParenthesis);
                yield return new("CloseParenthesis", this.CloseParenthesis);
            }
        }

        public override ParameterListSyntax ToRedNode(SyntaxNode? parent) => new(parent, this);
    }

    /// <summary>
    /// The open parenthesis token.
    /// </summary>
    public SyntaxToken OpenParenthesis => this.Green.OpenParenthesis;

    /// <summary>
    /// The close parenthesis token.
    /// </summary>
    public SyntaxToken CloseParenthesis => this.Green.CloseParenthesis;

    internal override GreenNode Green { get; }

    internal ParameterListSyntax(SyntaxNode? parent, GreenNode green)
    {
        this.Parent = parent;
        this.Green = green;
    }
}

/// <summary>
/// A code block expression.
/// </summary>
public sealed partial class BlockExpressionSyntax : ExpressionSyntax
{
    new internal sealed partial class GreenNode : ExpressionSyntax.GreenNode
    {
        public SyntaxToken OpenBrace { get; }

        public SyntaxToken CloseBrace { get; }

        public GreenNode(
            SyntaxToken openBrace,
            SyntaxToken closeBrace)
        {
            this.OpenBrace = openBrace;
            this.CloseBrace = closeBrace;
        }

        public override bool Equals(SyntaxNode.GreenNode? other) =>
               other is GreenNode o
            && this.OpenBrace.Equals(o.OpenBrace)
            && this.CloseBrace.Equals(o.CloseBrace);

        public override int GetHashCode() => HashCode.Combine(
            this.OpenBrace,
            this.CloseBrace);

        public override IEnumerable<KeyValuePair<string, object?>> Children
        {
            get
            {
                yield return new("OpenBrace", this.OpenBrace);
                yield return new("CloseBrace", this.CloseBrace);
            }
        }

        public override BlockExpressionSyntax ToRedNode(SyntaxNode? parent) => new(parent, this);
    }

    /// <summary>
    /// The open brace token.
    /// </summary>
    public SyntaxToken OpenBrace => this.Green.OpenBrace;

    /// <summary>
    /// The close brace token.
    /// </summary>
    public SyntaxToken CloseBrace => this.Green.CloseBrace;

    internal override GreenNode Green { get; }

    internal BlockExpressionSyntax(SyntaxNode? parent, GreenNode green)
    {
        this.Parent = parent;
        this.Green = green;
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
        IEnumerable<DeclarationSyntax> declarations,
        SyntaxToken end) => new(null, new(
        declarations.Select(n => n.Green).ToSequence(),
        end));

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
        ExpressionSyntax body) => new(null, new(
        funcKeyword,
        name,
        parameterList.Green,
        body.Green));

    /// <summary>
    /// Constructs a <see cref="ParameterListSyntax"/> from the given arguments.
    /// </summary>
    /// <param name="openParenthesis">The open parenthesis token.</param>
    /// <param name="closeParenthesis">The close parenthesis token.</param>
    public static ParameterListSyntax ParameterList(
        SyntaxToken openParenthesis,
        SyntaxToken closeParenthesis) => new(null, new(
        openParenthesis,
        closeParenthesis));

    /// <summary>
    /// Constructs a <see cref="BlockExpressionSyntax"/> from the given arguments.
    /// </summary>
    /// <param name="openBrace">The open brace token.</param>
    /// <param name="closeBrace">The close brace token.</param>
    public static BlockExpressionSyntax BlockExpression(
        SyntaxToken openBrace,
        SyntaxToken closeBrace) => new(null, new(
        openBrace,
        closeBrace));
}
/// <summary>
/// Provides extension methods for the syntax nodes.
/// </summary>
public static partial class SyntaxNodeExtensions
{
}

#nullable restore
