using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Fresh.Query.Hosting;

/// <summary>
/// Extensions for <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Allows the user to configure the query system.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configure">The delegate to configure the query system.</param>
    /// <returns>The <paramref name="builder"/> itself.</returns>
    public static IHostBuilder ConfigureQuerySystem(this IHostBuilder builder, Action<IQuerySystemConfigurator> configure)
    {
        // TODO
        throw new NotImplementedException();
        return builder;
    }
}
