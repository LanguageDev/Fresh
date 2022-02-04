// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fresh.Query.Results;

namespace Fresh.Query.Internal;

// NOTE: The interface is public so the SG can use it in user-code
public interface IQuerySystemProxyView : IQuerySystem
{
    public void RegisterProxy(IQueryGroupProxy proxy);

    public Revision IncrementRevision();

    public void DetectCycle(IQueryResult value);
    public void RegisterDependency(IQueryResult value);
    public void PushDependency(IQueryResult value);
    public void PopDependency();
}
