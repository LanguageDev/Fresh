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
/// Represents a syntax error.
/// </summary>
/// <param name="Location">The location of the error.</param>
/// <param name="Message">The error message.</param>
public sealed record class SyntaxError(
    Location Location,
    string Message);
