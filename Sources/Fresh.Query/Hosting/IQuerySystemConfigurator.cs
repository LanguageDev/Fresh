// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.Query.Hosting;

/// <summary>
/// Allows configuring the query system.
/// </summary>
public interface IQuerySystemConfigurator
{
    /// <summary>
    /// Adds an input query group to the system.
    /// </summary>
    /// <typeparam name="TInterface">The input query group interface.</typeparam>
    /// <returns>The configurator instance.</returns>
    public IQuerySystemConfigurator AddInputQueryGroup<TInterface>()
        where TInterface : IInputQueryGroup;

    /// <summary>
    /// Adds a query group to the system.
    /// </summary>
    /// <typeparam name="TInterface">The query group interface.</typeparam>
    /// <typeparam name="TImpl">The qzery group implementation.</typeparam>
    /// <returns>The configurator instance.</returns>
    public IQuerySystemConfigurator AddQueryGroup<TInterface, TImpl>()
        where TInterface : class, IQueryGroup
        where TImpl : class, TInterface;
}
