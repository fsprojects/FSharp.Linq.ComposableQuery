namespace FSharpComposableQuery

open Microsoft.FSharp.Quotations
open System.Reflection
open System.Linq

// Provides methods for translation between invocations of different 
// QueryBuilders (marked with the ReplaceWithAttribute)
//
// We need translation since nesting queries as quotations 
// generates calls to the ComposableQuery builder
// which are not recognized by the LINQ-to-SQL builder.
//
// Furthermore we cannot simply replace the variables these are called on
// since the MethodInfo objects have different signatures. 
module QueryTranslator = 

    // The native method to substitute for
    type NativeFunc = 
        | Run = 0
        | RunValue = 1
        | RunEnum = 2

    // Used to tag methods to be replaced by native ones (from Microsoft.FSharp.Core.ExtraTopLevels) when parsing nested queries
    type ReplaceWithAttribute(ftype : NativeFunc) = 
        member this.FType = ftype

    //vanilla LINQ query for reference
    let internal qNativeEnum = <@ query { for _ in [] do yield 0 } @>   //run as enumerable
    let internal qNativeVal = <@ query { for _ in [] do count } @>      //run as value
    let internal qNativeQue = <@ query { select 0 } @>                  //run as queryable

    // Separates the builder from the other args
    let internal parseArgs o args = 
        let allArgs = (Option.toList o) @ args
        (allArgs.First(), allArgs.Skip(1))
        
    // Gets the native methodinfo for RunAs___ from a simple query expression
    let internal getMethodData q = 
        match q with
        | Patterns.Application (Patterns.Lambda(_, Patterns.Call(o, mi, args)), _) -> 
            let builder, _ = parseArgs o args

            (mi.GetGenericMethodDefinition(), builder)
        | _ -> 
            failwith "Unable to initialize"

    // Contains mappings from NativeFunc to method data (MethodInfo * Expr)
    let internal nativeMethodData = 
        Map.empty
            .Add(NativeFunc.RunEnum, getMethodData qNativeEnum)
            .Add(NativeFunc.RunValue, getMethodData qNativeVal)
            .Add(NativeFunc.Run, getMethodData qNativeQue)



    // Translates a call to a recognized method to a call to the native QueryBuilder
    let translateNativeMethod att (o, (mi:MethodInfo), args) =
        let (newMi, newBuilder) = nativeMethodData.[att]
        let newMi' = newMi.MakeGenericMethod (mi.GetGenericArguments())
                    
        // the query builder is always first
        let actualArgs = ((Option.toList o) @ args).Tail

        match newMi.IsStatic with
        | true ->
            (None, newMi', newBuilder :: actualArgs)
        | false ->
            (Some newBuilder, newMi', actualArgs)

    // Checks whether a method is recognized as translatable
    let recogniseNativeMethod (mi:MethodInfo) =
        mi.GetCustomAttributes true
        |> Seq.filter (fun (a:System.Object) -> 
            a.GetType().IsAssignableFrom(typedefof<ReplaceWithAttribute>))
        |> Seq.cast<ReplaceWithAttribute>
        |> Seq.tryPick Some

    // Recursively traverses the given expression, applying the given function at every node and replacing it with its result
    let rec traverse f (e:Expr) =
        let rec traverseRaw e = 
            let traverseList = List.map traverseRaw
            match f e with
                | Patterns.AddressOf(e1) ->
                    Expr.AddressOf(traverseRaw e1)
                | Patterns.AddressSet(e1, e2) ->
                    Expr.AddressSet(traverseRaw e1, traverseRaw e2)
                | Patterns.Application(e1, e2) ->
                    Expr.Application(traverseRaw e1, traverseRaw e2)
                | Patterns.Call(e1, mi, l) ->
                    match e1 with
                    | Some e1 -> Expr.Call(traverseRaw e1, mi, traverseList l)
                    | None -> Expr.Call(mi, traverseList l)
                | Patterns.Coerce(e1, ty) ->
                    Expr.Coerce(traverseRaw e1, ty)
                | Patterns.DefaultValue(ty) ->
                    Expr.DefaultValue(ty)
                | Patterns.FieldGet(e1, fi) ->
                    match e1 with
                    | Some e1 -> Expr.FieldGet(traverseRaw e1, fi)
                    | None -> Expr.FieldGet(fi)
                | Patterns.FieldSet(e1, fi, e2) ->
                    match e1 with
                    | Some e1 -> Expr.FieldSet(traverseRaw e1, fi, traverseRaw e2)
                    | None -> Expr.FieldSet(fi, traverseRaw e2)
                | Patterns.ForIntegerRangeLoop(v, e1, e2, e3) ->
                    Expr.ForIntegerRangeLoop(v, traverseRaw e1, traverseRaw e2, traverseRaw e3)
                | Patterns.IfThenElse(e1, e2, e3) ->
                    Expr.IfThenElse(traverseRaw e1, traverseRaw e2, traverseRaw e3)
                | Patterns.Lambda(v, e1) ->
                    Expr.Lambda(v, traverseRaw e1)
                | Patterns.LetRecursive(l, e1) ->
                    Expr.LetRecursive(l, traverseRaw e1)
                | Patterns.Let(v, e1, e2) ->
                    Expr.Let(v, traverseRaw e1, traverseRaw e2)
                | Patterns.NewArray(ty, l) ->
                    Expr.NewArray(ty, traverseList l)
                | Patterns.NewDelegate(ty, l, e1) ->
                    Expr.NewDelegate(ty, l, traverseRaw e1)
                | Patterns.NewObject(ci, l) ->
                    Expr.NewObject(ci, traverseList l)
                | Patterns.NewRecord(ty, l) ->
                    Expr.NewRecord(ty, traverseList l)
                | Patterns.NewTuple(l) ->
                    Expr.NewTuple(l)
                | Patterns.NewUnionCase(ui, l) ->
                    Expr.NewUnionCase(ui, traverseList l)
                | Patterns.PropertyGet(e1, pi, l) ->
                    match e1 with
                    | Some e1 -> Expr.PropertyGet(traverseRaw e1, pi, traverseList l)
                    | None -> Expr.PropertyGet(pi, traverseList l)
                | Patterns.PropertySet(e1, pi, l, e2) ->
                    match e1 with
                    | Some e1 -> Expr.PropertySet(traverseRaw e1, pi, traverseRaw e2, traverseList l)
                    | None -> Expr.PropertySet(pi, traverseRaw e2, traverseList l)
                | Patterns.Quote(e1) ->
                    Expr.Quote(traverseRaw e1)
                | Patterns.Sequential(e1, e2) ->
                    Expr.Sequential(traverseRaw e1, traverseRaw e2)
                | Patterns.TryFinally(e1, e2) ->
                    Expr.TryFinally(traverseRaw e1, traverseRaw e2)
                | Patterns.TryWith(e1, v1, e2, v2, e3) ->
                    Expr.TryWith(traverseRaw e1, v1, traverseRaw e2, v2, traverseRaw e3)
                | Patterns.TupleGet(e1, int) ->
                    Expr.TupleGet(traverseRaw e1, int)
                | Patterns.TypeTest(e1, ty) ->
                    Expr.TypeTest(traverseRaw e1, ty)
                | Patterns.UnionCaseTest(e1, ui) ->
                    Expr.UnionCaseTest(traverseRaw e1, ui)
                | Patterns.Value(o, ty) ->
                    Expr.Value(o, ty)
                | Patterns.Var(v) ->
                    Expr.Var(v)
                | Patterns.VarSet(v, e1) ->
                    Expr.VarSet(v, traverseRaw e1)
                | Patterns.WhileLoop(e1, e2) ->
                    Expr.WhileLoop(traverseRaw e1, traverseRaw e2)
                | _ ->
                    failwith "Unrecognized expression!"
        traverseRaw e

    // Substitutes all calls to recognized methods with their native counterparts
    let replaceNativeMethods : (Expr -> Expr) = 
        traverse (fun e ->
            match e with
            | Patterns.Call(e1, mi, l) ->
                match recogniseNativeMethod(mi) with
                | Some a -> 
                    let e1, mi, l = translateNativeMethod a.FType (e1, mi, l)
                    match e1 with
                    | Some e1 -> Expr.Call(e1, mi, l)
                    | None -> Expr.Call(mi, l)
                | None -> e
            | _ -> e
            )