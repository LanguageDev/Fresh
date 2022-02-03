using Fresh.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.Query.Example;

[InputQueryGroup]
internal interface INumberInputs
{
    public int Variable(string name);
}

[QueryGroup]
internal interface IComputation
{
    public int CustomConstant { get; }
    public int CustomComputation(string varName, int k);
}

internal class Program
{
    internal static void Main(string[] args)
    {

    }
}
