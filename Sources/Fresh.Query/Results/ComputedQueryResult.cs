// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fresh.Query.Internal;

namespace Fresh.Query.Results;

// NOTE: Public so SGs can interact with it
public sealed class ComputedQueryResult<T> : IQueryResult<T>
{
    public Revision ChangedAt { get; private set; } = Revision.Invalid;
    public Revision VerifiedAt { get; private set; } = Revision.Invalid;

    public IList<IQueryResult> Dependencies { get; } = new List<IQueryResult>();

    private T? cachedValue;

    public Task Refresh(IQuerySystemProxyView system, CancellationToken cancellationToken) =>
        this.GetValueAsync(system, cancellationToken);

    public async Task<T> GetValueAsync(IQuerySystemProxyView system, CancellationToken cancellationToken)
    {
        if (system.DisableMemoization)
        {
            // We disabled memoization, recompute
            return await this.Recompute(system, cancellationToken);
        }
        else if (this.ChangedAt != Revision.Invalid)
        {
            // Value is already memoized, but potentially outdated
            // If we have been verified to be valid already in the current version, we can just clone and return
            if (this.VerifiedAt == system.CurrentRevision) return this.GetValueCloned();

            // Check for potential cancellation before calling out to dependencies
            cancellationToken.ThrowIfCancellationRequested();

            // Get the value of all dependencies, either forcing recomputation or verification
            var tasks = this.Dependencies.Select(dep => dep.Refresh(system, cancellationToken)).ToArray();
            // We need to wait for all tasks to finish
            Task.WaitAll(tasks, cancellationToken);

            // Now check wether dependencies have been updated since this one
            if (this.Dependencies.All(dep => dep.ChangedAt <= this.VerifiedAt))
            {
                // All dependencies came from earlier revisions, they are safe to reuse
                // Which means this value is also safe to reuse, update verification number
                this.VerifiedAt = system.CurrentRevision;
                return this.GetValueCloned();
            }

            // Some values must have gone outdated and got recomputed, we also need to recompute
            // That's potentially heavy, check cancellation token before that
            cancellationToken.ThrowIfCancellationRequested();

            // Actually recompute
            var newValue = await this.Recompute(system, cancellationToken);

            // To allow early-terminating some computations, we check if the new value is equivalent to the old one
            // This can happen for example when we insert whitespaces at the end of the line
            // The source text will change, but the lexed tokens will stay the same
            if (newValue!.Equals(this.cachedValue))
            {
                // They are equivalent, which means we are verified again
                this.VerifiedAt = system.CurrentRevision;
                // Note that since we already have a perfect clone, we can just return that
                return newValue;
            }

            // The new value is different
            this.cachedValue = newValue;
        }
        else
        {
            // The value has never been memoized yet
            // A recomputation can be expensive, check cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // This means that we need to maintain the dependency graph for this entry
            // First we need to detect cycles in the dependency graph that we are building
            system.DetectCycle(this);
            // If there are no cycles, we can register this entry as a dependency for the current computation
            system.PushDependency(this);

            // The dependency has to be popped no matter what, so we need to catch exceptions
            try
            {
                // Now we do the recomputation
                var newValue = await this.Recompute(system, cancellationToken);
                // Cache the result
                this.cachedValue = newValue;
            }
            finally
            {
                // We are done with the computation, pop off
                system.PopDependency();
            }
        }
        // A recomputation happened (either because the old value was invalid, or this was the first computation)
        // Cloning can be expensive, check cancellation
        cancellationToken.ThrowIfCancellationRequested();
        this.VerifiedAt = system.CurrentRevision;
        this.ChangedAt = system.CurrentRevision;
        return this.GetValueCloned();
    }

    private async Task<T> Recompute(IQuerySystemProxyView system, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    private T GetValueCloned()
    {
        if (this.cachedValue is ICloneable cloneable) return (T)cloneable.Clone();
        else return this.cachedValue!;
    }
}
