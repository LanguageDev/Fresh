// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    private const int indentSize = 2;

    private readonly StringBuilder codeBuilder = new();
    private readonly TreeModel tree;
    private readonly HashSet<string> allNodeNames;

    private Generator(TreeModel treeModel)
    {
        this.tree = treeModel;
        this.allNodeNames = treeModel.Nodes
            .Select(n => n.Name)
            .Append(treeModel.Root)
            .Concat(treeModel.Builtins)
            .ToHashSet();
    }

    public void GenerateCode()
    {
        // Emit namespace usings
        foreach (var u in this.tree.Usings) this.codeBuilder.AppendLine($"using {u};");
        if (this.tree.Usings.Length > 0) this.codeBuilder.AppendLine();

        this.codeBuilder.AppendLine("#nullable enable").AppendLine();

        // Emit file-scoped namespace declaration, if one is specified
        if (this.tree.Namespace is not null) this.codeBuilder.AppendLine($"namespace {this.tree.Namespace};").AppendLine();

        // Emit class definitions
        foreach (var node in this.tree.Nodes) this.GenerateRedClass(node);

        // Emit factory definition
        if (this.tree.Factory is not null) this.GenerateFactory();

        // Emit extensions
        this.GenerateSyntaxNodeExtensions();

        this.codeBuilder.AppendLine().AppendLine("#nullable restore").AppendLine();
    }

    private void GenerateRedClass(NodeModel node)
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

        // Green node
        this.GenerateGreenClass(node);
        if (node.Fields.Length > 0) this.codeBuilder.AppendLine();

        // Properties
        foreach (var field in node.Fields)
        {
            // Hack to add spacing inbetween
            if (!ReferenceEquals(field, node.Fields[0])) this.codeBuilder.AppendLine();

            // Doc comment
            if (field.Override)
            {
                this.codeBuilder.AppendLine("    /// <inheritdoc/>");
            }
            else if (field.Doc is not null)
            {
                this.codeBuilder.AppendLine("    /// <summary>");
                this.codeBuilder.AppendLine($"    /// {field.Doc}");
                this.codeBuilder.AppendLine("    /// </summary>");
            }

            // Property itself
            var overrideSpec = field.Override ? "override " : string.Empty;
            var fieldType = field.Type;
            var accessor = $"this.Green.{field.Name}";
            if (this.IsNodeSequence(fieldType, out var elementType))
            {
                fieldType = $"Syntax{fieldType}";
                accessor = $"new({accessor}, n => ({elementType})n.ToRedNode(this))";
            }
            else if (this.allNodeNames.Contains(fieldType))
            {
                accessor = $"{accessor}.ToRedNode(this)";
            }
            this.codeBuilder.AppendLine($"    public {overrideSpec}{fieldType} {field.Name} => {accessor};");
        }

        // Green node property
        this.codeBuilder.AppendLine();
        if (node.IsAbstract)
        {
            this.codeBuilder.AppendLine($"    internal abstract override GreenNode Green {{ get; }}");
        }
        else
        {
            this.codeBuilder.AppendLine($"    internal override GreenNode Green {{ get; }}");
        }

        // Constructor
        if (!node.IsAbstract && node.Fields.Length > 0)
        {
            // Separator between property and ctor
            this.codeBuilder.AppendLine();

            // Ctor itself
            this.codeBuilder.AppendLine($"    internal {node.Name}({this.tree.Root}? parent, GreenNode green)");
            this.codeBuilder.AppendLine("    {");
            this.codeBuilder.AppendLine("        this.Parent = parent;");
            this.codeBuilder.AppendLine("        this.Green = green;");
            this.codeBuilder.AppendLine("    }");
        }

        this.codeBuilder.AppendLine("}");
        this.codeBuilder.AppendLine();
    }

    private void GenerateGreenClass(NodeModel node)
    {
        string ToGreenPropertyType(FieldModel field) => this.IsNodeSequence(field.Type, out var elementType)
            ? $"Sequence<{elementType}.GreenNode>" : this.allNodeNames.Contains(field.Type)
            ? $"{field.Type}.GreenNode" : field.Type;

        var extModifier = node.IsAbstract ? "abstract" : "sealed";
        var baseModifier = node.Base is null ? string.Empty : $" : {node.Base}.GreenNode";
        this.codeBuilder.AppendLine($"    new internal {extModifier} partial class GreenNode{baseModifier}");
        this.codeBuilder.AppendLine("    {");

        // Properties
        foreach (var field in node.Fields)
        {
            // Hack to add spacing inbetween
            if (!ReferenceEquals(field, node.Fields[0])) this.codeBuilder.AppendLine();

            // Property itself
            var overrideSpec = field.Override ? "override " : string.Empty;
            this.codeBuilder.AppendLine($"        public {overrideSpec}{ToGreenPropertyType(field)} {field.Name} {{ get; }}");
        }

        // Constructor
        if (!node.IsAbstract && node.Fields.Length > 0)
        {
            // Separator between property and ctor
            this.codeBuilder.AppendLine();

            // Ctor itself
            this.codeBuilder.AppendLine($"        public GreenNode(");
            foreach (var field in node.Fields)
            {
                this.codeBuilder.Append($"            {ToGreenPropertyType(field)} {ToCamelCase(field.Name)}");
                if (ReferenceEquals(field, node.Fields[^1])) this.codeBuilder.AppendLine(")");
                else this.codeBuilder.AppendLine(",");
            }
            this.codeBuilder.AppendLine("        {");
            foreach (var field in node.Fields)
            {
                this.codeBuilder.AppendLine($"            this.{field.Name} = {ToCamelCase(field.Name)};");
            }
            this.codeBuilder.AppendLine("        }");
        }

        // Equals, GetHashCode, Children
        if (!node.IsAbstract)
        {
            this.codeBuilder.AppendLine();

            // Equality
            this.codeBuilder.AppendLine($"        public override bool Equals({this.tree.Root}.GreenNode? other) =>");
            this.codeBuilder.AppendLine($"               other is GreenNode o");
            foreach (var field in node.Fields)
            {
                this.codeBuilder.Append($"            && this.{field.Name}.Equals(o.{field.Name})");
                if (ReferenceEquals(field, node.Fields[^1])) this.codeBuilder.Append(';');
                this.codeBuilder.AppendLine();
            }

            this.codeBuilder.AppendLine();

            // Hash
            this.codeBuilder.AppendLine("        public override int GetHashCode() => HashCode.Combine(");
            foreach (var field in node.Fields)
            {
                this.codeBuilder.Append($"            this.{field.Name}");
                if (ReferenceEquals(field, node.Fields[^1])) this.codeBuilder.Append(");");
                else this.codeBuilder.Append(',');
                this.codeBuilder.AppendLine();
            }

            this.codeBuilder.AppendLine();

            // Children
            this.codeBuilder.AppendLine("        public override IEnumerable<KeyValuePair<string, object?>> Children");
            this.codeBuilder.AppendLine("        {");
            this.codeBuilder.AppendLine("            get");
            this.codeBuilder.AppendLine("            {");
            foreach (var field in node.Fields)
            {
                this.codeBuilder.AppendLine($"                yield return new(\"{field.Name}\", this.{field.Name});");
            }
            this.codeBuilder.AppendLine("            }");
            this.codeBuilder.AppendLine("        }");
        }

        // To red node
        if (node.Fields.Length > 0) this.codeBuilder.AppendLine();
        if (node.IsAbstract)
        {
            this.codeBuilder.AppendLine($"        public abstract override {node.Name} ToRedNode({this.tree.Root}? parent);");
        }
        else
        {
            this.codeBuilder.AppendLine($"        public override {node.Name} ToRedNode({this.tree.Root}? parent) => new(parent, this);");
        }

        this.codeBuilder.AppendLine("    }");
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
        this.codeBuilder.AppendLine($"    public static {node.Name} {methodName}(");
        foreach (var field in node.Fields)
        {
            var fieldType = field.Type;
            if (this.IsNodeSequence(fieldType, out var elementType)) fieldType = $"IEnumerable<{elementType}>";
            this.codeBuilder.Append($"        {fieldType} {ToCamelCase(field.Name)}");
            if (ReferenceEquals(field, node.Fields[^1])) this.codeBuilder.AppendLine($") => new(null, new(");
            else this.codeBuilder.AppendLine(",");
        }
        foreach (var field in node.Fields)
        {
            var fieldRef = ToCamelCase(field.Name);
            if (this.IsNodeSequence(field.Type, out var elementType))
            {
                fieldRef = $"{fieldRef}.Select(n => n.Green).ToSequence()";
            }
            else if (this.allNodeNames.Contains(field.Type))
            {
                fieldRef = $"{fieldRef}.Green";
            }
            this.codeBuilder.Append($"        {fieldRef}");
            if (ReferenceEquals(field, node.Fields[^1])) this.codeBuilder.AppendLine("));");
            else this.codeBuilder.AppendLine(",");
        }
    }

    private void GenerateSyntaxNodeExtensions()
    {
        this.codeBuilder.AppendLine("/// <summary>");
        this.codeBuilder.AppendLine("/// Provides extension methods for the syntax nodes.");
        this.codeBuilder.AppendLine("/// </summary>");
        this.codeBuilder.AppendLine($"public static partial class {this.tree.Root}Extensions");
        this.codeBuilder.AppendLine("{");
        foreach (var node in this.tree.Nodes) this.GenerateSyntaxNodeExtensions(node);
        this.codeBuilder.AppendLine("}");
    }

    private void GenerateSyntaxNodeExtensions(NodeModel node)
    {
        // Skip abstract nodes
        if (node.IsAbstract) return;

        // A hack to leave a line between the methods
        // if (!ReferenceEquals(node, this.tree.Nodes.First(n => !n.IsAbstract))) this.codeBuilder.AppendLine();

        // TODO: Implement
    }

    private bool IsNodeSequence(string type, [MaybeNullWhen(false)] out string elementType)
    {
        if (!type.StartsWith("Sequence<"))
        {
            elementType = null;
            return false;
        }
        elementType = type[9..^1];
        return this.allNodeNames.Contains(elementType);
    }

    private static string ToCamelCase(string name) => $"{char.ToLower(name[0])}{name[1..]}";
}
