// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fresh.RedGreenTree.Cli.Model;

namespace Fresh.RedGreenTree.Cli;

/// <summary>
/// Provides C# code generation for the tree model.
/// </summary>
public sealed class CodeGenerator
{
    public static string Generate(Tree tree)
    {
        var generator = new CodeGenerator();
        generator.GenerateImpl(tree);
        return generator.result.ToString();
    }

    private readonly StringBuilder result = new();

    private void GenerateImpl(Tree tree)
    {
        // Add the prefix
        this.result.AppendLine($"// Generated using Fresh.RedGreenTree.Cli on {DateTime.UtcNow}\n");

        // We generate all usings
        this.result
            .AppendJoin('\n', tree.Usings.OrderBy(x => x).Select(n => $"using {n};"))
            .AppendLine()
            .AppendLine();

        // Namespace
        if (tree.Namespace is not null) this.result.AppendLine($"namespace {tree.Namespace};").AppendLine();

        // If there is a factory, generate that
        if (tree.Factory is not null)
        {
            this.result
                .AppendLine($"public static class {tree.Factory}")
                .AppendLine("{");

            foreach (var node in tree.Nodes) this.GenerateNodeFactory(node);

            this.result
                .AppendLine("}")
                .AppendLine();
        }

        // Nodes
        foreach (var node in tree.Nodes) this.GenerateNodeClass(node);
    }

    private void GenerateNodeClass(Node node)
    {
        this.result.Append($"public ");
        if (node.IsAbstract) this.result.Append("abstract ");
        this.result.Append($"class {node.Name}");
        if (node.Base is not null) this.result.Append(" : ").Append(node.Base.Name);
        this.result.AppendLine();

        this.result
            .AppendLine("{")
            .AppendLine("    // TODO")
            .AppendLine("}")
            .AppendLine();
    }

    private void GenerateNodeFactory(Node node)
    {
        this.result
            .AppendLine($"    public static {node.Name} {node.Name}(/* TODO */) =>")
            .AppendLine("        throw new NotImplementedException();");
    }
}
