namespace FSharpComposableQuery

open Microsoft.FSharp.Quotations
open System.Reflection
open System.Linq

// Provides methods for translation from invocations of different 
// QueryBuilders (marked with the ReplaceWithAttribute)
module QueryTranslator = 

    // The native method to substitute for
    type NativeFunc = 
        | Run = 0
        | RunValue = 1
        | RunEnum = 2

    // Used to tag methods to be replaced by native ones (from Microsoft.FSharp.Core.ExtraTopLevels) when parsing nested queries
    type ReplaceWithAttribute(ftype : NativeFunc) = 
        member this.FType = ftype

    

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

    // Translates a call to a recognized method to a call to the original QueryBuilder
    let translateNativeMethod att (o, (mi:MethodInfo), args) =
        let (newMi, newBuilder) = nativeMethodData.[att]
        let newMi' = newMi.MakeGenericMethod (mi.GetGenericArguments())
                    
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


    
    let rec translateRaw expr = 
        let replaceList = List.map translateRaw
        match expr with
                | Patterns.Value(o, t) -> Expr.Value(o,t)
                | Patterns.Var(var) -> Expr.Var(var)
                | Patterns.Quote e -> Expr.Quote (translateRaw e)
                | Patterns.Lambda(param, body) -> Expr.Lambda(param, translateRaw body)
                | Patterns.Application(e1, e2) -> Expr.Application(translateRaw e1, translateRaw e2)
                | Patterns.Coerce(e, t) -> Expr.Coerce(translateRaw e, t)
                | Patterns.PropertyGet(o, pi, l) -> 
                    match o with
                    | Some(o) -> Expr.PropertyGet(translateRaw o, pi, replaceList l)
                    | None -> Expr.PropertyGet(pi, replaceList l)
                | Patterns.FieldGet(o, fi) -> 
                    match o with
                    | Some(o) -> Expr.FieldGet(translateRaw o, fi)
                    | None -> Expr.FieldGet(fi)
                | Patterns.NewRecord(t, l) -> Expr.NewRecord(t, replaceList l)
                | Patterns.NewObject(ci, l) -> Expr.NewObject(ci, replaceList l)
                | Patterns.IfThenElse(e, e1, e2) -> Expr.IfThenElse(translateRaw e, translateRaw e1, translateRaw e2)
                | Patterns.Let(var, e1, e2) -> Expr.Let(var, translateRaw e1, translateRaw e2)
                | Patterns.TupleGet(e, i) -> Expr.TupleGet(translateRaw e, i)
                | Patterns.NewTuple(l) -> Expr.NewTuple(replaceList l)
                | Patterns.Call(o, mi, l) ->
                    let (o, mi, l) = 
                        match recogniseNativeMethod mi with
                        | Some(nf) -> translateNativeMethod nf.FType (o, mi, l)
                        | None -> (o, mi, l)
                    match o with
                        | Some o -> Expr.Call(o, mi, replaceList l)
                        | _ -> Expr.Call(mi, replaceList l)
                | expr -> failwithf "unhandled expr: %A" expr

                
    // Replaces all calls to marked methods with calls to the default queryBuilder
    let translateExpr (expr:Expr<'T>) = 

        let rawExpr = expr.Raw
        let newRawExpr = translateRaw rawExpr
        Expr.Cast<'T>(newRawExpr)