using Fresh.Query;
using Fresh.Query.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fresh.Query.Example;

[InputQueryGroup]
internal partial interface INumberInputs
{
    public int Variable(string name);
}

[QueryGroup]
internal partial interface IComputation
{
    public int CustomConstant { get; }
    public Task<int> CustomComputation(string varName, int k, CancellationToken ct);
}

internal class MyComputation : IComputation
{
    public int CustomConstant => throw new NotImplementedException();

    public Task<int> CustomComputation(string varName, int k, CancellationToken ct) => throw new NotImplementedException();
}

internal class Program
{
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        var val = host.Services.GetRequiredService<INumberInputs>();
        var comp = host.Services.GetRequiredService<IComputation>();
        val.SetVariable("x", 3);
        var a = comp.CustomComputation("x", 4, CancellationToken.None);
    }

    public static IHostBuilder CreateHostBuilder(string[] args) => Host
        .CreateDefaultBuilder(args)
        .ConfigureQuerySystem(system => system
            .AddInputQueryGroup<INumberInputs>()
            .AddQueryGroup<IComputation, MyComputation>());
}
