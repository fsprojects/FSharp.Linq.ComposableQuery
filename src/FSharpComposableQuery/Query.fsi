namespace FSharpComposableQuery

open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq

//TODO: Update placeholder descriptions
module QueryImpl = 

    /// <summary>
    /// The type used to support advanced F# query syntax. Use 'query { ... }' to use the query syntax. 
    /// </summary>
    [<Class>]
    type QueryBuilder = 
        inherit Microsoft.FSharp.Linq.QueryBuilder

        /// <summary>
        /// A method used to support the F# query syntax. Runs the given quotation as a query using LINQ IQueryable rules.
        /// </summary>
        member Run : q:Expr<Linq.QuerySource<'T, System.Linq.IQueryable>> -> System.Linq.IQueryable<'T>

[<AutoOpen>]
module LowPriority = 
    type QueryImpl.QueryBuilder with
        /// <summary>
        /// A method used to support the F# query syntax. Runs the given quotation as a query using LINQ rules.
        /// </summary>
        [<CompiledName("RunQueryAsValue")>]
        member Run : q:Expr<'T> -> 'T

[<AutoOpen>]
module HighPriority = 
    type QueryImpl.QueryBuilder with
        /// <summary>
        /// A method used to support the F# query syntax. Runs the given quotation as a query using LINQ IEnumerable rules.
        /// </summary>
        [<CompiledName("RunQueryAsEnumerable")>]
        member Run : q:Expr<QuerySource<'T, System.Collections.IEnumerable>> -> seq<'T>
        
[<AutoOpen>]
module TopLevelValues =     
    /// <summary>
    /// Builds a query using query syntax and operators.
    /// </summary>
    val query : QueryImpl.QueryBuilder