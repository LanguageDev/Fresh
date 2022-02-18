// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.Syntax.TreeGenerator;

public sealed class Generator
{
    public static string Generate(TreeModel treeModel)
    {
        var generator = new Generator(treeModel);
        generator.GenerateCode();
        return generator.codeBuilder.ToString();
    }

    private readonly StringBuilder codeBuilder = new();
    private readonly TreeModel tree;
    private readonly Dictionary<string, NodeModel> nodes;

    private Generator(TreeModel treeModel)
    {
        this.tree = treeModel;
        this.nodes = treeModel.Nodes.ToDictionary(n => n.Name);
    }

    public void GenerateCode()
    {
        // Emit namespace usings
        foreach (var u in this.tree.Usings) this.codeBuilder.AppendLine($"using {u};");
        if (this.tree.Usings.Length > 0) this.codeBuilder.AppendLine();

        // Emit file-scoped namespace declaration, if one is specified
        if (this.tree.Namespace is not null) this.codeBuilder.AppendLine($"namespace {this.tree.Namespace};").AppendLine();

        // Emit class definitions
        foreach (var node in this.tree.Nodes) this.GenerateClass(node);

        // Emit factory definition
        if (this.tree.Factory is not null) this.GenerateFactory();
    }

    private void GenerateClass(NodeModel node)
    {
        var extModifier = node.IsAbstract ? "abstract" : "sealed";
        var baseModifier = node.Base is null ? string.Empty : $" : {node.Base}";

        if (node.Doc is not null)
        {
            this.codeBuilder.AppendLine("/// <summary>");
            this.codeBuilder.AppendLine($"/// {node.Doc}");
            this.codeBuilder.AppendLine("/// </summary>");
        }
        this.codeBuilder.AppendLine($"public {extModifier} partial class {node.Name}{baseModifier}");
        this.codeBuilder.AppendLine("{");
        this.codeBuilder.AppendLine("}");
        this.codeBuilder.AppendLine();
    }

    private void GenerateFactory()
    {
        this.codeBuilder.AppendLine("/// <summary>");
        this.codeBuilder.AppendLine($"/// Provides factory methods for the syntax nodes.");
        this.codeBuilder.AppendLine("/// </summary>");
        this.codeBuilder.AppendLine($"public static partial class {this.tree.Factory}");
        this.codeBuilder.AppendLine("{");
        this.codeBuilder.AppendLine("}");
    }
}
