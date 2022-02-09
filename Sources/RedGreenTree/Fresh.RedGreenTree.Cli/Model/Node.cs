// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.RedGreenTree.Cli.Model;

/// <summary>
/// Represents a single node in the syntax tree model.
/// </summary>
/// <param name="Name">The name of this syntax node.</param>
/// <param name="Base">The base of this syntax node.</param>
/// <param name="Attributes">The attributes in this syntax node.</param>
public sealed record class Node(
    string Name,
    Node? Base,
    IReadOnlyList<Attribute> Attributes);
