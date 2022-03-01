// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fresh.Syntax.TreeGenerator;

[Flags]
public enum Modifiers
{
    Struct   = 0b00000000,
    Class    = 0b00000001,
    Abstract = 0b00000010,
    Public   = 0b00000100,
    Override = 0b00001000,
    Static   = 0b00010000,
}

public record struct MemberInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Doc { get; set; } = null;
    public string Value { get; set; } = string.Empty;
}

public sealed class CodeBuilder
{
    private sealed record class TypeInfo(Modifiers Modifiers, string Name);

    private static readonly string[] keywords = new[] { "operator" };

    private readonly StringBuilder builder = new();
    private readonly Stack<TypeInfo> typeStack = new();

    public override string ToString() => SyntaxFactory
        .ParseCompilationUnit(this.builder.ToString())
        .NormalizeWhitespace()
        .GetText()
        .ToString();

    public CodeBuilder Write(string str)
    {
        this.builder.Append(str);
        return this;
    }

    public CodeBuilder WriteLine(string str)
    {
        this.builder.AppendLine(str);
        return this;
    }

    public CodeBuilder Using(string ns) => this.WriteLine($"using {ns};");

    public CodeBuilder NullableEnable() => this.WriteLine("#nullable enable");

    public CodeBuilder NullableRestore() => this.WriteLine("#nullable restore");

    public CodeBuilder Namespace(string ns) => this.WriteLine($"namespace {ns};");

    public CodeBuilder InheritDoc() => this.WriteLine("/// <inheritdoc/>");

    public CodeBuilder Doc(string tag, string text, params (string Name, string Value)[] attribs) => this
        .WriteLine($"/// <{tag}{string.Join("", attribs.Select(a => $" {a.Name}=\"{a.Value}\""))}>")
        .WriteLine($"/// {text}")
        .WriteLine($"/// </{tag}>");

    public CodeBuilder DocSummary(string text) => this.Doc("summary", text);

    public CodeBuilder DocParam(string name, string text) => this.Doc("param", text, ("name", name));

    public CodeBuilder DocReturns(string text) => this.Doc("returns", text);

    public CodeBuilder StartType(
        string? doc,
        Modifiers modifiers,
        string name,
        IEnumerable<MemberInfo>? genericParams,
        IEnumerable<string> bases)
    {
        if (doc is not null) this.DocSummary(doc);
        if (genericParams is not null)
        {
            foreach (var p in genericParams)
            {
                if (p.Doc is not null) this.Doc("typeparam", p.Doc, ("name", p.Name));
            }
        }
        if (this.typeStack.Count > 0) this.Write("new ");
        this.Write(modifiers.HasFlag(Modifiers.Public) ? "public " : "internal ");
        if (modifiers.HasFlag(Modifiers.Static)) this.Write("static ");
        if (modifiers.HasFlag(Modifiers.Class) && !modifiers.HasFlag(Modifiers.Static))
        {
            this.Write(modifiers.HasFlag(Modifiers.Abstract) ? "abstract " : "sealed ");
        }
        if (!modifiers.HasFlag(Modifiers.Class)) this.Write("readonly ");
        this.Write("partial ");
        this.Write(modifiers.HasFlag(Modifiers.Class) ? "class " : "struct ");
        this.Write(name);
        if (genericParams is not null && genericParams.Any())
        {
            this.Write("<");
            this.Write(string.Join(", ", genericParams.Select(p => p.Name)));
            this.Write(">");
        }
        if (bases.Any())
        {
            this.Write(" : ");
            this.WriteLine(string.Join(", ", bases));
        }
        this.WriteLine("{");
        this.typeStack.Push(new(modifiers, name));
        return this;
    }

    public CodeBuilder EndType()
    {
        this.typeStack.Pop();
        this.WriteLine("}");
        return this;
    }

    public CodeBuilder StartCtor(Modifiers modifiers, IEnumerable<MemberInfo> parameters)
    {
        // Doc
        this.DocSummary($"Creates a new instance of the <see cref=\"{this.typeStack.Peek().Name}\"> type.");
        foreach (var info in parameters)
        {
            if (info.Doc is not null) this.DocParam(info.Name, info.Doc);
        }

        // Actual definition
        this.Write(modifiers.HasFlag(Modifiers.Public) ? "public " : "internal ");
        this.Write(this.typeStack.Peek().Name);
        this.Write("(");
        this.Write(string.Join(", ", parameters.Select(p => $"{p.Type} {EscapeKeyword(p.Name)}")));
        this.WriteLine(")");
        this.WriteLine("{");
        return this;
    }

    public CodeBuilder EndCtor() => this.WriteLine("}");

    public CodeBuilder TrivialCtor(Modifiers modifiers, IEnumerable<MemberInfo> parameters)
    {
        this.StartCtor(modifiers, parameters.Select(p => p with { Name = ToCamelCase(p.Name) }));
        foreach (var p in parameters) this.WriteLine($"this.{p.Name} = {EscapeKeyword(ToCamelCase(p.Name))};");
        this.EndCtor();
        return this;
    }

    public CodeBuilder AutoGetterProp(string? doc, Modifiers modifiers, string type, string name)
    {
        if (doc is not null) this.DocSummary(doc);
        return this
            .DumpNonTypeModifiers(modifiers)
            .WriteLine($"{type} {name} {{ get; }}");
    }

    public CodeBuilder ArrowProp(string? doc, Modifiers modifiers, string type, string name, string value)
    {
        if (doc is not null) this.DocSummary(doc);
        return this
            .DumpNonTypeModifiers(modifiers)
            .WriteLine($"{type} {name} => {value};");
    }

    public CodeBuilder StartGetterProp(string? doc, Modifiers modifiers, string type, string name)
    {
        if (doc is not null) this.DocSummary(doc);
        return this
            .DumpNonTypeModifiers(modifiers)
            .WriteLine($"{type} {name} {{")
            .WriteLine("get {");
    }

    public CodeBuilder EndGetterProp() => this
        .WriteLine("}")
        .WriteLine("}");

    public CodeBuilder Equals(string baseType, IEnumerable<string> props)
    {
        var typeModifiers = this.typeStack.Peek().Modifiers;
        var isClass = typeModifiers.HasFlag(Modifiers.Class);
        var overrideSpec = isClass ? "override " : string.Empty;
        var derive = isClass ? $"this.Equals(other as {baseType})" : $"other is {baseType} o && this.Equals(o)";
        this
            .InheritDoc()
            .WriteLine($"public override bool Equals(object? other) => {derive};")
            .InheritDoc()
            .WriteLine($"public {overrideSpec}bool Equals([AllowNull] {baseType} other) =>")
            .WriteLine($"other is {this.typeStack.Peek().Name} o")
            .Write(string.Join("", props.Select(p => $"&& object.Equals(this.{p}, o.{p})")))
            .WriteLine(";");
        return this;
    }

    public CodeBuilder HashCode(IEnumerable<string> props) => this
        .InheritDoc()
        .WriteLine("public override int GetHashCode() => HashCode.Combine(")
        .WriteLine(string.Join(", ", props.Select(p => $"this.{p}")))
        .WriteLine(");");

    public CodeBuilder ArrowMethod(
        string? doc,
        Modifiers modifiers,
        MemberInfo ret,
        string name,
        IEnumerable<MemberInfo>? genericParams,
        IEnumerable<MemberInfo> parameters,
        string? body)
    {
        if (doc is not null) this.DocSummary(doc);
        if (genericParams is not null)
        {
            foreach (var p in genericParams)
            {
                if (p.Doc is not null) this.Doc("typeparam", p.Doc, ("name", p.Name));
            }
        }
        foreach (var p in parameters)
        {
            if (doc is not null && p.Doc is not null) this.DocParam(p.Name, p.Doc);
        }
        if (doc is not null && ret.Doc is not null) this.DocReturns(ret.Doc);
        this.DumpNonTypeModifiers(modifiers);
        this.Write($"{ret.Type} {name}");
        if (genericParams is not null && genericParams.Any())
        {
            this.Write("<");
            this.Write(string.Join(", ", genericParams.Select(p => p.Name)));
            this.Write(">");
        }
        this.Write("(");
        this.Write(string.Join(", ", parameters.Select(p => $"{p.Type} {p.Name}")));
        this.Write(")");
        if (body is not null) this.Write($" => {body}");
        this.WriteLine(";");
        return this;
    }

    private CodeBuilder DumpNonTypeModifiers(Modifiers modifiers)
    {
        var typeModifiers = this.typeStack.Peek().Modifiers;
        var isClass = typeModifiers.HasFlag(Modifiers.Class);
        this.Write(modifiers.HasFlag(Modifiers.Public) ? "public " : "internal ");
        if (modifiers.HasFlag(Modifiers.Static)) this.Write("static ");
        if (modifiers.HasFlag(Modifiers.Abstract)) this.Write("abstract ");
        if (isClass && modifiers.HasFlag(Modifiers.Override)) this.Write("override ");
        return this;
    }

    public static string EscapeKeyword(string name) => keywords.Contains(name) ? $"@{name}" : name;

    public static string ToCamelCase(string name) => $"{char.ToLower(name[0])}{name[1..]}";
}
