module internal FSharpComposableQuery.Expr
//
////open FSharpComposableQuery.SQL
//open Microsoft.FSharp.Quotations
//open Microsoft.FSharp.Quotations.Patterns
//open Microsoft.FSharp.Quotations.DerivedPatterns
//open Microsoft.FSharp.Linq
//open FSharpComposableQuery.Common
//open Microsoft.FSharp.Reflection
//open System.Reflection
//
//// TODO: Implement flat FSharpComposableQuery -> SQL translation; compare with SQL Server queries
//// TODO: Add support for idiomatic grouping/aggregation
//// TODO: Improve performance of fromExpr
//// TODO: Comments to relate code to reductions in paper
//
//let SeqTy(ty : System.Type) = 
//    typeof<seq<_>>.GetGenericTypeDefinition().MakeGenericType([| ty |])
//let GroupTy(ty1 : System.Type, ty2) = 
//    typeof<System.Linq.IGrouping<_, _>>.GetGenericTypeDefinition()
//        .MakeGenericType([| ty1; ty2 |])
//
//let (|SeqTy|_|) (ty : System.Type) = 
//    if ty.IsGenericType 
//       && ty.GetGenericTypeDefinition() = typeof<seq<_>>
//              .GetGenericTypeDefinition() then 
//        Some(ty.GetGenericArguments().[0])
//    else None
//
//let (|FunTy|_|) (ty : System.Type) = 
//    if ty.IsGenericType 
//       && ty.GetGenericTypeDefinition() = typeof<_ -> _>
//              .GetGenericTypeDefinition() then 
//        Some(ty.GetGenericArguments().[0], ty.GetGenericArguments().[1])
//    else None
//
//let (|GroupTy|_|) (ty : System.Type) = 
//    if ty.IsGenericType 
//       && ty.GetGenericTypeDefinition() = typeof<System.Linq.IGrouping<_, _>>
//              .GetGenericTypeDefinition() then 
//        Some(ty.GetGenericArguments().[0], ty.GetGenericArguments().[1])
//    else None
//
//// quick and dirty function to compute result type of an expression
//// does not check expression well-typed; relies on variable annotations
//let rec getType exp = 
//    match exp with
//    | EVar x -> x.Type
//    | ELet(x, e1, e2) -> getType e2
//    | Op(op, es) -> getOpType op
//    | IntC i -> typeof<int>
//    | BoolC b -> typeof<bool>
//    | StringC s -> typeof<string>
//    | Unit -> typeof<unit>
//    | Tuple(tty, _) -> tty
//    | Proj(e, i) -> 
//        let tty = getType e
//        FSharpType.GetTupleElements(tty).[i]
//    | IfThenElse(e, e1, e2) -> getType e1
//    | Record(rty, _) -> rty
//    | Field(e, f) -> f.info.PropertyType
//    | Empty ty -> SeqTy ty
//    | Singleton e -> SeqTy(getType e)
//    | Union(e1, e2) -> getType e1
//    | Comp(e2, x, e1) -> getType e2
//    | Exists(e) -> typeof<bool>
//    | Lam(x, e) -> FunTy(x.Type, getType (e))
//    | App(e1, e2) -> 
//        match getType (e1) with
//        | FunTy(ty1, ty2) -> ty2
//        | ty -> failwithf "Unexpected type %A" ty
//    | Table(e, ty) -> SeqTy ty
//    | Unknown(unk, ty, eopt, es) -> ty
//
//let rec reduce exp = 
//    match exp with
//    | EVar x -> EVar x
//    | ELet(x, e1, e2) -> reduce (subst e1 x e2)
//    | Op(op, es) -> Op(op, List.map reduce es)
//    | IntC i -> IntC i
//    | BoolC b -> BoolC b
//    | StringC s -> StringC s
//    | Unit -> Unit
//    | Tuple(tty, es) -> Tuple(tty, List.map reduce es)
//    | Proj(e, i) -> 
//        match reduce e with
//        | Tuple(_tty, es) -> List.nth es i
//        | IfThenElse(m, n1, n2) -> 
//            reduce (IfThenElse(m, Proj(n1, i), Proj(n2, i)))
//        | n -> Proj(n, i)
//    | IfThenElse(e, e1, e2) -> 
//        match reduce e with
//        | BoolC true -> reduce e1
//        | BoolC false -> reduce e2
//        | n -> 
//            match (getType e1) with
//            | SeqTy ty -> reduceIfThenElseSeq n (reduce e1) (reduce e2) ty
//            | _ -> IfThenElse(n, reduce e1, reduce e2)
//    | Record(rty, r) -> Record(rty, List.map (fun (l, e) -> (l, reduce e)) r)
//    | Field(e, l) -> 
//        match reduce e with
//        | RecordWith l e' -> e'
//        | IfThenElse(m, n1, n2) -> 
//            reduce (IfThenElse(m, Field(n1, l), Field(n2, l)))
//        | n -> Field(n, l)
//    | Empty ty -> Empty ty
//    | Singleton e -> Singleton(reduce e)
//    | Union(e1, e2) -> Union(reduce e1, reduce e2)
//    | Comp(e2, x, e1) -> 
//        match reduce e1 with
//        | Empty ty -> 
//            match (getType e2) with
//            | SeqTy ty -> Empty ty
//            | ty -> failwithf "Unhandled type %A" ty
//        | Singleton m -> reduce (subst m x e2)
//        | Union(m1, m2) -> 
//            Union(reduce (Comp(e2, x, m1)), reduce (Comp(e2, x, m2)))
//        | Comp(m2, y, m1) -> 
//            let (y', m2') = 
//                if x <> y then (y, m2)
//                else 
//                    let y' = fresh (y)
//                    (y', freshen y y' m2)
//            reduce (Comp(Comp(e2, x, m2), y, m1))
//        | IfThenElse(m, n1, n2) -> 
//            IfThenElse(m, reduce (Comp(e2, x, n1)), reduce (Comp(e2, x, n2)))
//        | n -> Comp(reduce e2, x, n)
//    | Lam(x, e) -> Lam(x, reduce e)
//    | App(e1, e2) -> 
//        match reduce e1 with
//        | Lam(x, m) -> reduce (subst e2 x m)
//        | IfThenElse(m, n1, n2) -> 
//            reduce (IfThenElse(m, App(n1, e2), App(n2, e2)))
//        | n -> App(n, reduce e2)
//    | Exists e -> 
//        match getType e with
//        | SeqTy(UnitTy) -> Exists(reduce e) // TODO
//        | SeqTy ty -> 
//            Exists(Comp(Singleton(Unit), new Var("__dummy", ty), reduce e))
//        | ty -> failwithf "Unhandled type %A" ty
//    | Table(e, ty) -> Table(e, ty)
//    | Unknown(unk, ty, eopt, es) -> 
//        Unknown(unk, ty, Option.map reduce eopt, List.map reduce es)
//
//and reduceIfThenElseSeq test thenExp elseExp ty = 
//    match thenExp, elseExp with
//    | Empty _, Empty _ -> Empty ty
//    | Comp(n, x, m), Empty _ -> Comp(IfThenElse(test, n, Empty ty), x, m)
//    | Union(m, n), Empty _ -> 
//        Union(IfThenElse(test, m, Empty ty), IfThenElse(test, n, Empty ty))
//    | _, Empty _ -> IfThenElse(test, thenExp, Empty ty)
//    | m, n -> 
//        Union
//            (IfThenElse(test, m, Empty ty), 
//             IfThenElse(Op(Not, [ test ]), n, Empty ty))
//
//let rec nf exp = 
//    let exp' = reduce exp
//    if exp = exp' then exp'
//    else nf exp'
//
//let recognizeNF exp = 
//    let rec recognizeUnion exp = 
//        match exp with
//        | Empty _ -> true
//        | Union(e1, e2) -> recognizeUnion e1 && recognizeUnion e2
//        | f -> recognizeFrom f
//    
//    and recognizeFrom exp = 
//        match exp with
//        | Comp(f, x, Table(e, ty)) -> recognizeFrom f
//        | w -> recognizeWhere w
//    
//    and recognizeWhere exp = 
//        match exp with
//        | IfThenElse(b, w, Empty _) -> recognizeBase b && recognizeWhere w
//        | Singleton r -> recognizeRecord r
//        | _ -> 
//            printfn "Warning: not where clause %A" exp
//            false
//    
//    and recognizeRecord exp = 
//        match exp with
//        | Record(_, les) -> List.forall (fun (l, e) -> recognizeBase e) les
//        | Tuple(_, es) -> List.forall recognizeBase es
//        | Unit -> true
//        | EVar x -> true // not a normalized record though...
//        | _ -> 
//            printfn "Warning: not record %A" exp
//            false
//    
//    and recognizeBase exp = 
//        match exp with
//        | IfThenElse(e, e1, e2) -> 
//            recognizeBase e && recognizeBase e1 && recognizeBase e2
//        | Exists e -> recognizeUnion e
//        | Field(EVar x, l) -> true
//        | Proj(EVar x, i) -> true
//        | IntC _ -> true
//        | BoolC _ -> true
//        | StringC _ -> true
//        | Unit -> true
//        | Op(op, es) -> List.forall recognizeBase es
//        | EVar x -> true
//        | _ -> 
//            printfn "Warning: not base %A" exp
//            false
//    
//    recognizeUnion exp
//
//type Func = 
//    | AppendF
//    | SingletonF
//    | EmptyF
//    | CollectF
//    | ConcatF
//    | MapF
//    | ExistsF
//    | ForallF
//    | IsEmptyF
//    | FilterF
//    | WhereF
//    | SeqF
//    | DelayF
//    | MinusF
//    | PlusF
//    | TimesF
//    | DivF
//    | ModF
//    | EqF
//    | NeqF
//    | LtF
//    | LeqF
//    | GtF
//    | GeqF
//    | StrConcatF
//    | AndF
//    | OrF
//    | NotF
//    | AppRF
//    | AppLF
//
//let appendMi = getGenericMethodInfo <@@ Seq.append @@>
//let singletonMi = getGenericMethodInfo <@@ Seq.singleton @@>
//let emptyMi = getGenericMethodInfo <@@ Seq.empty @@>
//let collectMi = getGenericMethodInfo <@@ Seq.collect @@>
//let concatMi = getGenericMethodInfo <@@ Seq.concat @@>
//let mapMi = getGenericMethodInfo <@@ Seq.map @@>
//let existsMi = getGenericMethodInfo <@@ Seq.exists @@>
//let forallMi = getGenericMethodInfo <@@ Seq.forall @@>
//let isEmptyMi = getGenericMethodInfo <@@ Seq.isEmpty @@>
//let filterMi = getGenericMethodInfo <@@ Seq.filter @@>
//let whereMi = getGenericMethodInfo <@@ Seq.where @@>
//let seqMi = getGenericMethodInfo <@@ seq @@>
//let delayMi = getGenericMethodInfo <@@ Seq.delay @@>
//
//let recognizeFunc (methodInfo' : System.Reflection.MethodInfo) = 
//    let methodInfo = getGenericMethodInfo' methodInfo'
//    if methodInfo = appendMi then AppendF
//    else if methodInfo = singletonMi then SingletonF
//    else if methodInfo = emptyMi then EmptyF
//    else if methodInfo = collectMi then CollectF
//    else if methodInfo = concatMi then ConcatF
//    else if methodInfo = mapMi then MapF
//    else if methodInfo = existsMi then ExistsF
//    else if methodInfo = forallMi then ForallF
//    else if methodInfo = emptyMi then IsEmptyF
//    else if methodInfo = filterMi then FilterF
//    else if methodInfo = whereMi then WhereF
//    else if methodInfo = seqMi then SeqF
//    else if methodInfo = delayMi then DelayF
//    else if methodInfo = unaryMinusMi then MinusF
//    else if methodInfo = plusMi then PlusF
//    else if methodInfo = minusMi then MinusF
//    else if methodInfo = timesMi then TimesF
//    else if methodInfo = divMi then DivF
//    else if methodInfo = modMi then ModF
//    else if methodInfo = eqMi then EqF
//    else if methodInfo = neqMi then NeqF
//    else if methodInfo = ltMi then LtF
//    else if methodInfo = leqMi then LeqF
//    else if methodInfo = gtMi then GtF
//    else if methodInfo = geqMi then GeqF
//    else if methodInfo = strconcatMi then StrConcatF
//    else if methodInfo = andMi then AndF
//    else if methodInfo = orMi then OrF
//    else if methodInfo = notMi then NotF
//    else if methodInfo = apprMi then AppRF
//    else if methodInfo = applMi then AppLF
//    else failwithf "unhandled methodInfo: %A" methodInfo
//
//// could add these to expr as extension methods
//let emptyExp ty = Expr.Call(emptyMi.MakeGenericMethod([| ty |]), [])
//let singletonExp (e : Expr) = 
//    Expr.Call(singletonMi.MakeGenericMethod([| e.Type |]), [ e ])
//
//let unionExp (e1 : Expr) (e2 : Expr) = 
//    match e1.Type, e2.Type with
//    | SeqTy ty1, SeqTy ty2 -> 
//        assert (ty1 = ty2)
//        Expr.Call(appendMi.MakeGenericMethod([| ty1 |]), [ e1; e2 ])
//    | ty1, ty2 -> failwithf "Unhandled types %A %A" ty1 ty2
//
//let existsExp (e' : Expr) = 
//    match e'.Type with
//    | SeqTy tyinner -> 
//        Expr.Call(existsMi.MakeGenericMethod([| tyinner |]), 
//                  [ Expr.Lambda(new Var("__dummy", tyinner), Expr.Value true)
//                    e' ])
//    | _ -> failwithf "Unexpected type %A" e'.Type
//
//let compExp (e2 : Expr) (x : Var) (e1 : Expr) = 
//    match e1.Type, e2.Type with
//    | SeqTy(tyCollection), SeqTy(tyResult) -> 
//        assert (x.Type = tyCollection)
//        Expr.Call(collectMi.MakeGenericMethod([| x.Type; e2.Type; tyResult |]), 
//                  [ Expr.Lambda(x, e2)
//                    e1 ])
//    | ty1, ty2 -> failwithf "Unhandled types %A %A" ty1 ty2
//
//let toExpr exp = 
//    let rec toExp exp = 
//        match exp with
//        | IntC x -> Expr.Value(x, typeof<int>)
//        | BoolC b -> Expr.Value(b, typeof<bool>)
//        | StringC s -> Expr.Value(s, typeof<string>)
//        | Unit -> Expr.Value((), typeof<unit>)
//        | Tuple(tty, es) -> Expr.NewTuple(List.map toExp es)
//        | Proj(e, i) -> Expr.TupleGet(toExp e, i)
//        | IfThenElse(e, e1, e2) -> Expr.IfThenElse(toExp e, toExp e1, toExp e2)
//        | EVar x -> Expr.Var x
//        | ELet(x, e1, e2) -> Expr.Let(x, toExp e1, toExp e2)
//        | BinOp(e1, binop, e2) -> getBinOp binop (toExp e1) (toExp e2)
//        | UnOp(unop, e) -> getUnOp unop (toExp e)
//        | Field(e, l) -> Expr.PropertyGet(toExp e, l.info, [])
//        | Record(rty, r) -> 
//            Expr.NewRecord(rty, List.map (fun (_, e) -> toExp e) r)
//        | Lam(x, e) -> Expr.Lambda(x, toExp e)
//        | App(e1, e2) -> Expr.Application(toExp e1, toExp e2)
//        | Empty ty -> emptyExp ty
//        | Union(e1, e2) -> unionExp (toExp e1) (toExp e2)
//        | Singleton e -> singletonExp (toExp e)
//        | Comp(e2, x, e1) -> compExp (toExp e2) x (toExp e1)
//        | Exists(e) -> existsExp (toExp e)
//        | Table(e, ty) -> Expr.Coerce(e, SeqTy ty)
//        | Unknown(unk, _, eopt, es) -> 
//            unknownToExpr unk (Option.map toExp eopt) (List.map toExp es)
//        | _ -> failwithf "Unhandled expression %A" exp
//    toExp exp
//
//let fromExpr expr = 
//    let rec from expr = 
//        match expr with
//        | Int32(n) -> IntC n
//        | DerivedPatterns.Bool(b) -> BoolC b
//        | String(str) -> StringC str
//        | Var(var) -> EVar(var)
//        | DerivedPatterns.Unit -> Unit
//        | Value(value, typ) -> failwith (sprintf "Value %A %A" value typ)
//        | Lambda(param, body) -> Lam(param, from body)
//        | Application(e1, e2) -> App(from e1, from e2)
//        | Patterns.Coerce(PropertyGet(Some(PropertyGet(None, db, [])), tbl, []) as e, 
//                          SeqTy ty) -> Table(e, ty) // recognize free vars as db table refs
//        | Patterns.Coerce(PropertyGet(None, tbl, []) as e, SeqTy ty) -> 
//            Table(e, ty) // recognize free vars as db table refs
//        | Patterns.Coerce(e, ty) -> from e // catchall to ignore other coercions
//        | PropertyGet(Some(PropertyGet(None, db, [])), _tbl, []) as e -> 
//            match e.Type with
//            | SeqTy ty -> Table(e, ty) // assume it's a db table ref
//            | TableTy ty -> Table(e, ty) // assume it's a db table ref
//            | ty' -> failwithf "Unexpected table reference %A %A" e ty'
//        | Patterns.PropertyGet(Some(e), l, []) -> 
//            Field(from e, 
//                  { name = l.Name
//                    info = l }) // otherwise assume field ref
//        | Patterns.NewRecord(rty, values) -> 
//            let fields = 
//                Reflection.FSharpType.GetRecordFields rty |> Array.toList
//            Record(rty, 
//                   List.zip (List.map (fun (f : PropertyInfo) -> 
//                                 { name = f.Name
//                                   info = f }) fields) (List.map from values))
//        | Patterns.IfThenElse(e, e1, e2) -> IfThenElse(from e, from e1, from e2)
//        | Patterns.Let(var, expr1, expr2) -> ELet(var, from expr1, from expr2)
//        | Patterns.TupleGet(expr, i) -> Proj(from expr, i)
//        | Patterns.NewTuple(es) -> Tuple(expr.GetType(), List.map from es)
//        | Call(obj, methodInfo, args) -> 
//            handleSpecificCall obj methodInfo args expr.Type
//        | expr -> failwithf "unhandled expr: %A" expr
//    
//    and handleSpecificCall obj func args ty = 
//        match (recognizeFunc func), args with
//        | AppendF, [ e1; e2 ] -> Union(from e1, from e2)
//        | SingletonF, [ e ] -> Singleton(from e)
//        | EmptyF, [] -> 
//            match ty with
//            | SeqTy ty -> Empty(ty)
//            | _ -> failwithf "Unexpected type %A" ty
//        | CollectF, [ e1; e2 ] -> 
//            match e1.Type with
//            | FunTy(ty, _) -> 
//                let x = fresh (new Var("x", ty))
//                Comp(App(from e1, EVar x), x, from e2)
//            | _ -> failwithf "Unexpected type %A" e1.Type
//        | ConcatF, [ e ] -> 
//            match e.Type with
//            | SeqTy(ty) -> 
//                let x = fresh (new Var("x", ty))
//                Comp(EVar x, x, from e)
//            | _ -> failwithf "Unexpected type %A" e.Type
//        | MapF, [ e1; e2 ] -> 
//            match e1.Type with
//            | FunTy(ty, _) -> 
//                let x = fresh (new Var("x", ty))
//                Comp(Singleton(App(from e1, EVar x)), x, from e2)
//            | _ -> failwithf "Unexpected type %A" e1.Type
//        | ExistsF, [ e1; e2 ] -> 
//            match e1.Type with
//            | FunTy(ty, _) -> 
//                let x = fresh (new Var("x", ty))
//                let px = App(from e1, EVar x)
//                let unitIfPx = 
//                    IfThenElse(px, Singleton(Unit), Empty(typeof<bool>))
//                Exists(Comp(unitIfPx, x, from e2))
//            | _ -> failwithf "Unexpected type %A" e1.Type
//        | ForallF, [ e1; e2 ] -> 
//            match e1.Type with
//            | FunTy(ty, _) -> 
//                let x = fresh (new Var("x", ty))
//                let notPx = Op(Not, [ App(from e1, EVar x) ])
//                let unitIfNotPx = 
//                    IfThenElse(notPx, Singleton(Unit), Empty(typeof<bool>))
//                Op(Not, [ Exists(Comp(unitIfNotPx, x, from e2)) ])
//            | _ -> failwithf "Unexpected type %A" e1.Type
//        | IsEmptyF, [ e ] -> Op(Not, [ Exists(from e) ])
//        | FilterF, [ e1; e2 ] | WhereF, [ e1; e2 ] -> 
//            match e1.Type with
//            | FunTy(ty, _) -> 
//                let x = fresh (new Var("x", ty))
//                Comp
//                    (IfThenElse
//                         (App(from e1, EVar x), Singleton(EVar x), Empty(ty)), x, 
//                     from e2)
//            | _ -> failwithf "Unexpected type %A" e1.Type
//        | SeqF, [ e ] -> from e
//        | DelayF, [ Lambda(_, e) ] -> from e
//        | PlusF, [ e1; e2 ] -> 
//            Op(Plus, 
//               [ from e1
//                 from e2 ])
//        | MinusF, [ e1; e2 ] -> 
//            Op(Minus, 
//               [ from e1
//                 from e2 ])
//        | TimesF, [ e1; e2 ] -> 
//            Op(Times, 
//               [ from e1
//                 from e2 ])
//        | DivF, [ e1; e2 ] -> 
//            Op(Div, 
//               [ from e1
//                 from e2 ])
//        | ModF, [ e1; e2 ] -> 
//            Op(Mod, 
//               [ from e1
//                 from e2 ])
//        | EqF, [ e1; e2 ] -> 
//            Op(Equal, 
//               [ from e1
//                 from e2 ])
//        | NeqF, [ e1; e2 ] -> 
//            Op(Nequal, 
//               [ from e1
//                 from e2 ])
//        | GtF, [ e1; e2 ] -> 
//            Op(Gt, 
//               [ from e1
//                 from e2 ])
//        | GeqF, [ e1; e2 ] -> 
//            Op(Geq, 
//               [ from e1
//                 from e2 ])
//        | LtF, [ e1; e2 ] -> 
//            Op(Lt, 
//               [ from e1
//                 from e2 ])
//        | LeqF, [ e1; e2 ] -> 
//            Op(Leq, 
//               [ from e1
//                 from e2 ])
//        | StrConcatF, [ e1; e2 ] -> 
//            Op(Concat, 
//               [ from e1
//                 from e2 ])
//        | AndF, [ e1; e2 ] -> 
//            Op(And, 
//               [ from e1
//                 from e2 ])
//        | OrF, [ e1; e2 ] -> 
//            Op(Or, 
//               [ from e1
//                 from e2 ])
//        | NotF, [ e ] -> Op(Not, [ from e ])
//        | MinusF, [ e ] -> Op(Neg, [ from e ])
//        | AppRF, [ e1; e2 ] -> App(from e2, from e1)
//        | AppLF, [ e1; e2 ] -> App(from e1, from e2)
//        // Extensions: groupby etc. uninterpreted
//        | _, args -> 
//            Unknown
//                (UnknownCall(func), ty, Option.map from obj, List.map from args)
//    
//    from expr
//
//
//    // TODO: clean this stuff up, export or remove it
//
//let testNF nrcExp = 
//    let nrc_nf = nf nrcExp
//    printfn "NRC_NF: %A" nrc_nf
//    if recognizeNF nrc_nf then printfn "In normal form"
//    else printfn "Not in normal form!"
//    nrc_nf
//
//let test f expr = 
//    let nrcExp = fromExpr expr
//    printfn "NRC: %A" nrcExp
//    let nrc_f = f nrcExp
//    printfn "NRC': %A" nrc_f
//    let result1 = toExpr nrc_f
//    printfn "Expr': %A" result1
//    result1
//
//let testSeq f (expr : Expr<'T>) = 
//    printfn "Expr: %A" expr
//    let result1 = test f expr
//    printfn "Result1: %A" result1
//    let result = Expr.Cast<'T>(result1)
//    printfn "Result: %A" result
//    result
//
//let testSeqNF expr = testSeq testNF expr
//let testSeqID expr = testSeq id expr
//
////let testQuery expr = testSeqNF expr |> Query.query
////let testQueryID expr = testSeqID expr |> Query.query
//let nf_expr (expr : Expr<'T>) = 
//    let nrcExp = fromExpr expr
//    let nrc_nf = nf nrcExp
//    //if not(recognizeNF nrc_nf )
//    //    then printfn "Warning: Not in normal form!"
//    let result = toExpr nrc_nf
//    Expr.Cast<'T>(result)
//
////let runQuery expr =  Query.query (nf_expr expr)
//let time_nf_expr (expr : Expr<seq<'T>>) = 
//    let nrcExp = duration 1 "fromExpr" (fun () -> fromExpr expr)
//    let nrc_nf = duration 1 "nf" (fun () -> nf nrcExp)
//    duration 1 "recognizeNF" (fun () -> 
//        if not (recognizeNF nrc_nf) then printfn "Warning: Not in normal form!")
//    let result = duration 1 "toExpr" (fun () -> toExpr nrc_nf)
//    duration 1 "cast" (fun () -> Expr.Cast<seq<'T>>(result))
//
////let timeQuery expr = let q = duration 1 "Query normalization time" (fun () -> nf_expr expr)
////                     in q |> Query.query