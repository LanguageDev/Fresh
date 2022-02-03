// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Fresh.Query;

[Generator]
public sealed class QueryProxyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add all injected sources
        context.RegisterPostInitializationOutput(InjectSources);
    }

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
