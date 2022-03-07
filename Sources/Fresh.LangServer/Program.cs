

using Fresh.Compiler;
using Fresh.Query.Hosting;
using Microsoft.Extensions.Hosting;

internal static class Program
{
    internal static void Main(string[] args)
    {

    }

    internal static IHostBuilder CreateHostBuilder(string[] args) => Host
        .CreateDefaultBuilder(args)
        .ConfigureQuerySystem(system => system
            .AddInputQueryGroup<IInputService>()
            .AddQueryGroup<ISyntaxService, SyntaxService>());
}
