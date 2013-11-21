(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

(**


Introducing your project 
========================

(__work in progress__)

Compositional Query Framework for F# Queries, based on "A Practical Theory of Language-Integrated Query"
 
Referencing the library

*)
#r "FSharpComposableQuery.dll"
#r "FSharp.Data.TypeProviders.dll"
#r "System.Data.dll"
#r "System.Data.Linq.dll"

open FSharpComposableQuery

(**
All existing F# database and in-memory queries work as normal. For example:
*)

let data = [1; 5; 7; 11; 18; 21]
let lastNumberInSortedList =
    query {
        for s in data do
        sortBy s
        last
    }

(**
In addition, more queries and query compositions work. See the paper "A Practical Theory of Language-Integrated Query"

More documentation will be added.
*)
