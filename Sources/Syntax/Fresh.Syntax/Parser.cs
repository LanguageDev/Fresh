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
        return parser.ParseModuleDeclaration();
    }

    private enum ParseMode
    {
        Expression,
        Statement,
    }

    private enum OperatorKind
    {
        Prefix,
        Postfix,
        CallLike,
        LeftAssociative,
        RightAssociative,
    }

    private readonly record struct PredecenceDescriptor(OperatorKind Kind, OperatorDescriptor[] Operators);
    private readonly record struct OperatorDescriptor(TokenType Operator, TokenType? CloseOperator);

    private static PredecenceDescriptor PrefixOp(params TokenType[] operators) =>
        new(OperatorKind.Prefix, operators.Select(o => new OperatorDescriptor(o, null)).ToArray());

    private static PredecenceDescriptor PostfixOp(params TokenType[] operators) =>
        new(OperatorKind.Postfix, operators.Select(o => new OperatorDescriptor(o, null)).ToArray());

    private static PredecenceDescriptor LeftAssocOp(params TokenType[] operators) =>
        new(OperatorKind.LeftAssociative, operators.Select(o => new OperatorDescriptor(o, null)).ToArray());

    private static PredecenceDescriptor RightAssocOp(params TokenType[] operators) =>
        new(OperatorKind.RightAssociative, operators.Select(o => new OperatorDescriptor(o, null)).ToArray());

    private static PredecenceDescriptor CallOp(params (TokenType Open, TokenType Close)[] operators) =>
        new(OperatorKind.CallLike, operators.Select(ops => new OperatorDescriptor(ops.Open, ops.Close)).ToArray());

    // Low -> high precedence
    private static readonly PredecenceDescriptor[] PrecedenceTable = new[]
    {
        RightAssocOp(TokenType.Assign), // TODO: Missing compound operators
        LeftAssocOp(TokenType.OperatorOr),
        LeftAssocOp(TokenType.OperatorAnd),
        LeftAssocOp(TokenType.OperatorNot),
        LeftAssocOp(TokenType.OperatorEquals, TokenType.OperatorNotEquals,
                    TokenType.OperatorGreater, TokenType.OperatorLess,
                    TokenType.OperatorGreaterEquals, TokenType.OperatorLessEquals),
        LeftAssocOp(TokenType.OperatorPlus, TokenType.OperatorMinus),
        LeftAssocOp(TokenType.OperatorMultiply, TokenType.OperatorDivide,
                    TokenType.OperatorMod, TokenType.OperatorRem),
        PrefixOp(TokenType.OperatorPlus, TokenType.OperatorMinus),
        CallOp((TokenType.OpenParenthesis, TokenType.CloseParenthesis),
               (TokenType.OpenBracket, TokenType.CloseBracket)),
        LeftAssocOp(TokenType.Dot),
    };

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

    private ExpressionSyntax.GreenNode ParseExpression() => this.ParsePrecedence(0);

    private ExpressionSyntax.GreenNode ParsePrecedence(int index)
    {
        // If out of precedence entries, we have passed the highest-precedence entries, time to parse atoms
        if (index >= PrecedenceTable.Length) return this.ParseAtomicExpression();

        // Otherwise we need to check what the current precedence level allows
        var level = PrecedenceTable[index];
        if (level.Kind == OperatorKind.Prefix)
        {
            if (level.Operators.Any(op => op.Operator == this.Peek().Type))
            {
                // A prefix operator is found on this same level, recurse without precedence change
                var op = this.Take();
                var subexpr = this.ParsePrecedence(index);
                return new PrefixUnaryExpressionSyntax.GreenNode(op, subexpr);
            }
            else
            {
                // Not a prefix operator on this level, parse with higher precedence
                return this.ParsePrecedence(index + 1);
            }
        }
        else if (level.Kind == OperatorKind.Postfix)
        {
            // We start by parsing a higher precedence construct
            var result = this.ParsePrecedence(index + 1);
            // While there is an operator on the same level, we fold it in
            while (level.Operators.Any(op => op.Operator == this.Peek().Type))
            {
                var op = this.Take();
                result = new PostfixUnaryExpressionSyntax.GreenNode(result, op);
            }
            return result;
        }
        else if (level.Kind == OperatorKind.CallLike)
        {
            // Initially we handle it like a postfix operator
            // We start by parsing a higher precedence construct
            var result = this.ParsePrecedence(index + 1);
            // While there is an operator on the same level, we parse an argument list and expect the matching close operator
            while (true)
            {
                var it = level.Operators.Where(op => op.Operator == this.Peek().Type).GetEnumerator();
                if (!it.MoveNext()) break;
                // There is such an operator
                var (expectedOpen, expectedClose) = it.Current;
                var args = this.ParseArgumentList(expectedOpen, expectedClose!.Value);
                result = new CallExpressionSyntax.GreenNode(result, args);
            }
            return result;
        }
        else if (level.Kind == OperatorKind.LeftAssociative)
        {
            // Each element will be a higher precedence construct left-folded for each operator here
            var result = this.ParsePrecedence(index + 1);
            while (level.Operators.Any(op => op.Operator == this.Peek().Type))
            {
                var op = this.Take();
                var right = this.ParsePrecedence(index + 1);
                result = new BinaryExpressionSyntax.GreenNode(result, op, right);
            }
            return result;
        }
        else if (level.Kind == OperatorKind.RightAssociative)
        {
            // We start by parsing a higher precedence construct
            var left = this.ParsePrecedence(index + 1);
            if (level.Operators.Any(op => op.Operator == this.Peek().Type))
            {
                // There's a right-associative operator on this level
                var op = this.Take();
                // Right-recurse, meaning no change in precedence for right operand
                var right = this.ParsePrecedence(index);
                return new BinaryExpressionSyntax.GreenNode(left, op, right);
            }
            else
            {
                // Nothing on this level
                return left;
            }
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    private ExpressionSyntax.GreenNode ParseAtomicExpression()
    {
        var head = this.Peek();
        return head.Token.Type switch
        {
            TokenType.KeywordIf => this.ParseIfExpression(ParseMode.Expression),
            TokenType.KeywordWhile => this.ParseWhileExpression(ParseMode.Expression),
            TokenType.OpenBrace => this.ParseBlockExpression(ParseMode.Expression),
            TokenType.OpenParenthesis => this.ParseGroupExpression(),
            TokenType.Identifier => new IdentifierSyntax.GreenNode(this.Take()),
            TokenType.IntegerLiteral => new LiteralSyntax.GreenNode(this.Take()),
            // TODO: Handle proper errors
            _ => throw new NotImplementedException(),
        };
    }

    private ExpressionSyntax.GreenNode ParseGroupExpression()
    {
        var openParen = this.Expect(TokenType.OpenParenthesis);
        var expr = this.ParseExpression();
        var closeParen = this.Expect(TokenType.CloseParenthesis);
        return new GroupExpressionSyntax.GreenNode(openParen, expr, closeParen);
    }

    private ExpressionSyntax.GreenNode ParseBlockExpression(ParseMode parseMode)
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
                case TokenType.KeywordFunc:
                {
                    var decl = this.ParseDeclaration();
                    statements.Add(decl);
                }
                break;

                case TokenType.KeywordVar:
                case TokenType.KeywordVal:
                {
                    var stmt = this.ParseStatement();
                    statements.Add(stmt);
                }
                break;

                default:
                {
                    // Parse as an expression
                    var expr = this.ParseExpression();
                    if (this.TryPeek(TokenType.CloseBrace))
                    {
                        // This is the end of the block, this becomes the value
                        value = expr;
                    }
                    else if (CanOmitSemicolon(expr))
                    {
                        // Can simply be promoted to a statement
                        statements.Add(new ExpressionStatementSyntax.GreenNode(expr, null));
                    }
                    else
                    {
                        // Needs a semocilon
                        var semicol = this.Expect(TokenType.Semicolon);
                        statements.Add(new ExpressionStatementSyntax.GreenNode(expr, semicol));
                    }
                }
                break;
                }
            }
        }
        var closeBrace = this.Expect(TokenType.CloseBrace);
        return new BlockExpressionSyntax.GreenNode(openBrace, SyntaxFactory.SyntaxSequence(statements), value, closeBrace);
    }

    private ExpressionSyntax.GreenNode ParseIfExpression(ParseMode parseMode)
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
        return new IfExpressionSyntax.GreenNode(ifKw, condition, thenKw, thenBody, elseKw, elseBody);
    }

    private ExpressionSyntax.GreenNode ParseWhileExpression(ParseMode parseMode)
    {
        var whileKw = this.Expect(TokenType.KeywordWhile);
        var condition = this.ParseExpression();
        var doKw = this.Expect(TokenType.KeywordDo);
        var body = this.ParseByParseMode(parseMode);
        return new WhileExpressionSyntax.GreenNode(whileKw, condition, doKw, body);
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

    private static bool CanOmitSemicolon(SyntaxNode.GreenNode expr) => expr switch
    {
        BlockExpressionSyntax.GreenNode => true,
        ExpressionStatementSyntax.GreenNode es => CanOmitSemicolon(es.Expression),
        IfExpressionSyntax.GreenNode ifExpr => CanOmitSemicolon(ifExpr.Else ?? ifExpr.Then),
        WhileExpressionSyntax.GreenNode whileExpr => CanOmitSemicolon(whileExpr.Body),
        _ => false,
    };

    // Elemental operations on syntax

    private SyntaxToken.GreenNode Expect(TokenType tokenType)
    {
        // TODO: Proper errors
        if (!this.TryMatch(tokenType, out var t)) throw new NotImplementedException("TODO: Syntax error");
        return t;
    }

    private bool TryMatch(TokenType tokenType, [MaybeNullWhen(false)] out SyntaxToken.GreenNode token)
    {
        if (this.TryPeek(tokenType))
        {
            token = this.Take();
            return true;
        }
        else
        {
            token = default;
            return false;
        }
    }

    private bool TryPeek(TokenType tokenType) =>
        this.TryPeek(0, out var token) && token.Type == tokenType;

    private SyntaxToken.GreenNode Take()
    {
        this.Peek();
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
