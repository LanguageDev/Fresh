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
public static class CodeGenerator
{
    public static string Generate(Tree tree) =>
         GenerateCompilationUnit(tree)
        .NormalizeWhitespace()
        .GetText()
        .ToString();

    private static CompilationUnitSyntax GenerateCompilationUnit(Tree tree) =>
         CompilationUnit()
        .WithMembers(List(tree.Nodes.Select(GenerateRedNodeClass)));

    private static MemberDeclarationSyntax GenerateRedNodeClass(Node node)
    {
        // Collect class modifiers
        var modifiers = new List<SyntaxToken> { Token(SyntaxKind.PublicKeyword) };
        if (node.IsAbstract) modifiers.Add(Token(SyntaxKind.AbstractKeyword));

        // Collect class members
        var members = new List<MemberDeclarationSyntax>();

        // Add green node, if not abstract
        if (!node.IsAbstract) members.Add(GenerateGreenNodeClass(node));

        // Class properties
        members.AddRange(node.Attributes.Select(GenerateRedClassProperty));

        // Add green node property, if not abstract
        if (!node.IsAbstract)
        {
            members.Add(PropertyDeclaration(TranslateType("GreenNode"), "Green")
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword)))
                .WithAccessorList(AccessorList(SingletonList(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken))))));
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

    private static MemberDeclarationSyntax GenerateGreenNodeClass(Node node)
    {
        var decl = ClassDeclaration("GreenNode")
            .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword)))
            .WithMembers(List(node.Attributes.Select(GenerateGreenClassProperty)));
        return decl;
    }

    private static MemberDeclarationSyntax GenerateRedClassProperty(Attribute attribute) =>
         PropertyDeclaration(TranslateType(attribute.Type), attribute.Name)
        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
        .WithExpressionBody(ArrowExpressionClause(MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName("Green"),
            IdentifierName(attribute.Name))))
        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

    private static MemberDeclarationSyntax GenerateGreenClassProperty(Attribute attribute) =>
         PropertyDeclaration(TranslateType(attribute.Type), attribute.Name)
        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
        .WithAccessorList(AccessorList(SingletonList(
            AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))));

    private static void GenerateNodeFactoryMethod(Node node) =>
        throw new NotImplementedException();

    // NOTE: Quite cheesy solution
    private static TypeSyntax TranslateType(string type) =>
        ParseTypeName(type.Replace('[', '<').Replace(']', '>'));
}
