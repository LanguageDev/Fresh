// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Fresh.Syntax.TreeGenerator;

public sealed class Generator
{
    public static string Generate(TreeModel treeModel)
    {
        var generator = new Generator(treeModel);
        generator.GenerateCode();
        return generator.codeBuilder.ToString();
    }

    private readonly CodeBuilder codeBuilder = new();
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
        foreach (var u in this.tree.Usings) this.codeBuilder.Using(u);

        this.codeBuilder.WriteLine("#pragma warning disable CS0109");
        this.codeBuilder.WriteLine("#pragma warning disable CS0282");
        this.codeBuilder.NullableEnable();

        // Emit file-scoped namespace declaration, if one is specified
        if (this.tree.Namespace is not null) this.codeBuilder.Namespace(this.tree.Namespace);

        // Emit class definitions
        foreach (var node in this.tree.Nodes) this.GenerateRedClass(node);

        // Emit factory definition
        if (this.tree.Factory is not null) this.GenerateFactory();

        // Emit extensions
        this.GenerateSyntaxNodeExtensions();

        this.codeBuilder.NullableRestore();
        this.codeBuilder.WriteLine("#pragma warning restore CS0282");
        this.codeBuilder.WriteLine("#pragma warning restore CS0109");
    }

    private void GenerateRedClass(NodeModel node)
    {
        // Modifiers
        var modifiers = Modifiers.Public;
        if (!node.IsStruct) modifiers |= Modifiers.Class;
        if (node.IsAbstract) modifiers |= Modifiers.Abstract;

        // Bases
        var bases = new List<string>();
        if (node.Base is not null) bases.Add(node.Base);
        if (node.IsStruct) bases.Add($"IEquatable<{node.Name}>");

        this.codeBuilder.StartType(
            doc: node.Doc,
            modifiers: modifiers,
            name: node.Name,
            genericParams: node.GenericParameters?.Select(p => new MemberInfo
            {
                Name = p.Name,
                Doc = p.Doc,
            }),
            bases: bases);

        // Green node
        this.GenerateGreenClass(node);

        // Properties
        foreach (var field in node.Fields)
        {
            // Property itself
            var fieldType = field.Type;
            var accessor = $"this.Green.{field.Name}";
            if (this.IsNodeSequence(fieldType, out var elementType))
            {
                accessor = $"new({accessor}.Underlying, n => ({elementType})(({elementType}.GreenNode)n).ToRedNode(this))";
            }
            else if (this.allNodeNames.Contains(fieldType))
            {
                if (field.IsOptional) accessor = $"{accessor}?";
                accessor = $"{accessor}.ToRedNode(this)";
            }
            if (field.IsOptional) fieldType = $"{fieldType}?";
            this.codeBuilder.ArrowProp(
                doc: field.Doc,
                modifiers: Modifiers.Public,
                type: fieldType,
                name: field.Name,
                value: accessor);
        }

        // Green node property
        modifiers = Modifiers.Override;
        if (node.IsAbstract) modifiers |= Modifiers.Abstract;
        this.codeBuilder.AutoGetterProp(
            doc: null,
            modifiers: modifiers,
            type: "GreenNode",
            name: "Green");

        // Constructor
        if (!node.IsAbstract)
        {
            // Ctor itself
            this.codeBuilder.TrivialCtor(
                modifiers: 0,
                parameters: new[]
                {
                    new MemberInfo { Name = "Parent", Type = $"{this.tree.Root}?", Doc = "The parent node of this one." },
                    new MemberInfo { Name = "Green", Type = "GreenNode", Doc = "The wrapped green node." },
                });
        }

        this.codeBuilder.EndType();
    }

    private void GenerateGreenClass(NodeModel node)
    {
        string ToGreenPropertyType(FieldModel field)
        {
            var fieldType = this.IsNodeSequence(field.Type, out var elementType)
                ? $"SyntaxSequence<{elementType}.GreenNode>" : this.allNodeNames.Contains(field.Type)
                ? $"{field.Type}.GreenNode" : field.Type;
            if (field.IsOptional) fieldType = $"{fieldType}?";
            return fieldType;
        }

        // Modifiers
        Modifiers modifiers = 0;
        if (!node.IsStruct) modifiers |= Modifiers.Class;
        if (node.IsAbstract) modifiers |= Modifiers.Abstract;

        // Bases
        var bases = new List<string>();
        if (node.Base is not null && !node.IsStruct) bases.Add($"{node.Base}.GreenNode");
        if (node.IsStruct)
        {
            bases.Add("ISyntaxElement");
            bases.Add("IEquatable<GreenNode>");
        }

        this.codeBuilder.StartType(
            doc: null,
            modifiers: modifiers,
            name: "GreenNode",
            genericParams : null,
            bases: bases);

        // Properties
        foreach (var field in node.Fields)
        {
            this.codeBuilder.AutoGetterProp(
                doc: null,
                modifiers: Modifiers.Public,
                type: ToGreenPropertyType(field),
                name: field.Name);
        }

        // Constructor
        if (!node.IsAbstract)
        {
            // Ctor itself
            this.codeBuilder.TrivialCtor(
                modifiers: Modifiers.Public,
                parameters: node.Fields.Select(f => new MemberInfo
                {
                    Name = f.Name,
                    Type = ToGreenPropertyType(f),
                }));
        }

        // Equals, GetHashCode, Children
        if (!node.IsAbstract)
        {
            // Equality
            this.codeBuilder.Equals(
                baseType: node.IsStruct ? "GreenNode" : $"{this.tree.Root}.GreenNode",
                node.Fields.Select(f => f.Name));

            // Hash
            this.codeBuilder.HashCode(node.Fields.Select(f => f.Name));

            // Children
            this.codeBuilder.StartGetterProp(
                doc: null,
                modifiers: Modifiers.Public | Modifiers.Override,
                type: "IEnumerable<KeyValuePair<string, object?>>",
                name: "Children");
            foreach (var field in node.Fields)
            {
                this.codeBuilder.WriteLine($"yield return new(nameof(this.{field.Name}), this.{field.Name});");
            }
            this.codeBuilder.EndGetterProp();
        }

        // To red node
        modifiers = Modifiers.Public | Modifiers.Override;
        if (node.IsAbstract) modifiers |= Modifiers.Abstract;
        this.codeBuilder.ArrowMethod(
            doc: null,
            modifiers: modifiers,
            ret: new MemberInfo { Type = node.Name },
            name: "ToRedNode",
            genericParams: null,
            parameters: new[] { new MemberInfo { Name = "parent", Type = $"{this.tree.Root}?" } },
            body: node.IsAbstract ? null : "new(parent, this)");

        this.codeBuilder.EndType();
    }

    private void GenerateFactory()
    {
        if (this.tree.Factory is null) return;

        this.codeBuilder.StartType(
            doc: "Provides factory methods for the syntax nodes.",
            modifiers: Modifiers.Public | Modifiers.Static | Modifiers.Class,
            name: this.tree.Factory,
            genericParams: null,
            bases: Enumerable.Empty<string>());

        foreach (var node in this.tree.Nodes) this.GenerateFactory(node);

        this.codeBuilder.EndType();
    }

    private void GenerateFactory(NodeModel node)
    {
        string ToParameterType(FieldModel field)
        {
            var fieldType = field.Type;
            if (this.IsNodeSequence(fieldType, out var elementType)) fieldType = $"IEnumerable<{elementType}>";
            if (field.IsOptional) fieldType = $"{fieldType}?";
            return fieldType;
        }

        // Skip abstract nodes
        if (node.IsAbstract) return;

        // We infer a nice method name
        string methodName;
        if (node.FactoryHintName is not null)
        {
            methodName = node.FactoryHintName;
        }
        else
        {
            methodName = node.Name;
            if (methodName.EndsWith("Syntax")) methodName = methodName[..^6];
        }

        var fieldProjections = new List<string>();
        foreach (var field in node.Fields)
        {
            var fieldRef = CodeBuilder.EscapeKeyword(CodeBuilder.ToCamelCase(field.Name));
            if (this.IsNodeSequence(field.Type, out var elementType))
            {
                fieldRef = $"SyntaxSequence({fieldRef}.Select(n => n.Green))";
            }
            else if (this.allNodeNames.Contains(field.Type))
            {
                if (field.IsOptional) fieldRef = $"{fieldRef}?";
                fieldRef = $"{fieldRef}.Green";
            }
            fieldProjections.Add(fieldRef);
        }

        this.codeBuilder.ArrowMethod(
            doc: $"Constructs a <see cref=\"{node.Name}\"/> from the given arguments.",
            modifiers: Modifiers.Public | Modifiers.Static,
            ret: new MemberInfo() { Type = node.Name, Doc = "The constructed syntax node." },
            name: methodName,
            genericParams: node.GenericParameters?.Select(p => new MemberInfo
            {
                Name = p.Name,
                Doc = p.Doc,
            }),
            parameters: node.Fields.Select(f => new MemberInfo
            {
                Name = CodeBuilder.EscapeKeyword(CodeBuilder.ToCamelCase(f.Name)),
                Type = ToParameterType(f),
                Doc = f.Doc,
            }),
            body: $"new(null, new({string.Join(", ", fieldProjections)}))");
    }

    private void GenerateSyntaxNodeExtensions()
    {
#if false
        this.codeBuilder.AppendLine("/// <summary>");
        this.codeBuilder.AppendLine("/// Provides extension methods for the syntax nodes.");
        this.codeBuilder.AppendLine("/// </summary>");
        this.codeBuilder.AppendLine($"public static partial class {this.tree.Root}Extensions");
        this.codeBuilder.AppendLine("{");
        foreach (var node in this.tree.Nodes) this.GenerateSyntaxNodeExtensions(node);
        this.codeBuilder.AppendLine("}");
#endif
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
        if (!type.StartsWith("SyntaxSequence<"))
        {
            elementType = null;
            return false;
        }
        elementType = type[15..^1];
        return this.allNodeNames.Contains(elementType);
    }
}
