// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fresh.RedGreenTree.Cli.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Attribute = Fresh.RedGreenTree.Cli.Model.Attribute;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Fresh.RedGreenTree.Cli;

/// <summary>
/// Provides C# code generation for the tree model.
/// </summary>
public sealed class CodeGenerator
{
    public static string Generate(Tree tree) => new CodeGenerator(tree)
        .GenerateCompilationUnit()
        .NormalizeWhitespace()
        .GetText()
        .ToString();

    private readonly Tree tree;
    private readonly Dictionary<string, Node> nodes;

    private CodeGenerator(Tree tree)
    {
        this.tree = tree;
        this.nodes = tree.Nodes.ToDictionary(n => n.Name);
    }

    private CompilationUnitSyntax GenerateCompilationUnit()
    {
        var members = new List<MemberDeclarationSyntax>();
        members.AddRange(this.tree.Nodes.Select(this.GenerateRedNodeClass));

        if (this.tree.Factory is not null)
        {
            members.Add(ClassDeclaration(this.tree.Factory)
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                .WithMembers(List(this.tree.Nodes
                    .Where(n => !n.IsAbstract)
                    .Select(this.GenerateNodeFactoryMethod))));
        }

        return CompilationUnit()
            .WithMembers(List(members));
    }

    private MemberDeclarationSyntax GenerateRedNodeClass(Node node)
    {
        // Collect class modifiers
        var modifiers = new List<SyntaxToken> { Token(SyntaxKind.PublicKeyword) };
        if (node.IsAbstract) modifiers.Add(Token(SyntaxKind.AbstractKeyword));

        // Collect class members
        var members = new List<MemberDeclarationSyntax>();

        // Add green node, if not abstract
        if (!node.IsAbstract) members.Add(this.GenerateGreenNodeClass(node));

        // Class properties
        members.AddRange(node.Attributes.Select(attr => this.GenerateRedProperty(node, attr)));

        // Add green node property and constructor, if not abstract
        if (!node.IsAbstract)
        {
            // Green node property
            members.Add(PropertyDeclaration(IdentifierName("GreenNode"), "Green")
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword)))
                .WithAccessorList(AccessorList(SingletonList(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken))))));

            // Constructor
            members.Add(ConstructorDeclaration(node.Name)
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword)))
                .WithParameterList(ParameterList(SingletonSeparatedList(Parameter(Identifier("green")).WithType(IdentifierName("GreenNode")))))
                .WithBody(Block(SingletonList(ExpressionStatement(AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ThisExpression(),
                        IdentifierName("Green")),
                    IdentifierName("green")))))));
        }

        var decl = ClassDeclaration(node.Name)
            .WithModifiers(TokenList(modifiers))
            .WithMembers(List(members));

        // Add base
        if (node.Base is not null)
        {
            decl = decl.WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(TranslateType(node.Base.Name)))));
        }

        return decl;
    }

    private MemberDeclarationSyntax GenerateGreenNodeClass(Node node)
    {
        var members = new List<MemberDeclarationSyntax>();

        // Add properties
        members.AddRange(node.Attributes.Select(this.GenerateGreenProperty));

        // Add constructor
        members.Add(ConstructorDeclaration("GreenNode")
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(ParameterList(SeparatedList(node.Attributes.Select(attr =>
                Parameter(Identifier(ToCamelCase(attr.Name))).WithType(TranslateType(attr.Type))))))
            .WithBody(Block(List(node.Attributes.Select(attr => ExpressionStatement(AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression(),
                    IdentifierName(attr.Name)),
                IdentifierName(ToCamelCase(attr.Name)))))))));

        var decl = ClassDeclaration("GreenNode")
            .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword)))
            .WithMembers(List(members));
        return decl;
    }

    private MemberDeclarationSyntax GenerateRedProperty(Node node, Attribute attribute) =>
        PropertyDeclaration(TranslateType(attribute.Type), attribute.Name)
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithExpressionBody(ArrowExpressionClause(
                this.nodes.TryGetValue(attribute.Type, out var attrNode)
                ? InvocationExpression(MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName("Green")),
                        IdentifierName(attribute.Name)),
                    IdentifierName("ToRed")))
                : MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName("Green")),
                    IdentifierName(attribute.Name))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

    private MemberDeclarationSyntax GenerateGreenProperty(Attribute attribute)
    {
        var decl = PropertyDeclaration(TranslateType(attribute.Type), attribute.Name)
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithAccessorList(AccessorList(SingletonList(
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))));
        return decl;
    }

    private MemberDeclarationSyntax GenerateNodeFactoryMethod(Node node) =>
         MethodDeclaration(
             IdentifierName(node.Name),
             node.Name.EndsWith("Syntax") ? node.Name[..^"Syntax".Length] : node.Name)
        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
        .WithParameterList(ParameterList(SeparatedList(node.Attributes.Select(attr =>
             Parameter(Identifier(ToCamelCase(attr.Name))).WithType(TranslateType(attr.Type))))))
        .WithExpressionBody(ArrowExpressionClause(
            ObjectCreationExpression(IdentifierName(node.Name))
                .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(
                    ObjectCreationExpression(QualifiedName(IdentifierName(node.Name), IdentifierName("GreenNode")))
                        .WithArgumentList(ArgumentList(SeparatedList(node.Attributes.Select(attr =>
                            Argument(IdentifierName(ToCamelCase(attr.Name)))))))))))))
        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

    // NOTE: Quite cheesy solution
    private static TypeSyntax TranslateType(string type) =>
        ParseTypeName(type.Replace('[', '<').Replace(']', '>'));

    private static string ToCamelCase(string s) => $"{char.ToLower(s[0])}{s[1..]}";
}
