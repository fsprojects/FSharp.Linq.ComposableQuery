namespace FSharpComposableQuery.Tests

open System
open System.Linq
open Microsoft.FSharp.Linq
open Microsoft.FSharp.Linq.QuotationEvaluation
open Microsoft.FSharp.Quotations

open FSharpComposableQuery

module QueryBuilders = 
    let TLinq = FSharpComposableQuery.TopLevelValues.query
    let FSharp3 = ExtraTopLevelOperators.query


type UtilsMode = 
    | RunThrow = 0
    | CompPrint = 1
    | TimePrint = 2

type Utils() = 

    //The number of test runs to do when timing a query. 
    [<Literal>]
    static let nRuns = 10
    
    // Times the execution of a function and returns the elapsed time
    // paired with its result
    static let getTime f arg =
        let sw = System.Diagnostics.Stopwatch.StartNew()
        let ans = f arg
        sw.Stop()
        (int(sw.ElapsedMilliseconds), ans)
        

    // Gets the mean of an array of float values
    static let mean l = (Array.reduce (+) l) / float l.Length

    // Gets the standard deviation of an array of float values with a given mean
    static let sigma l m = 
        l
        |> Array.map (fun v -> (v-m) * (v-m))
        |> Array.reduce (+)
        |> (fun v -> System.Math.Sqrt (float v / float l.Length))


    // Forces the enumeration of a sequence
    // Requires a type anotation as otherwise tries casting seq<'T> to queryable<'T> (?)
    static let force (l:seq<'T>) = Seq.iter ignore l

    // Gets the body of an expression of the type "query { <body> }"
    static let extractBodyRaw(e:Expr<'T>) =
        match e with
        | Patterns.Application (Patterns.Lambda(_, Patterns.Call(Some _, mi, [Patterns.Quote(b)])), _)
        | Patterns.Application (Patterns.Lambda(_, Patterns.Call(None,   mi, [_; Patterns.Quote(b)])), _) ->
            b
        | _ ->
            failwith "Unable to find an outermost query expression"

    // Gets the body of an expression and casts it to the appropriate type
    static let extractValue(e:Expr<'T>) : Expr<'T> = 
        Expr.Cast (extractBodyRaw e)
    static let extractQuery(e:Expr<IQueryable<'T>>) : Expr<QuerySource<'T, IQueryable>> = 
        Expr.Cast (extractBodyRaw e)
    static let extractEnum(e:Expr<seq<'T>>) : Expr<QuerySource<'T, System.Collections.IEnumerable>> = 
        Expr.Cast (extractBodyRaw e)
    


    // Substitutes the default builder in an expression with the given one
    static let substituteBuilder (qb : #QueryBuilder) (e:Expr<'T>) : Expr<'T> = 
        e.Substitute(fun v ->
                if(v.Name = "builder@") then
                    Some (Expr.Value(qb, qb.GetType()))
                else
                    None)
        |> QueryTranslator.replaceNativeMethods
        |> Expr.Cast


    // The methods used to execute different types of queries under the two different builders
    // Passing the QueryBuilder as parameter wouldn't work since then the base class' Run method will be called
    
    static let runQuery (translate:bool) (e:Expr<QuerySource<'T, IQueryable>>) = 
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
        
        
    // The methods used to compare the results of the different types of queries. 
    

    // Compare as unordered sets
    static let compQuery (a : IQueryable<'T>) (b : IQueryable<'T>) = 
        let aa = a.ToArray()
        let bb = b.ToArray()

        
        let ans = (aa.Length = bb.Length && not (aa.Except( bb).Any()))
        ans

    // Compare values
    static let compVal (a : 'T) (b : 'T) = 
        let ans = (a = b)
        ans

    // Compare sequences
    static let compEnum (a : seq<'T>) (b : seq<'T>) = 
        let ans = (a.SequenceEqual b)
        ans


    // Runs the given expression using the runMethod, 
    static let run runMethod translate = 
        match translate with
        | true -> QueryBuilders.FSharp3 
        | false -> QueryBuilders.TLinq :> QueryBuilder
        |> substituteBuilder 
        >> runMethod translate

    // Prints the text in green or red, depending on the pass value
    static let printResult c text = 
        let old = Console.ForegroundColor 
        try 
            Console.ForegroundColor <- c
            printf "%-10s" text
        finally
            System.Console.ForegroundColor <- old

    static let printOk() = printResult ConsoleColor.Green "OK"
    static let printFail() = printResult ConsoleColor.Red "FAIL"

    

    // Evaluates the expression, catching any exceptions. 
    // returns ('T option) indicating whether the call was successful
    static let tryEval f arg = 
        try Some (f arg)
        with exn -> None

    // Tries evaluating (f arg) and prints the outcome
    static let eval f arg = 
        let ans = tryEval f arg 

        match ans with 
        | Some v -> printOk()
        | None   -> printFail()

        ans

    // Runs the query using the FSharpComposableQuery builder
    // and throws an exception if unsuccessful. 
    static let runThrow (compMethod : 'b -> 'b -> bool) (runMethod : bool -> Expr<'a> -> 'b) e = 
        run runMethod false e
        |> ignore
        
    // Compares the results of a query when run under the 2 different builders
    // and prints the outcome (running on the default builder, running on the composable builder, and comparison of results)
    static let compPrint (compMethod : 'b -> 'b -> bool) (runMethod : bool -> Expr<'a> -> 'b) e = 
        let fsharp = eval (run runMethod true) e
        let tlinq = eval (run runMethod false) e

        if fsharp.IsSome && tlinq.IsSome then
            if (compMethod fsharp.Value tlinq.Value) then
                printOk()
            else
                printFail()
        else
            printResult Console.ForegroundColor "N/A"

        printfn ""


    // Runs the query under the two different builders then prints the mean execution time for each. 
    static let timePrint runMethod e = 

        // prepare the simplified expressions so we can time just the execution
        let expA, expB = substituteBuilder QueryBuilders.FSharp3 e, substituteBuilder (QueryBuilders.TLinq :> QueryBuilder) e
        let qbA, qbB = true, false
        
        // gets the timing results for the execution of the Expr e' on the QueryBuilder qb
        let getResults qb e' = 
            [|1..nRuns|]
            |> Array.map (fun _ ->
                let ta,ra = getTime (runMethod qb) e'
                float ta)

        // tries running the Expr e' on the QueryBuilder qb and prints the mean execution time
        let ev qb e' = 
            match tryEval (getResults qb) e' with
            | Some t -> 
                let m = mean t
                printResult ConsoleColor.Green (sprintf "%.2fms" m)
            | None -> 
                printFail()

        ev qbA expA
        ev qbB expB
            
        printfn ""

    // Throws an invalid RunMode exception
    static let invalidRunMode() = 
        failwithf "Invalid RunMode (%s) specified!" (Utils.RunMode.ToString())

    // Gets or sets the execution mode of tests. 
    static member val RunMode = UtilsMode.RunThrow with get, set

    static member Run (e:Expr<IQueryable<'T>>) = 
        (extractQuery e) 
        |> match Utils.RunMode with
            | UtilsMode.RunThrow -> runThrow compQuery runQuery
            | UtilsMode.CompPrint -> compPrint compQuery runQuery
            | UtilsMode.TimePrint -> timePrint runQuery
            | _ -> invalidRunMode()

    static member internal RunAsValue (e:Expr<'T>) = 
        (extractValue e) 
        |> match Utils.RunMode with
            | UtilsMode.RunThrow -> runThrow compVal runVal
            | UtilsMode.CompPrint -> compPrint compVal runVal
            | UtilsMode.TimePrint -> timePrint runVal
            | _ -> invalidRunMode()

    static member internal RunAsEnumerable (e:Expr<seq<'T>>) = 
        (extractEnum e) 
        |> match Utils.RunMode with
            | UtilsMode.RunThrow -> runThrow compEnum runEnum
            | UtilsMode.CompPrint -> compPrint compEnum runEnum
            | UtilsMode.TimePrint -> timePrint runEnum
            | _ -> invalidRunMode()

// The overloads for different query types are written as extension methods 
// as otherwise the compiler requires us to explicitly state which overload
// to call, two of them being special cases of the other. 
[<AutoOpen>]
module UtilsHi = 
    type Utils with

    [<CompiledName("RunAsValue")>]
    static member Run (e:Expr<'T>) = Utils.RunAsValue e

[<AutoOpen>]
module UtilsLo = 
    type Utils with

    [<CompiledName("RunAsEnumerable")>]
    static member Run (e:Expr<seq<'T>>) = Utils.RunAsEnumerable e