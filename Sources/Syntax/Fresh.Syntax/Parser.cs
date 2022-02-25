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
    public static ModuleDeclarationSyntax Parse(IEnumerable<SyntaxToken> tokens)
    {
        var parser = new Parser(tokens.GetEnumerator());
        return parser.ParseFileDeclaration();
    }

    private readonly IEnumerator<SyntaxToken> tokens;
    private readonly RingBuffer<SyntaxToken.GreenNode> peekBuffer = new();

    private Parser(IEnumerator<SyntaxToken> tokenSource)
    {
        this.tokens = tokenSource;
    }

    public ModuleDeclarationSyntax ParseFileDeclaration()
    {
        var declarations = new List<DeclarationSyntax.GreenNode>();
        SyntaxToken.GreenNode end;
        while (!this.TryMatch(TokenType.End, out end))
        {
            declarations.Add(this.ParseDeclaration());
        }
        return new(null, new(declarations.ToSequence(), end));
    }

    private DeclarationSyntax.GreenNode ParseDeclaration()
    {
        Debug.Assert(this.TryPeek(0, out var head));
        return head.Token.Type switch
        {
            TokenType.KeywordFunc => this.ParseFunctionDeclaration(),
            // TODO: Handle proper errors
            _ => throw new NotImplementedException(),
        };
    }

    private FunctionDeclarationSyntax.GreenNode ParseFunctionDeclaration()
    {
        var funcKw = this.Take();
        Debug.Assert(funcKw.Type == TokenType.KeywordFunc);
        // TODO: Handle proper errors
        if (!this.TryMatch(TokenType.Identifier, out var name)) throw new NotImplementedException();
        var paramList = this.ParseParameterList();
        TypeSpecifierSyntax.GreenNode? typeSpecifier = null;
        if (this.TryMatch(TokenType.Colon, out var colon))
        {
            var returnType = this.ParseType();
            typeSpecifier = new(colon, returnType);
        }
        var body = this.ParseExpression();
        return new(funcKw, name, paramList, typeSpecifier, body);
    }

    private ParameterListSyntax.GreenNode ParseParameterList()
    {
        // TODO: Handle proper errors
        if (!this.TryMatch(TokenType.OpenParenthesis, out var openParen)) throw new NotImplementedException();

        var parameters = new List<ParameterSyntax.GreenNode>();
        while (!this.TryPeek(TokenType.CloseParenthesis))
        {
            // TODO: Handle proper errors
            if (!this.TryMatch(TokenType.Identifier, out var parameterName)) throw new NotImplementedException();

            // TODO: Handle proper errors
            if (!this.TryMatch(TokenType.Colon, out var parameterColon)) throw new NotImplementedException();

            var parameterType = this.ParseType();
            var keepGoing = this.TryMatch(TokenType.Comma, out var comma);

            parameters.Add(new(parameterName, new(parameterColon, parameterType), keepGoing ? comma : null));

            if (!keepGoing) break;
        }

        // TODO: Handle proper errors
        if (!this.TryMatch(TokenType.CloseParenthesis, out var closeParen)) throw new NotImplementedException();

        return new(openParen, parameters.ToSequence(), closeParen);
    }

    private ExpressionSyntax.GreenNode ParseExpression()
    {
        Debug.Assert(this.TryPeek(0, out var head));
        return head.Token.Type switch
        {
            TokenType.OpenBrace => this.ParseBlockExpression(),
            // TODO: Handle proper errors
            _ => throw new NotImplementedException(),
        };
    }

    private BlockExpressionSyntax.GreenNode ParseBlockExpression()
    {
        var openBrace = this.Take();
        Debug.Assert(openBrace.Type == TokenType.OpenBrace);
        // TODO: Handle proper errors
        if (!this.TryMatch(TokenType.CloseBrace, out var closeBrace)) throw new NotImplementedException();
        return new(openBrace, closeBrace);
    }

    private TypeSyntax.GreenNode ParseType()
    {
        if (this.TryMatch(TokenType.Identifier, out var typeIdent)) return new IdentifierSyntax.GreenNode(typeIdent);
        // TODO: Handle proper errors
        throw new NotImplementedException();
    }

    private bool TryMatch(TokenType tokenType, [MaybeNullWhen(false)] out SyntaxToken.GreenNode token)
    {
        if (!this.TryPeek(0, out token) || token.Type != tokenType) return false;
        token = this.Take();
        return true;
    }

    private bool TryPeek(TokenType tokenType) =>
        this.TryPeek(0, out var token) && token.Type == tokenType;

    private SyntaxToken.GreenNode Take()
    {
        if (!this.TryPeek(0, out _)) throw new InvalidOperationException($"Could nod take a token");
        return this.peekBuffer.RemoveFront();
    }

    private bool TryPeek(int offset, [MaybeNullWhen(false)] out SyntaxToken.GreenNode token)
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
            this.peekBuffer.AddBack(this.tokens.Current.Green);
        }
        // We have enough tokens in the buffer
        token = this.peekBuffer[offset];
        return true;
    }
}
