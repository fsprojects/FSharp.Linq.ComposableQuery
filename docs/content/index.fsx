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
=====================
(__work in progress__)


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
--------

The **FSharpComposableQuery** library exposes a new query builder in place of the old F# one. 

All existing F# database and in-memory queries should work as normal.
In addition, queries can be _composed_ using lambda-abstraction and are explicitly transformed to a _normal form_ 
before execution. 

See the paper ["A Practical Theory of Language-Integrated Query" (ICFP 2013)](http://dl.acm.org/citation.cfm?id=2500586)
for further information.  

Simple forms of _query composition_, such as parameterizing a value of base type, already work in LINQ, 
but 'FSharpComposableQuery' additionally supports passing functions as parameters to allow for higher-order query 
operations.

The library also performs _normalisation_ on the input query, which allows us to provide strong guarantees 
about the runtime of composed queries. It thus allows the user to use higher-order abstraction and dynamically 
construct queries without having to worry about the efficiency of the generated SQL code. 

Installation
---------------------

You can install the library from [NuGet](https://nuget.org/packages/FSharpComposableQuery). 
Alternatively you can find the source code on [GitHub](http://github.com/fsharp/FSharpComposableQuery) and build it 
yourself. 

To then include the library in your project or script simply open the `FSharpComposableQuery` namespace:

*)

#if INTERACTIVE     // reference required libraries if run as a script
#r "FSharpComposableQuery.dll"
#endif

open FSharpComposableQuery

(**

Example
------------

The following example demonstrates query composition using a parameterized predicate.  

We assume the following simple database schema:
  
    type dbSchema = { people : { name : string; age : int } list; }

We will use the LINQ-to-SQL `TypeProvider` to connect to the database and get the record types:
*)

#if INTERACTIVE
#r "FSharp.Data.TypeProviders.dll"
#r "System.Data.Linq.dll"
#endif

open System
open Microsoft.FSharp.Data.TypeProviders

type dbSchema = SqlDataConnection<ConnectionStringName="PeopleConnectionString", ConfigFile="db.config">

type internal Person = dbSchema.ServiceTypes.People

let db = dbSchema.GetDataContext()

(**
We can then construct a function `satisfies` that takes a predicate on ages and returns all people that satisfy it
*)

let satisfies  = 
 <@ fun p -> query { 
    for u in db.People do
        if p u.Age then 
            yield p
   } @>

(** We can now use this function, for example, to find all people with age at least 20 and less than 30: *)

let ex1 = query { yield! (%satisfies) (fun x -> 20 <= x && x < 30 ) }


(** Find all people with an even age: *)

let ex2 = query { yield! (%satisfies) (fun x ->  x % 2 = 0 ) }

(**

An overview of the main use cases of the library can be found [here](./tutorial.html)


Caveats
-------

 * WARNING: F# compiler optimizations are no longer applied to in-memory queries. 
   This library should only be used for database query programming.  If both in-memory querying and 
   composable database queries are needed, you can explicitly bind the **FSharpComposableQuery** builder
   to a variable instead of opening the whole namespace to avoid shadowing the built-in `query` keyword: *)

let dbQuery =  FSharpComposableQuery.TopLevelValues.query

(**
 * Please check the [issues page][issues] on GitHub for further information. 


Samples & documentation
-----------------------

The API reference is automatically generated from Markdown comments in the library implementation.

 * [The tutorial](tutorial.html) contains a further overview of this library's main use cases.

 * [Query Examples](QueryExamples.html) contains a comprehensive set of default queries from the MSDN documentation.

 * [API Reference](reference/index.html) contains automatically generated documentation for all types, modules
   and functions in the library. This includes additional brief samples on using most of the functions.
 
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
