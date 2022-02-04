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

public sealed partial class GenericsTests
{
    [InputQueryGroup]
    public partial interface IInputs<T>
    {
        public T Variable(string name);
    }

    [QueryGroup]
    public partial interface IComputation<T>
    {
        public (T, T) ComputeSomething(string n1, string n2);
    }

    public sealed class MyComputation<T> : IComputation<T>
    {
        private readonly IInputs<T> inputs;

        public MyComputation(IInputs<T> inputs)
        {
            this.inputs = inputs;
        }

        public (T, T) ComputeSomething(string n1, string n2) =>
            (this.inputs.Variable("x"), this.inputs.Variable("y"));
    }

    [Fact]
    public void Simple()
    {
        var host = GetHost();

        var inputs = host.Services.GetRequiredService<IInputs<int>>();
        var computation = host.Services.GetRequiredService<IComputation<int>>();

        inputs.SetVariable("x", 1);
        inputs.SetVariable("y", 2);

        var _1 = computation.ComputeSomething("x", "y");

        Assert.Equal((1, 2), _1);
    }

    private static IHost GetHost() => Host
        .CreateDefaultBuilder()
        .ConfigureQuerySystem(system => system
            .AddInputQueryGroup<IInputs<int>>()
            .AddQueryGroup<IComputation<int>, MyComputation<int>>())
        .Build();
}
