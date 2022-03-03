// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.Syntax;

/// <summary>
/// The different kinds of token the lexer can produce.
/// </summary>
public enum TokenType
{
    // Basic

    /// <summary>
    /// End of input text.
    /// </summary>
    End,

    /// <summary>
    /// An unknown token (likely an error).
    /// </summary>
    Unknown,

    /// <summary>
    /// Denotes a missing token when parsing.
    /// </summary>
    Missing,

    // Trivia

    /// <summary>
    /// A single-line comment.
    /// </summary>
    LineComment,

    /// <summary>
    /// A sequence of whitespace characters.
    /// </summary>
    Whitespace,

    /// <summary>
    /// A newline sequence. Either '\n', `\r` or `\r\n`.
    /// </summary>
    Newline,

    // Language

    /// <summary>
    /// '('.
    /// </summary>
    OpenParenthesis,

    /// <summary>
    /// ')'.
    /// </summary>
    CloseParenthesis,

    /// <summary>
    /// '{'.
    /// </summary>
    OpenBrace,

    /// <summary>
    /// '}'.
    /// </summary>
    CloseBrace,

    /// <summary>
    /// '['.
    /// </summary>
    OpenBracket,

    /// <summary>
    /// ']'.
    /// </summary>
    CloseBracket,

    /// <summary>
    /// '.'.
    /// </summary>
    Dot,

    /// <summary>
    /// ','.
    /// </summary>
    Comma,

    /// <summary>
    /// ':'.
    /// </summary>
    Colon,

    /// <summary>
    /// ';'.
    /// </summary>
    Semicolon,

    /// <summary>
    /// '='.
    /// </summary>
    Assign,

    /// <summary>
    /// The 'func' keyword.
    /// </summary>
    KeywordFunc,

    /// <summary>
    /// The 'var' keyword.
    /// </summary>
    KeywordVar,

    /// <summary>
    /// The 'val' keyword.
    /// </summary>
    KeywordVal,

    /// <summary>
    /// The 'if' keyword.
    /// </summary>
    KeywordIf,

    /// <summary>
    /// The 'then' keyword.
    /// </summary>
    KeywordThen,

    /// <summary>
    /// The 'else' keyword.
    /// </summary>
    KeywordElse,

    /// <summary>
    /// The 'while' keyword.
    /// </summary>
    KeywordWhile,

    /// <summary>
    /// The 'do' keyword.
    /// </summary>
    KeywordDo,

    /// <summary>
    /// The 'or' operator.
    /// </summary>
    OperatorOr,

    /// <summary>
    /// The 'and' operator.
    /// </summary>
    OperatorAnd,

    /// <summary>
    /// The 'not' operator.
    /// </summary>
    OperatorNot,

    /// <summary>
    /// The 'not' operator.
    /// </summary>
    OperatorMod,

    /// <summary>
    /// The 'rem' operator.
    /// </summary>
    OperatorRem,

    /// <summary>
    /// The '>' operator.
    /// </summary>
    OperatorGreater,

    /// <summary>
    /// The '<' operator.
    /// </summary>
    OperatorLess,

    /// <summary>
    /// The '>=' operator.
    /// </summary>
    OperatorGreaterEquals,

    /// <summary>
    /// The '<=' operator.
    /// </summary>
    OperatorLessEquals,

    /// <summary>
    /// The '==' operator.
    /// </summary>
    OperatorEquals,

    /// <summary>
    /// The '!=' operator.
    /// </summary>
    OperatorNotEquals,

    /// <summary>
    /// The '+' operator.
    /// </summary>
    OperatorPlus,

    /// <summary>
    /// The '-' operator.
    /// </summary>
    OperatorMinus,

    /// <summary>
    /// The '*' operator.
    /// </summary>
    OperatorMultiply,

    /// <summary>
    /// The '/' operator.
    /// </summary>
    OperatorDivide,

    /// <summary>
    /// A general identifier.
    /// </summary>
    Identifier,

    /// <summary>
    /// A simple integer.
    /// </summary>
    IntegerLiteral,
}

/// <summary>
/// Utilities on <see cref="TokenType"/>s.
/// </summary>
public static class TokenTypeExtensions
{
    /// <summary>
    /// Checks if a given <see cref="TokenType"/> is considered comment.
    /// </summary>
    /// <param name="tokenType">The token type to check.</param>
    /// <returns>True, if <paramref name="tokenType"/> is a comment token type.</returns>
    public static bool IsComment(this TokenType tokenType) =>
        tokenType is TokenType.LineComment;

    /// <summary>
    /// Checks if a given <see cref="TokenType"/> is considered spacing (whitespace or newline).
    /// </summary>
    /// <param name="tokenType">The token type to check.</param>
    /// <returns>True, if <paramref name="tokenType"/> is a spacing token type.</returns>
    public static bool IsSpacing(this TokenType tokenType) =>
        tokenType is TokenType.Whitespace
                  or TokenType.Newline;

    /// <summary>
    /// Checks if a given <see cref="TokenType"/> is considered trivia.
    /// </summary>
    /// <param name="tokenType">The token type to check.</param>
    /// <returns>True, if <paramref name="tokenType"/> is a trivia token type.</returns>
    public static bool IsTrivia(this TokenType tokenType) =>
        tokenType is TokenType.LineComment
                  or TokenType.Whitespace
                  or TokenType.Newline;
}
