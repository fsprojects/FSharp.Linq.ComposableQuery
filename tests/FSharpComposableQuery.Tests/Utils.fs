namespace FSharpComposableQuery.Tests

open System
open System.Linq
open System.Reflection
open Microsoft.FSharp.Linq
open Microsoft.FSharp.Linq.QuotationEvaluation
open Microsoft.FSharp.Quotations
open FSharpComposableQuery

module QueryBuilders = 
    let TLinq = FSharpComposableQuery.TopLevelValues.query
    let FSharp3 = ExtraTopLevelOperators.query


module ExprUtils = 
    open FSharpComposableQuery.Common

    let runQueryMi = ForwardDeclarations.RunQueryMi.Query
    let runValueMi = ForwardDeclarations.RunQueryMi.Value
    let runEnumMi =  ForwardDeclarations.RunQueryMi.Enum

    // Recursively traverses the given expression, applying the given function at every item and replacing it with its result
    let rec traverseExpr f e = 
        let rec tExp e = 
            let tList = List.map tExp
            match f e with
            | Patterns.AddressOf(e1) -> Expr.AddressOf(tExp e1)
            | Patterns.AddressSet(e1, e2) -> Expr.AddressSet(tExp e1, tExp e2)
            | Patterns.Application(e1, e2) -> Expr.Application(tExp e1, tExp e2)
            | Patterns.Call(e1, mi, l) -> 
                match e1 with
                | Some e1 -> Expr.Call(tExp e1, mi, tList l)
                | None -> Expr.Call(mi, tList l)
            | Patterns.Coerce(e1, ty) -> Expr.Coerce(tExp e1, ty)
            | Patterns.DefaultValue(ty) -> Expr.DefaultValue(ty)
            | Patterns.FieldGet(e1, fi) -> 
                match e1 with
                | Some e1 -> Expr.FieldGet(tExp e1, fi)
                | None -> Expr.FieldGet(fi)
            | Patterns.FieldSet(e1, fi, e2) -> 
                match e1 with
                | Some e1 -> Expr.FieldSet(tExp e1, fi, tExp e2)
                | None -> Expr.FieldSet(fi, tExp e2)
            | Patterns.ForIntegerRangeLoop(v, e1, e2, e3) -> Expr.ForIntegerRangeLoop(v, tExp e1, tExp e2, tExp e3)
            | Patterns.IfThenElse(e1, e2, e3) -> Expr.IfThenElse(tExp e1, tExp e2, tExp e3)
            | Patterns.Lambda(v, e1) -> Expr.Lambda(v, tExp e1)
            | Patterns.LetRecursive(l, e1) -> Expr.LetRecursive(l, tExp e1)
            | Patterns.Let(v, e1, e2) -> Expr.Let(v, tExp e1, tExp e2)
            | Patterns.NewArray(ty, l) -> Expr.NewArray(ty, tList l)
            | Patterns.NewDelegate(ty, l, e1) -> Expr.NewDelegate(ty, l, tExp e1)
            | Patterns.NewObject(ci, l) -> Expr.NewObject(ci, tList l)
            | Patterns.NewRecord(ty, l) -> Expr.NewRecord(ty, tList l)
            | Patterns.NewTuple(l) -> Expr.NewTuple(l)
            | Patterns.NewUnionCase(ui, l) -> Expr.NewUnionCase(ui, tList l)
            | Patterns.PropertyGet(e1, pi, l) -> 
                match e1 with
                | Some e1 -> Expr.PropertyGet(tExp e1, pi, tList l)
                | None -> Expr.PropertyGet(pi, tList l)
            | Patterns.PropertySet(e1, pi, l, e2) -> 
                match e1 with
                | Some e1 -> Expr.PropertySet(tExp e1, pi, tExp e2, tList l)
                | None -> Expr.PropertySet(pi, tExp e2, tList l)
            | Patterns.Quote(e1) -> Expr.Quote(tExp e1)
            | Patterns.Sequential(e1, e2) -> Expr.Sequential(tExp e1, tExp e2)
            | Patterns.TryFinally(e1, e2) -> Expr.TryFinally(tExp e1, tExp e2)
            | Patterns.TryWith(e1, v1, e2, v2, e3) -> Expr.TryWith(tExp e1, v1, tExp e2, v2, tExp e3)
            | Patterns.TupleGet(e1, int) -> Expr.TupleGet(tExp e1, int)
            | Patterns.TypeTest(e1, ty) -> Expr.TypeTest(tExp e1, ty)
            | Patterns.UnionCaseTest(e1, ui) -> Expr.UnionCaseTest(tExp e1, ui)
            | Patterns.Value(o, ty) -> Expr.Value(o, ty)
            | Patterns.Var(v) -> Expr.Var(v)
            | Patterns.VarSet(v, e1) -> Expr.VarSet(v, tExp e1)
            | Patterns.WhileLoop(e1, e2) -> Expr.WhileLoop(tExp e1, tExp e2)
            | _ -> failwith "Unrecognized expression!"
        tExp e
                    
    // Substitutes all calls to recognized methods with their native counterparts
    let replaceNestedQueries (e:Expr<'T>) : Expr<'T> = 
        e 
        |> traverseExpr (fun e ->
            match e with
            | Patterns.Call(e1, mi, l) ->
                let genMi = getGenericMethodDefinition mi
                if genMi = runQueryMi then
                    let runMi = runNativeQueryMi.MakeGenericMethod(mi.GetGenericArguments())
                    Expr.Call(nativeBuilderExpr, runMi, l)
                else if genMi = runEnumMi then
                    let runMi = runNativeEnumMi.MakeGenericMethod(mi.GetGenericArguments())
                    Expr.Call(runMi, nativeBuilderExpr :: l.Tail)
                else if genMi = runValueMi then
                    let runMi = runNativeValueMi.MakeGenericMethod(mi.GetGenericArguments())
                    Expr.Call(runMi, nativeBuilderExpr :: l.Tail)
                else e
            | _ -> e)
        |> Expr.Cast


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
    // Requires a type anotation as otherwise casts seq<'T> to queryable<'T> (?)
    static let force (l:seq<'T>) = Seq.iter ignore l

    // Gets the body of an expression of the type "query { <body> }"
    static let extractBodyRaw(e:Expr<'T>) =
        match e with
        | Patterns.Application (Patterns.Lambda(_, Patterns.Call(Some _, mi, [Patterns.Quote(q)])), _)
        | Patterns.Application (Patterns.Lambda(_, Patterns.Call(None,   mi, [_; Patterns.Quote(q)])), _) ->
            q
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
    static let substituteBuilder translate =
        let subst (qb : #QueryBuilder) (e:Expr<'T>) : Expr<'T> = 
            let qbExpr = Expr.Value(qb, qb.GetType())
            e.Substitute(fun v ->
                if(v.Name = "builder@") then
                    Some qbExpr
                else
                    None)
            |> Expr.Cast
        match translate with
            | true -> ExprUtils.replaceNestedQueries >> (subst QueryBuilders.FSharp3)    //replace query.Run calls as well
            | false -> subst QueryBuilders.TLinq


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
        
    // Runs the given expression
    static let run runMethod translate = 
        substituteBuilder translate >> runMethod translate


        
    // The methods used to compare the results of different types of queries. 

    // Compare queryables
    // TODO: queries of this type are sometimes ordered
    static let compQuery (a : IQueryable<'T>) (b : IQueryable<'T>) = 
        let aa = a.ToArray()
        let bb = b.ToArray()
        
        let ans = (aa.Length = bb.Length && not (aa.Except(bb).Any()))
        ans

    // Compare values
    static let compVal (a : 'T) (b : 'T) = 
        let ans = (a = b)
        ans

    // Compare sequences. 
    // TODO: are queries of this type always ordered?
    static let compEnum (a : seq<'T>) (b : seq<'T>) = 
        let ans = (a.SequenceEqual b)
        ans


    // Formats and prints the text using a specific color
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
        let expA, expB = substituteBuilder true e, substituteBuilder false e
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