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
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
            .MinimumLevel.Verbose()
            .CreateLogger();

        var server = await CreateLanguageServer(args);
        await server.Initialize(CancellationToken.None);
        await server.WaitForExit;
    }

    internal static Task<LanguageServer> CreateLanguageServer(string[] args) => LanguageServer.From(options => options
        .WithInput(Console.OpenStandardInput())
        .WithOutput(Console.OpenStandardOutput())
        .ConfigureLogging(loggingBuilder => loggingBuilder
            .AddSerilog(Log.Logger)
            .AddLanguageProtocolLogging()
            .SetMinimumLevel(LogLevel.Trace))
        .WithServices(services => services
            .ConfigureQuerySystem(system => system
                .AddInputQueryGroup<IInputService>()
                .AddQueryGroup<ISyntaxService, SyntaxService>()))
        .WithHandler<TextDocumentHandler>());
}
