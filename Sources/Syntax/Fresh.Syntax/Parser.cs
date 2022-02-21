// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fresh.Common;

namespace Fresh.Syntax;

/// <summary>
/// Produces a syntax tree from a sequence of tokens.
/// </summary>
public sealed class Parser
{
    public static FileDeclarationSyntax Parse(IEnumerable<SyntaxToken> tokens)
    {
        var parser = new Parser(tokens.GetEnumerator());
        return parser.ParseFileDeclaration();
    }

    private readonly IEnumerator<SyntaxToken> tokens;
    private readonly RingBuffer<SyntaxToken> peekBuffer = new();

    private Parser(IEnumerator<SyntaxToken> tokenSource)
    {
        this.tokens = tokenSource;
    }

    public FileDeclarationSyntax ParseFileDeclaration()
    {
        var declarations = new List<DeclarationSyntax>();
        SyntaxToken end;
        while (!this.TryMatch(TokenType.End, out end))
        {
            declarations.Add(this.ParseDeclaration());
        }
        return SyntaxFactory.FileDeclaration(declarations.ToSequence(), end);
    }

    private DeclarationSyntax ParseDeclaration()
    {
        Debug.Assert(this.TryPeek(0, out var head));
        return head.Token.Type switch
        {
            TokenType.KeywordFunc => this.ParseFunctionDeclaration(),
            // TODO: Handle proper errors
            _ => throw new NotImplementedException(),
        };
    }

    private FunctionDeclarationSyntax ParseFunctionDeclaration()
    {
        var funcKw = this.Take();
        Debug.Assert(funcKw.Type == TokenType.KeywordFunc);
        // TODO: Handle proper errors
        if (!this.TryMatch(TokenType.Identifier, out var name)) throw new NotImplementedException();
        var paramList = this.ParseParameterList();
        var body = this.ParseExpression();
        return SyntaxFactory.FunctionDeclaration(funcKw, name, paramList, body);
    }

    private ParameterListSyntax ParseParameterList()
    {
        // TODO: Handle proper errors
        if (!this.TryMatch(TokenType.OpenParenthesis, out var openParen)) throw new NotImplementedException();
        // TODO: Handle proper errors
        if (!this.TryMatch(TokenType.CloseParenthesis, out var closeParen)) throw new NotImplementedException();
        return SyntaxFactory.ParameterList(openParen, closeParen);
    }

    private ExpressionSyntax ParseExpression()
    {
        Debug.Assert(this.TryPeek(0, out var head));
        return head.Token.Type switch
        {
            TokenType.OpenBrace => this.ParseBlockExpression(),
            // TODO: Handle proper errors
            _ => throw new NotImplementedException(),
        };
    }

    private BlockExpressionSyntax ParseBlockExpression()
    {
        var openBrace = this.Take();
        Debug.Assert(openBrace.Type == TokenType.OpenBrace);
        // TODO: Handle proper errors
        if (!this.TryMatch(TokenType.CloseBrace, out var closeBrace)) throw new NotImplementedException();
        return SyntaxFactory.BlockExpression(openBrace, closeBrace);
    }

    private bool TryMatch(TokenType tokenType, [MaybeNullWhen(false)] out SyntaxToken token)
    {
        if (!this.TryPeek(0, out token) || token.Type != tokenType) return false;
        token = this.Take();
        return true;
    }

    private SyntaxToken Take()
    {
        if (!this.TryPeek(0, out _)) throw new InvalidOperationException($"Could nod take a token");
        return this.peekBuffer.RemoveFront();
    }

    private bool TryPeek(int offset, [MaybeNullWhen(false)] out SyntaxToken token)
    {
        // Read as long as there aren't enough tokens in the peek buffer
        while (this.peekBuffer.Count <= offset)
        {
            if (!this.tokens.MoveNext())
            {
                // No more to read
                token = default;
                return false;
            }
            // This token was read successfully
            this.peekBuffer.AddBack(this.tokens.Current);
        }
        // We have enough tokens in the buffer
        token = this.peekBuffer[offset];
        return true;
    }
}
