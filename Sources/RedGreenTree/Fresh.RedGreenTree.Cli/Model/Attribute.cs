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
/// Represents a single attribute/property in a syntax tree node.
/// </summary>
/// <param name="Name">The name of the attribute.</param>
/// <param name="Type">The type of the attribute.</param>
public sealed record class Attribute(string Name, string Type);
