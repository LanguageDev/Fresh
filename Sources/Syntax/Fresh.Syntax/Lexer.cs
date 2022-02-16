// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fresh.Common;

namespace Fresh.Syntax;

/// <summary>
/// Lexes a <see cref="SourceText"/> into a sequence of <see cref="Token"/>s.
/// </summary>
public sealed class Lexer
{
    /// <summary>
    /// Lexes a given source text into a sequence of tokens.
    /// </summary>
    /// <param name="sourceText">The source to lex.</param>
    /// <returns>The contents of <paramref name="sourceText"/> lexed into a token sequence.</returns>
    public static IEnumerable<Token> Lex(SourceText sourceText)
    {
        var lexer = new Lexer(sourceText);
        while (true)
        {
            var token = lexer.Next();
            yield return token;
            if (token.Type == TokenType.End) break;
        }
    }

    private readonly SourceText sourceText;
    private readonly TextReader sourceReader;
    private readonly RingBuffer<char> peekBuffer = new();
    private Cursor cursor;

    private Lexer(SourceText sourceText)
    {
        this.sourceText = sourceText;
        this.sourceReader = sourceText.Reader;
    }

    /// <summary>
    /// Lexes the next token from the source.
    /// </summary>
    /// <returns>The next token in the source.</returns>
    public Token Next()
    {
        // End of source
        if (!this.TryPeek(0, out var ch)) return this.Take(0, TokenType.End);

        // UNIX-newline
        if (ch == '\n') return this.Take(1, TokenType.Newline);
        // Windows or OS-X 9 newline
        if (ch == '\r')
        {
            // Windows
            if (this.TryPeek(1, out var ch2) && ch2 == '\n') return this.Take(2, TokenType.Newline);
            // OS-X 9
            return this.Take(1, TokenType.Newline);
        }

        // Whitespace or control character, but not newline
        if (IsSpace(ch))
        {
            var offset = 1;
            for (; IsSpace(this.Peek(offset, '\n')); ++offset) ;
            return this.Take(offset, TokenType.Whitespace);
        }

        // Line-comment
        if (this.Matches("//"))
        {
            // Skip the line
            var offset = 2;
            for (; !IsNewline(this.Peek(offset, '\n')); ++offset) ;
            // Return the comment
            return this.Take(offset, TokenType.LineComment);
        }

        // Punctuation
        switch (ch)
        {
        case '(': return this.Take(1, TokenType.OpenParenthesis);
        case ')': return this.Take(1, TokenType.CloseParenthesis);
        case '{': return this.Take(1, TokenType.OpenBrace);
        case '}': return this.Take(1, TokenType.CloseBrace);
        case '.': return this.Take(1, TokenType.Dot);
        case ',': return this.Take(1, TokenType.Comma);
        case ':': return this.Take(1, TokenType.Colon);
        case ';': return this.Take(1, TokenType.Semicolon);
        }

        // A number
        if (char.IsDigit(ch))
        {
            // Consume all digits
            var offset = 1;
            for (; char.IsDigit(this.Peek(offset)); ++offset) ;
            // Return the integer
            return this.Take(offset, TokenType.IntegerLiteral);
        }

        // Identifier or keyword
        if (IsIdentifier(ch))
        {
            // Consume all identifier characters
            var offset = 1;
            for (; IsIdentifier(this.Peek(offset)); ++offset) ;
            // Construct the token
            var ident = this.Take(offset, TokenType.Identifier);
            // See if another token type is needed
            var tokenType = ident.Text switch
            {
                "func" => TokenType.KeywordFunc,
                "var" => TokenType.KeywordVar,
                "if" => TokenType.KeywordIf,
                "then" => TokenType.KeywordThen,
                "else" => TokenType.KeywordElse,
                _ => TokenType.Identifier,
            };
            // Construct result
            return new(ident.Location, tokenType, ident.Text);
        }

        // Unknown
        return this.Take(1, TokenType.Unknown);
    }

    private Token Take(int length, TokenType type)
    {
        var startPosition = this.cursor.Position;
        var text = this.Take(length);
        var endPosition = this.cursor.Position;
        var location = new Location(this.sourceText, new(startPosition, endPosition));
        return new(location, type, text);
    }

    private string Take(int length)
    {
        if (length == 0) return string.Empty;
        if (!this.TryPeek(length - 1, out _)) throw new InvalidOperationException($"Could nod take {length} amount");
        var result = new StringBuilder();
        for (var i = 0; i < length; ++i)
        {
            var ch = this.peekBuffer.RemoveFront();
            this.cursor.Append(ch);
            result.Append(ch);
        }
        return result.ToString();
    }

    private void Skip(int length)
    {
        if (length == 0) return;
        if (!this.TryPeek(length - 1, out _)) throw new InvalidOperationException($"Could nod skip {length} amount");
        for (var i = 0; i < length; ++i) this.cursor.Append(this.peekBuffer.RemoveFront());
        this.peekBuffer.Clear();
    }

    private bool Matches(string text, int offset = 0)
    {
        if (text.Length == 0) return true;
        if (!this.TryPeek(offset + text.Length - 1, out _)) return false;
        for (var i = 0; i < text.Length; ++i)
        {
            if (text[i] != this.peekBuffer[offset + i]) return false;
        }
        return true;
    }

    private char Peek(int offset, char @default = '\0') =>
        this.TryPeek(offset, out var ch) ? ch : @default;

    private bool TryPeek(int offset, out char ch)
    {
        // Read as long as there aren't enough chars in the peek buffer
        while (this.peekBuffer.Count <= offset)
        {
            var read = this.sourceReader.Read();
            if (read == -1)
            {
                // No more to read
                ch = default;
                return false;
            }
            // This character was read successfully
            this.peekBuffer.AddBack((char)read);
        }
        // We have enough characters in the buffer
        ch = this.peekBuffer[offset];
        return true;
    }

    private static bool IsIdentifier(char ch) => char.IsLetterOrDigit(ch) || ch == '_';

    private static bool IsSpace(char ch) =>
        ch != '\n' && ch != '\r' && (char.IsWhiteSpace(ch) || char.IsControl(ch));

    private static bool IsNewline(char ch) => ch == '\n' || ch == '\r';
}
