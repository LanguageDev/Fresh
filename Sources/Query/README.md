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

TODO
