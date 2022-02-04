// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fresh.Query.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Fresh.Query.Tests;

public sealed partial class AsyncTests
{
    [InputQueryGroup]
    public partial interface INumberInputs
    {
        public int Variable(string name);
    }

    [QueryGroup]
    public partial interface IAsyncComputation
    {
        public Task<int> ComputeKeylessValue();
        public Task<int> ComputeKeyedValue(string s1, string s2);
    }

    public sealed class MyAsyncComputation : IAsyncComputation
    {
        private readonly INumberInputs inputs;
        private readonly IAsyncComputation computation;

        public MyAsyncComputation(INumberInputs inputs, IAsyncComputation computation)
        {
            this.inputs = inputs;
            this.computation = computation;
        }

        public Task<int> ComputeKeylessValue() =>
            Task.FromResult(this.inputs.Variable("x") + this.inputs.Variable("y"));

        public async Task<int> ComputeKeyedValue(string s1, string s2)
        {
            var v1 = await this.computation.ComputeKeylessValue();
            return v1 * this.inputs.Variable(s1) + this.inputs.Variable(s2);
        }
    }

    [Fact]
    public void BasicTests()
    {
        var host = GetHost();

        var input = host.Services.GetRequiredService<INumberInputs>();
        var computations = host.Services.GetRequiredService<IAsyncComputation>();

        input.SetVariable("x", 3);
        input.SetVariable("y", 4);
        input.SetVariable("z", 2);
        input.SetVariable("w", 9);

        Assert.Equal(7, computations.ComputeKeylessValue().Result);
        Assert.Equal(23, computations.ComputeKeyedValue("z", "w").Result);
    }

    private static IHost GetHost() => Host
        .CreateDefaultBuilder()
        .ConfigureQuerySystem(system => system
            .AddInputQueryGroup<INumberInputs>()
            .AddQueryGroup<IAsyncComputation, MyAsyncComputation>())
        .Build();
}
