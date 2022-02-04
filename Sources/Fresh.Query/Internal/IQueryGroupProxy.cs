// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.Query.Internal;

// NOTE: The interface is public so the SG can implement it on user-code
public interface IQueryGroupProxy
{
    public void Clear(Revision revision);
}
