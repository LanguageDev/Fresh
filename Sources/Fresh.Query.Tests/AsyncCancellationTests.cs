// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fresh.Query.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Fresh.Query.Tests;

public sealed partial class AsyncCancellationTests
{
    [InputQueryGroup]
    public partial interface INumberInputs
    {
        public int Variable(string name);
    }

    [QueryGroup]
    public partial interface IAsyncCancellableComputation
    {
        public Task<int> ComputeKeylessValue(CancellationToken cancellationToken);
        public Task<int> ComputeKeyedValue(string s1, string s2, CancellationToken cancellationToken);
    }

    public sealed class MyAsyncCancellableComputation : IAsyncCancellableComputation
    {
        public int keylessCount = 0;
        public Dictionary<(string, string), int> keyedCount = new();

        private readonly INumberInputs inputs;
        private readonly IAsyncCancellableComputation computation;

        public MyAsyncCancellableComputation(INumberInputs inputs, IAsyncCancellableComputation computation)
        {
            this.inputs = inputs;
            this.computation = computation;
        }

        public Task<int> ComputeKeylessValue(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return Task.FromResult(0);
            ++this.keylessCount;
            return Task.FromResult(this.inputs.Variable("x") + this.inputs.Variable("y"));
        }

        public async Task<int> ComputeKeyedValue(string s1, string s2, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return 0;
            var v1 = await this.computation.ComputeKeylessValue(cancellationToken);
            if (cancellationToken.IsCancellationRequested) return 0;
            var newCount = 0;
            if (this.keyedCount.TryGetValue((s1, s2), out var oldCount)) newCount = oldCount;
            this.keyedCount[(s1, s2)] = newCount + 1;
            return v1 * this.inputs.Variable(s1) + this.inputs.Variable(s2);
        }
    }

    [Fact]
    public void TestCancellationTokenNotPartOfKey()
    {
        var host = GetHost();

        var input = host.Services.GetRequiredService<INumberInputs>();
        var computationProxy = host.Services.GetRequiredService<IAsyncCancellableComputation>();
        var computation = host.Services.GetRequiredService<MyAsyncCancellableComputation>();

        input.SetVariable("x", 2);
        input.SetVariable("y", 3);
        input.SetVariable("z", 4);

        // Call it with one CT source
        var ctSource1 = new CancellationTokenSource();
        var v1 = computationProxy.ComputeKeylessValue(ctSource1.Token).Result;
        var v2 = computationProxy.ComputeKeyedValue("x", "z", ctSource1.Token).Result;
        var v3 = computationProxy.ComputeKeyedValue("y", "z", ctSource1.Token).Result;
        Assert.Equal(5, v1);
        Assert.Equal(14, v2);
        Assert.Equal(19, v3);
        Assert.Equal(1, computation.keylessCount);
        Assert.Equal(1, computation.keyedCount[("x", "z")]);
        Assert.Equal(1, computation.keyedCount[("y", "z")]);

        // Now the other CT source
        var ctSource2 = new CancellationTokenSource();
        v1 = computationProxy.ComputeKeylessValue(ctSource2.Token).Result;
        v2 = computationProxy.ComputeKeyedValue("x", "z", ctSource2.Token).Result;
        v3 = computationProxy.ComputeKeyedValue("y", "z", ctSource2.Token).Result;
        Assert.Equal(5, v1);
        Assert.Equal(14, v2);
        Assert.Equal(19, v3);
        // No recomputation should happen
        Assert.Equal(1, computation.keylessCount);
        Assert.Equal(1, computation.keyedCount[("x", "z")]);
        Assert.Equal(1, computation.keyedCount[("y", "z")]);
    }

    private static IHost GetHost() => Host
        .CreateDefaultBuilder()
        .ConfigureQuerySystem(system => system
            .AddInputQueryGroup<INumberInputs>()
            .AddQueryGroup<IAsyncCancellableComputation, MyAsyncCancellableComputation>())
        .Build();
}
