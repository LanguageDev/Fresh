// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fresh.Query.Internal;

namespace Fresh.Query.Results;

// NOTE: Public so SGs can interact with it
public interface IQueryResult
{
    public Revision ChangedAt { get; }
    public Revision VerifiedAt { get; }

    public IList<IQueryResult> Dependencies { get; }

    public Task Refresh(IQuerySystemProxyView system, CancellationToken cancellationToken);
}

public interface IQueryResult<T> : IQueryResult
{
    public Task<T> GetValueAsync(IQuerySystemProxyView system, CancellationToken cancellationToken);
}
