// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fresh.Query.Internal;

namespace Fresh.Query.Results;

// NOTE: Public so SGs can interact with it
public sealed class InputQueryResult<T> : IQueryResult<T>
{
    public Revision ChangedAt { get; private set; } = Revision.Invalid;

    // Inputs are set from the outside, they always count as valid
    public Revision VerifiedAt => Revision.MaxValue;

    public IList<IQueryResult> Dependencies => Array.Empty<IQueryResult>();

    private T? value;

    public Task Refresh(IQuerySystemProxyView system, CancellationToken cancellationToken) =>
        this.GetValueAsync(system, cancellationToken);

    public Task<T> GetValueAsync(IQuerySystemProxyView system, CancellationToken cancellationToken) =>
        Task.FromResult(this.GetValue(system));

    public T GetValue(IQuerySystemProxyView system)
    {
        system.RegisterDependency(this);
        if (this.ChangedAt == Revision.Invalid) throw new InvalidOperationException($"Tried to retrieve input value before it was ever set");
        return this.value!;
    }

    public void SetValue(IQuerySystemProxyView system, T value)
    {
        this.value = value;
        this.ChangedAt = system.IncrementRevision();
    }
}
