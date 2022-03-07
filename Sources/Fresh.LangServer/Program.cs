using System;
using System.Threading;
using System.Threading.Tasks;
using Fresh.Compiler;
using Fresh.Compiler.Services;
using Fresh.LangServer.Handlers;
using Fresh.Query.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;

internal static class Program
{
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
    private static void Main(string[] args) => MainAsync(args).Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

    internal static async Task MainAsync(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        var server = host.Services.GetRequiredService<LanguageServer>();
        await server.Initialize(CancellationToken.None);
        await server.WaitForExit;
    }

    internal static IHostBuilder CreateHostBuilder(string[] args) => Host
        .CreateDefaultBuilder(args)
        .ConfigureQuerySystem(system => system
            .AddInputQueryGroup<IInputService>()
            .AddQueryGroup<ISyntaxService, SyntaxService>())
        .ConfigureServices(services => services.AddLanguageServer(options => options
            .WithInput(Console.OpenStandardInput())
            .WithOutput(Console.OpenStandardOutput())
            .WithLoggerFactory(new LoggerFactory())
            .ConfigureLogging(loggingBuilder => loggingBuilder
                .AddLanguageProtocolLogging()
                .SetMinimumLevel(LogLevel.Debug))
            .WithHandler<TextDocumentHandler>()));
}
