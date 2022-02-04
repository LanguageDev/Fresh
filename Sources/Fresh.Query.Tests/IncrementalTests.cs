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

public sealed partial class IncrementalTests
{
    [InputQueryGroup]
    public partial interface IIncrementalInputs
    {
        public int SomeConstant { get; set; }
        public string SomeValue(string k1);
    }

    [QueryGroup]
    public partial interface IIncrementalQuery
    {
        public int CalculatedValue { get; }
        public string CalculateFoo(string k1, int k2);
    }

    public sealed class MyIncrementalQuery : IIncrementalQuery
    {
        public int CalculatedValue_invocations = 0;
        public int CalculateFoo_invocations = 0;

        private readonly IIncrementalInputs inputs;

        public MyIncrementalQuery(IIncrementalInputs inputs)
        {
            this.inputs = inputs;
        }

        public int CalculatedValue
        {
            get
            {
                ++this.CalculatedValue_invocations;
                return this.inputs.SomeConstant * 3;
            }
        }

        public string CalculateFoo(string k1, int k2)
        {
            ++this.CalculateFoo_invocations;
            return $"{this.inputs.SomeValue(k1)}_{this.inputs.SomeConstant * k2}";
        }
    }

    [Fact]
    public void NoRecalculationWhenNoInputChange()
    {
        var host = GetHost();

        var inputs = host.Services.GetRequiredService<IIncrementalInputs>();
        var queryProxy = host.Services.GetRequiredService<IIncrementalQuery>();
        var query = host.Services.GetRequiredService<MyIncrementalQuery>();

        inputs.SomeConstant = 7;
        inputs.SetSomeValue("abc", "xyz");

        // First invocation should cause a recomputation
        var _1 = queryProxy.CalculatedValue;
        var _2 = queryProxy.CalculateFoo("abc", 4);
        Assert.Equal(1, query.CalculatedValue_invocations);
        Assert.Equal(1, query.CalculateFoo_invocations);

        // Next recomputation should not
        var _3 = queryProxy.CalculatedValue;
        var _4 = queryProxy.CalculateFoo("abc", 4);
        Assert.Equal(1, query.CalculatedValue_invocations);
        Assert.Equal(1, query.CalculateFoo_invocations);
    }

    [Fact]
    public void RecalculationWhenInputChange()
    {
        var host = GetHost();

        var inputs = host.Services.GetRequiredService<IIncrementalInputs>();
        var queryProxy = host.Services.GetRequiredService<IIncrementalQuery>();
        var query = host.Services.GetRequiredService<MyIncrementalQuery>();

        inputs.SomeConstant = 7;
        inputs.SetSomeValue("abc", "xyz");

        // First invocation should cause a recomputation
        var _1 = queryProxy.CalculatedValue;
        var _2 = queryProxy.CalculateFoo("abc", 4);
        Assert.Equal(1, query.CalculatedValue_invocations);
        Assert.Equal(1, query.CalculateFoo_invocations);

        // Next recomputation should not
        var _3 = queryProxy.CalculatedValue;
        var _4 = queryProxy.CalculateFoo("abc", 4);
        Assert.Equal(1, query.CalculatedValue_invocations);
        Assert.Equal(1, query.CalculateFoo_invocations);

        // Changing again should
        inputs.SomeConstant = 6;
        inputs.SetSomeValue("abc", "xyw");
        var _5 = queryProxy.CalculatedValue;
        var _6 = queryProxy.CalculateFoo("abc", 4);
        Assert.Equal(2, query.CalculatedValue_invocations);
        Assert.Equal(2, query.CalculateFoo_invocations);

        // Changing just one should just recompute one
        inputs.SetSomeValue("abc", "qwe");
        var _7 = queryProxy.CalculatedValue;
        var _8 = queryProxy.CalculateFoo("abc", 4);
        Assert.Equal(2, query.CalculatedValue_invocations);
        Assert.Equal(3, query.CalculateFoo_invocations);
    }

    [Fact]
    public void RecalculationWhenClear()
    {
        var host = GetHost();

        var inputs = host.Services.GetRequiredService<IIncrementalInputs>();
        var queryProxy = host.Services.GetRequiredService<IIncrementalQuery>();
        var query = host.Services.GetRequiredService<MyIncrementalQuery>();

        inputs.SomeConstant = 7;
        inputs.SetSomeValue("abc", "xyz");

        // First invocation should cause a recomputation
        var _1 = queryProxy.CalculatedValue;
        var _2 = queryProxy.CalculateFoo("abc", 4);
        Assert.Equal(1, query.CalculatedValue_invocations);
        Assert.Equal(1, query.CalculateFoo_invocations);

        // Clear to cause recomputation
        host.Services.GetRequiredService<IQuerySystem>().Clear();

        // Next recomputation should also recompute everything
        var _3 = queryProxy.CalculatedValue;
        var _4 = queryProxy.CalculateFoo("abc", 4);
        Assert.Equal(2, query.CalculatedValue_invocations);
        Assert.Equal(2, query.CalculateFoo_invocations);
    }

    [Fact]
    public void RecalculationWhenDisableMemoization()
    {
        var host = GetHost();

        var inputs = host.Services.GetRequiredService<IIncrementalInputs>();
        var queryProxy = host.Services.GetRequiredService<IIncrementalQuery>();
        var query = host.Services.GetRequiredService<MyIncrementalQuery>();

        host.Services.GetRequiredService<IQuerySystem>().DisableMemoization = true;

        inputs.SomeConstant = 7;
        inputs.SetSomeValue("abc", "xyz");

        // First invocation should cause a recomputation
        var _1 = queryProxy.CalculatedValue;
        var _2 = queryProxy.CalculateFoo("abc", 4);
        Assert.Equal(1, query.CalculatedValue_invocations);
        Assert.Equal(1, query.CalculateFoo_invocations);

        // Next recomputation should also, since memoization is disabled
        var _3 = queryProxy.CalculatedValue;
        var _4 = queryProxy.CalculateFoo("abc", 4);
        Assert.Equal(2, query.CalculatedValue_invocations);
        Assert.Equal(2, query.CalculateFoo_invocations);
    }

    private static IHost GetHost() => Host
        .CreateDefaultBuilder()
        .ConfigureQuerySystem(system => system
            .AddInputQueryGroup<IIncrementalInputs>()
            .AddQueryGroup<IIncrementalQuery, MyIncrementalQuery>())
        .Build();
}
