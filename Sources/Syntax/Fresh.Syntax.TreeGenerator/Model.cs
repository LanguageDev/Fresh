// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.Syntax.TreeGenerator;

public sealed class TreeModel
{
    public string[] Usings { get; set; } = Array.Empty<string>();
    public string? Namespace { get; set; }
    public string? Factory { get; set; }
    public string Root { get; set; } = string.Empty;
    public string[] Builtins { get; set; } = Array.Empty<string>();
    public NodeModel[] Nodes { get; set; } = Array.Empty<NodeModel>();
}

public sealed class NodeModel
{
    public string Name { get; set; } = string.Empty;
    public string? Doc { get; set; }
    public string? Base { get; set; }
    public bool IsAbstract { get; set; } = false;
    public bool IsStruct { get; set; } = false;
    public string? FactoryHintName { get; set; }
    public FieldModel[] Fields { get; set; } = Array.Empty<FieldModel>();
}

public sealed class FieldModel
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Doc { get; set; }
    public bool IsOptional { get; set; } = false;
}
