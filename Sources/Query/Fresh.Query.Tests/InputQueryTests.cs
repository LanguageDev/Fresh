// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using Fresh.Query.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Fresh.Query.Tests;

public sealed partial class InputQueryTests
{
    [InputQueryGroup]
    public partial interface IKeylessInputs
    {
        public string PropInput { get; set; }

        public string MethodInput();
    }

    [InputQueryGroup]
    public partial interface IKeyedInputs
    {
        public string OneKeyedInput(string k1);
        public string TwoKeyedInput(string k1, int k2);
        public int ThreeKeyedInput(string k1, int k2, string k3);
    }

    [Fact]
    public void KeylessInputQueryTests()
    {
        var host = GetHost();

        var input = host.Services.GetRequiredService<IKeylessInputs>();

        // Try to access both without initialization, should throw
        Assert.Throws<InvalidOperationException>(() => input.PropInput);
        Assert.Throws<InvalidOperationException>(() => input.MethodInput());

        // Initialize
        input.PropInput = "Some prop value";
        input.SetMethodInput("Some method value");

        // Compare
        Assert.Equal("Some prop value", input.PropInput);
        Assert.Equal("Some method value", input.MethodInput());

        // Change
        input.PropInput = "A";
        input.SetMethodInput("B");

        // Compare
        Assert.Equal("A", input.PropInput);
        Assert.Equal("B", input.MethodInput());
    }

    [Fact]
    public void KeyedInputQueryTests()
    {
        var host = GetHost();

        var input = host.Services.GetRequiredService<IKeyedInputs>();

        // Try to access all without initialization
        Assert.Throws<InvalidOperationException>(() => input.OneKeyedInput("a"));
        Assert.Throws<InvalidOperationException>(() => input.OneKeyedInput("b"));
        Assert.Throws<InvalidOperationException>(() => input.TwoKeyedInput("a", 1));
        Assert.Throws<InvalidOperationException>(() => input.TwoKeyedInput("b", 1));
        Assert.Throws<InvalidOperationException>(() => input.ThreeKeyedInput("a", 1, "x"));
        Assert.Throws<InvalidOperationException>(() => input.ThreeKeyedInput("b", 1, "y"));

        // Set some of them
        input.SetOneKeyedInput("a", "hello");
        input.SetTwoKeyedInput("a", 1, "there");
        input.SetThreeKeyedInput("a", 1, "x", 42);

        // Half of them should yield the set results, other half should still throw
        Assert.Equal("hello", input.OneKeyedInput("a"));
        Assert.Throws<InvalidOperationException>(() => input.OneKeyedInput("b"));
        Assert.Equal("there", input.TwoKeyedInput("a", 1));
        Assert.Throws<InvalidOperationException>(() => input.TwoKeyedInput("b", 1));
        Assert.Equal(42, input.ThreeKeyedInput("a", 1, "x"));
        Assert.Throws<InvalidOperationException>(() => input.ThreeKeyedInput("b", 1, "y"));

        // Set the other half, update the first half
        input.SetOneKeyedInput("b", "abc");
        input.SetTwoKeyedInput("b", 1, "xyz");
        input.SetThreeKeyedInput("a", 1, "x", 21);
        input.SetOneKeyedInput("a", "bye");
        input.SetTwoKeyedInput("a", 1, "here");
        input.SetThreeKeyedInput("b", 1, "y", 123);

        // Now all of them should work
        Assert.Equal("bye", input.OneKeyedInput("a"));
        Assert.Equal("abc", input.OneKeyedInput("b"));
        Assert.Equal("here", input.TwoKeyedInput("a", 1));
        Assert.Equal("xyz", input.TwoKeyedInput("b", 1));
        Assert.Equal(21, input.ThreeKeyedInput("a", 1, "x"));
        Assert.Equal(123, input.ThreeKeyedInput("b", 1, "y"));
    }

    private static IHost GetHost() => Host
        .CreateDefaultBuilder()
        .ConfigureQuerySystem(system => system
            .AddInputQueryGroup<IKeylessInputs>()
            .AddInputQueryGroup<IKeyedInputs>())
        .Build();
}
