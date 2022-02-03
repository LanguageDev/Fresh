// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Fresh.Query;

[Generator]
public sealed class QueryProxyGenerator : IIncrementalGenerator
{
    private sealed record InputQueryModel(
        ISymbol Symbol,
        IReadOnlyList<ITypeSymbol> Keys,
        ITypeSymbol StoredType);

    private sealed record InputQueryGroupModel(
        INamedTypeSymbol Symbol,
        IReadOnlyList<InputQueryModel> InputQueries);

    private sealed record QueryModel(
        ISymbol Symbol,
        IReadOnlyList<ITypeSymbol> Keys,
        ITypeSymbol ReturnType,
        ITypeSymbol? AwaitedType,
        bool HasCancellationToken);

    private sealed record QueryGroupModel(
        INamedTypeSymbol Symbol,
        IReadOnlyList<QueryModel> Queries);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add all injected sources
        context.RegisterPostInitializationOutput(InjectSources);

        // Filter for interfaces with the InputQueryGroupAttribute
        var inputQueryGroupDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsInterfaceWithAttributes,
                transform: MakeSemanticTargetTransformer(typeof(InputQueryGroupAttribute)))
            .Where(m => m is not null);

        // Filter for interfaces with the QueryGroupAttribute
        var queryGroupDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsInterfaceWithAttributes,
                transform: MakeSemanticTargetTransformer(typeof(QueryGroupAttribute)))
            .Where(m => m is not null);

        // Combine both with the compilation
        var compilationAndInputQueryGroups = context.CompilationProvider.Combine(inputQueryGroupDeclarations.Collect());
        var compilationAndQueryGroups = context.CompilationProvider.Combine(queryGroupDeclarations.Collect());

        // Register the generation method for both
        context.RegisterSourceOutput(
            compilationAndInputQueryGroups,
            static (spc, source) => MakeGenerationExecutor(
                ToInputQueryGroupModel,
                ToSource));
        context.RegisterSourceOutput(
            compilationAndQueryGroups,
            static (spc, source) => MakeGenerationExecutor(
                ToQueryGroupModel,
                ToSource));
    }

    private static InputQueryGroupModel ToInputQueryGroupModel(
        Compilation compilation,
        InterfaceDeclarationSyntax syntax,
        SourceProductionContext context)
    {
        var inputQueries = new List<InputQueryModel>();
        var symbol = (INamedTypeSymbol)compilation
            .GetSemanticModel(syntax.SyntaxTree)
            .GetDeclaredSymbol(syntax)!;
        foreach (var member in IgnorePropertyMethods(symbol.GetMembers()))
        {
            if (member is IPropertySymbol prop)
            {
                inputQueries.Add(new(
                    Symbol: member,
                    Keys: ImmutableArray<INamedTypeSymbol>.Empty,
                    StoredType: prop.Type));
            }
            else if (member is IMethodSymbol method)
            {
                inputQueries.Add(new(
                    Symbol: member,
                    Keys: method.Parameters.Select(p => p.Type).ToList(),
                    StoredType: method.ReturnType));
            }
        }
        return new(Symbol: symbol, InputQueries: inputQueries);
    }

    private static QueryGroupModel ToQueryGroupModel(
        Compilation compilation,
        InterfaceDeclarationSyntax syntax,
        SourceProductionContext context)
    {
        var queries = new List<QueryModel>();
        var symbol = (INamedTypeSymbol)compilation
            .GetSemanticModel(syntax.SyntaxTree)
            .GetDeclaredSymbol(syntax)!;
        foreach (var member in IgnorePropertyMethods(symbol.GetMembers()))
        {
            IReadOnlyList<ITypeSymbol> keys;
            ITypeSymbol returnType;
            ITypeSymbol? awaitedType = null;
            bool hasCt = false;
            if (member is IPropertySymbol prop)
            {
                keys = ImmutableArray<INamedTypeSymbol>.Empty;
                returnType = prop.Type;
            }
            else if (member is IMethodSymbol method)
            {
                keys = method.Parameters.Select(p => p.Type).ToList();
                returnType = method.ReturnType;
                // Determine, if has CT
                if (keys.Count > 0 && keys[keys.Count - 1].Name == typeof(CancellationToken).FullName)
                {
                    // Last parameter is a CT
                    ((List<ITypeSymbol>)keys).RemoveAt(keys.Count - 1);
                    hasCt = true;
                }
            }
            else
            {
                continue;
            }

            // Now determine if it's a task
            if (IsAwaitable(returnType, out var awaited)) awaitedType = awaited;

            // Got all info
            queries.Add(new(
                Symbol: member,
                Keys: keys,
                ReturnType: returnType,
                AwaitedType: awaitedType,
                HasCancellationToken: hasCt));
        }
        return new(Symbol: symbol, Queries: queries);
    }

    private static (string Name, string Text) ToSource(InputQueryGroupModel model)
    {
        throw new NotImplementedException();
    }

    private static (string Name, string Text) ToSource(QueryGroupModel model)
    {
        throw new NotImplementedException();
    }

    private static bool IsAwaitable(ITypeSymbol symbol, [MaybeNullWhen(false)] out ITypeSymbol awaitedType)
    {
        awaitedType = null;
        var getAwaiterMethods = symbol
            .GetMembers("GetAwaiter")
            .OfType<IMethodSymbol>()
            .Where(m => m.Parameters.Length == 0);
        foreach (var getAwaiterMethod in getAwaiterMethods)
        {
            var awaiterType = getAwaiterMethod.ReturnType;
            var awaiterGetResult = awaiterType
                .GetMembers("GetResult")
                .OfType<IMethodSymbol>()
                .Where(m => m.Parameters.Length == 0)
                .FirstOrDefault();
            awaitedType = awaiterGetResult.ReturnType;
            return true;
        }
        return false;
    }

    private static IEnumerable<ISymbol> IgnorePropertyMethods(IEnumerable<ISymbol> symbols)
    {
        // Collect property names in a set
        var propertyNames = new HashSet<string>(symbols.OfType<IPropertySymbol>().Select(p => p.Name));
        // Keep symbols without get_ or set_ + a property name
        return symbols.Where(sym => sym is not IMethodSymbol methodSymbol
                                 || !((sym.Name.StartsWith("get_") || sym.Name.StartsWith("set_"))
                                    && propertyNames.Contains(sym.Name.Substring(4))));
    }

    private static Action<Compilation, ImmutableArray<InterfaceDeclarationSyntax>, SourceProductionContext>
        MakeGenerationExecutor<TModel>(
            Func<Compilation, InterfaceDeclarationSyntax, SourceProductionContext, TModel> toModel,
            Func<TModel, (string Name, string Text)> toSource
        ) => (compilation, interfaces, context) =>
        {
            // If there are not interfaces, don't attempt to do anything
            if (interfaces.IsEmpty) return;

            // Stolen idea
            var distinctInterfaces = interfaces.Distinct();

            // Convert each to a model object
            var models = interfaces.Select(i => toModel(compilation, i, context)).ToList();

            // Convert each to source
            foreach (var (name, text) in models.Select(toSource))
            {
                context.AddSource(name, SourceText.From(text, Encoding.UTF8));
            }
        };

    private static bool IsInterfaceWithAttributes(SyntaxNode node, CancellationToken cancellationToken) =>
        node is InterfaceDeclarationSyntax i && i.AttributeLists.Count > 0;

    private static Func<GeneratorSyntaxContext, CancellationToken, InterfaceDeclarationSyntax?>
        MakeSemanticTargetTransformer(Type attrType) =>
        (context, ct) =>
        {
            // We know it's an interface declaration
            var interfaceDeclarationSyntax = (InterfaceDeclarationSyntax)context.Node;

            // Go through all attribute lists
            foreach (var attributeListSyntax in interfaceDeclarationSyntax.AttributeLists)
            {
                // Go throught all attributes in the list
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                {
                    // Get the symbol info
                    if (context.SemanticModel.GetSymbolInfo(attributeSyntax, ct).Symbol is not IMethodSymbol attributeSymbol) continue;

                    // Check if it's the attribute we are looking for
                    var attrTypeSymbol = attributeSymbol.ContainingType;
                    if (attrTypeSymbol.ToDisplayString() == attrType.FullName) return interfaceDeclarationSyntax;
                }
            }

            // No luck
            return null;
        };

    private static void InjectSources(IncrementalGeneratorPostInitializationContext context)
    {
        var assembly = Assembly.GetExecutingAssembly();
        // Get the resource names that start with 'InjectedSources.'
        var injectedSourceNames = assembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith("InjectedSources."));
        // Loop through them and add them to the compilation
        foreach (var sourceName in injectedSourceNames)
        {
            // Get the stream
            using var stream = assembly.GetManifestResourceStream(sourceName);
            // Read out the source
            using var reader = new StreamReader(stream);
            var text = reader.ReadToEnd();
            // Add to build
            context.AddSource(sourceName, SourceText.From(text, Encoding.UTF8));
        }
    }
}
