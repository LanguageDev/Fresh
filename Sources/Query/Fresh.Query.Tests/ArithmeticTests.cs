using Xunit;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Fresh.Query.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Fresh.Query.Tests;

// a   b   c
//  \ / \ /
//   v1  v2
//    \ /
//     v3
public sealed partial class ArithmeticTests
{
    [InputQueryGroup]
    public partial interface INumberInputs
    {
        public int Variable(string name);
    }

    [QueryGroup]
    public partial interface IArithmetic
    {
        public int V1 { get; }
        public int V2 { get; }
        public int V3(int add);
        public int V4(string v1, int k2);
    }

    public sealed class MyArithmetic : IArithmetic
    {
        public int V1_invocations = 0;
        public int V2_invocations = 0;
        public Dictionary<int, int> V3_invocations = new();

        public int V1
        {
            get
            {
                ++this.V1_invocations;
                return this.inputs.Variable("a") + this.inputs.Variable("b");
            }
        }

        public int V2
        {
            get
            {
                ++this.V2_invocations;
                return this.inputs.Variable("b") * this.inputs.Variable("c");
            }
        }

        private readonly INumberInputs inputs;
        private readonly IArithmetic arithmetic;

        public MyArithmetic(INumberInputs inputs, IArithmetic arithmetic)
        {
            this.inputs = inputs;
            this.arithmetic = arithmetic;
        }

        public int V3(int add)
        {
            if (!this.V3_invocations.TryGetValue(add, out var n)) n = 0;
            ++n;
            this.V3_invocations[add] = n;
            return this.arithmetic.V2 * add;
        }

        public int V4(string v1, int k2) =>
            this.inputs.Variable(v1) + k2;
    }

    [Fact]
    public void FullComputationOnce()
    {
        var host = GetHost();

        var input = host.Services.GetRequiredService<INumberInputs>();
        var arithProxy = host.Services.GetRequiredService<IArithmetic>();
        var arith = host.Services.GetRequiredService<MyArithmetic>();

        input.SetVariable("a", 1);
        input.SetVariable("b", 2);
        input.SetVariable("c", 3);

        Assert.Equal(3, arithProxy.V1);
        Assert.Equal(6, arithProxy.V3(1));
        Assert.Equal(1, arith.V1_invocations);
        Assert.Equal(1, arith.V2_invocations);
        Assert.Equal(1, arith.V3_invocations[1]);
        Assert.Equal(6, arithProxy.V2);
        Assert.Equal(1, arith.V2_invocations);
    }

    [Fact]
    public void UpdateB()
    {
        var host = GetHost();

        var input = host.Services.GetRequiredService<INumberInputs>();
        var arithProxy = host.Services.GetRequiredService<IArithmetic>();
        var arith = host.Services.GetRequiredService<MyArithmetic>();

        input.SetVariable("a", 1);
        input.SetVariable("b", 2);
        input.SetVariable("c", 3);

        // Now force-eval everyone
        var _1 = arithProxy.V1;
        var _2 = arithProxy.V3(1);

        // Update b
        input.SetVariable("b", 3);

        // Everything should be computed twice
        Assert.Equal(4, arithProxy.V1);
        Assert.Equal(9, arithProxy.V3(1));
        Assert.Equal(2, arith.V1_invocations);
        Assert.Equal(2, arith.V2_invocations);
        Assert.Equal(2, arith.V3_invocations[1]);
        Assert.Equal(9, arithProxy.V2);
        Assert.Equal(2, arith.V2_invocations);
    }

    [Fact]
    public void FlipBandCEarlyTerminatesV3()
    {
        var host = GetHost();

        var input = host.Services.GetRequiredService<INumberInputs>();
        var arithProxy = host.Services.GetRequiredService<IArithmetic>();
        var arith = host.Services.GetRequiredService<MyArithmetic>();

        input.SetVariable("a", 1);
        input.SetVariable("b", 2);
        input.SetVariable("c", 3);

        // Now force-eval everyone
        var _1 = arithProxy.V1;
        var _2 = arithProxy.V3(1);

        // Swap values of b and c
        input.SetVariable("b", 3);
        input.SetVariable("c", 2);

        // v1 and v2 should be computed twice, v3 shouldn't be re-computed
        Assert.Equal(4, arithProxy.V1);
        Assert.Equal(6, arithProxy.V3(1));
        Assert.Equal(2, arith.V1_invocations);
        Assert.Equal(2, arith.V2_invocations);
        Assert.Equal(1, arith.V3_invocations[1]);
        Assert.Equal(6, arithProxy.V2);
    }

    [Fact]
    public void DifferentKeysAreOrthogonal()
    {
        var host = GetHost();

        var input = host.Services.GetRequiredService<INumberInputs>();
        var arithProxy = host.Services.GetRequiredService<IArithmetic>();
        var arith = host.Services.GetRequiredService<MyArithmetic>();

        input.SetVariable("a", 1);
        input.SetVariable("b", 2);
        input.SetVariable("c", 3);

        Assert.Equal(3, arithProxy.V1);
        Assert.Equal(6, arithProxy.V2);
        Assert.Equal(6, arithProxy.V3(1));
        Assert.Equal(12, arithProxy.V3(2));
        Assert.Equal(1, arith.V1_invocations);
        Assert.Equal(1, arith.V2_invocations);
        Assert.Equal(1, arith.V3_invocations[1]);
        Assert.Equal(1, arith.V3_invocations[2]);
    }

    [Fact]
    public void KeyPointingAtDependency()
    {
        var host = GetHost();

        var input = host.Services.GetRequiredService<INumberInputs>();
        var arithProxy = host.Services.GetRequiredService<IArithmetic>();

        input.SetVariable("a", 1);
        input.SetVariable("b", 2);
        input.SetVariable("c", 3);

        Assert.Equal(1, arithProxy.V4("a", 0));
        Assert.Equal(3, arithProxy.V4("a", 2));
        Assert.Equal(7, arithProxy.V4("b", 5));
        Assert.Equal(9, arithProxy.V4("b", 7));
        Assert.Equal(8, arithProxy.V4("c", 5));

        input.SetVariable("b", 1);

        Assert.Equal(1, arithProxy.V4("a", 0));
        Assert.Equal(3, arithProxy.V4("a", 2));
        Assert.Equal(6, arithProxy.V4("b", 5));
        Assert.Equal(8, arithProxy.V4("b", 7));
        Assert.Equal(8, arithProxy.V4("c", 5));
    }

    private static IHost GetHost() => Host
        .CreateDefaultBuilder()
        .ConfigureQuerySystem(system => system
            .AddInputQueryGroup<INumberInputs>()
            .AddQueryGroup<IArithmetic, MyArithmetic>())
        .Build();
}
