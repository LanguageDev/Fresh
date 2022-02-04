# Querying module

This is a short documentation about the motivation, usage and inner workings of the querying module.

## Traditional vs Modern compilers

The traditional compiler architecture can be throught of as a strict pipeline, where computation has a well-defined order. A really simplistic visualization of such pipeline could be something like:
```
   +---------+
   | Sources |
   +---------+
        |
    Lex & Parse
        |
        V
     +-----+
     | AST |
     +-----+
        |
Semantic Analysis
        |
        V
 +--------------+
 | Symbol Table |
 +--------------+
        |
 Code Generation
        |
        V
  +------------+
  | Executable |
  +------------+
```
Every phase could more-or-less finish, before starting the next one. This made the mental model of compilers very simple. This architecture was acceptable, because the compiler was just a CLI tool, that had one job: given a source, spit out the compiled output.

The modern era of software development has changed the requirements towards compilers drastically. Most of the time we work in an editor or IDE and we expect it to aid us during coding. A few things we can expect from such a tool:
 * Show where errors are (maybe even provide a fix)
 * Jump to function definition from the call-site
 * Rename a symbol
 * Show documentation comment on hover
 * And a bunch more

While this all could be implemented by an external tool, it's a much more sensible approach to give it to the compiler, which already contains most of the functionality. The problem is that executing the full pipeline each time the user types something can be extremely expensive. And so, a new architecture is needed to save on computation!

## Query-Based compiler architecture

A relatively modern development in compiler architecture is called query-based compiler architecture (read more about them [here](https://ollef.github.io/blog/posts/query-based-compilers.html)). The basic idea is to split up the steps of compilation into smaller queries. Some example queries might be:
 * Give me the AST of file `main.xyz`!
 * Tell me the type of symbol `Main.foo`!
 * Generate source code for this `if` statement!

Then we can [memoize](https://en.wikipedia.org/wiki/Memoization) the results of these queries, and only recompute them, when they get invalidated. This means, that most query results will probably still be valid, when the user only types a few characters. This is very similar to how Make and other build systems work!

## Using the query system

The system implemented here is heavily inspired by [Salsa](https://github.com/salsa-rs/salsa). If a project needs to include it, make sure to include both the core and the generator project like so:
```xml
  <ItemGroup>
    <ProjectReference Include="..\Fresh.Query\Fresh.Query.csproj" />
    <ProjectReference Include="..\Fresh.Query.Generator\Fresh.Query.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="False" />
  </ItemGroup>
```

The library allows definig queries in groups, which just helps organizing the related queries. You can define query groups and input query groups with the `[QueryGroup]` and `[InputQueryGroup]` respectively. Input query groups represent the raw inputs to the system, that do not get computed from anything else. Regular query groups are computed from values given by input queries or computed by other queries. These queries will automatically memoize their results, and only get recomputed, when any of their dependencies go outdated. Let's look at a simplified example for a compiler:
```cs
using Fresh.Query;

// This imput query group simply represents how a compiler could look at a project with a manifest and source files
// Note the partial modifier, which is required by all query group interfaces
// This is because the framework generates the memoizing proxy directly inside the interface
[InputQueryGroup]
public partial interface IProjectInputs
{
    // Queries without parameters can be properties
    // Since this is an input query, it needs to have a setter
    public Manifest Manifest { get; set; }

    // Queries can have any number of parameters
    // Since this is an input query, any method queries will have a setter automatically generated
    // In this case it would look like so:
    // public void SetSourceText(string fileName, string value);
    public string SourceText(string fileName);
}

// This query group contains operations related to syntax analysis
[QueryGroup]
public partial interface ISyntaxService
{
    // Non-input queries can also be properties, but these only have a getter
    public int FilesInProject { get; }

    public TokenSequence LexFile(string fileName);

    public Ast ParseFile(string fileName);
}

// Now we can implement a query group simply by implementing the interface on a class
// The implementation does not have to deal with any memoization or versioning, that is all done by the framework
// Note, that input query groups require no implementations from the user at all, those can be generated completely
public class SyntaxService : ISyntaxService
{
    // The other query groups are given to us with DI
    private readonly IProjectInputs inputs;
    // IMPORTANT: We could call other syntax queries on 'this', and it would be technically correct
    // BUT then that query will not be memoized, since it does not go through the memoizing proxy
    private readonly ISyntaxService syntaxService;

    // Other query groups are given to us in the constructor
    public SyntaxService(IProjectInputs inputs, ISyntaxService syntaxService)
    {
        this.inputs = inputs;
        this.syntaxService = syntaxService;
    }

    public int FilesInProject => this.inputs.Manifest.Files.Count();

    public TokenSequence LexFile(string fileName)
    {
        var text = this.input.SourceText(fileName);
        // lexing code...
    }

    public Ast ParseFile(string fileName)
    {
        // Again, we could just call this.LexFile, but then it won't get memoized!
        var tokens = this.syntaxService.LexFile(fileName);
        // parsing code...
    }
}
```

Using the framework is done through the generic host API, registering all query groups in the DI. Registering and using the above example like so:
```cs
using Fresh.Query;
using Fresh.Query.Hosting;

internal class Program
{
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        // Ask for the input service
        var projectInputs = host.Services.GetRequiredService<IProjectInputs>();
        // Use the setters to provide some input
        projectInputs.Manifest = /* TODO */;
        projectInputs.SetSourceText("main.xyz", /* TODO: read file */);
        // Now get the syntax service
        var syntaxService = host.Services.GetRequiredService<ISyntaxService>();
        // Use it to compute something
        var ast = syntaxService.ParseFile("main.xyz");

        // Let's assume that "main.xyz" changed a bit, but does not affect the token sequence (like a whitespace typed at the end of a line)
        // Then we update the value of main.xyz
        projectInputs.SetSourceText("main.xyz", /* TODO: read file */);
        // We re-compute the ast for the file
        // Since the text changed, the file will be re-lexed, but then the framework will notice, that there were no changes in the token sequence
        // This causes the computation to terminate early, and the old - and still valid - AST will be returned instead of re-parsing the file
        ast = syntaxService.ParseFile("main.xyz");
    }

    public static IHostBuilder CreateHostBuilder(string[] args) => Host
        .CreateDefaultBuilder(args)
        // This configures a new query system with the query groups defined below
        .ConfigureQuerySystem(system => system
            // Input query groups just require the interface
            .AddInputQueryGroup<IProjectInputs>()
            // Non-input query groups require the interface and the implementation by the user
            // The actual implementation injected by asking for this interface will be the memoizing proxy, calling out to the user implementation when needed
            .AddQueryGroup<ISyntaxService, SyntaxService>());
}
```

## Asynchronous computation

TODO

## Things to watch out for

TODO:
 * don't call another query with this., use the injected proxy
 * don't conditionally call another query
 * computations have to be pure (side-effect free)
 * the parameters have to be hashable and equatable, the return type has to be equatable
 * the return type has to be either immutable or clonable
