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
/// Represents a syntax tree model.
/// </summary>
/// <param name="Root">The root node of the syntax tree.</param>
/// <param name="Namespace">The namespace the tree should be generated in.</param>
/// <param name="Factory">The factory the builder methods need to be generated in.</param>
/// <param name="Usings">The usings to inject into the code generation.</param>
/// <param name="Nodes">The node models in this tree.</param>
public sealed record class Tree(
    Node Root,
    string? Namespace,
    string? Factory,
    IReadOnlySet<string> Usings,
    IReadOnlyList<Node> Nodes);
