// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.Syntax;

/// <summary>
/// Represents some location in a source text.
/// </summary>
/// <param name="Source">The source text the location points into.</param>
/// <param name="Range">The pointed range in the source.</param>
public record class Location(SourceText Source, Range Range);
