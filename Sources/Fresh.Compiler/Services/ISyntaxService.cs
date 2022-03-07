// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fresh.Query;
using Fresh.Syntax;

namespace Fresh.Compiler.Services;

/// <summary>
/// Service for syntactic operations.
/// </summary>
[QueryGroup]
public partial interface ISyntaxService
{
    /// <summary>
    /// Retrieves the syntax tree for a given file.
    /// </summary>
    /// <param name="file">The file to get the syntax tree for.</param>
    /// <returns>The corresponding syntax of <paramref name="file"/>.</returns>
    public ModuleDeclarationSyntax SyntaxTree(string file);
}
