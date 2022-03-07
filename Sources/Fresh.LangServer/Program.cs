using System;
using System.Threading.Tasks;
using Fresh.Compiler;
using Fresh.Compiler.Services;
using Fresh.LangServer.Services;
using Fresh.Query.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OmniSharp.Extensions.LanguageServer.Server;

internal static class Program
{
    internal static async Task Main(string[] args)
    {
        // TODO: Figure out if we can configure the server alongside the services
        var host = CreateHostBuilder(args).Build();
        await host.RunAsync();
    }

    internal static IHostBuilder CreateHostBuilder(string[] args) => Host
        .CreateDefaultBuilder(args)
        .ConfigureQuerySystem(system => system
            .AddInputQueryGroup<IInputService>()
            .AddQueryGroup<ISyntaxService, SyntaxService>())
        .ConfigureServices(services => services.AddLanguageServer(options => options
            .WithInput(Console.OpenStandardInput())
            .WithOutput(Console.OpenStandardOutput())
            // TODO: Configure logging
            .WithHandler<TextDocumentHandler>()
            .OnInitialize(async (server, request, token) =>
            {
                Console.WriteLine("HELLO LANGSERVER");
            })));
}
