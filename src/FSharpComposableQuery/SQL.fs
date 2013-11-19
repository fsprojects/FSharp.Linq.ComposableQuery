module internal FSharpComposableQuery.SQL

open FSharpComposableQuery.Common

type TableRef = string

type Var = string

type Field = string

type SqlQuery = 
    | UnionAll of SqlComp list

and SqlComp = 
    | Select of SqlRecord * SqlFrom * SqlExp option

and SqlFrom = 
    | From of (Var * TableRef) list

and SqlRecord = (Var * SqlExp) list

and SqlExp = 
    | Field of Var * Field
    | Case of SqlExp * SqlExp * SqlExp
    | Op of Op * SqlExp list
    | Exists of SqlQuery
    | IntC of int
    | BoolC of bool
    | StringC of string

let paren s = "(" + s + ")"

let toSqlBinop op e1 e2 = 
    match op with
    | Plus -> e1 + " + " + e2
    | Minus -> e1 + " - " + e2
    | Times -> e1 + " * " + e2
    | Div -> e1 + " / " + e2
    | Equal -> e1 + " = " + e2
    | Nequal -> e1 + " != " + e2
    | Leq -> e1 + " <= " + e2
    | Lt -> e1 + " < " + e2
    | Geq -> e1 + " >= " + e2
    | Gt -> e1 + " > " + e2
    | And -> e1 + " AND " + e2
    | Or -> e1 + " OR " + e2
    | Mod -> e1 + " % " + e2
    | Concat -> "CONCAT(" + e1 + "," + e2 + ")"
    | _ -> raise NYI

let toSqlUnop op e = 
    match op with
    | Neg -> "-" + e
    | Not -> "NOT(" + e + ")"
    | _ -> raise NYI

let rec toSqlQuery (UnionAll qs) = 
    String.concat " UNION ALL " (List.map toSqlComp qs)

and toSqlComp (Select(r, gs, x)) = 
    "SELECT " + toSqlRecord r + "\n" + "FROM " + toSqlFrom gs + "\n" 
    + (match x with
       | Some e -> "WHERE " + toSqlExp e
       | None -> "")

and toSqlFrom (From gs) = 
    String.concat (" AS ") 
        (List.map (fun (x, t) -> t + " AS " + x.ToString()) gs)

and toSqlRecord (rs) = 
    String.concat ", " 
        (List.map (fun (x, e) -> toSqlExp e + " AS " + x.ToString()) rs)

and toSqlExp exp = 
    match exp with
    | Field(x, f) -> x.ToString() + "." + f
    | Case(e, e1, e2) -> 
        paren 
            ("CASE WHEN " + toSqlExp e + " THEN " + toSqlExp e1 + " ELSE " 
             + toSqlExp e2 + " END")
    | Op(op, [ e1; e2 ]) when arity (op) = 2 -> 
        paren (toSqlBinop op (toSqlExp e1) (toSqlExp e2))
    | Op(op, [ e ]) when arity (op) = 1 -> paren (toSqlUnop op (toSqlExp e))
    | Exists(q) -> paren ("EXISTS " + paren (toSqlQuery q))
    | IntC i -> string i
    | BoolC b -> string b
    | StringC s -> "'" + s + "'"
    | _ -> raise NYI

let getTableName (e, _ty) = e.ToString() // TODO

let conjoinWhere where e = 
    match where with
    | Some e' -> Some(SqlExp.Op(And, [ e; e' ]))
    | None -> Some e

let toSql exp = 
    let rec recognizeUnion exp = 
        match exp with
        | Empty _ -> UnionAll([])
        | Union(e1, e2) -> 
            match recognizeUnion e1, recognizeUnion e2 with
            | UnionAll(q1), UnionAll(q2) -> UnionAll(List.append q1 q2)
        | f -> UnionAll([ recognizeFrom f ])
    
    and recognizeFrom exp = 
        match exp with
        | Comp(f, x, Table(e, ty)) -> 
            match recognizeFrom f with
            | Select(r, From gs, where) -> 
                Select
                    (r, From((x.ToString(), getTableName (e, ty)) :: gs), where)
        | w -> recognizeWhere w
    
    and recognizeWhere exp = 
        match exp with
        | IfThenElse(b, w, Empty _) -> 
            match recognizeWhere w with
            | Select(r, gs, where) -> 
                Select(r, gs, conjoinWhere where (recognizeBase b))
        | Singleton r -> Select(recognizeRecord r, From [], None)
        | _ -> failwithf "Warning: not where clause %A" exp
    
    and recognizeRecord exp = 
        match exp with
        | Record(_, les) -> 
            List.map (fun (l, e) -> (l.name, recognizeBase e)) les
        | Tuple _ -> failwith "tuple not handled"
        | Unit -> failwith "unit not handled"
        | EVar _ -> failwith "record variable not handled"
        | _ -> failwithf "Warning: not record %A" exp
    
    and recognizeBase exp = 
        match exp with
        | IfThenElse(e, e1, e2) -> 
            SqlExp.Case(recognizeBase e, recognizeBase e1, recognizeBase e2)
        | Exp.Exists e -> Exists(recognizeUnion e)
        | Exp.Field(EVar x, l) -> Field(x.ToString(), l.name)
        | Exp.Proj(EVar _, _) -> failwith "proj not handled"
        | Exp.IntC i -> IntC i
        | Exp.BoolC b -> BoolC b
        | Exp.StringC s -> StringC s
        | Exp.Unit -> failwith "unit not handled"
        | Exp.Op(op, es) -> SqlExp.Op(op, List.map recognizeBase es)
        | EVar _ -> failwith "var not handled"
        | _ -> failwithf "Warning: not base %A" exp
    
    recognizeUnion exp
