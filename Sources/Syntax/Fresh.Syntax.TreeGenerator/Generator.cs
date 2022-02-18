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

        // Properties
        foreach (var field in node.Fields)
        {
            // Hack to add spacing inbetween
            if (!ReferenceEquals(field, node.Fields[0])) this.codeBuilder.AppendLine();

            // Doc comment
            if (field.Doc is not null)
            {
                this.codeBuilder.AppendLine("    /// <summary>");
                this.codeBuilder.AppendLine($"    /// {field.Doc}");
                this.codeBuilder.AppendLine("    /// </summary>");
            }

            // Property itself
            this.codeBuilder.AppendLine($"    public {field.Type} {field.Name} {{ get; }}");
        }

        // Constructor
        if (!node.IsAbstract && node.Fields.Length > 0)
        {
            // Separator between property and ctor
            this.codeBuilder.AppendLine();

            // Doc comment
            this.codeBuilder.AppendLine("    /// <summary>");
            this.codeBuilder.AppendLine($"    /// Initializes a new instance of the <see cref=\"{node.Name}\"/> class.");
            this.codeBuilder.AppendLine("    /// </summary>");
            foreach (var field in node.Fields) this.codeBuilder.AppendLine($"    /// <param name=\"{ToCamelCase(field.Name)}\">{field.Doc}</param>");

            // Ctot itself
            this.codeBuilder.Append($"    public {node.Name}(");
            this.codeBuilder.AppendJoin(", ", node.Fields.Select(f => $"{f.Type} {ToCamelCase(f.Name)}"));
            this.codeBuilder.AppendLine(")");
            this.codeBuilder.AppendLine("    {");
            foreach (var field in node.Fields) this.codeBuilder.AppendLine($"        this.{field.Name} = {ToCamelCase(field.Name)};");
            this.codeBuilder.AppendLine("    }");
        }

        // Equality and hash
        if (!node.IsAbstract)
        {
            if (node.Fields.Length > 0) this.codeBuilder.AppendLine();

            // Equality
            this.codeBuilder.AppendLine("    /// <inheritdoc/>");
            this.codeBuilder.AppendLine($"    public override bool Equals({this.tree.Root}? other) =>");
            this.codeBuilder.AppendLine($"           other is {node.Name} o");
            foreach (var field in node.Fields)
            {
                this.codeBuilder.Append($"        && this.{field.Name}.Equals(o.{field.Name})");
                if (ReferenceEquals(field, node.Fields[^1])) this.codeBuilder.Append(';');
                this.codeBuilder.AppendLine();
            }

            this.codeBuilder.AppendLine();

            // Hash
            this.codeBuilder.AppendLine("    /// <inheritdoc/>");
            this.codeBuilder.AppendLine("    public override int GetHashCode() => HashCode.Combine(");
            foreach (var field in node.Fields)
            {
                this.codeBuilder.Append($"        this.{field.Name}");
                if (ReferenceEquals(field, node.Fields[^1])) this.codeBuilder.Append(");");
                else this.codeBuilder.Append(',');
                this.codeBuilder.AppendLine();
            }
        }

        this.codeBuilder.AppendLine("}");
        this.codeBuilder.AppendLine();
    }

    private void GenerateFactory()
    {
        this.codeBuilder.AppendLine("/// <summary>");
        this.codeBuilder.AppendLine("/// Provides factory methods for the syntax nodes.");
        this.codeBuilder.AppendLine("/// </summary>");
        this.codeBuilder.AppendLine($"public static partial class {this.tree.Factory}");
        this.codeBuilder.AppendLine("{");
        foreach (var node in this.tree.Nodes) this.GenerateFactory(node);
        this.codeBuilder.AppendLine("}");
    }

    private void GenerateFactory(NodeModel node)
    {
        // Skip abstract nodes
        if (node.IsAbstract) return;

        // A hack to leave a line between the methods
        if (!ReferenceEquals(node, this.tree.Nodes.First(n => !n.IsAbstract))) this.codeBuilder.AppendLine();

        // We infer a nice method name
        var methodName = node.Name;
        if (methodName.EndsWith("Syntax")) methodName = methodName[..^6];

        this.codeBuilder.AppendLine("    /// <summary>");
        this.codeBuilder.AppendLine($"    /// Constructs a <see cref=\"{node.Name}\"/> from the given arguments.");
        this.codeBuilder.AppendLine("    /// </summary>");
        foreach (var field in node.Fields) this.codeBuilder.AppendLine($"    /// <param name=\"{ToCamelCase(field.Name)}\">{field.Doc}</param>");
        this.codeBuilder.Append($"    public static {node.Name} {methodName}(");
        this.codeBuilder.AppendJoin(", ", node.Fields.Select(f => $"{f.Type} {ToCamelCase(f.Name)}"));
        this.codeBuilder.AppendLine(") =>");
        this.codeBuilder.Append($"        new {node.Name}(");
        this.codeBuilder.AppendJoin(", ", node.Fields.Select(f => ToCamelCase(f.Name)));
        this.codeBuilder.AppendLine(");");
    }

    private static string ToCamelCase(string name) => $"{char.ToLower(name[0])}{name[1..]}";
}
