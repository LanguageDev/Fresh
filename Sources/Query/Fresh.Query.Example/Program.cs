using Fresh.Query.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fresh.Query.Example;

[InputQueryGroup]
public partial interface IInputService
{
    public string Var(string name);
}

[QueryGroup]
public partial interface IMathService
{
    public int Fib(int n);
    public int ParseVar(string name);
    public int FibFromVar(string name);
}

public sealed class MathService : IMathService
{
    private readonly IInputService input;
    private readonly IMathService math;

    public MathService(
        IInputService input,
        IMathService math)
    {
        this.input = input;
        this.math = math;
    }

    public int Fib(int n)
    {
        Console.WriteLine($"Start of Fib({n})");
        var result = n switch
        {
            < 2 => 1,
            _ => this.math.Fib(n - 1) + this.math.Fib(n - 2),
        };
        Console.WriteLine($"End of Fib({n})");
        return result;
    }

    public int ParseVar(string name)
    {
        Console.WriteLine($"Start of ParseVar(\"{name}\")");
        var result = int.Parse(this.input.Var(name));
        Console.WriteLine($"End of ParseVar(\"{name}\")");
        return result;
    }

    public int FibFromVar(string name)
    {
        Console.WriteLine($"Start of FibFromVar(\"{name}\")");
        var n = this.math.ParseVar(name);
        var result = this.math.Fib(n);
        Console.WriteLine($"End of FibFromVar(\"{name}\")");
        return result;
    }
}

internal class Program
{
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        var numbers = host.Services.GetRequiredService<IInputService>();
        var math = host.Services.GetRequiredService<IMathService>();

        numbers.SetVar("n", "5");
        Console.WriteLine("=======================");
        Console.WriteLine($"Fib(5) = {math.FibFromVar("n")}");
        Console.WriteLine("=======================");

        numbers.SetVar("n", "8");
        Console.WriteLine("=======================");
        Console.WriteLine($"Fib(8) = {math.FibFromVar("n")}");
        Console.WriteLine("=======================");

        numbers.SetVar("n", "8 ");
        Console.WriteLine("=======================");
        Console.WriteLine($"Fib(8) = {math.FibFromVar("n")}");
        Console.WriteLine("=======================");
    }

    public static IHostBuilder CreateHostBuilder(string[] args) => Host
        .CreateDefaultBuilder(args)
        .ConfigureQuerySystem(system => system
            .AddInputQueryGroup<IInputService>()
            .AddQueryGroup<IMathService, MathService>());
}
