namespace FSharpComposableQuery

module internal Helpers = 
    open Microsoft.FSharp.Quotations
    open FSharpComposableQuery.Common

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

    //represents a Field or a Property
    let (|Variable|_|) e =
        match e with
        | Patterns.FieldGet(oe, ty) -> 
            Some ((oe, {
                    name = ty.Name 
                    ty = ty.FieldType  
                    info = ty  
                    isProperty = false }))
        | Patterns.PropertyGet(oe, ty, []) ->
            Some ((oe, {
                    name = ty.Name 
                    ty = ty.PropertyType  
                    info = ty  
                    isProperty = true }))
        | _ -> 
            None

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
    open Microsoft.FSharp.Linq
    open System.Linq
    open System.Reflection

    open FSharpComposableQuery.Common
    open FSharpComposableQuery
    open Helpers

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
        | SelectF
        | ForF
        | ZeroF
        | YieldF
        | UnknownF
        | UnionF
        | RunQueryAsValueF
        | RunQueryAsEnumerableF
        | RunQueryAsQueryableF
        | SourceQueryF
        | SourceEnumF


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
        let rec getType (exp:Exp) = 
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
            | Field(_, f) -> f.ty
            | Empty ty -> QuerySourceTy(ty, IQueryableTy)
            | Singleton e -> QuerySourceTy(getType e, IQueryableTy)
            | Union(e1, _) -> getType e1
            | Comp(e2, _, _) -> getType e2
            | Exists(_) -> typeof<bool>
            | Lam(x, e) -> FunTy(x.Type, getType (e))
            | App(e1, _) -> getFunTy (getType (e1)) (fun _ty1 ty2 -> ty2)
            | Table(_, ty) -> QuerySourceTy(ty, IQueryableTy)
            | Unknown(_, ty, _, _) -> ty
            | Quote(e) -> typedefof<Expr<_>>.MakeGenericType(getType e)
            | Source(mi, _) -> mi.ReturnType
            | RunAsValue(_, ty) -> ty
            | RunAsQueryable(_, ty) -> typedefof<IQueryable<_>>.MakeGenericType ty
            | RunAsEnumerable(_, ty) -> typedefof<seq<_>>.MakeGenericType ty
    
        let rec removeRunAs exp = 
            match exp with
            | Source(_, RunAsQueryable(Quote(e), _)) -> e     // removes occurences of RunQueryAsQueryable. 
//            | RunAsQueryable(Quote(e), _) -> e                    // removes occurences of RunQueryAsQueryable
//            | RunAsValue(Quote(e), _) -> e                    // removes occurences of RunQueryAsValue
            //TODO: also check for RunQueryAsEnumerable?


            | EVar x -> EVar x
            | ELet(x, e1, e2) -> ELet(x, removeRunAs e1, removeRunAs e2)
            | Op(op, es) -> Op(op, List.map removeRunAs es)
            | IntC i -> IntC i
            | BoolC b -> BoolC b
            | StringC s -> StringC s
            | Unit -> Unit
            | Tuple(tty, es) -> Tuple(tty, List.map removeRunAs es)
            | Proj(e, i) -> Proj (removeRunAs e, i)
            | IfThenElse(e, e1, e2) -> IfThenElse (removeRunAs e, removeRunAs e1, removeRunAs e2)
            | Record(rty, r) -> Record(rty, List.map (fun (l, e) -> (l, removeRunAs e)) r)
            | Field(e, l) -> Field(removeRunAs e, l)
            | Empty ty -> Empty ty
            | Singleton e -> Singleton(removeRunAs e)
            | Union(e1, e2) -> Union(removeRunAs e1, removeRunAs e2)
            | Comp(e2, x, e1) -> Comp(removeRunAs e2, x, removeRunAs e1)
            | Lam(x, e) -> Lam(x, removeRunAs e)
            | App(e1, e2) -> App(removeRunAs e1, removeRunAs e2)
            | Exists e -> Exists(removeRunAs e)
            | Table(e, ty) -> Table(e, ty)
            | Unknown(unk, ty, eopt, es) -> 
                Unknown(unk, ty, Option.map removeRunAs eopt, List.map removeRunAs es)
            | RunAsQueryable(e, ty) -> RunAsQueryable(removeRunAs e, ty)
            | RunAsEnumerable(e, ty) -> RunAsEnumerable(removeRunAs e, ty)
            | RunAsValue(e, ty) -> RunAsValue(removeRunAs e, ty)
            | Quote(e1) -> Quote(removeRunAs e1)
            | Source(mi, e1) -> Source(mi, removeRunAs e1)

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
                    reduce (Comp(Comp(e2, x, m2'), y', m1))
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
            | RunAsQueryable(e, ty) -> RunAsQueryable(reduce e, ty)
            | RunAsEnumerable(e, ty) -> RunAsEnumerable(reduce e, ty)
            | RunAsValue(e, ty) -> RunAsValue(reduce e, ty)
            | Quote(e1) -> Quote(reduce e1)
            | Source(mi, e1) -> Source(mi, reduce e1)

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
            
            let expNorm = removeRunAs expR
            
//            assert (reduce expR = expR) //one normalisation pass should be sufficient
            if exp = expNorm then 
                expNorm 
            else 
                Debug.printfn("Norm exp:\n%s\n", (Debug.prettyPrint expNorm))
                nf expNorm
             
        // MethodInfo data
        let yieldMi = getGenericMethodInfo <@ fun (q:QueryBuilder) -> q.Yield @>
        let zeroMi = getGenericMethodInfo <@ fun (q:QueryBuilder) -> q.Zero @>
        let forMi = getGenericMethodInfo <@ fun (q:QueryBuilder) -> q.For @>
//        let existsMi = getGenericMethodInfo  <@ fun (q:QueryBuilderr) -> q.Exists @>
        let whereMi = getGenericMethodInfo  <@ fun (q:QueryBuilder) -> q.Where @>
        let selectMi = getGenericMethodInfo <@ fun (q:QueryBuilder) -> q.Select @>
        let unionQueryMi = getGenericMethodInfo <@ fun (qa : IQueryable<_>) (qb : IQueryable<_>) -> Queryable.Union(qa, qb) @>
        let unionEnumMi = getGenericMethodInfo <@ fun (qa : seq<_>) (qb : seq<_>) -> System.Linq.Enumerable.Union(qa, qb) @>

        let sourceQueryMi = getGenericMethodInfo <@ fun (q:QueryBuilder) (l:IQueryable<_>) -> q.Source(l) @>
        let sourceEnumMi = getGenericMethodInfo <@ fun (q:QueryBuilder) (l:seq<_>) -> q.Source(l) @>

        member internal this.recognizeFunc (methodInfo':MethodInfo) = 
            let methodInfo = getGenericMethodDefinition methodInfo' 
            if methodInfo = yieldMi then YieldF
            else if methodInfo = zeroMi then ZeroF
            else if methodInfo = forMi then ForF
            else if methodInfo = whereMi then WhereF
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
//            else if methodInfo = existsMi then ExistsF
            else if methodInfo = apprMi then AppRF
            else if methodInfo = applMi then AppLF
            else if methodInfo = selectMi then SelectF
            else if methodInfo = unionEnumMi then UnionF
            else if methodInfo = unionQueryMi then UnionF
            else if methodInfo = ForwardDeclarations.RunQueryMi.Enum then RunQueryAsEnumerableF
            else if methodInfo = ForwardDeclarations.RunQueryMi.Value then RunQueryAsValueF
            else if methodInfo = ForwardDeclarations.RunQueryMi.Query then RunQueryAsQueryableF
            else if methodInfo = sourceQueryMi then SourceQueryF
            else if methodInfo = sourceEnumMi then SourceEnumF
            else UnknownF
         
        member internal this.emptyExp ty = 
            Expr.Call(Expr.Value this,zeroMi.MakeGenericMethod([|ty;IQueryableTy|]),[]) 

        member internal this.singletonExp (e:Expr) = 
            Expr.Call(Expr.Value this,yieldMi.MakeGenericMethod( [|e.Type;IQueryableTy|] ), [e])

//        member internal this.existsExp(e':Expr) = 
//            match e'.Type with
//                QuerySourceTy (tyinner,qty) ->    
//                    Expr.Call(Expr.Value this,existsMi.MakeGenericMethod([|tyinner;qty|]), [ e';Expr.Lambda (new Var("__dummy",tyinner),Expr.Value true)])
//              | _ -> failwithf "Unexpected type %A" e'.Type

        member internal this.forExp (e2:Expr) (x:Var) (e1:Expr) = 
            match e1.Type,e2.Type with
                QuerySourceTy (tyCollection,qty1), QuerySourceTy(tyResult,qty2) -> 
                    assert (x.Type = tyCollection)
                    Expr.Call(Expr.Value this, forMi.MakeGenericMethod([|tyCollection;qty1;tyResult;qty2|]), [e1;Expr.Lambda (x,e2)])
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
                | Field(e, l) -> 
                    if (l.isProperty) then
                        Expr.PropertyGet(toExp e, (l.info :?> PropertyInfo), [])
                    else
                        Expr.FieldGet(toExp e, (l.info :?> FieldInfo))
                | Record(rty, r) -> Expr.NewRecord(rty, List.map (fun (_, e) -> toExp e) r)
                | Lam(x, e) -> Expr.Lambda(x, toExp e)
                | App(e1, e2) -> Expr.Application(toExp e1, toExp e2)
                | Empty ty -> this.emptyExp ty
                | Singleton e -> this.singletonExp (toExp e)
                | Comp(e2, x, e1) -> this.forExp (toExp e2) x (toExp e1)
//                | Exists(e) -> this.existsExp (toExp e)
                | Table(e, _ty) -> e
                | Union(e1, e2) ->
                    //a union of 2 queries. Args and results are either IEnumerable<'T> or IQueryable<'T>
                    let e = [toExp e1; toExp e2]

                    let tArgs = e.Head.Type.GetGenericArguments()       //get arg type to cast the generic methodInfo
                    let tDef = e.Head.Type.GetGenericTypeDefinition()

                    if tDef = typedefof<System.Linq.IQueryable<_>> then     //construct the call based on the return type
                        Expr.Call(unionQueryMi.MakeGenericMethod tArgs, e)
                    else
                        assert (tDef = typedefof<seq<_>>)
                        Expr.Call(unionEnumMi.MakeGenericMethod tArgs, e)
                | RunAsQueryable(e, ty) ->
                    let mi = runNativeQueryMi.MakeGenericMethod ty
                    Expr.Call(nativeBuilderExpr, mi, [toExp e])
                | RunAsValue(e, ty) ->
                    let mi = runNativeValueMi.MakeGenericMethod ty
                    Expr.Call(mi, [nativeBuilderExpr; toExp e])
                | RunAsEnumerable(e, ty) ->
                    let mi = runNativeEnumMi.MakeGenericMethod ty
                    Expr.Call(mi, [nativeBuilderExpr; toExp e])
                | Quote(e) -> Expr.Quote(toExp e)
                | Source(mi, e1) ->
                    Expr.Call(nativeBuilderExpr, mi, [toExp e1])

                | Unknown(unk, _, eopt, es) -> 
                    unknownToExpr unk (Option.map toExp eopt) (List.map toExp es)

                | _ -> 
                    raise NYI
            
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
                | Patterns.Quote(e) -> Quote(from e)
                | Lambda(param, body) -> Lam(param, from body)
                | Application(e1, e2) -> App(from e1, from e2)

                //ToDo: should these PropertyGet calls be replaced with Variable calls instead?
                | Patterns.Coerce(PropertyGet(Some(Variable(None, _db)), _tbl, []) as e, QuerySourceTy(ty, _)) ->
                    Table(e, ty)    // recognize free vars as db table refs
                | Patterns.Coerce(PropertyGet(None, _tbl, []) as e, QuerySourceTy(ty, _)) -> 
                    Table(e, ty)    // recognize free vars as db table refs
                | Patterns.Coerce(e, _ty) -> 
                    from e          // catchall to ignore other coercions
                | Patterns.PropertyGet(Some(Variable(None, _db)), _tbl, []) as e -> 
                    match e.Type with
                    | QuerySourceTy(ty, _) -> Table(e, ty)  // assume it's a db table ref
                    | TableTy ty -> Table(e, ty)            // assume it's a db table ref
                    | ty' -> failwithf "Unexpected table reference %A %A" e ty'
                | Variable(Some(e), l) ->
                    Field(from e, l)                        // otherwise assume field ref
                | Variable(None, _) -> 
                    Unknown(UnknownRef(expr), expr.Type, None, []) // otherwise assume global var
                | Patterns.NewRecord(rty, values) -> 
                    let fields = Reflection.FSharpType.GetRecordFields(rty, BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance) |> Array.toList
                    let fieldsR = fields |> List.map (fun f -> { name = f.Name; info = f; ty = f.PropertyType; isProperty = true })
                    Record(rty, List.zip fieldsR (List.map from values))
                | Patterns.NewObject(ci, es) -> 
                    Unknown(UnknownNew(ci), expr.Type, None, List.map from es)
                | Patterns.IfThenElse(e, e1, e2) -> 
                    IfThenElse(from e, from e1, from e2)
                | Patterns.Let(var, expr1, expr2) -> 
                    ELet(var, from expr1, from expr2)
                | Patterns.TupleGet(expr, i) -> 
                    Proj(from expr, i)
                | Patterns.NewTuple(es) -> 
                    Tuple(expr.Type, List.map from es)
                | Patterns.Call(obj, methodInfo, args) -> 
                    handleSpecificCall obj methodInfo args expr.Type
                | expr -> 
                    failwithf "unhandled expr: %A" expr

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
                | UnionF, [e1;e2] ->
                    Union(from e1, from e2)
                    
                | RunQueryAsValueF, [_;e1] -> RunAsValue(from e1, func.ReturnType)
                | RunQueryAsEnumerableF, [_;e1] -> RunAsEnumerable(from e1, func.ReturnType.GetGenericArguments().[0])
                | RunQueryAsQueryableF, [e1] -> RunAsQueryable(from e1, func.ReturnType.GetGenericArguments().[0])
                | SourceEnumF, [e1] ->
                    let ty = func.ReturnType.GetGenericArguments().[0]
                    let mi = sourceEnumMi.MakeGenericMethod(ty)

                    Source(mi, from e1)
                | SourceQueryF, [e1] ->
                    let ty = func.ReturnType.GetGenericArguments()
                    let mi = sourceQueryMi.MakeGenericMethod(ty)

                    Source(mi, from e1)
                | _, args ->                         //pass the call along
                    Unknown(UnknownCall (func), expr_ty, Option.map from obj, List.map from args)
            from expr


        member internal this.Norm (expr:Expr<'T>) : Expr<'T> = 
            let exp = this.fromExpr expr.Raw
            
            Debug.printfn("Initial exp:\n%s\n", (Debug.prettyPrint exp))
            let expNorm = nf exp

            let exprNorm = this.toExpr expNorm
            Expr.Cast<'T>(exprNorm)
        
        member this.Run(q : Expr<Linq.QuerySource<'T, System.Linq.IQueryable>>) : System.Linq.IQueryable<'T> = 
            let qNorm = this.Norm q
            base.Run qNorm
        
        member internal this.RunAsEnumerable(q : Expr<Linq.QuerySource<'T, System.Collections.IEnumerable>>) : seq<'T> = 
            let qNorm = this.Norm q
            base.Run qNorm
        
        member internal this.RunAsValue(q : Expr<'T>) : 'T = 
            let qNorm = this.Norm q
            base.Run qNorm 
        
     
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq
open FSharpComposableQuery.Common

[<AutoOpen>]
module LowPriority = 
    type QueryImpl.QueryBuilder with
        [<CompiledName("RunQueryAsValue")>]
        member this.Run(q : Expr<'T>) = this.RunAsValue q

[<AutoOpen>]
module HighPriority = 
    type QueryImpl.QueryBuilder with
        [<CompiledName("RunQueryAsEnumerable")>]
        member this.Run(q : Expr<QuerySource<'T, System.Collections.IEnumerable>>) = this.RunAsEnumerable q

[<AutoOpen>]
module TopLevelValues = 
    let query = QueryImpl.QueryBuilder()

    ForwardDeclarations.RunQueryMi <- 
        {
            new ForwardDeclarations.IRunQuery with
                member this.Value = getGenericMethodInfo <@ fun (q:QueryImpl.QueryBuilder) (e:Expr<bool>) -> q.Run e @>
                member this.Enum = getGenericMethodInfo <@ fun (q:QueryImpl.QueryBuilder) (e : Expr<QuerySource<_, System.Collections.IEnumerable>>) -> q.Run e @>
                member this.Query = getGenericMethodInfo <@ fun (q:QueryImpl.QueryBuilder) (e : Expr<QuerySource<_, System.Linq.IQueryable>>) -> q.Run e @>
        }