using Fresh.Query.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fresh.Query.Example;

[InputQueryGroup]
internal partial interface INumberInputs
{
    public int Steps { get; set; }
}

[QueryGroup]
internal partial interface IMathService
{
    public int Fibonacci(int n);
    public int Factorial(int n);
}

internal class MathService : IMathService
{
    private readonly INumberInputs inputs;
    private readonly IMathService mathService;

    public MathService(INumberInputs inputs, IMathService mathService)
    {
        this.inputs = inputs;
        this.mathService = mathService;
    }

    public int Fibonacci(int n)
    {
        Console.WriteLine($"Computing Fibonacci({n})");
        return n switch
        {
            < 2 => 1,
            _ => this.mathService.Fibonacci(n - 1) + this.mathService.Fibonacci(n - 2),
        };
    }

    public int Factorial(int n)
    {
        var order = this.inputs.Steps;
        Console.WriteLine($"Computing Factorial({n}) of order {order}");
        return n switch
        {
            <= 1 => 1,
            _ => n * this.mathService.Factorial(n - order),
        };
    }
}

internal class Program
{
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        var numbers = host.Services.GetRequiredService<INumberInputs>();
        var math = host.Services.GetRequiredService<IMathService>();

        Console.WriteLine($"Fibonacci(5) = {math.Fibonacci(5)}");
        Console.WriteLine($"Fibonacci(10) = {math.Fibonacci(10)}");

        Console.WriteLine("Factorials of order 1");
        numbers.Steps = 1;
        Console.WriteLine($"Factorial(4) = {math.Factorial(4)}");
        Console.WriteLine($"Factorial(6) = {math.Factorial(6)}");
        Console.WriteLine("Factorials of order 2");
        Console.WriteLine("Don't get weirder out by the random non-order 2 computations!");
        Console.WriteLine("That is just the old results getting invalidated, because the dependent value 'Steps' changed!");
        numbers.Steps = 2;
        Console.WriteLine($"Factorial(4) = {math.Factorial(4)}");
        Console.WriteLine($"Factorial(6) = {math.Factorial(6)}");
    }

    public static IHostBuilder CreateHostBuilder(string[] args) => Host
        .CreateDefaultBuilder(args)
        .ConfigureQuerySystem(system => system
            .AddInputQueryGroup<INumberInputs>()
            .AddQueryGroup<IMathService, MathService>());
}
