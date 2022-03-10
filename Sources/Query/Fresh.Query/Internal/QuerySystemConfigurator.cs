// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using Fresh.Query.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fresh.Query.Internal;

internal sealed class QuerySystemConfigurator : IQuerySystemConfigurator
{
    private readonly IServiceCollection serviceCollection;
    private readonly QuerySystem querySystem = new();

    public QuerySystemConfigurator(IServiceCollection serviceCollection)
    {
        this.serviceCollection = serviceCollection;
        this.serviceCollection.AddSingleton<IQuerySystem>(this.querySystem);
    }

    public IQuerySystemConfigurator AddInputQueryGroup<TInterface>()
        where TInterface : IInputQueryGroup
    {
        var tInterface = typeof(TInterface);
        var tProxy = GetProxyType(tInterface);
        this.serviceCollection.AddSingleton(tInterface, tProxy);
        return this;
    }

    public IQuerySystemConfigurator AddQueryGroup<TInterface, TImpl>()
        where TInterface : class, IQueryGroup
        where TImpl : class, TInterface
    {
        var tInterface = typeof(TInterface);
        var tImpl = typeof(TImpl);
        var tProxy = GetProxyType(tInterface);

        // We register the implementation type exactly as is
        this.serviceCollection.AddSingleton<TImpl>();

        // The proxy gets registered through the interface
        this.serviceCollection.AddSingleton(provider =>
               (TInterface?)Activator.CreateInstance(tProxy, provider, tImpl)
            ?? throw new InvalidOperationException("Could not instantiate generated proxy"));

        return this;
    }

    private static Type GetProxyType(Type tInterface)
    {
        var proxyClass = tInterface.GetNestedType("Proxy")
                      ?? throw new InvalidOperationException($"The interface type {tInterface.Name} does not contain a generated proxy!");
        if (tInterface.GenericTypeArguments.Length > 0) proxyClass = proxyClass.MakeGenericType(tInterface.GenericTypeArguments);
        return proxyClass;
    }
}
