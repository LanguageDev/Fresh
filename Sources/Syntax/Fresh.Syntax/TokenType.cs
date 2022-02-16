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
    /// The 'func' keyword.
    /// </summary>
    KeywordFunc,

    /// <summary>
    /// The 'var' keyword.
    /// </summary>
    KeywordVar,

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
    /// Checks if a given <see cref="TokenType"/>
    /// </summary>
    /// <param name="tokenType"></param>
    /// <returns></returns>
    public static bool IsTrivia(this TokenType tokenType) =>
        tokenType is TokenType.LineComment
                  or TokenType.Whitespace
                  or TokenType.Newline;
}
