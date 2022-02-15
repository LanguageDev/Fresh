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

public abstract record class SyntaxNode(
    Sequence<CommentGroup>? LeadingTrivia = null,
    CommentGroup? Documentation = null,
    Sequence<CommentGroup>? TrailingTrivia = null);

public sealed record class CommentGroup(Sequence<Token> Comments) : SyntaxNode;

public abstract record class StatementSyntax : SyntaxNode;

public abstract record class DeclarationSyntax : StatementSyntax;

public sealed record class FunctionDeclarationSyntax(
    Token FuncKeyword,
    Token Name,
    ArgumentListSyntax ArgumentList) : DeclarationSyntax;

public sealed record class ArgumentListSyntax(
    Token OpenParenthesis,
    Token CloseParenthesis) : SyntaxNode;

public abstract record class ExpressionSyntax : SyntaxNode;

public sealed record class BlockSyntax(
    Token OpenBrace,
    Token CloseBrace) : ExpressionSyntax;
