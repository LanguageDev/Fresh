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
/// Represents a single token the lexer can produce.
/// </summary>
/// <param name="Location">The location of the token.</param>
/// <param name="Type">The type of the token.</param>
/// <param name="Text">The text the token was parsed from.</param>
public sealed record class Token(Location Location, TokenType Type, string Text);
