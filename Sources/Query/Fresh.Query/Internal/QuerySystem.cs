// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using Fresh.Query.Results;

namespace Fresh.Query.Internal;

internal sealed class QuerySystem : IQuerySystem, IQuerySystemProxyView
{
    public bool DisableMemoization { get; set; } = false;

    public Revision CurrentRevision { get; private set; } = new(0);

    private readonly List<IQueryGroupProxy> proxies = new();
    // Runtime "call-stack" for computed values
    private readonly Stack<IQueryResult> valueStack = new();

    public void Clear(Revision revision)
    {
        foreach (var proxy in this.proxies) proxy.Clear(revision);
    }

    public void RegisterProxy(IQueryGroupProxy proxy) => this.proxies.Add(proxy);

    public Revision IncrementRevision()
    {
        this.CurrentRevision = new(this.CurrentRevision.Number + 1);
        return this.CurrentRevision;
    }

    public void DetectCycle(IQueryResult value)
    {
        if (this.valueStack.Contains(value)) throw new InvalidOperationException("Cycle detected!");
    }

    public void RegisterDependency(IQueryResult value)
    {
        if (this.valueStack.TryPeek(out var top) && !top.Dependencies.Contains(value)) top.Dependencies.Add(value);
    }

    public void PushDependency(IQueryResult value)
    {
        this.RegisterDependency(value);
        this.valueStack.Push(value);
    }

    public void PopDependency() => this.valueStack.Pop();
}
