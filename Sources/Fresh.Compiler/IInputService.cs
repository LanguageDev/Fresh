using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fresh.Query;
using Fresh.Syntax;

namespace Fresh.Compiler;

/// <summary>
/// The service for setting inputs in the compiler.
/// </summary>
[InputQueryGroup]
public partial interface IInputService
{
    /// <summary>
    /// The source text of a given file.
    /// </summary>
    /// <param name="file">The file identifier.</param>
    /// <returns>The source text of the given file.</returns>
    public SourceText SourceText(string file);
}
