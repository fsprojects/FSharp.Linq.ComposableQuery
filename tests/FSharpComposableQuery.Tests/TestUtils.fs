namespace FSharpComposableQuery.Tests

open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq
open Microsoft.FSharp.Linq.QuotationEvaluation
open System.Linq

open FSharpComposableQuery


module Utils = 

    // Gets the middle element of a list
    let internal median (l : float array) = 
        match l.Length with
        | 0 -> -1.0
        | n -> l.[n/2]

    let internal getTime f arg =
        let sw = System.Diagnostics.Stopwatch.StartNew()
        let ans = f arg
        sw.Stop()
        (sw.ElapsedMilliseconds, ans)
        
    // replaces all calls to the FSharpComposableQuery QueryBuilder with instances of the other builder
    let replaceNested (q:QueryBuilder) (e:Expr<'T>) = 
        failwith "NYI"

    let runQuery (q:QueryBuilder) (e:Expr<'T>) = 
        replaceNested q e
        |> q.Run

    module QueryBuilders = 
        let TLinq = new FSharpComposableQuery.QueryImpl.QueryBuilder()
        let FSharp2 = new FSharpComposableQuery.QueryImpl.QueryBuilder()
        let FSharp3 = new Microsoft.FSharp.Linq.QueryBuilder()
