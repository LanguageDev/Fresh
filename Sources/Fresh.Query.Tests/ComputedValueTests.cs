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

public sealed partial class ComputedValueTests
{
    [InputQueryGroup]
    public partial interface IInputs
    {
        public int MyConstant { get; set; }
        public int Variable(string name);
    }

    [QueryGroup]
    public partial interface IComputation
    {
        public int ComputeSomething();
        public int ComputeOther(string v1, string v2);
    }

    public sealed class MyComputation : IComputation
    {
        private readonly IInputs inputs;

        public MyComputation(IInputs inputs)
        {
            this.inputs = inputs;
        }

        public int ComputeSomething() =>
            this.inputs.MyConstant + this.inputs.Variable("a");

        public int ComputeOther(string v1, string v2) =>
            this.inputs.MyConstant * (this.inputs.Variable(v1) + this.inputs.Variable(v2));
    }

    [Fact]
    public void ComputeWithoutKey()
    {
        var host = GetHost();

        var inputs = host.Services.GetRequiredService<IInputs>();
        var computation = host.Services.GetRequiredService<IComputation>();

        // Asking for the computation should throw initially as the inputs are not set
        Assert.Throws<AggregateException>(() => computation.ComputeSomething());

        // Let's set one of the inputs
        inputs.MyConstant = 3;
        // Should still throw
        Assert.Throws<AggregateException>(() => computation.ComputeSomething());

        // Setting the other should make it valid
        inputs.SetVariable("a", 5);
        Assert.Equal(8, computation.ComputeSomething());

        // Updating a value should change result
        inputs.SetVariable("a", 7);
        Assert.Equal(10, computation.ComputeSomething());
    }

    [Fact]
    public void ComputeWithKey()
    {
        var host = GetHost();

        var inputs = host.Services.GetRequiredService<IInputs>();
        var computation = host.Services.GetRequiredService<IComputation>();

        // Asking for the computation should throw initially as the inputs are not set
        Assert.Throws<AggregateException>(() => computation.ComputeOther("x", "y"));

        // Setting the variables is not enough, the constant is required
        inputs.SetVariable("x", 4);
        inputs.SetVariable("y", 7);
        Assert.Throws<AggregateException>(() => computation.ComputeOther("x", "y"));

        // Setting the constant should resolve it
        inputs.MyConstant = 2;
        Assert.Equal(22, computation.ComputeOther("x", "y"));

        // Updating a value should change result
        inputs.SetVariable("y", 3);
        Assert.Equal(14, computation.ComputeOther("x", "y"));
    }

    private static IHost GetHost() => Host
        .CreateDefaultBuilder()
        .ConfigureQuerySystem(system => system
            .AddInputQueryGroup<IInputs>()
            .AddQueryGroup<IComputation, MyComputation>())
        .Build();
}
