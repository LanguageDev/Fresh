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
}

public sealed class CodeBuilder
{
    private static readonly string[] keywords = new[] { "operator" };

    private readonly StringBuilder builder = new();
    private readonly Stack<string> typeStack = new();

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

    public CodeBuilder StartType(Modifiers modifiers, string name, params string[] bases)
    {
        if (this.typeStack.Count > 0) this.Write("new ");
        this.Write(modifiers.HasFlag(Modifiers.Public) ? "public " : "internal ");
        if (modifiers.HasFlag(Modifiers.Class)) this.Write(modifiers.HasFlag(Modifiers.Abstract) ? "abstract " : "sealed ");
        this.Write("partial ");
        this.Write(modifiers.HasFlag(Modifiers.Class) ? "class " : "readonly struct ");
        this.Write(name);
        if (bases.Length > 0)
        {
            this.Write(" : ");
            this.WriteLine(string.Join(", ", bases));
        }
        this.WriteLine("{");
        this.typeStack.Push(name);
        return this;
    }

    public CodeBuilder EndType()
    {
        this.typeStack.Pop();
        this.WriteLine("}");
        return this;
    }

    public CodeBuilder StartCtor(Modifiers modifiers, IEnumerable<(string Name, string Type, string Doc)> parameters)
    {
        // Doc
        this.DocSummary($"Creates a new instance of the <see cref=\"{this.typeStack.Peek()}\"> type.");
        foreach (var (name, _, doc) in parameters) this.DocParam(name, doc);

        // Actual definition
        this.Write(modifiers.HasFlag(Modifiers.Public) ? "public " : "internal ");
        this.Write(this.typeStack.Peek());
        this.Write("(");
        this.Write(string.Join(", ", parameters.Select(p => $"{p.Type} {EscapeKeyword(p.Name)}")));
        this.WriteLine(")");
        this.WriteLine("{");
        return this;
    }

    public CodeBuilder EndCtor() => this.WriteLine("}");

    public CodeBuilder TrivialCtor(Modifiers modifiers, IEnumerable<(string Name, string Type, string Doc)> parameters)
    {
        this.StartCtor(modifiers, parameters.Select(p => (ToCamelCase(p.Name), p.Type, p.Doc)));
        foreach (var (n, _, _) in parameters) this.WriteLine($"this.{n} = {EscapeKeyword(ToCamelCase(n))}");
        this.EndCtor();
        return this;
    }

    public CodeBuilder AutoGetterProp(Modifiers modifiers, string name, string type, string doc) => this
        .DocSummary(doc)
        .DumpNonTypeModifiers(modifiers)
        .WriteLine($"{type} {name} {{ get; }}");

    public CodeBuilder ArrowProp(Modifiers modifiers, string name, string type, string doc, string value) => this
        .DocSummary(doc)
        .DumpNonTypeModifiers(modifiers)
        .WriteLine($"{type} {name} => {value};");

    public CodeBuilder StartGetterProp(Modifiers modifiers, string name, string type, string doc) => this
        .DocSummary(doc)
        .DumpNonTypeModifiers(modifiers)
        .WriteLine($"{type} {name} {{")
        .WriteLine("get {");

    public CodeBuilder EndGetterProp() => this
        .WriteLine("}")
        .WriteLine("}");

    public CodeBuilder Equals(string baseType, IEnumerable<string> props) => this
        .InheritDoc()
        .WriteLine($"public override bool Equals(object? other) => this.Equals(other as {baseType});")
        .InheritDoc()
        .WriteLine($"public override bool Equals([MaybeNull] {baseType} other) =>")
        .WriteLine($"other is {baseType} o")
        .Write(string.Join("", props.Select(p => $"&& object.Equals(this.{p}, o.{p})")))
        .WriteLine(";");

    public CodeBuilder HashCode(IEnumerable<string> props) => this
        .InheritDoc()
        .WriteLine("public override int GetHashCode() => HashCode.Combine(")
        .WriteLine(string.Join(", ", props.Select(p => $"this.{p}")))
        .WriteLine(");");

    public CodeBuilder ArrowMethod(
        string summary,
        Modifiers modifiers,
        (string Type, string Doc) ret,
        string name,
        IEnumerable<(string Name, string Type, string Doc)> parameters,
        string? body)
    {
        this.DocSummary(summary);
        foreach (var (n, _, doc) in parameters) this.DocParam(n, doc);
        this.DocReturns(ret.Doc);
        this.DumpNonTypeModifiers(modifiers);
        this.Write($"{ret.Type} {name}(");
        this.Write(string.Join(", ", parameters.Select(p => $"{p.Type} {p.Name}")));
        this.Write(")");
        if(body is not null) this.Write($" => {body}");
        this.WriteLine(";");
        return this;
    }

    private CodeBuilder DumpNonTypeModifiers(Modifiers modifiers)
    {
        this.Write(modifiers.HasFlag(Modifiers.Public) ? "public " : "internal ");
        if (modifiers.HasFlag(Modifiers.Abstract)) this.Write("abstract ");
        if (modifiers.HasFlag(Modifiers.Override)) this.Write("override ");
        return this;
    }

    public static string EscapeKeyword(string name) => keywords.Contains(name) ? $"@{name}" : name;

    public static string ToCamelCase(string name) => $"{char.ToLower(name[0])}{name[1..]}";
}
