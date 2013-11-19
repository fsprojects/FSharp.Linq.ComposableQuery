(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

(**
FSharpComposableQuery
===================

Documentation

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      The FSharpComposableQuery library can be <a href="https://nuget.org/packages/FSharpComposableQuery">installed from NuGet</a>:
      <pre>PM> Install-Package FSharpComposableQuery</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

<img src="img/logo.png" alt="F# Project" style="float:right;width:150px;margin:10px" />

Overview
-------

When you open 'FSharpComposableQuery', a new 'query' builder is available. The old F# query builder is out-scoped.

All existing F# database and in-memory queries work as normal. 

Example
-------

This example demonstrates a query.

*)
#r "FSharpComposableQuery.dll"
open FSharpComposableQuery


let data = [1; 5; 7; 11; 18; 21]
let lastNumberInSortedList =
    query {
        for s in data do
        sortBy s
        last
    }

(**
In addition, more queries and query compositions work. See the paper "A Practical Theory of Language-Integrated Query"
*)


(**
Some more info

Samples & documentation
-----------------------

The API reference is automatically generated from Markdown comments in the library implementation.

 * [Tutorial](tutorial.html) contains a further explanation of this sample library.

 * [Query Examples](QueryExamples.html) contains a more comprehensive set of queries from the MSDN documentation.

 * [API Reference](reference/index.html) contains automatically generated documentation for all types, modules
   and functions in the library. This includes additional brief samples on using most of the
   functions.
 
Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding new public API, please also 
consider adding [samples][content] that can be turned into a documentation. You might
also want to read [library design notes][readme] to understand how it works.

The library is available under MIT license, which allows modification and 
redistribution for both commercial and non-commercial purposes. For more information see the 
[License file][license] in the GitHub repository. 

  [content]: https://github.com/fsharp/FSharpComposableQuery/tree/master/docs/content
  [gh]: https://github.com/fsharp/FSharpComposableQuery
  [issues]: https://github.com/fsharp/FSharpComposableQuery/issues
  [readme]: https://github.com/fsharp/FSharpComposableQuery/blob/master/README.md
  [license]: https://github.com/fsharp/FSharpComposableQuery/blob/master/LICENSE.txt
*)
