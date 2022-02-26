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

    private static string EscapeKeyword(string name) => keywords.Contains(name)
        ? $"@{name}"
        : name;
}
