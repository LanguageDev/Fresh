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

internal sealed class QuerySystem : IQuerySystem
{
    public bool AllowMemoization { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public Revision CurrentRevision => throw new NotImplementedException();

    public void Clear(Revision revision) => throw new NotImplementedException();
}
