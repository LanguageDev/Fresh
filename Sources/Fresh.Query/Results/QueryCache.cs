// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.Query.Results;

// NOTE: Public so SGs can use it from user code
public sealed class QueryCache<TKey, TStored>
    where TKey : notnull
    where TStored : IQueryResult
{
    private readonly Dictionary<TKey, TStored> values = new();

    public void Clear(Revision revision)
    {
        var keysToRemove = this.values
            .Where(kv => kv.Value.VerifiedAt <= revision)
            .Select(kv => kv.Key)
            .ToList();
        foreach (var key in keysToRemove) this.values.Remove(key);
    }

    public TStored Get(TKey key, Func<TStored> makeStored)
    {
        if (!this.values.TryGetValue(key, out var value))
        {
            value = makeStored();
            this.values.Add(key, value);
        }
        return value;
    }
}
