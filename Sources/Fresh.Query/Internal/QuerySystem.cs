// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fresh.Query.Hosting;

namespace Fresh.Query.Internal;

internal sealed class QuerySystem : IQuerySystem, IQuerySystemProxyView
{
    public bool AllowMemoization { get; set; }

    public Revision CurrentRevision { get; private set; } = new(0);

    private readonly List<IQueryGroupProxy> proxies = new();

    public void Clear(Revision revision)
    {
        foreach (var proxy in this.proxies) proxy.Clear(revision);
    }

    public void RegisterProxy(IQueryGroupProxy proxy) => this.proxies.Add(proxy);

    public Revision IncrementRevision()
    {
        this.CurrentRevision = new(this.CurrentRevision.Number);
        return this.CurrentRevision;
    }
}
