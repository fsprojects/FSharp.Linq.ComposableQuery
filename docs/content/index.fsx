(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"
[<Literal>]
let ConnectionString = 
  "Data Source=(localdb)\MyInstance;\
   Initial Catalog=MyPeople;
   Integrated Security=SSPI";;

(**
FSharpComposableQuery
===================


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
#r "FSharp.Data.TypeProviders.dll"
#r "System.Data.dll"
#r "System.Data.Linq.dll"
#r "FSharp.PowerPack.Linq.dll"
#r "FSharpComposableQuery.dll"

open System
open System.Data.Linq.SqlClient
open System.Linq

open Microsoft.FSharp.Data.TypeProviders
open Microsoft.FSharp.Linq

open FSharpComposableQuery



let data = [1; 5; 7; 11; 18; 21]
let lastNumberInSortedList =
    query {
        for s in data do
        sortBy s
        last
    }

(**
In addition, more queries and query compositions work. See the paper 
["A Practical Theory of Language-Integrated Query" (ICFP 2013)](http://dl.acm.org/citation.cfm?id=2500586)

For example, lambda-abstractions can be used to build up queries.  (We assume ConnectionString is a database connection string 
pointing to a database with appropriate tables matching the query). 
*)

type dbSchema = SqlDataConnection<ConnectionString>;;
let db = dbSchema.GetDataContext();;
type People = dbSchema.ServiceTypes.People
type PeopleR = {name:string;age:int}

(**
satisfies is a function that takes a predicate on ages and returns all people satisfying the predicate 
*)

let satisfies  = 
 <@ fun p -> query { 
    for u in db.People do
    if p u.Age 
    then yield {name=u.Name;age=u.Age}
   } @>

(** For example, find all people with age at least 20 and less than 30 *)

let ex1 = query { for x in (%satisfies) (fun x -> 20 <= x && x < 30 ) do yield x }


(** Another example: find all people with even age *)

let ex2 = query {for x in (%satisfies) (fun x ->  x % 2 = 0 ) do yield x }

(**

WARNING: F# compiler optimizations are no longer applied to in-memory queries. 
This library should only be used for database query programming, or you 
can explicitly bind 

let dbQuery =  FSharpComposableQuery.TopLevelValues.query

to bind dbQuery for use with database queries, without changing the behavior of query {} on in-memory queries.

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
