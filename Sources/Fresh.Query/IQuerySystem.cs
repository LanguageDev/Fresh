// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

namespace Fresh.Query;

/// <summary>
/// Interface of the query system service.
/// </summary>
public interface IQuerySystem
{
    /// <summary>
    /// Allows turning on and off memoization.
    /// </summary>
    public bool DisableMemoization { get; set; }

    /// <summary>
    /// The current revision the system is at.
    /// </summary>
    public Revision CurrentRevision { get; }

    /// <summary>
    /// Clears all memoized values before a given revision.
    /// </summary>
    /// <param name="revision">The revision to erase memoized values up to (inclusive).</param>
    public void Clear(Revision revision);

    /// <summary>
    /// Clears all memoized values.
    /// </summary>
    public void Clear() => this.Clear(Revision.MaxValue);
}
