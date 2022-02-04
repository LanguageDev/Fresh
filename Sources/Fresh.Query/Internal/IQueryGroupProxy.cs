// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

namespace Fresh.Query.Internal;

// NOTE: The interface is public so the SG can implement it on user-code
public interface IQueryGroupProxy
{
    public void Clear(Revision revision);
}
