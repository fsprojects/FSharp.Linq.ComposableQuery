namespace FSharpComposableQuery

module internal Helpers = 
    let IQueryableTy = typeof<System.Linq.IQueryable>
    let IEnumerableTy = typeof<System.Collections.IEnumerable>

    let QuerySourceTy (ty:System.Type,qty:System.Type) = 
            typeof<Linq.QuerySource<_,_>>.GetGenericTypeDefinition().MakeGenericType([|ty;qty|])

    let EnumerableTy (ty:System.Type) = 
        typeof<System.Collections.Generic.IEnumerable<_>>.GetGenericTypeDefinition().MakeGenericType([|ty|])

    let GroupTy (ty1:System.Type,ty2) = 
        typeof<System.Linq.IGrouping<_,_>>.GetGenericTypeDefinition().MakeGenericType([|ty1;ty2|])
      
    
    let (|IQueryableTy|_|) ty = if (ty = typeof<System.Linq.IQueryable>) then Some () else None
    let (|IEnumerableTy|_|) ty = if (ty = typeof<System.Collections.IEnumerable>) then Some () else None

    let (|QuerySourceTy|_|) (ty:System.Type) = 
        if ty.IsGenericType && ty.GetGenericTypeDefinition() = typeof<Linq.QuerySource<_,_>>.GetGenericTypeDefinition()
        then Some (ty.GetGenericArguments().[0],ty.GetGenericArguments().[1])
        else None


    let (|EnumerableTy|_|) (ty:System.Type) = 
        if ty.IsGenericType && ty.GetGenericTypeDefinition() = typeof<System.Collections.Generic.IEnumerable<_>>.GetGenericTypeDefinition()
        then Some (ty.GetGenericArguments().[0])
        else None

    let (|GroupTy|_|) (ty:System.Type) = 
        if ty.IsGenericType && ty.GetGenericTypeDefinition() = typeof<System.Linq.IGrouping<_,_>>.GetGenericTypeDefinition()
        then Some (ty.GetGenericArguments().[0],ty.GetGenericArguments().[1])
        else None

module QueryImpl = 


    open Microsoft.FSharp.Quotations
    open Microsoft.FSharp.Quotations.ExprShape
    open Microsoft.FSharp.Quotations.Patterns
    open Microsoft.FSharp.Quotations.DerivedPatterns
    open Microsoft.FSharp.Reflection
    open Microsoft.FSharp.Reflection
    open System.Reflection

    open FSharpComposableQuery.Common
    open Helpers

#if TIMING 
    let lastQueryNormTime = ref []
    let resetNormTimes() = lastQueryNormTime := []
    let addNormTime(t) = lastQueryNormTime := t :: (!lastQueryNormTime)
    let medianNormTime() = 
      median(!lastQueryNormTime)
#endif

    type internal Func = 
        | ExistsF
        | ForallF
        | WhereF
        | MinusF
        | PlusF
        | TimesF
        | DivF
        | ModF
        | EqF
        | NeqF
        | LtF
        | LeqF
        | GtF
        | GeqF
        | StrConcatF
        | LikeF
        | AndF
        | OrF
        | NotF
        | AppRF
        | AppLF
        // new querybuilder cases
        | SelectF
        | ForF
        | ZeroF
        | YieldF
        //| SourceF
        | UnknownF
        | RunQueryAsValueF

    type QueryBuilder() = 
        inherit Microsoft.FSharp.Linq.QueryBuilder()


        let getQuerySourceTy ty k = 
            match ty with
            | QuerySourceTy(ty', qty) -> k ty' qty
            | _ -> raise NYI
        
        let getFunTy ty k = 
            match ty with
            | FunTy(ty1, ty2) -> k ty1 ty2
            | _ -> raise NYI
        
        // quick and dirty function to compute result type of an expression
        // does not check expression well-typed; relies on variable annotations
        let rec getType exp = 
            match exp with
            | EVar x -> x.Type
            | ELet(_, _, e2) -> getType e2
            | Op(op, _) -> getOpType op
            | IntC _ -> typeof<int>
            | BoolC _ -> typeof<bool>
            | StringC _ -> typeof<string>
            | Unit -> typeof<unit>
            | Tuple(tty, _) -> tty
            | Proj(e, i) -> 
                let tty = getType e
                FSharpType.GetTupleElements(tty).[i]
            | IfThenElse(_, e1, _) -> getType e1
            | Record(rty, _) -> rty
            | Field(_, f) -> f.info.PropertyType
            | Empty ty -> QuerySourceTy(ty, IQueryableTy)
            | Singleton e -> QuerySourceTy(getType e, IQueryableTy)
            | Union(e1, _) -> getType e1
            | Comp(e2, _, _) -> getType e2
            | Exists(_) -> typeof<bool>
            | Lam(x, e) -> FunTy(x.Type, getType (e))
            | App(e1, _) -> getFunTy (getType (e1)) (fun _ty1 ty2 -> ty2)
            | Table(_, ty) -> QuerySourceTy(ty, IQueryableTy)
            | Unknown(_, ty, _, _) -> ty
    

        let rec reduce exp = 
            match exp with
            | EVar x -> EVar x
            | ELet(x, e1, e2) -> reduce (subst e1 x e2)
            | Op(op, es) -> Op(op, List.map reduce es)
            | IntC i -> IntC i
            | BoolC b -> BoolC b
            | StringC s -> StringC s
            | Unit -> Unit
            | Tuple(tty, es) -> Tuple(tty, List.map reduce es)
            | Proj(e, i) -> 
                match reduce e with
                | Tuple(_tty, es) -> List.nth es i
                | IfThenElse(m, n1, n2) -> 
                    reduce (IfThenElse(m, Proj(n1, i), Proj(n2, i)))
                | n -> Proj(n, i)
            | IfThenElse(e, e1, e2) -> 
                match reduce e with
                | BoolC true -> reduce e1
                | BoolC false -> reduce e2
                | n -> 
                    match (getType e1) with
                    | QuerySourceTy(ty, _) -> 
                        reduceIfThenElseSeq n (reduce e1) (reduce e2) ty
                    | _ -> IfThenElse(n, reduce e1, reduce e2)
            | Record(rty, r) -> Record(rty, List.map (fun (l, e) -> (l, reduce e)) r)
            | Field(e, l) -> 
                match reduce e with
                | RecordWith l e' -> e'
                | IfThenElse(m, n1, n2) -> 
                    reduce (IfThenElse(m, Field(n1, l), Field(n2, l)))
                | n -> Field(n, l)
            | Empty ty -> Empty ty
            | Singleton e -> Singleton(reduce e)
            | Union(e1, e2) -> Union(reduce e1, reduce e2)
            | Comp(e2, x, e1) -> 
                match reduce e1 with
                | Empty _ty -> getQuerySourceTy (getType e2) (fun ty _ -> Empty ty)
                | Singleton m -> reduce (subst m x e2)
                | Union(m1, m2) -> 
                    Union(reduce (Comp(e2, x, m1)), reduce (Comp(e2, x, m2)))
                | Comp(m2, y, m1) -> 
                    // TODO: these results are unused, is this a bug?
                    let (y', m2') = 
                        if x <> y then (y, m2)
                        else 
                            let y' = fresh (y)
                            (y', freshen y y' m2)
                    reduce (Comp(Comp(e2, x, m2), y, m1))
                | IfThenElse(m, n1, n2) -> 
                    IfThenElse(m, reduce (Comp(e2, x, n1)), reduce (Comp(e2, x, n2)))
                | n -> Comp(reduce e2, x, n)
            | Lam(x, e) -> Lam(x, reduce e)
            | App(e1, e2) -> 
                match reduce e1 with
                | Lam(x, m) -> reduce (subst e2 x m)
                | IfThenElse(m, n1, n2) -> 
                    reduce (IfThenElse(m, App(n1, e2), App(n2, e2)))
                | n -> App(n, reduce e2)
            | Exists e -> 
                match getType e with
                | QuerySourceTy(UnitTy, _) -> Exists(reduce e) 
                | QuerySourceTy(ty, _) -> 
                    Exists(Comp(Singleton(Unit), new Var("__dummy", ty), reduce e))
                | _ -> raise NYI
            | Table(e, ty) -> Table(e, ty)
            | Unknown(unk, ty, eopt, es) -> 
                Unknown(unk, ty, Option.map reduce eopt, List.map reduce es)
        
        and reduceIfThenElseSeq test thenExp elseExp ty = 
            match thenExp, elseExp with
            | Empty _, Empty _ -> Empty ty
            | Comp(n, x, m), Empty _ -> Comp(IfThenElse(test, n, Empty ty), x, m)
            | Union(m, n), Empty _ -> 
                Union(IfThenElse(test, m, Empty ty), IfThenElse(test, n, Empty ty))
            | _, Empty _ -> IfThenElse(test, thenExp, Empty ty)
            | m, n -> 
                Union
                    (IfThenElse(test, m, Empty ty), 
                     IfThenElse(Op(Not, [ test ]), n, Empty ty))

        let rec nf exp = 
            let expR = reduce exp 
            if exp = expR then expR else nf expR
  
  #if UNUSED
        let recognizeNF exp = 
            let rec recognizeToplevel exp = 
                match exp with
                | e -> recognizeUnion exp
            
            and recognizeUnion exp = 
                match exp with
                | Empty _ -> true
                | Union(e1, e2) -> recognizeUnion e1 && recognizeUnion e2
                | f -> recognizeFrom f
            
            and recognizeFrom exp = 
                match exp with
                | Comp(f, x, Table(e, ty)) -> recognizeFrom f
                | w -> recognizeWhere w
            
            and recognizeWhere exp = 
                match exp with
                | IfThenElse(b, w, Empty _) -> recognizeBase b && recognizeWhere w
                | Singleton r -> recognizeRecord r
                | _ -> 
                    printfn "Warning: not where clause %A" exp
                    false
            
            and recognizeRecord exp = 
                match exp with
                | Record(_, les) -> List.forall (fun (l, e) -> recognizeBase e) les
                | Tuple(_, es) -> List.forall recognizeBase es
                | Unit -> true
                | EVar x -> true // not a normalized record though...
                | _ -> 
                    printfn "Warning: not record %A" exp
                    false
            
            and recognizeBase exp = 
                match exp with
                | IfThenElse(e, e1, e2) -> 
                    recognizeBase e && recognizeBase e1 && recognizeBase e2
                | Exists e -> recognizeUnion e
                | Field(EVar x, l) -> true
                | Proj(EVar x, i) -> true
                | IntC _ -> true
                | BoolC _ -> true
                | StringC _ -> true
                | Unit -> true
                | Op(op, es) -> List.forall recognizeBase es
                | EVar x -> true
                | _ -> 
                    printfn "Warning: not base %A" exp
                    false
            
            recognizeUnion exp// evil hack to compute method info efficiently
  #endif
                              
            // store dummy MethodInfor records; replace them with real ones later
        let yieldMi = ref (getGenericMethodInfo <@@ id @@>)
        let zeroMi = ref  (getGenericMethodInfo <@@ id @@>)
        let forMi = ref (getGenericMethodInfo <@@ id @@>)
        let existsMi = ref(getGenericMethodInfo <@@ id @@>)
        let forallMi = ref(getGenericMethodInfo <@@ id @@>)
        let whereMi = ref (getGenericMethodInfo <@@ id @@>)
        let selectMi = ref (getGenericMethodInfo <@@ id @@>)
       // let sourceMi = ref(getGenericMethodInfo <@@ id @@>)
        let runQueryAsValueMi = ref(getGenericMethodInfo <@@ id @@>)

        member internal this.initMi() = 
          yieldMi :=  (getGenericMethodInfo <@@ this.Yield @@>)
          zeroMi :=  (getGenericMethodInfo <@@ this.Zero @@>)
          forMi :=  ( getGenericMethodInfo <@@ this.For @@>)
          existsMi :=  ( getGenericMethodInfo  <@@ this.Exists @@>)
          forallMi :=  ( getGenericMethodInfo <@@ this.All @@>)
          whereMi :=  ( getGenericMethodInfo  <@@ this.Where @@>)
          selectMi :=  ( getGenericMethodInfo <@@ this.Select @@>)
          //sourceMi :=  ( getGenericMethodInfo <@@ this.Source @@>)
          let q = <@ query { if query{for x in [1] do exists(x=1)} then yield 1 }@> 
          let mi = match q with 
                        Application (Lambda (_,Call (_, 
                                                     _run,
                                                     [Quote (Patterns.IfThenElse(Application (Lambda (_,
                                                                                                      Call (_, x,_)),_),_,_))])),_)
                        -> x.GetGenericMethodDefinition()
                      | _ -> raise NYI 
          runQueryAsValueMi :=  mi
   

        member internal this.recognizeFunc (methodInfo':System.Reflection.MethodInfo) = 
            let methodInfo = getGenericMethodInfo' methodInfo' 
            if methodInfo = !yieldMi then YieldF
            else if methodInfo = !zeroMi then ZeroF
            else if methodInfo = !forMi then ForF
            //else if methodInfo = this.existsMi then ExistsF
            //else if methodInfo = this.forallMi then ForallF
            else if methodInfo = !whereMi then WhereF
            else if methodInfo = unaryMinusMi then MinusF
            else if methodInfo = plusMi then PlusF
            else if methodInfo = minusMi then MinusF
            else if methodInfo = timesMi then TimesF
            else if methodInfo = divMi then DivF
            else if methodInfo = modMi then ModF
            else if methodInfo = eqMi then EqF
            else if methodInfo = neqMi then NeqF
            else if methodInfo = ltMi then LtF
            else if methodInfo = leqMi then LeqF
            else if methodInfo = gtMi then GtF
            else if methodInfo = geqMi then GeqF
            else if methodInfo = strconcatMi then StrConcatF
            else if methodInfo = likeMi then LikeF
            else if methodInfo = andMi then AndF
            else if methodInfo = orMi then OrF
            else if methodInfo = notMi then NotF
            else if methodInfo = apprMi then AppRF
            else if methodInfo = applMi then AppLF
            else if methodInfo = !selectMi then  SelectF
            //else if methodInfo = !sourceMi then  SourceF
            else if methodInfo = !runQueryAsValueMi then  RunQueryAsValueF

            else UnknownF
         
        member internal this.emptyExp ty = Expr.Call(Expr.Value this,(!zeroMi).MakeGenericMethod([|ty;IQueryableTy|]),[]) 
        member internal this.singletonExp (e:Expr) = Expr.Call(Expr.Value this,(!yieldMi).MakeGenericMethod( [|e.Type;IQueryableTy|] ), [e])


        member internal this.existsExp(e':Expr) = 
            match e'.Type with
                QuerySourceTy (tyinner,qty) ->    
                    Expr.Call(Expr.Value this,(!existsMi).MakeGenericMethod([|tyinner;qty|]), [ e';Expr.Lambda (new Var("__dummy",tyinner),Expr.Value true)])
              | _ -> failwithf "Unexpected type %A" e'.Type

        member internal this.forExp (e2:Expr) (x:Var) (e1:Expr) = 
            match e1.Type,e2.Type with
                QuerySourceTy (tyCollection,qty1), QuerySourceTy(tyResult,qty2) -> 
                    assert (x.Type = tyCollection)
                    Expr.Call(Expr.Value this,(!forMi).MakeGenericMethod([|tyCollection;qty1;tyResult;qty2|]), [e1;Expr.Lambda (x,e2)])
              | _ -> failwithf "Unexpected type %A %A" e1.Type e2.Type

        member internal this.toExpr exp = 
            let rec toExp exp = 
                match exp with
                | IntC x -> Expr.Value(x, typeof<int>)
                | BoolC b -> Expr.Value(b, typeof<bool>)
                | StringC s -> Expr.Value(s, typeof<string>)
                | Unit -> Expr.Value((), typeof<unit>)
                | Tuple(_tty, es) -> Expr.NewTuple(List.map toExp es)
                | Proj(e, i) -> Expr.TupleGet(toExp e, i)
                | IfThenElse(e, e1, e2) -> Expr.IfThenElse(toExp e, toExp e1, toExp e2)
                | EVar x -> Expr.Var x
                | ELet(x, e1, e2) -> Expr.Let(x, toExp e1, toExp e2)
                | BinOp(e1, binop, e2) -> getBinOp binop (toExp e1) (toExp e2)
                | UnOp(unop, e) -> getUnOp unop (toExp e)
                | Field(e, l) -> Expr.PropertyGet(toExp e, l.info, [])
                | Record(rty, r) -> Expr.NewRecord(rty, List.map (fun (_, e) -> toExp e) r)
                | Lam(x, e) -> Expr.Lambda(x, toExp e)
                | App(e1, e2) -> Expr.Application(toExp e1, toExp e2)
                | Empty ty -> this.emptyExp ty
                | Singleton e -> this.singletonExp (toExp e)
                | Comp(e2, x, e1) -> this.forExp (toExp e2) x (toExp e1)
                | Exists(e) -> this.existsExp (toExp e)
                (*| Table(e,ty) -> this.sourceExp( e,ty)*)
                | Table(e, _ty) -> e
                | Unknown(unk, _, eopt, es) -> 
                    unknownToExpr unk (Option.map toExp eopt) (List.map toExp es)
                | _ -> raise NYI
            
            toExp exp



        member internal this.fromExpr expr =
            let rec from expr = 
                match expr with
                | Int32(n) -> IntC n
                | DerivedPatterns.Bool(b) -> BoolC b
                | String(str) -> StringC str
                | Var(var) -> EVar(var)
                | DerivedPatterns.Unit -> Unit
                | Value _ -> Unknown(UnknownRef(expr), expr.Type, None, [])
                | Quote e -> Unknown(UnknownQuote, expr.Type, None, [ from e ])
                | Lambda(param, body) -> Lam(param, from body)
                | Application(e1, e2) -> App(from e1, from e2)
                | Patterns.Coerce(PropertyGet(Some(PropertyGet(None, _db, [])), _tbl, []) as e, 
                                  QuerySourceTy(ty, _)) -> Table(e, ty) // recognize free vars as db table refs
                | Patterns.Coerce(PropertyGet(None, _tbl, []) as e, QuerySourceTy(ty, _)) -> 
                    Table(e, ty) // recognize free vars as db table refs
                | Patterns.Coerce(e, _ty) -> from e // catchall to ignore other coercions
                | PropertyGet(Some(PropertyGet(None, _db, [])), _tbl, []) as e -> 
                    match e.Type with
                    | QuerySourceTy(ty, _) -> Table(e, ty) // assume it's a db table ref
                    | TableTy ty -> Table(e, ty) // assume it's a db table ref
                    | ty' -> failwithf "Unexpected table reference %A %A" e ty'
                | Patterns.PropertyGet(Some(e), l, []) -> 
                    Field(from e, 
                          { name = l.Name
                            info = l }) // otherwise assume field ref
                | Patterns.PropertyGet(None, _, []) -> 
                    Unknown(UnknownRef(expr), expr.Type, None, []) // otherwise assume global var
                | Patterns.NewRecord(rty, values) -> 
                    let fields = Reflection.FSharpType.GetRecordFields rty |> Array.toList
                    let fieldsR = fields |> List.map (fun f -> { name = f.Name; info = f })
                    Record(rty, List.zip fieldsR (List.map from values))
                | Patterns.NewObject(ci, es) -> 
                    Unknown(UnknownNew(ci), expr.Type, None, List.map from es)
                | Patterns.IfThenElse(e, e1, e2) -> IfThenElse(from e, from e1, from e2)
                | Patterns.Let(var, expr1, expr2) -> ELet(var, from expr1, from expr2)
                | Patterns.TupleGet(expr, i) -> Proj(from expr, i)
                | Patterns.NewTuple(es) -> Tuple(expr.Type, List.map from es)
                | Call(obj, methodInfo, args) -> 
                    handleSpecificCall obj methodInfo args expr.Type
                | expr -> failwithf "unhandled expr: %A" expr

            and handleSpecificCall obj func args expr_ty = 
              match (this.recognizeFunc func),args with 
                | YieldF, [e] ->
                    Singleton(from e)
                | ZeroF, [] -> getQuerySourceTy expr_ty (fun ty _ -> Empty ty)
                | ForF, [e;f] ->
                    getFunTy f.Type (fun ty _ -> 
                       let x = fresh(new Var("x",ty)) 
                       Comp(App(from f, EVar x),  x, from e))
                | ExistsF, [e;f] ->
                        getFunTy f.Type (fun ty _ ->
                                         let x = fresh(new Var("x",ty)) 
                                         let px = App(from f, EVar x)
                                         let unitIfPx = IfThenElse(px,Singleton (Unit),Empty (typeof<unit>))
                                         Exists(Comp(unitIfPx, x,from e)))
                    
                | ForallF, [e;f] ->
                        getFunTy f.Type (fun ty _ -> 
                            let x = fresh(new Var("x",ty)) 
                            let notPx = Op(Not,[App(from f, EVar x)])
                            let unitIfNotPx = IfThenElse(notPx,Singleton (Unit),Empty (typeof<unit>))
                            Op(Not,[Exists(Comp(unitIfNotPx, x,from e))]))
                | WhereF, [e;f] ->
                        getFunTy f.Type (fun ty1 _ty2 -> 
                                         let x = fresh(new Var("x",ty1)) 
                                         Comp(IfThenElse(App(from f, EVar x),Singleton (EVar x),Empty (ty1)), x,from e))
                | PlusF, [e1;e2] ->
                    Op(Plus, [from e1;from e2])
                | MinusF, [e1;e2] ->
                    Op(Minus, [from e1;from e2])
                | TimesF, [e1;e2] ->
                    Op(Times, [from e1;from e2])
                | DivF, [e1;e2] ->
                    Op(Div, [from e1;from e2])
                | ModF, [e1;e2] ->
                    Op(Mod, [from e1;from e2])
                | EqF, [e1;e2] ->
                    Op(Equal, [from e1;from e2])
                | NeqF, [e1;e2] ->
                    Op(Nequal, [from e1;from e2])
                | GtF, [e1;e2] ->
                    Op(Gt, [from e1;from e2])
                | GeqF, [e1;e2] ->
                    Op(Geq, [from e1;from e2])
                | LtF, [e1;e2] ->
                    Op(Lt, [from e1;from e2])
                | LeqF, [e1;e2] ->
                    Op(Leq, [from e1;from e2])
                | StrConcatF, [e1;e2] ->
                    Op(Concat, [from e1;from e2])
                | LikeF, [e1;e2] ->
                    Op(Like, [from e1;from e2])
                | AndF, [e1;e2] ->
                    Op(And, [from e1;from e2])
                | OrF, [e1;e2] ->
                    Op(Or, [from e1;from e2])
                | NotF, [e] ->
                    Op(Not, [from e])
                | MinusF, [e] ->
                    Op(Neg, [from e])
                | AppRF, [e1;e2] ->
                    App(from e2, from e1)
                | AppLF, [e1;e2] ->
                    App(from e1, from e2)
                | SelectF, [e;f] -> 
                    getFunTy f.Type (fun ty _ -> 
                                     let x = fresh(new Var("x",ty)) 
                                     Comp(Singleton(App(from f,EVar x)),  x, from e))
                | _,args -> Unknown(UnknownCall (func), expr_ty, Option.map from obj, List.map from args)
            from expr


        member internal this.ToFrom (exp:Expr<'T>) : Expr<'T> = 
            let e = this.fromExpr exp.Raw
            //printfn "tofrom %A : %A" e (getType e)
            let expR = this.toExpr e
            Expr.Cast<'T>(expR)
    
        member internal this.NormRaw (exp:Expr<'T>) : Expr<'T> = 
            let e = this.fromExpr exp.Raw
            //printfn "norm1 %A : %A" e (getType e)
            let e' = nf e
            //printfn "norm2 %A : %A" e' (getType(e'))
            let expR = this.toExpr e'
            Expr.Cast<'T>(expR)
    
        member internal this.Norm (exp:Expr<'T>) : Expr<'T> = 
            let e',_t = withDuration 1 (fun () ->  this.NormRaw exp)
#if TIMING 
            addNormTime(_t)
#endif
            e'
        
        member __.Quote(q:Quotations.Expr<'a>) = 
            //printfn "Quote"
            base.Quote(q)


        member this.Run(q : Expr<Linq.QuerySource<'T, System.Linq.IQueryable>>) : System.Linq.IQueryable<'T> = 
            //printfn "run queryable %A : %A" q q.Type
            let q' = this.Norm q
            //printfn "run2 %A : %A %A" q' q'.Type (q' = q)
            base.Run(q')
        
        member internal this.RunAsEnumerable(q : Expr<Linq.QuerySource<'T, System.Collections.IEnumerable>>) : seq<'T> = 
            //printfn "run seq %A : %A" q q.Type
            let q' = this.Norm q
            //printfn "run2 %A : %A %A" q' q'.Type (q' = q)
            base.Run(q')
        
        [<CompiledName("RunQueryAsValue")>]
        member internal this.RunAsValue(q : Expr<'T>) : 'T = 
            //printfn "run base %A : %A" q q.Type
            //let timer = new System.Diagnostics.Stopwatch()
            //timer.Start()
            let q' = this.Norm q
            //timer.Stop()
            //let t1 = timer.Elapsed.TotalMilliseconds
            //printfn "run2 %A : %A %A" q' q'.Type (q' = q)
            //timer.Restart()
            let q'' = base.Run(q')
            //timer.Stop()
            //let t2 = timer.Elapsed.TotalMilliseconds
            //printfn "%f %f" t1 t2
            q''
        
     
namespace FSharpComposableQuery

open FSharpComposableQuery.QueryImpl
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq

[<AutoOpen>]
module LowPriority = 
    type FSharpComposableQuery.QueryImpl.QueryBuilder with
        [<CompiledName("RunQueryAsValue")>]
        member this.Run(q : Expr<'T>) = 
            this.RunAsValue q

[<AutoOpen>]
module HighPriority = 
    type FSharpComposableQuery.QueryImpl.QueryBuilder with
        [<CompiledName("RunQueryAsEnumerable")>]
        member this.Run(q : Expr<QuerySource<'T, System.Collections.IEnumerable>>) = 
            this.RunAsEnumerable q

[<AutoOpen>]
module TopLevelValues = 
    let query = FSharpComposableQuery.QueryImpl.QueryBuilder()
    
    query.initMi()

