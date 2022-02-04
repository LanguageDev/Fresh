// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fresh.Query.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Fresh.Query.Tests;

public sealed partial class CancellationTests
{
    [InputQueryGroup]
    public partial interface INumberInputs
    {
        public int Variable(string name);
    }

    [QueryGroup]
    public partial interface ICancellableComputation
    {
        public int ComputeKeylessValue(CancellationToken ct);
        public int ComputeKeyedValue(string s1, string s2, CancellationToken ct);
    }

    public sealed class MyCancellableComputation : ICancellableComputation
    {
        public int keylessCount = 0;
        public Dictionary<(string, string), int> keyedCount = new();

        private readonly INumberInputs inputs;

        public MyCancellableComputation(INumberInputs inputs)
        {
            this.inputs = inputs;
        }

        public int ComputeKeylessValue(CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return 0;
            ++this.keylessCount;
            return this.inputs.Variable("x") * this.inputs.Variable("y");
        }

        public int ComputeKeyedValue(string s1, string s2, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return 0;
            int newCount = 0;
            if (this.keyedCount.TryGetValue((s1, s2), out var oldCount)) newCount = oldCount;
            this.keyedCount[(s1, s2)] = newCount + 1;
            return this.inputs.Variable(s1) * this.inputs.Variable(s2);
        }
    }

    [Fact]
    public void TestCancellationTokenNotPartOfKey()
    {
        var host = GetHost();

        var input = host.Services.GetRequiredService<INumberInputs>();
        var computationProxy = host.Services.GetRequiredService<ICancellableComputation>();
        var computation = host.Services.GetRequiredService<MyCancellableComputation>();

        input.SetVariable("x", 2);
        input.SetVariable("y", 3);
        input.SetVariable("z", 4);

        // Call it with one CT source
        var ctSource1 = new CancellationTokenSource();
        var v1 = computationProxy.ComputeKeylessValue(ctSource1.Token);
        var v2 = computationProxy.ComputeKeyedValue("x", "z", ctSource1.Token);
        var v3 = computationProxy.ComputeKeyedValue("y", "z", ctSource1.Token);
        Assert.Equal(6, v1);
        Assert.Equal(8, v2);
        Assert.Equal(12, v3);
        Assert.Equal(1, computation.keylessCount);
        Assert.Equal(1, computation.keyedCount[("x", "z")]);
        Assert.Equal(1, computation.keyedCount[("y", "z")]);

        // Now the other CT source
        var ctSource2 = new CancellationTokenSource();
        v1 = computationProxy.ComputeKeylessValue(ctSource2.Token);
        v2 = computationProxy.ComputeKeyedValue("x", "z", ctSource2.Token);
        v3 = computationProxy.ComputeKeyedValue("y", "z", ctSource2.Token);
        Assert.Equal(6, v1);
        Assert.Equal(8, v2);
        Assert.Equal(12, v3);
        // No recomputation should happen
        Assert.Equal(1, computation.keylessCount);
        Assert.Equal(1, computation.keyedCount[("x", "z")]);
        Assert.Equal(1, computation.keyedCount[("y", "z")]);
    }

    private static IHost GetHost() => Host
        .CreateDefaultBuilder()
        .ConfigureQuerySystem(system => system
            .AddInputQueryGroup<INumberInputs>()
            .AddQueryGroup<ICancellableComputation, MyCancellableComputation>())
        .Build();
}
