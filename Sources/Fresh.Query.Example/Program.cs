using Fresh.Query;
using Fresh.Query.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.Query.Example;

[InputQueryGroup]
internal interface INumberInputs
{
    public int Variable(string name);
}

[QueryGroup]
internal interface IComputation
{
    public int CustomConstant { get; }
    public int CustomComputation(string varName, int k);
}

class MyComputation : IComputation
{
    public int CustomConstant => throw new NotImplementedException();

    public int CustomComputation(string varName, int k) => throw new NotImplementedException();
}

internal class Program
{
    public static void Main(string[] args) => CreateHostBuilder(args)
        .Build()
        .Run();

    public static IHostBuilder CreateHostBuilder(string[] args) => Host
        .CreateDefaultBuilder(args)
        .ConfigureQuerySystem(system => system
            .AddInputQueryGroup<INumberInputs>()
            .AddQueryGroup<IComputation, MyComputation>());
}
