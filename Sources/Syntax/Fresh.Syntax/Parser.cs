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
    private enum ParseMode
    {
        Expression,
        Statement,
    }

    public static ModuleDeclarationSyntax Parse(IEnumerable<SyntaxToken> tokens)
    {
        var parser = new Parser(tokens.GetEnumerator());
        return parser.ParseModuleDeclaration();
    }

    private readonly IEnumerator<SyntaxToken> tokens;
    private readonly RingBuffer<SyntaxToken.GreenNode> peekBuffer = new();

    private Parser(IEnumerator<SyntaxToken> tokenSource)
    {
        this.tokens = tokenSource;
    }

    public ModuleDeclarationSyntax ParseModuleDeclaration()
    {
        var declarations = new List<DeclarationSyntax.GreenNode>();
        while (!this.TryPeek(TokenType.End))
        {
            declarations.Add(this.ParseDeclaration());
        }
        var end = this.Expect(TokenType.End);
        return new(null, new(SyntaxFactory.SyntaxSequence(declarations), end));
    }

    private DeclarationSyntax.GreenNode ParseDeclaration()
    {
        var head = this.Peek();
        return head.Token.Type switch
        {
            TokenType.KeywordFunc => this.ParseFunctionDeclaration(),
            // TODO: Handle proper errors
            _ => throw new NotImplementedException(),
        };
    }

    private StatementSyntax.GreenNode ParseStatement()
    {
        var head = this.Peek();
        return head.Token.Type switch
        {
            TokenType.KeywordVar or TokenType.KeywordVal => this.ParseVariableDeclaration(),
            TokenType.OpenBrace =>
                new ExpressionStatementSyntax.GreenNode(this.ParseBlockExpression(ParseMode.Statement), null),
            TokenType.KeywordIf =>
                new ExpressionStatementSyntax.GreenNode(this.ParseIfExpression(ParseMode.Statement), null),
            TokenType.KeywordWhile =>
                new ExpressionStatementSyntax.GreenNode(this.ParseWhileExpression(ParseMode.Statement), null),
            _ => this.ParseExpressionStatement(),
        };
    }

    private ExpressionStatementSyntax.GreenNode ParseExpressionStatement()
    {
        var expr = this.ParseExpression();
        var semicolon = this.Expect(TokenType.Semicolon);
        return new(expr, semicolon);
    }

    private VariableDeclarationSyntax.GreenNode ParseVariableDeclaration()
    {
        if (!this.TryPeek(TokenType.KeywordVar) && !this.TryPeek(TokenType.KeywordVal))
        {
            // TODO: Proper error
            throw new NotImplementedException("TODO: Syntax error");
        }
        var kw = this.Take();
        var name = this.Expect(TokenType.Identifier);
        // Type
        TypeSpecifierSyntax.GreenNode? typeSpecifier = null;
        if (this.TryMatch(TokenType.Colon, out var colon))
        {
            var type = this.ParseType();
            typeSpecifier = new(colon, type);
        }
        // Value
        ValueSpecifierSyntax.GreenNode? valueSpecifier = null;
        if (this.TryMatch(TokenType.Assign, out var assign))
        {
            var value = this.ParseExpression();
            valueSpecifier = new(assign, value, null);
        }
        // Semicolon
        var semicolon = this.Expect(TokenType.Semicolon);
        return new(kw, name, typeSpecifier, valueSpecifier, semicolon);
    }

    private FunctionDeclarationSyntax.GreenNode ParseFunctionDeclaration()
    {
        var funcKw = this.Expect(TokenType.KeywordFunc);
        var name = this.Expect(TokenType.Identifier);
        var paramList = this.ParseParameterList();
        // Return type
        TypeSpecifierSyntax.GreenNode? typeSpecifier = null;
        if (this.TryMatch(TokenType.Colon, out var colon))
        {
            var returnType = this.ParseType();
            typeSpecifier = new(colon, returnType);
        }
        // Body
        SyntaxNode.GreenNode body;
        if (this.TryMatch(TokenType.Assign, out var assign))
        {
            // = value ';'
            var value = this.ParseExpression();
            var semicol = this.Expect(TokenType.Semicolon);
            body = new ValueSpecifierSyntax.GreenNode(assign, value, semicol);
        }
        else if (this.TryPeek(TokenType.OpenBrace))
        {
            body = this.ParseBlockExpression(ParseMode.Statement);
        }
        else
        {
            // TODO: Proper error
            throw new NotImplementedException("TODO: Syntax error");
        }
        return new(funcKw, name, paramList, typeSpecifier, body);
    }

    private ParameterListSyntax.GreenNode ParseParameterList()
    {
        var openParen = this.Expect(TokenType.OpenParenthesis);

        var parameters = new List<ParameterSyntax.GreenNode>();
        while (!this.TryPeek(TokenType.CloseParenthesis))
        {
            var parameterName = this.Expect(TokenType.Identifier);
            var parameterColon = this.Expect(TokenType.Colon);
            var parameterType = this.ParseType();
            var keepGoing = this.TryMatch(TokenType.Comma, out var comma);

            parameters.Add(new(parameterName, new(parameterColon, parameterType), keepGoing ? comma : null));

            if (!keepGoing) break;
        }

        var closeParen = this.Expect(TokenType.CloseParenthesis);

        return new(openParen, SyntaxFactory.SyntaxSequence(parameters), closeParen);
    }

    private ArgumentListSyntax.GreenNode ParseArgumentList(TokenType open, TokenType close)
    {
        var openToken = this.Expect(open);

        var arguments = new List<ArgumentSyntax.GreenNode>();
        while (!this.TryPeek(close))
        {
            var value = this.ParseExpression();
            var keepGoing = this.TryMatch(TokenType.Comma, out var comma);

            arguments.Add(new(value, keepGoing ? comma : null));

            if (!keepGoing) break;
        }

        var closeToken = this.Expect(close);

        return new(openToken, SyntaxFactory.SyntaxSequence(arguments), closeToken);
    }

    private ExpressionSyntax.GreenNode ParseExpression()
    {
        var head = this.Peek();
        return head.Token.Type switch
        {
            TokenType.OpenBrace => this.ParseBlockExpression(ParseMode.Expression),
            // TODO: Handle proper errors
            _ => throw new NotImplementedException(),
        };
    }

    private BlockExpressionSyntax.GreenNode ParseBlockExpression(ParseMode parseMode)
    {
        var openBrace = this.Expect(TokenType.OpenBrace);
        var statements = new List<StatementSyntax.GreenNode>();
        ExpressionSyntax.GreenNode? value = null;
        while (!this.TryPeek(TokenType.CloseBrace))
        {
            if (parseMode == ParseMode.Statement)
            {
                var statement = this.ParseStatement();
                statements.Add(statement);
            }
            else
            {
                // This case is a little harder, this is potentially the value expression coming up
                // We peek ahead a token and see if it's a declaration, statement, or an expression that can
                // act as a statement
                var head = this.Peek();
                // Declaration and statement starting tokens are pretty simple
                // They are always considered a statement
                // Potential statement-like expressions ('if', 'while', block, ...) are parsed, and are
                // promoted to statement, if there's no closing brace yet
                // Anything else is parsed as an expression, and then promoted to statement, if a ';' follows
                switch (head.Type)
                {
                default:
                    // TODO: Proper error handling
                    throw new NotImplementedException("TODO: Syntax error");
                }
            }
        }
        var closeBrace = this.Expect(TokenType.CloseBrace);
        return new(openBrace, SyntaxFactory.SyntaxSequence(statements), value, closeBrace);
    }

    private IfExpressionSyntax.GreenNode ParseIfExpression(ParseMode parseMode)
    {
        var ifKw = this.Expect(TokenType.KeywordIf);
        var condition = this.ParseExpression();
        var thenKw = this.Expect(TokenType.KeywordThen);
        var thenBody = this.ParseByParseMode(parseMode);
        SyntaxToken.GreenNode? elseKw = null;
        SyntaxNode.GreenNode? elseBody = null;
        if (this.TryMatch(TokenType.KeywordElse, out var elseKw1))
        {
            elseKw = elseKw1;
            elseBody = this.ParseByParseMode(parseMode);
        }
        return new(ifKw, condition, thenKw, thenBody, elseKw, elseBody);
    }

    private WhileExpressionSyntax.GreenNode ParseWhileExpression(ParseMode parseMode)
    {
        var whileKw = this.Expect(TokenType.KeywordWhile);
        var condition = this.ParseExpression();
        var doKw = this.Expect(TokenType.KeywordDo);
        var body = this.ParseByParseMode(parseMode);
        return new(whileKw, condition, doKw, body);
    }

    private TypeSyntax.GreenNode ParseType()
    {
        if (this.TryMatch(TokenType.Identifier, out var typeIdent)) return new IdentifierSyntax.GreenNode(typeIdent);
        // TODO: Handle proper errors
        throw new NotImplementedException();
    }

    private SyntaxNode.GreenNode ParseByParseMode(ParseMode parseMode) => parseMode == ParseMode.Expression
        ? this.ParseExpression()
        : this.ParseStatement();

    // Elemental operations on syntax

    private SyntaxToken.GreenNode Expect(TokenType tokenType)
    {
        // TODO: Proper errors
        if (!this.TryMatch(tokenType, out var t)) throw new NotImplementedException("TODO: Syntax error");
        return t;
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

    private SyntaxToken.GreenNode Peek()
    {
        // NOTE: This should probably never fail when called because there must be an end token
        // TODO: Proper errors
        if (!this.TryPeek(0, out var t)) throw new InvalidOperationException("TODO: Error");
        return t;
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
