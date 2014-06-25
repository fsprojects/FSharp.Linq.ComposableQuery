namespace FSharpComposableQuery.Tests

open Microsoft.FSharp.Linq
open Microsoft.FSharp.Linq.QuotationEvaluation
open Microsoft.FSharp.Quotations
open System.Linq

open FSharpComposableQuery

module QueryBuilders = 
    let TLinq = new QueryImpl.QueryBuilder()
    let FSharp3 = new Microsoft.FSharp.Linq.QueryBuilder()

type Utils() = 

    static let getTime f arg =
        let sw = System.Diagnostics.Stopwatch.StartNew()
        let ans = f arg
        sw.Stop()
        (int(sw.ElapsedMilliseconds), ans)
        
    // Forces the enumeration of a sequence
    static let force (l:seq<'T>) = Seq.iter ignore l

    // Gets the inner expression of an expression of the type "query { <inner-expr> }"
    static let extractBodyRaw(e:Expr<'T>) =
        match e with
        | Patterns.Application (Patterns.Lambda(_, Patterns.Call(Some _, mi, [Patterns.Quote(b)])), _)
        | Patterns.Application (Patterns.Lambda(_, Patterns.Call(None, mi, [_; Patterns.Quote(b)])), _) ->
            let min = mi.Name
            b
        | _ ->
            failwith "Unable to find an outermost query expression"

    // Gets the inner expression and casts it to the appropriate type
    static let extractValue(e:Expr<'T>) : Expr<'T> = 
        Expr.Cast (extractBodyRaw e)
    static let extractQuery(e:Expr<IQueryable<'T>>) : Expr<QuerySource<'T, IQueryable>> = 
        Expr.Cast (extractBodyRaw e)
    static let extractEnum(e:Expr<seq<'T>>) : Expr<QuerySource<'T, System.Collections.IEnumerable>> = 
        Expr.Cast (extractBodyRaw e)

    // Substitutes the default builder in an expression with the given one
    // TODO: see if we can use builder@ by changing the type it refers to
    static let substituteBuilder (qb : #QueryBuilder) (e:Expr<'T>) : Expr<'T> = 
        e.Substitute(fun v ->
            if(v.Name = "builder@") then
                Some (Expr.Value(qb, qb.GetType()))
            else
                None)
        |> Expr.Cast
    
        
    static let runQuery (translate:bool) (e:Expr<QuerySource<'T, IQueryable>>) = 
        //can't pass the QueryBuilder as a parameter
        //or the base class' Run method will be called
        let res = 
            match translate with
            | true -> QueryBuilders.FSharp3.Run e
            | false -> QueryBuilders.TLinq.Run e
        force res
        res

    static let runEnum (translate:bool) (e:Expr<QuerySource<'T, System.Collections.IEnumerable>>) = 
        let res = 
            match translate with
            | true -> QueryBuilders.FSharp3.Run e
            | false -> QueryBuilders.TLinq.Run e
        force res
        res

    static let runVal (translate:bool) (e:Expr<'T>) = 
        match translate with
        | true -> QueryBuilders.FSharp3.Run e
        | false -> QueryBuilders.TLinq.Run e
            
    

    static let run f translate = 
        let qb = if translate then QueryBuilders.FSharp3 else QueryBuilders.TLinq :> QueryBuilder
        substituteBuilder qb >> f translate

    static let comp f e = (run f true e, run f false e)
        
    static let time f n e = 
        let expA, expB = substituteBuilder QueryBuilders.FSharp3 e, substituteBuilder (QueryBuilders.TLinq :> QueryBuilder) e
        let qbA, qbB = true, false

        let results = 
            [|1..n|]
            |> Array.map (fun _ ->
                let ta,ra = getTime (f qbA) expA
                let tb,rb = getTime (f qbB) expB
                [|ta;tb|])

        let mean = 
            results
            |> Array.reduce (Array.map2 (+))
            |> Array.map (fun t -> t / n)

        let sigma = 
            results
            |> Array.map (Array.map2 (-) mean >> Array.map (fun x -> x * x)) //subtract mean then square
            |> Array.reduce (Array.map2 (+))    //sum
            |> Array.map ((fun t -> t / n) >> float >> System.Math.Sqrt)    //divide by n, take sqrt

        (mean.[0], mean.[1])


    static member Run (e:Expr<IQueryable<'T>>) : IQueryable<'T> = run runQuery false (extractQuery e)
    static member RunAsValue (e:Expr<'T>) : 'T = run runVal false (extractValue e)
    static member RunAsEnumerable (e:Expr<seq<'T>>) : seq<'T> = run runEnum false (extractEnum e)
   
    static member Comp (e:Expr<IQueryable<'T>>) = 
        let a,b = comp runQuery (extractQuery e)
        a.Count() = b.Count() && not (a.Except(b).Any())

    static member CompAsValue (e:Expr<'T>) = 
        let a,b = comp runVal (extractValue e)
        (a :> obj).Equals(b)

    static member CompAsEnumerable (e:Expr<seq<'T>>) = 
        let a,b = comp runEnum (extractEnum e)
        a.SequenceEqual(b)

    static member public Time (n:int, e:Expr<IQueryable<'T>>) = time runQuery n (extractQuery e)
    static member public TimeAsValue (n:int, e:Expr<'T>) = time runVal n (extractValue e)
    static member public TimeAsEnumerable (n:int, e:Expr<seq<'T>>) = time runEnum n (extractEnum e)


// Add the overloads as extension methods as otherwise we have to specify which one to call
[<AutoOpen>]
module UtilsHi = 
    type Utils with

    [<CompiledName("RunAsValue")>]
    static member public Run (e:Expr<'T>) : 'T = Utils.RunAsValue e

    [<CompiledName("CompAsValue")>]
    static member public Comp (e:Expr<'T>) = Utils.CompAsValue e
        
    [<CompiledName("TimeAsValue")>]
    static member public Time (n:int, e:Expr<'T>) = Utils.TimeAsValue(n, e)

[<AutoOpen>]
module UtilsLo = 
    type Utils with

    [<CompiledName("RunAsEnumerable")>]
    static member public Run (e:Expr<seq<'T>>) : seq<'T> = Utils.RunAsEnumerable e

    [<CompiledName("CompAsEnumerable")>]
    static member public Comp (e:Expr<seq<'T>>) = Utils.CompAsEnumerable e
        
    [<CompiledName("TimeAsEnumerable")>]
    static member public Time (n:int, e:Expr<seq<'T>>) = Utils.TimeAsEnumerable(n, e)
