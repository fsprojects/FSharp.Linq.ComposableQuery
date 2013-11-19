module internal FSharpComposableQuery.Common

open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns
open Microsoft.FSharp.Linq
open Microsoft.FSharp.Reflection
open System.Reflection

type Op = 
    | Plus
    | Minus
    | Times
    | Div
    | Mod
    | Equal
    | Nequal
    | Leq
    | Lt
    | Geq
    | Gt
    | And
    | Or
    | Concat
    | Not // unary
    | Neg // unary
    | Like // SQL

let arity op = 
    match op with
    | Plus | Times | Div | Minus | And | Or | Mod | Equal | Nequal | Gt | Geq | Lt | Leq | Concat | Like -> 
        2
    | Neg | Not -> 1

let getOpType op = 
    match op with
    | Plus | Times | Div | Minus | Mod -> typeof<int>
    | Equal | Nequal | Gt | Geq | Lt | Leq | And | Or -> typeof<bool>
    | Concat | Like -> typeof<string>
    | Neg -> typeof<int>
    | Not -> typeof<bool>
#if TIMING

let median (l : float list) = 
    if l.Length = 0 then -1.0
    else List.nth l (l.Length / 2)

let timeIt n f = 
    let l = 
        List.map (fun _ -> 
            let timer = new System.Diagnostics.Stopwatch()
            timer.Start()
            let returnValue = f()
            returnValue, timer.Elapsed.TotalMilliseconds) [ 1..n ]
    
    let (vs, ts) = List.unzip l
    (List.head vs, median ts)

let withDuration n f = 
    let returnValue, elapsed = timeIt n f
    returnValue, elapsed

let duration n msg f = 
    let returnValue, elapsed = withDuration n f
    printfn "%s: \t %f ms" msg elapsed
    returnValue
#else
let withDuration _n f = f(), 0.0
let duration _n _msg f = f()
#endif


let UnitTy = typeof<unit>
let IntTy = typeof<int>
let BoolTy = typeof<bool>
let StringTy = typeof<string>
let FunTy(ty1 : System.Type, ty2) = 
    typeof<_ -> _>.GetGenericTypeDefinition().MakeGenericType([| ty1; ty2 |])
let TableTy(ty : System.Type) = 
    typeof<System.Data.Linq.Table<_>>.GetGenericTypeDefinition()
        .MakeGenericType([| ty |])

let (|UnitTy|_|) ty = 
    if ty = typeof<unit> then Some()
    else None

let (|IntTy|_|) ty = 
    if ty = typeof<int> then Some()
    else None

let (|BoolTy|_|) ty = 
    if ty = typeof<bool> then Some()
    else None

let (|StringTy|_|) ty = 
    if ty = typeof<string> then Some()
    else None

let (|FunTy|_|) (ty : System.Type) = 
    if ty.IsGenericType 
       && ty.GetGenericTypeDefinition() = typeof<_ -> _>
              .GetGenericTypeDefinition() then 
        Some(ty.GetGenericArguments().[0], ty.GetGenericArguments().[1])
    else None

let (|TableTy|_|) (ty : System.Type) = 
    if ty.IsGenericType 
       && ty.GetGenericTypeDefinition() = typeof<System.Data.Linq.Table<_>>
              .GetGenericTypeDefinition() then 
        Some(ty.GetGenericArguments().[0])
    else None

type Field = 
    { name : string
      info : PropertyInfo }

let equalsOn (f : 'T -> bool) x (yobj : obj) = 
    match yobj with
    | :? 'T as y -> (f x = f y)
    | _ -> false

type UnknownThing = 
    | UnknownCall of MethodInfo
    | UnknownNew of ConstructorInfo
    | UnknownRef of Expr
    | UnknownQuote

let unkFreshen x x' unk = 
    match unk with
    | UnknownRef(expr) -> 
        UnknownRef(expr.Substitute(fun y -> 
                       if x = y then Some(Expr.Var(x'))
                       else None))
    | _ -> unk

let castArgs ps args = 
    let tys = List.map (fun (p : ParameterInfo) -> p.ParameterType) (ps)
    
    let coerceIfNeeded (arg : Expr, ty) = 
        if arg.Type = ty then arg
        else Expr.Coerce(arg, ty)
    List.map coerceIfNeeded (List.zip args tys)

let unknownToExpr unk obj0 args = 
    match unk, obj0, args with
    | UnknownNew ci, None, _ -> 
        Expr.NewObject(ci, castArgs (Array.toList (ci.GetParameters())) args)
    | UnknownRef e, None, _ -> e
    | UnknownCall(mi), None, _ -> Expr.Call(mi, args)
    | UnknownCall(mi), Some obj, _ -> 
        Expr.Call(obj, mi, castArgs (Array.toList (mi.GetParameters())) args)
    | UnknownQuote, _, [ e ] -> Expr.Quote(e)
    | _ -> failwith "Impossible case"

type Exp = 
    | EVar of Var
    | ELet of Var * Exp * Exp
    | Op of Op * Exp list
    | IntC of int
    | BoolC of bool
    | StringC of string
    | Unit
    | Tuple of System.Type * Exp list
    | Proj of Exp * int
    | IfThenElse of Exp * Exp * Exp
    | Record of System.Type * (Field * Exp) list
    | Field of Exp * Field
    | Empty of System.Type
    | Singleton of Exp
    | Union of Exp * Exp
    | Comp of Exp * Var * Exp
    | Exists of Exp
    | Lam of Var * Exp
    | App of Exp * Exp
    | Table of Expr * System.Type
    | Unknown of UnknownThing * System.Type * Exp option * Exp list

exception NYI

let tag = ref 0

let fresh (x : Var) = 
    tag := !tag + 1
    let newname = x.Name.Split('_').[0] + "_" + string (!tag)
    new Var(newname, x.Type, x.IsMutable)

// TODO: Pair var's up with integer tags
let rec freshen x x' e0 = 
    match e0 with
    | EVar y -> 
        if x = y then (EVar x')
        else EVar y
    | App(e1, e2) -> App(freshen x x' e1, freshen x x' e2)
    | Lam(y, e1) -> Lam(y, freshen x x' e1) // binding
    | Op(op, es) -> Op(op, List.map (freshen x x') es)
    | ELet(y, e1, e2) -> ELet(y, freshen x x' e1, freshen x x' e2) // binding
    | IntC i -> IntC i
    | BoolC b -> BoolC b
    | StringC s -> StringC s
    | Unit -> Unit
    | Tuple(tty, es) -> Tuple(tty, List.map (freshen x x') es)
    | Proj(e0, i) -> Proj(freshen x x' e0, i)
    | IfThenElse(e0, e1, e2) -> 
        IfThenElse(freshen x x' e0, freshen x x' e1, freshen x x' e2)
    | Record(rty, les) -> 
        Record(rty, List.map (fun (l, el) -> (l, freshen x x' el)) les)
    | Field(e0, l) -> Field(freshen x x' e0, l)
    | Empty ty -> Empty ty
    | Singleton(e0) -> Singleton(freshen x x' e0)
    | Union(e1, e2) -> Union(freshen x x' e1, freshen x x' e2)
    | Comp(e2, y, e1) -> // binding
                         
        Comp(freshen x x' e2, y, freshen x x' e1)
    | Exists(e0) -> Exists(freshen x x' e0)
    | Table(expr, ty) -> 
        Table(expr.Substitute(fun y -> 
                  if x = y then Some(Expr.Var(x'))
                  else None), ty)
    | Unknown(unk, ty, eopt, es) -> 
        Unknown
            (unkFreshen x x' unk, ty, Option.map (freshen x x') eopt, 
             List.map (freshen x x') es)

let rec subst e x e0 = 
    match e0 with
    | EVar y -> 
        if x = y then e
        else EVar y
    | App(e1, e2) -> App(subst e x e1, subst e x e2)
    | Lam(y, e1) -> 
        let y' = fresh (y)
        Lam(y', subst e x (freshen y y' e1)) // binding
    | Op(op, es) -> Op(op, List.map (subst e x) es)
    | ELet(y, e1, e2) -> 
        let y' = fresh (y)
        ELet(y', subst e x e1, subst e x (freshen y y' e2)) // binding
    | IntC i -> IntC i
    | BoolC b -> BoolC b
    | StringC s -> StringC s
    | Unit -> Unit
    | Tuple(tty, es) -> Tuple(tty, List.map (subst e x) es)
    | Proj(e0, i) -> Proj(subst e x e0, i)
    | IfThenElse(e0, e1, e2) -> 
        IfThenElse(subst e x e0, subst e x e1, subst e x e2)
    | Record(rty, les) -> 
        Record(rty, List.map (fun (l, el) -> (l, subst e x el)) les)
    | Field(e0, l) -> Field(subst e x e0, l)
    | Empty ty -> Empty ty
    | Singleton(e0) -> Singleton(subst e x e0)
    | Union(e1, e2) -> Union(subst e x e1, subst e x e2)
    | Comp(e2, y, e1) -> // binding
                         
        let y' = fresh y
        Comp(subst e x (freshen y y' e2), y', subst e x e1)
    | Exists(e0) -> Exists(subst e x e0)
    | Table(expr, ty) -> Table(expr, ty)
    | Unknown(unk, ty, eopt, es) -> 
        match unk with
        | UnknownRef(e) -> 
            if Seq.exists (fun y -> y = x) (e.GetFreeVars()) then 
                failwithf "Cannot substitute %A for %A in %A" e x e0
            else ()
        | _ -> ()
        Unknown(unk, ty, Option.map (subst e x) eopt, List.map (subst e x) es)

type UnitRecord = 
    { unit : int }

// translates tuples away
let rec elimTuples exp = 
    match exp with
    | EVar y -> EVar y
    | App(e1, e2) -> App(elimTuples e1, elimTuples e2)
    | Lam(y, e1) -> Lam(y, elimTuples e1)
    | Op(op, es) -> Op(op, List.map (elimTuples) es)
    | ELet(y, e1, e2) -> ELet(y, elimTuples e1, elimTuples e2)
    | IntC i -> IntC i
    | BoolC b -> BoolC b
    | StringC s -> StringC s
    | Unit -> Unit // todo: use dummy record
    | Tuple(tty, es) -> Tuple(tty, List.map (elimTuples) es) // todo: use dummy records
    | Proj(e0, i) -> Proj(elimTuples e0, i) // todo: use dummy record fields
    | IfThenElse(e0, e1, e2) -> 
        IfThenElse(elimTuples e0, elimTuples e1, elimTuples e2)
    | Record(rty, les) -> 
        Record(rty, List.map (fun (l, el) -> (l, elimTuples el)) les)
    | Field(e0, l) -> Field(elimTuples e0, l)
    | Empty ty -> Empty ty
    | Singleton(e0) -> Singleton(elimTuples e0)
    | Union(e1, e2) -> Union(elimTuples e1, elimTuples e2)
    | Comp(e2, y, e1) -> // binding
                         
        Comp(elimTuples e2, y, elimTuples e1)
    | Exists(e0) -> Exists(elimTuples e0)
    | Table(expr, ty) -> Table(expr, ty)
    | Unknown(unk, ty, eopt, es) -> 
        Unknown(unk, ty, Option.map elimTuples eopt, List.map (elimTuples) es)

let (|RecordWith|_|) l = 
    function 
    | (Record(_rty, r)) -> 
        match List.tryFind (fun (l', _) -> l = l') r with
        | Some(_, e) -> Some(e)
        | None -> None
    | _ -> None

let rec getMethodInfo q = 
    match q with
    | (Patterns.Call(_, info, _)) -> info
    | Patterns.Lambda(_x, q) -> getMethodInfo q
    | Patterns.Let(_, _, q) -> getMethodInfo q
    | Patterns.Coerce(q, _) -> getMethodInfo q
    | Patterns.LetRecursive(_, q) -> getMethodInfo q
    | _ -> failwithf "Unexpected method %A" q

let getGenericMethodInfo' (methodInfo : System.Reflection.MethodInfo) = 
    if methodInfo.IsGenericMethod then methodInfo.GetGenericMethodDefinition()
    else methodInfo

let getGenericMethodInfo q = getGenericMethodInfo' (getMethodInfo q)

(* Method recognition stuff *)

let plusMi = getGenericMethodInfo <@@ (+) @@>
let minusMi = getGenericMethodInfo <@@ (-) @@>
let unaryMinusMi = getGenericMethodInfo <@@ fun x -> -x @@>
let timesMi = getGenericMethodInfo <@@ (*) @@>
let divMi = getGenericMethodInfo <@@ (/) @@>
let modMi = getGenericMethodInfo <@@ (%) @@>
let eqMi = getGenericMethodInfo <@@ (=) @@>
let neqMi = getGenericMethodInfo <@@ (<>) @@>
let gtMi = getGenericMethodInfo <@@ (>) @@>
let geqMi = getGenericMethodInfo <@@ (>=) @@>
let ltMi = getGenericMethodInfo <@@ (<) @@>
let leqMi = getGenericMethodInfo <@@ (<=) @@>
let strconcatMi = getGenericMethodInfo <@@ (^) @@>
let likeMi = getGenericMethodInfo <@@ fun x y -> System.Data.Linq.SqlClient.SqlMethods.Like(x, y) @@>
let andMi = getGenericMethodInfo <@@ (&&) @@>
let orMi = getGenericMethodInfo <@@ (||) @@>
let notMi = getGenericMethodInfo <@@ (not) @@>
let apprMi = getGenericMethodInfo <@@ (|>) @@>
let applMi = getGenericMethodInfo <@@ (<|) @@>

let getBinOp binop (e1 : Expr) (e2 : Expr) = 
    match binop, e1.Type, e2.Type with
    | Plus, IntTy, IntTy -> 
        Expr.Call
            (plusMi.MakeGenericMethod([| IntTy; IntTy; IntTy |]), [ e1; e2 ])
    | Times, IntTy, IntTy -> 
        Expr.Call
            (timesMi.MakeGenericMethod([| IntTy; IntTy; IntTy |]), [ e1; e2 ])
    | Minus, IntTy, IntTy -> 
        Expr.Call
            (minusMi.MakeGenericMethod([| IntTy; IntTy; IntTy |]), [ e1; e2 ])
    | Div, IntTy, IntTy -> 
        Expr.Call
            (divMi.MakeGenericMethod([| IntTy; IntTy; IntTy |]), [ e1; e2 ])
    | Mod, IntTy, IntTy -> 
        Expr.Call
            (modMi.MakeGenericMethod([| IntTy; IntTy; IntTy |]), [ e1; e2 ])
    | Equal, ty1, ty2 -> 
        assert (ty1 = ty2)
        Expr.Call(eqMi.MakeGenericMethod([| ty1 |]), [ e1; e2 ])
    | Nequal, ty1, ty2 -> 
        assert (ty1 = ty2)
        Expr.Call(neqMi.MakeGenericMethod([| ty1 |]), [ e1; e2 ])
    | Lt, ty1, ty2 -> 
        assert (ty1 = ty2)
        Expr.Call(ltMi.MakeGenericMethod([| ty1 |]), [ e1; e2 ])
    | Leq, ty1, ty2 -> 
        assert (ty1 = ty2)
        Expr.Call(leqMi.MakeGenericMethod([| ty1 |]), [ e1; e2 ])
    | Gt, ty1, ty2 -> 
        assert (ty1 = ty2)
        Expr.Call(gtMi.MakeGenericMethod([| ty1 |]), [ e1; e2 ])
    | Geq, ty1, ty2 -> 
        assert (ty1 = ty2)
        Expr.Call(geqMi.MakeGenericMethod([| ty1 |]), [ e1; e2 ])
    | Concat, StringTy, StringTy -> Expr.Call(strconcatMi, [ e1; e2 ])
    | Like, StringTy, StringTy -> Expr.Call(likeMi, [ e1; e2 ])
    | And, BoolTy, BoolTy -> Expr.Call(andMi, [ e1; e2 ])
    | Or, BoolTy, BoolTy -> Expr.Call(orMi, [ e1; e2 ])
    | _ -> failwith "not yet implemented"

let getUnOp unop (e : Expr) = 
    match unop, e.Type with
    | Not, BoolTy -> Expr.Call(notMi, [ e ])
    | Neg, IntTy -> 
        Expr.Call(unaryMinusMi.MakeGenericMethod([| IntTy |]), [ e ])
    | _ -> failwith "not yet implemented"

let (|BinOp|_|) exp = 
    match exp with
    | Op(op, [ e1; e2 ]) when arity op = 2 -> Some(e1, op, e2)
    | _ -> None

let (|UnOp|_|) exp = 
    match exp with
    | Op(op, [ e ]) when arity op = 1 -> Some(op, e)
    | _ -> None