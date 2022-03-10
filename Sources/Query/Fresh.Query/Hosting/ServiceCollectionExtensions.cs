// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fresh.Query.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fresh.Query.Hosting;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Allows the user to configure the query system.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="configure">The delegate to configure the query system.</param>
    /// <returns>The <paramref name="serviceCollection"/> itself.</returns>
    public static IServiceCollection ConfigureQuerySystem(this IServiceCollection serviceCollection, Action<IQuerySystemConfigurator> configure)
    {
        configure(new QuerySystemConfigurator(serviceCollection));
        return serviceCollection;
    }
}
