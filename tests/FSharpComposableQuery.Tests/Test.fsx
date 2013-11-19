#if INTERACTIVE
#r "FSharp.Data.TypeProviders.dll"
#r "System.Data.dll"
#r "System.Data.Linq.dll"
#r "FSharp.PowerPack.Linq.dll"
#nowarn "62"
#load "Common.fs"
#load "SQL.fs"
#load "Expr.fs"
#load "Query.fs"
#endif

open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq
open Microsoft.FSharp.Linq.QuotationEvaluation
open System.Linq

open FSharpComposableQuery.Expr
open FSharpComposableQuery.Common
open FSharpComposableQuery.QueryRunExtensions.TopLevelValues

// Testing stuff
let test msg f = try f ()
                     printfn "%s: \tSuccess" msg 
                 with exn -> printfn "%s: \tFailure %A" msg exn


let testNaive msg (q:Expr<'T>) p = test msg (fun x -> q.Eval() |> p)
let testFS2 msg (q:Expr<'T>) p = test msg (fun x -> q |> Query.query |> p)
let testFS3 msg (q:Expr<'T>) p = test msg (fun x -> query { for x in (%q) do yield x } |> p)
//let testPLinq msg (q:Expr<seq<'T>>) p = test msg (fun x -> q |> runQuery |> p)
let testPLinqQ msg (q:Expr<'T>) p = test msg (fun x -> pquery { for x in (%q) do yield x } |> p)

let testAll (q:Expr<seq<'T>>) (q':Expr<IQueryable<'T>>) (p:seq<'T> -> unit) = 
  //testNaive "Naive" q p
  testFS2 "FSharp 2.0" q p
  testFS3 "FSharp 3.0" q' p
  //testPLinq "FSharpComposableQuery" q p
  testPLinqQ "PLinqQ" q' p

let testTime msg f = try let _,time = f ()
                         printfn "%s: \tSuccess\t %f ms" msg time
                     with exn -> printfn "%s: \tFailure" msg 

let testTime' f = try let _,time = f() 
                      time
                  with exn -> -1.0
    


let timeNaive' (q:Expr<'T>) p = withDuration 21 (fun () -> q.Eval() |> p)
let timeFS2' (q:Expr<'T>) p = withDuration 21 (fun () -> q |> Query.query |> p)
let timeFS3' (q:Expr<'T>) p = withDuration 21 (fun () -> query { for x in (%q) do yield x }|> p)
//let timePLinq' (q:Expr<'T>) p = withDuration 21 (fun () -> q |> runQuery |> p)
let timePLinqQ' (q:Expr<'T>) p =  withDuration 21 (fun () -> pquery { for x in (%q) do yield x }|> p)
let timeNorm' (q:Expr<'T>) = withDuration 21 (fun () -> nf_expr q)

let timeNaive msg (q:Expr<'T>) p = testTime msg (fun () -> timeNaive' q p)
let timeFS2 msg (q:Expr<'T>) p = testTime msg (fun x -> timeFS2' q p)
let timeFS3 msg (q:Expr<'T>) p = testTime msg (fun x -> timeFS3' q p)
//let timePLinq msg (q:Expr<seq<'T>>) p = testTime msg (fun x -> timePLinq' q p)
let timePLinqQ msg (q:Expr<'T>) p = testTime msg (fun x -> timePLinqQ' q p)
let timeNorm msg (q:Expr<'T>) = testTime msg (fun x -> timeNorm' q)

let timeAll (q:Expr<seq<'T>>) (q':Expr<IQueryable<'T>>) (p:seq<'T> -> unit) = 
  //timeNaive "Naive" q p
  timeFS2 "FSharp 2.0" q p
  timeFS3 "FSharp 3.0" q' p
  //timePLinq "FSharpComposableQuery" q p
  timePLinqQ "PLinqQ" q' p
  timeNorm "Norm" q


let timeAll' (q:Expr<seq<'T>>) (q':Expr<IQueryable<'T>>) (p:seq<'T> -> unit) = 
  //timeNaive "Naive" q p
  let tfs2 = testTime' (fun () -> timeFS2'  q p)
  let tfs3 = testTime' (fun () -> timeFS3' q' p)
  //let tPLinq = testTime' (fun () -> timePLinq'  q p)
  let tPLinqQ = testTime' (fun () -> timePLinqQ'  q' p)
  let tnorm = testTime' (fun () -> timeNorm'  q)
  (tfs2,tfs3,(* tPLinq, *) tPLinqQ,tnorm)
