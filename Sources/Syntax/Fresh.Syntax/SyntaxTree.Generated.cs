using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Fresh.Common;

#pragma warning disable CS0109
#pragma warning disable CS0282
#nullable enable
namespace Fresh.Syntax;
/// <summary>
/// Represents a single token with trivia as part of the syntax tree.
/// </summary>
public readonly partial struct SyntaxToken : ISyntaxElement, IEquatable<SyntaxToken>
{
    new internal readonly partial struct GreenNode : IEquatable<GreenNode>
    {
        public Sequence<Token> LeadingTrivia { get; }

        public Token Token { get; }

        public Sequence<Token> TrailingTrivia { get; }

        /// <summary>
        /// Creates a new instance of the <see cref = "GreenNode"> type.
        /// </summary>
        public GreenNode(Sequence<Token> leadingTrivia, Token token, Sequence<Token> trailingTrivia)
        {
            this.LeadingTrivia = leadingTrivia;
            this.Token = token;
            this.TrailingTrivia = trailingTrivia;
        }

        /// <inheritdoc/>
        public override bool Equals(object? other) => other is GreenNode o && this.Equals(o);
        /// <inheritdoc/>
        public bool Equals([AllowNull] GreenNode other) => other is GreenNode o && object.Equals(this.LeadingTrivia, o.LeadingTrivia) && object.Equals(this.Token, o.Token) && object.Equals(this.TrailingTrivia, o.TrailingTrivia);
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(this.LeadingTrivia, this.Token, this.TrailingTrivia);
        public IEnumerable<KeyValuePair<string, object?>> Children
        {
            get
            {
                yield return new(nameof(this.LeadingTrivia), this.LeadingTrivia);
                yield return new(nameof(this.Token), this.Token);
                yield return new(nameof(this.TrailingTrivia), this.TrailingTrivia);
            }
        }

        public SyntaxToken ToRedNode(SyntaxNode? parent) => new(parent, this);
    }

    /// <summary>
    /// The trivia that comes before this token in the syntax tree.
    /// </summary>
    public Sequence<Token> LeadingTrivia => this.Green.LeadingTrivia;
    /// <summary>
    /// The token itself that is the significant part of the tree.
    /// </summary>
    public Token Token => this.Green.Token;
    /// <summary>
    /// The trivia that comes after this token in the syntax tree.
    /// </summary>
    public Sequence<Token> TrailingTrivia => this.Green.TrailingTrivia;
    internal GreenNode Green { get; }

    /// <summary>
    /// Creates a new instance of the <see cref = "SyntaxToken"> type.
    /// </summary>
    /// <param name = "parent">
    /// The parent node of this one.
    /// </param>
    /// <param name = "green">
    /// The wrapped green node.
    /// </param>
    internal SyntaxToken(SyntaxNode? parent, GreenNode green)
    {
        this.Parent = parent;
        this.Green = green;
    }
}

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
/// The base for all type syntax nodes.
/// </summary>
public abstract partial class TypeSyntax : ExpressionSyntax
{
    new internal abstract partial class GreenNode : ExpressionSyntax.GreenNode
    {
        public abstract override TypeSyntax ToRedNode(SyntaxNode? parent);
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
/// A full, parsed module containing all of its declarations.
/// </summary>
public sealed partial class ModuleDeclarationSyntax : DeclarationSyntax
{
    new internal sealed partial class GreenNode : DeclarationSyntax.GreenNode
    {
        public Sequence<DeclarationSyntax.GreenNode> Declarations { get; }

        public SyntaxToken.GreenNode End { get; }

        /// <summary>
        /// Creates a new instance of the <see cref = "GreenNode"> type.
        /// </summary>
        public GreenNode(Sequence<DeclarationSyntax.GreenNode> declarations, SyntaxToken.GreenNode end)
        {
            this.Declarations = declarations;
            this.End = end;
        }

        /// <inheritdoc/>
        public override bool Equals(object? other) => this.Equals(other as SyntaxNode.GreenNode);
        /// <inheritdoc/>
        public override bool Equals([AllowNull] SyntaxNode.GreenNode other) => other is GreenNode o && object.Equals(this.Declarations, o.Declarations) && object.Equals(this.End, o.End);
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(this.Declarations, this.End);
        public override IEnumerable<KeyValuePair<string, object?>> Children
        {
            get
            {
                yield return new(nameof(this.Declarations), this.Declarations);
                yield return new(nameof(this.End), this.End);
            }
        }

        public override ModuleDeclarationSyntax ToRedNode(SyntaxNode? parent) => new(parent, this);
    }

    /// <summary>
    /// The declarations contained in the module.
    /// </summary>
    public SyntaxSequence<DeclarationSyntax> Declarations => new(this.Green.Declarations, n => (DeclarationSyntax)n.ToRedNode(this));
    /// <summary>
    /// The end of file token.
    /// </summary>
    public SyntaxToken End => this.Green.End.ToRedNode(this);
    internal override GreenNode Green { get; }

    /// <summary>
    /// Creates a new instance of the <see cref = "ModuleDeclarationSyntax"> type.
    /// </summary>
    /// <param name = "parent">
    /// The parent node of this one.
    /// </param>
    /// <param name = "green">
    /// The wrapped green node.
    /// </param>
    internal ModuleDeclarationSyntax(SyntaxNode? parent, GreenNode green)
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
        public SyntaxToken.GreenNode FuncKeyword { get; }

        public SyntaxToken.GreenNode Name { get; }

        public ParameterListSyntax.GreenNode ParameterList { get; }

        public TypeSpecifierSyntax.GreenNode? TypeSpecifier { get; }

        public ExpressionSyntax.GreenNode Body { get; }

        /// <summary>
        /// Creates a new instance of the <see cref = "GreenNode"> type.
        /// </summary>
        public GreenNode(SyntaxToken.GreenNode funcKeyword, SyntaxToken.GreenNode name, ParameterListSyntax.GreenNode parameterList, TypeSpecifierSyntax.GreenNode? typeSpecifier, ExpressionSyntax.GreenNode body)
        {
            this.FuncKeyword = funcKeyword;
            this.Name = name;
            this.ParameterList = parameterList;
            this.TypeSpecifier = typeSpecifier;
            this.Body = body;
        }

        /// <inheritdoc/>
        public override bool Equals(object? other) => this.Equals(other as SyntaxNode.GreenNode);
        /// <inheritdoc/>
        public override bool Equals([AllowNull] SyntaxNode.GreenNode other) => other is GreenNode o && object.Equals(this.FuncKeyword, o.FuncKeyword) && object.Equals(this.Name, o.Name) && object.Equals(this.ParameterList, o.ParameterList) && object.Equals(this.TypeSpecifier, o.TypeSpecifier) && object.Equals(this.Body, o.Body);
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(this.FuncKeyword, this.Name, this.ParameterList, this.TypeSpecifier, this.Body);
        public override IEnumerable<KeyValuePair<string, object?>> Children
        {
            get
            {
                yield return new(nameof(this.FuncKeyword), this.FuncKeyword);
                yield return new(nameof(this.Name), this.Name);
                yield return new(nameof(this.ParameterList), this.ParameterList);
                yield return new(nameof(this.TypeSpecifier), this.TypeSpecifier);
                yield return new(nameof(this.Body), this.Body);
            }
        }

        public override FunctionDeclarationSyntax ToRedNode(SyntaxNode? parent) => new(parent, this);
    }

    /// <summary>
    /// The function keyword.
    /// </summary>
    public SyntaxToken FuncKeyword => this.Green.FuncKeyword.ToRedNode(this);
    /// <summary>
    /// The name of the function.
    /// </summary>
    public SyntaxToken Name => this.Green.Name.ToRedNode(this);
    /// <summary>
    /// The parameters of the function.
    /// </summary>
    public ParameterListSyntax ParameterList => this.Green.ParameterList.ToRedNode(this);
    /// <summary>
    /// The return type specifier.
    /// </summary>
    public TypeSpecifierSyntax? TypeSpecifier => this.Green.TypeSpecifier?.ToRedNode(this);
    /// <summary>
    /// The body of the function.
    /// </summary>
    public ExpressionSyntax Body => this.Green.Body.ToRedNode(this);
    internal override GreenNode Green { get; }

    /// <summary>
    /// Creates a new instance of the <see cref = "FunctionDeclarationSyntax"> type.
    /// </summary>
    /// <param name = "parent">
    /// The parent node of this one.
    /// </param>
    /// <param name = "green">
    /// The wrapped green node.
    /// </param>
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
        public SyntaxToken.GreenNode OpenParenthesis { get; }

        public Sequence<ParameterSyntax.GreenNode> Parameters { get; }

        public SyntaxToken.GreenNode CloseParenthesis { get; }

        /// <summary>
        /// Creates a new instance of the <see cref = "GreenNode"> type.
        /// </summary>
        public GreenNode(SyntaxToken.GreenNode openParenthesis, Sequence<ParameterSyntax.GreenNode> parameters, SyntaxToken.GreenNode closeParenthesis)
        {
            this.OpenParenthesis = openParenthesis;
            this.Parameters = parameters;
            this.CloseParenthesis = closeParenthesis;
        }

        /// <inheritdoc/>
        public override bool Equals(object? other) => this.Equals(other as SyntaxNode.GreenNode);
        /// <inheritdoc/>
        public override bool Equals([AllowNull] SyntaxNode.GreenNode other) => other is GreenNode o && object.Equals(this.OpenParenthesis, o.OpenParenthesis) && object.Equals(this.Parameters, o.Parameters) && object.Equals(this.CloseParenthesis, o.CloseParenthesis);
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(this.OpenParenthesis, this.Parameters, this.CloseParenthesis);
        public override IEnumerable<KeyValuePair<string, object?>> Children
        {
            get
            {
                yield return new(nameof(this.OpenParenthesis), this.OpenParenthesis);
                yield return new(nameof(this.Parameters), this.Parameters);
                yield return new(nameof(this.CloseParenthesis), this.CloseParenthesis);
            }
        }

        public override ParameterListSyntax ToRedNode(SyntaxNode? parent) => new(parent, this);
    }

    /// <summary>
    /// The open parenthesis token.
    /// </summary>
    public SyntaxToken OpenParenthesis => this.Green.OpenParenthesis.ToRedNode(this);
    /// <summary>
    /// The parameters of the function.
    /// </summary>
    public SyntaxSequence<ParameterSyntax> Parameters => new(this.Green.Parameters, n => (ParameterSyntax)n.ToRedNode(this));
    /// <summary>
    /// The close parenthesis token.
    /// </summary>
    public SyntaxToken CloseParenthesis => this.Green.CloseParenthesis.ToRedNode(this);
    internal override GreenNode Green { get; }

    /// <summary>
    /// Creates a new instance of the <see cref = "ParameterListSyntax"> type.
    /// </summary>
    /// <param name = "parent">
    /// The parent node of this one.
    /// </param>
    /// <param name = "green">
    /// The wrapped green node.
    /// </param>
    internal ParameterListSyntax(SyntaxNode? parent, GreenNode green)
    {
        this.Parent = parent;
        this.Green = green;
    }
}

/// <summary>
/// Syntax for a single parameter in a function declaration.
/// </summary>
public sealed partial class ParameterSyntax : SyntaxNode
{
    new internal sealed partial class GreenNode : SyntaxNode.GreenNode
    {
        public SyntaxToken.GreenNode Name { get; }

        public TypeSpecifierSyntax.GreenNode TypeSpecifier { get; }

        public SyntaxToken.GreenNode? Comma { get; }

        /// <summary>
        /// Creates a new instance of the <see cref = "GreenNode"> type.
        /// </summary>
        public GreenNode(SyntaxToken.GreenNode name, TypeSpecifierSyntax.GreenNode typeSpecifier, SyntaxToken.GreenNode? comma)
        {
            this.Name = name;
            this.TypeSpecifier = typeSpecifier;
            this.Comma = comma;
        }

        /// <inheritdoc/>
        public override bool Equals(object? other) => this.Equals(other as SyntaxNode.GreenNode);
        /// <inheritdoc/>
        public override bool Equals([AllowNull] SyntaxNode.GreenNode other) => other is GreenNode o && object.Equals(this.Name, o.Name) && object.Equals(this.TypeSpecifier, o.TypeSpecifier) && object.Equals(this.Comma, o.Comma);
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(this.Name, this.TypeSpecifier, this.Comma);
        public override IEnumerable<KeyValuePair<string, object?>> Children
        {
            get
            {
                yield return new(nameof(this.Name), this.Name);
                yield return new(nameof(this.TypeSpecifier), this.TypeSpecifier);
                yield return new(nameof(this.Comma), this.Comma);
            }
        }

        public override ParameterSyntax ToRedNode(SyntaxNode? parent) => new(parent, this);
    }

    /// <summary>
    /// The name of the parameter.
    /// </summary>
    public SyntaxToken Name => this.Green.Name.ToRedNode(this);
    /// <summary>
    /// The type specifier of the parameter.
    /// </summary>
    public TypeSpecifierSyntax TypeSpecifier => this.Green.TypeSpecifier.ToRedNode(this);
    /// <summary>
    /// The comma token.
    /// </summary>
    public SyntaxToken? Comma => this.Green.Comma?.ToRedNode(this);
    internal override GreenNode Green { get; }

    /// <summary>
    /// Creates a new instance of the <see cref = "ParameterSyntax"> type.
    /// </summary>
    /// <param name = "parent">
    /// The parent node of this one.
    /// </param>
    /// <param name = "green">
    /// The wrapped green node.
    /// </param>
    internal ParameterSyntax(SyntaxNode? parent, GreenNode green)
    {
        this.Parent = parent;
        this.Green = green;
    }
}

/// <summary>
/// Syntax for specifying a type of a parameter or a variable.
/// </summary>
public sealed partial class TypeSpecifierSyntax : SyntaxNode
{
    new internal sealed partial class GreenNode : SyntaxNode.GreenNode
    {
        public SyntaxToken.GreenNode Colon { get; }

        public TypeSyntax.GreenNode Type { get; }

        /// <summary>
        /// Creates a new instance of the <see cref = "GreenNode"> type.
        /// </summary>
        public GreenNode(SyntaxToken.GreenNode colon, TypeSyntax.GreenNode type)
        {
            this.Colon = colon;
            this.Type = type;
        }

        /// <inheritdoc/>
        public override bool Equals(object? other) => this.Equals(other as SyntaxNode.GreenNode);
        /// <inheritdoc/>
        public override bool Equals([AllowNull] SyntaxNode.GreenNode other) => other is GreenNode o && object.Equals(this.Colon, o.Colon) && object.Equals(this.Type, o.Type);
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(this.Colon, this.Type);
        public override IEnumerable<KeyValuePair<string, object?>> Children
        {
            get
            {
                yield return new(nameof(this.Colon), this.Colon);
                yield return new(nameof(this.Type), this.Type);
            }
        }

        public override TypeSpecifierSyntax ToRedNode(SyntaxNode? parent) => new(parent, this);
    }

    /// <summary>
    /// The colon token.
    /// </summary>
    public SyntaxToken Colon => this.Green.Colon.ToRedNode(this);
    /// <summary>
    /// The specified type.
    /// </summary>
    public TypeSyntax Type => this.Green.Type.ToRedNode(this);
    internal override GreenNode Green { get; }

    /// <summary>
    /// Creates a new instance of the <see cref = "TypeSpecifierSyntax"> type.
    /// </summary>
    /// <param name = "parent">
    /// The parent node of this one.
    /// </param>
    /// <param name = "green">
    /// The wrapped green node.
    /// </param>
    internal TypeSpecifierSyntax(SyntaxNode? parent, GreenNode green)
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
        public SyntaxToken.GreenNode OpenBrace { get; }

        public SyntaxToken.GreenNode CloseBrace { get; }

        /// <summary>
        /// Creates a new instance of the <see cref = "GreenNode"> type.
        /// </summary>
        public GreenNode(SyntaxToken.GreenNode openBrace, SyntaxToken.GreenNode closeBrace)
        {
            this.OpenBrace = openBrace;
            this.CloseBrace = closeBrace;
        }

        /// <inheritdoc/>
        public override bool Equals(object? other) => this.Equals(other as SyntaxNode.GreenNode);
        /// <inheritdoc/>
        public override bool Equals([AllowNull] SyntaxNode.GreenNode other) => other is GreenNode o && object.Equals(this.OpenBrace, o.OpenBrace) && object.Equals(this.CloseBrace, o.CloseBrace);
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(this.OpenBrace, this.CloseBrace);
        public override IEnumerable<KeyValuePair<string, object?>> Children
        {
            get
            {
                yield return new(nameof(this.OpenBrace), this.OpenBrace);
                yield return new(nameof(this.CloseBrace), this.CloseBrace);
            }
        }

        public override BlockExpressionSyntax ToRedNode(SyntaxNode? parent) => new(parent, this);
    }

    /// <summary>
    /// The open brace token.
    /// </summary>
    public SyntaxToken OpenBrace => this.Green.OpenBrace.ToRedNode(this);
    /// <summary>
    /// The close brace token.
    /// </summary>
    public SyntaxToken CloseBrace => this.Green.CloseBrace.ToRedNode(this);
    internal override GreenNode Green { get; }

    /// <summary>
    /// Creates a new instance of the <see cref = "BlockExpressionSyntax"> type.
    /// </summary>
    /// <param name = "parent">
    /// The parent node of this one.
    /// </param>
    /// <param name = "green">
    /// The wrapped green node.
    /// </param>
    internal BlockExpressionSyntax(SyntaxNode? parent, GreenNode green)
    {
        this.Parent = parent;
        this.Green = green;
    }
}

/// <summary>
/// A simple identifier.
/// </summary>
public sealed partial class IdentifierSyntax : TypeSyntax
{
    new internal sealed partial class GreenNode : TypeSyntax.GreenNode
    {
        public SyntaxToken.GreenNode Identifier { get; }

        /// <summary>
        /// Creates a new instance of the <see cref = "GreenNode"> type.
        /// </summary>
        public GreenNode(SyntaxToken.GreenNode identifier)
        {
            this.Identifier = identifier;
        }

        /// <inheritdoc/>
        public override bool Equals(object? other) => this.Equals(other as SyntaxNode.GreenNode);
        /// <inheritdoc/>
        public override bool Equals([AllowNull] SyntaxNode.GreenNode other) => other is GreenNode o && object.Equals(this.Identifier, o.Identifier);
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(this.Identifier);
        public override IEnumerable<KeyValuePair<string, object?>> Children
        {
            get
            {
                yield return new(nameof(this.Identifier), this.Identifier);
            }
        }

        public override IdentifierSyntax ToRedNode(SyntaxNode? parent) => new(parent, this);
    }

    /// <summary>
    /// The identifier token.
    /// </summary>
    public SyntaxToken Identifier => this.Green.Identifier.ToRedNode(this);
    internal override GreenNode Green { get; }

    /// <summary>
    /// Creates a new instance of the <see cref = "IdentifierSyntax"> type.
    /// </summary>
    /// <param name = "parent">
    /// The parent node of this one.
    /// </param>
    /// <param name = "green">
    /// The wrapped green node.
    /// </param>
    internal IdentifierSyntax(SyntaxNode? parent, GreenNode green)
    {
        this.Parent = parent;
        this.Green = green;
    }
}

/// <summary>
/// A prefix unary expression.
/// </summary>
public sealed partial class PrefixUnaryExpressionSyntax : ExpressionSyntax
{
    new internal sealed partial class GreenNode : ExpressionSyntax.GreenNode
    {
        public SyntaxToken.GreenNode Operator { get; }

        public ExpressionSyntax.GreenNode Operand { get; }

        /// <summary>
        /// Creates a new instance of the <see cref = "GreenNode"> type.
        /// </summary>
        public GreenNode(SyntaxToken.GreenNode @operator, ExpressionSyntax.GreenNode operand)
        {
            this.Operator = @operator;
            this.Operand = operand;
        }

        /// <inheritdoc/>
        public override bool Equals(object? other) => this.Equals(other as SyntaxNode.GreenNode);
        /// <inheritdoc/>
        public override bool Equals([AllowNull] SyntaxNode.GreenNode other) => other is GreenNode o && object.Equals(this.Operator, o.Operator) && object.Equals(this.Operand, o.Operand);
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(this.Operator, this.Operand);
        public override IEnumerable<KeyValuePair<string, object?>> Children
        {
            get
            {
                yield return new(nameof(this.Operator), this.Operator);
                yield return new(nameof(this.Operand), this.Operand);
            }
        }

        public override PrefixUnaryExpressionSyntax ToRedNode(SyntaxNode? parent) => new(parent, this);
    }

    /// <summary>
    /// The operator token.
    /// </summary>
    public SyntaxToken Operator => this.Green.Operator.ToRedNode(this);
    /// <summary>
    /// The operand expression.
    /// </summary>
    public ExpressionSyntax Operand => this.Green.Operand.ToRedNode(this);
    internal override GreenNode Green { get; }

    /// <summary>
    /// Creates a new instance of the <see cref = "PrefixUnaryExpressionSyntax"> type.
    /// </summary>
    /// <param name = "parent">
    /// The parent node of this one.
    /// </param>
    /// <param name = "green">
    /// The wrapped green node.
    /// </param>
    internal PrefixUnaryExpressionSyntax(SyntaxNode? parent, GreenNode green)
    {
        this.Parent = parent;
        this.Green = green;
    }
}

/// <summary>
/// A binary expression.
/// </summary>
public sealed partial class BinaryExpressionSyntax : ExpressionSyntax
{
    new internal sealed partial class GreenNode : ExpressionSyntax.GreenNode
    {
        public ExpressionSyntax.GreenNode Left { get; }

        public SyntaxToken.GreenNode Operator { get; }

        public ExpressionSyntax.GreenNode Right { get; }

        /// <summary>
        /// Creates a new instance of the <see cref = "GreenNode"> type.
        /// </summary>
        public GreenNode(ExpressionSyntax.GreenNode left, SyntaxToken.GreenNode @operator, ExpressionSyntax.GreenNode right)
        {
            this.Left = left;
            this.Operator = @operator;
            this.Right = right;
        }

        /// <inheritdoc/>
        public override bool Equals(object? other) => this.Equals(other as SyntaxNode.GreenNode);
        /// <inheritdoc/>
        public override bool Equals([AllowNull] SyntaxNode.GreenNode other) => other is GreenNode o && object.Equals(this.Left, o.Left) && object.Equals(this.Operator, o.Operator) && object.Equals(this.Right, o.Right);
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(this.Left, this.Operator, this.Right);
        public override IEnumerable<KeyValuePair<string, object?>> Children
        {
            get
            {
                yield return new(nameof(this.Left), this.Left);
                yield return new(nameof(this.Operator), this.Operator);
                yield return new(nameof(this.Right), this.Right);
            }
        }

        public override BinaryExpressionSyntax ToRedNode(SyntaxNode? parent) => new(parent, this);
    }

    /// <summary>
    /// The left operand.
    /// </summary>
    public ExpressionSyntax Left => this.Green.Left.ToRedNode(this);
    /// <summary>
    /// The operator token.
    /// </summary>
    public SyntaxToken Operator => this.Green.Operator.ToRedNode(this);
    /// <summary>
    /// The right operand.
    /// </summary>
    public ExpressionSyntax Right => this.Green.Right.ToRedNode(this);
    internal override GreenNode Green { get; }

    /// <summary>
    /// Creates a new instance of the <see cref = "BinaryExpressionSyntax"> type.
    /// </summary>
    /// <param name = "parent">
    /// The parent node of this one.
    /// </param>
    /// <param name = "green">
    /// The wrapped green node.
    /// </param>
    internal BinaryExpressionSyntax(SyntaxNode? parent, GreenNode green)
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
    /// Constructs a <see cref = "SyntaxToken"/> from the given arguments.
    /// </summary>
    /// <param name = "leadingTrivia">
    /// The trivia that comes before this token in the syntax tree.
    /// </param>
    /// <param name = "token">
    /// The token itself that is the significant part of the tree.
    /// </param>
    /// <param name = "trailingTrivia">
    /// The trivia that comes after this token in the syntax tree.
    /// </param>
    /// <returns>
    /// The constructed syntax node.
    /// </returns>
    public static SyntaxToken Token(Sequence<Token> leadingTrivia, Token token, Sequence<Token> trailingTrivia) => new(null, new(leadingTrivia, token, trailingTrivia));
    /// <summary>
    /// Constructs a <see cref = "ModuleDeclarationSyntax"/> from the given arguments.
    /// </summary>
    /// <param name = "declarations">
    /// The declarations contained in the module.
    /// </param>
    /// <param name = "end">
    /// The end of file token.
    /// </param>
    /// <returns>
    /// The constructed syntax node.
    /// </returns>
    public static ModuleDeclarationSyntax ModuleDeclaration(IEnumerable<DeclarationSyntax> declarations, SyntaxToken end) => new(null, new(declarations.Select(n => n.Green).ToSequence(), end.Green));
    /// <summary>
    /// Constructs a <see cref = "FunctionDeclarationSyntax"/> from the given arguments.
    /// </summary>
    /// <param name = "funcKeyword">
    /// The function keyword.
    /// </param>
    /// <param name = "name">
    /// The name of the function.
    /// </param>
    /// <param name = "parameterList">
    /// The parameters of the function.
    /// </param>
    /// <param name = "typeSpecifier">
    /// The return type specifier.
    /// </param>
    /// <param name = "body">
    /// The body of the function.
    /// </param>
    /// <returns>
    /// The constructed syntax node.
    /// </returns>
    public static FunctionDeclarationSyntax FunctionDeclaration(SyntaxToken funcKeyword, SyntaxToken name, ParameterListSyntax parameterList, TypeSpecifierSyntax? typeSpecifier, ExpressionSyntax body) => new(null, new(funcKeyword.Green, name.Green, parameterList.Green, typeSpecifier?.Green, body.Green));
    /// <summary>
    /// Constructs a <see cref = "ParameterListSyntax"/> from the given arguments.
    /// </summary>
    /// <param name = "openParenthesis">
    /// The open parenthesis token.
    /// </param>
    /// <param name = "parameters">
    /// The parameters of the function.
    /// </param>
    /// <param name = "closeParenthesis">
    /// The close parenthesis token.
    /// </param>
    /// <returns>
    /// The constructed syntax node.
    /// </returns>
    public static ParameterListSyntax ParameterList(SyntaxToken openParenthesis, IEnumerable<ParameterSyntax> parameters, SyntaxToken closeParenthesis) => new(null, new(openParenthesis.Green, parameters.Select(n => n.Green).ToSequence(), closeParenthesis.Green));
    /// <summary>
    /// Constructs a <see cref = "ParameterSyntax"/> from the given arguments.
    /// </summary>
    /// <param name = "name">
    /// The name of the parameter.
    /// </param>
    /// <param name = "typeSpecifier">
    /// The type specifier of the parameter.
    /// </param>
    /// <param name = "comma">
    /// The comma token.
    /// </param>
    /// <returns>
    /// The constructed syntax node.
    /// </returns>
    public static ParameterSyntax Parameter(SyntaxToken name, TypeSpecifierSyntax typeSpecifier, SyntaxToken? comma) => new(null, new(name.Green, typeSpecifier.Green, comma?.Green));
    /// <summary>
    /// Constructs a <see cref = "TypeSpecifierSyntax"/> from the given arguments.
    /// </summary>
    /// <param name = "colon">
    /// The colon token.
    /// </param>
    /// <param name = "type">
    /// The specified type.
    /// </param>
    /// <returns>
    /// The constructed syntax node.
    /// </returns>
    public static TypeSpecifierSyntax TypeSpecifier(SyntaxToken colon, TypeSyntax type) => new(null, new(colon.Green, type.Green));
    /// <summary>
    /// Constructs a <see cref = "BlockExpressionSyntax"/> from the given arguments.
    /// </summary>
    /// <param name = "openBrace">
    /// The open brace token.
    /// </param>
    /// <param name = "closeBrace">
    /// The close brace token.
    /// </param>
    /// <returns>
    /// The constructed syntax node.
    /// </returns>
    public static BlockExpressionSyntax BlockExpression(SyntaxToken openBrace, SyntaxToken closeBrace) => new(null, new(openBrace.Green, closeBrace.Green));
    /// <summary>
    /// Constructs a <see cref = "IdentifierSyntax"/> from the given arguments.
    /// </summary>
    /// <param name = "identifier">
    /// The identifier token.
    /// </param>
    /// <returns>
    /// The constructed syntax node.
    /// </returns>
    public static IdentifierSyntax Identifier(SyntaxToken identifier) => new(null, new(identifier.Green));
    /// <summary>
    /// Constructs a <see cref = "PrefixUnaryExpressionSyntax"/> from the given arguments.
    /// </summary>
    /// <param name = "@operator">
    /// The operator token.
    /// </param>
    /// <param name = "operand">
    /// The operand expression.
    /// </param>
    /// <returns>
    /// The constructed syntax node.
    /// </returns>
    public static PrefixUnaryExpressionSyntax PrefixUnaryExpression(SyntaxToken @operator, ExpressionSyntax operand) => new(null, new(@operator.Green, operand.Green));
    /// <summary>
    /// Constructs a <see cref = "BinaryExpressionSyntax"/> from the given arguments.
    /// </summary>
    /// <param name = "left">
    /// The left operand.
    /// </param>
    /// <param name = "@operator">
    /// The operator token.
    /// </param>
    /// <param name = "right">
    /// The right operand.
    /// </param>
    /// <returns>
    /// The constructed syntax node.
    /// </returns>
    public static BinaryExpressionSyntax BinaryExpression(ExpressionSyntax left, SyntaxToken @operator, ExpressionSyntax right) => new(null, new(left.Green, @operator.Green, right.Green));
}
#nullable restore
#pragma warning restore CS0282
#pragma warning restore CS0109
