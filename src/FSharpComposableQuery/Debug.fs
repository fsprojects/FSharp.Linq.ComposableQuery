module internal FSharpComposableQuery.Debug

open FSharpComposableQuery.Common

let rec prettyPrintRec(lvl : int) (exp : FSharpComposableQuery.Common.Exp) : string List = 
    match exp with
        | IntC x -> [string x]
        | BoolC b -> [string b]
        | StringC s -> [s]
        | Unit -> ["null"]
        | Tuple(_tty, es) -> ["Tuple:"] @ List.concat (List.map (prettyPrintRec 1) es)
        | Proj(e, i) -> ["Proj"] @ (prettyPrintRec 1 e)
        | IfThenElse(e, e1, e2) -> ["if"] @ (prettyPrintRec 1 e) @ ["then"] @ (prettyPrintRec 1 e1) @ ["else"] @ (prettyPrintRec 1 e2)
        | EVar x -> [string x]
        | ELet(x, e1, e2) -> ["Let"] @ (prettyPrintRec 1 e1) @ (prettyPrintRec 1 e2)
        | BinOp(e1, binop, e2) -> (prettyPrintRec 0 e1) @ [string binop] @ (prettyPrintRec 0 e2)
        | UnOp(unop, e) -> [(string unop)] @ (prettyPrintRec 1 e)
        | Field(e, l) -> prettyPrintRec 0 e
        | Record(rty, r) -> ["Record"] @ (List.map (fun (f,e) -> string f) r)
        | Lam(x, e) -> prettyPrintRec 0 e
        | App(e1, e2) -> prettyPrintRec 0 e1
        | Empty ty -> ["Empty"]
        | Singleton e -> ["yield"] @ prettyPrintRec 1 e
        | Comp(e2, x, e1) -> ["foreach var " + (string x) + " in:"] @ (prettyPrintRec 1 e2) @ ["do"] @ (prettyPrintRec 1 e1)
        | Exists(e) -> ["Exists:"] @ prettyPrintRec 1 e
        (*| Table(e,ty) -> sourceExp( e,ty)*)
        | Table(e, _ty) -> ["Table:"]// @ (prettyPrintRec 1 (fromExpr e))
        | Unknown(unk, _, eopt, es) -> 
            (List.concat (List.map (prettyPrintRec 1) es))
        | _ -> raise NYI
    |> List.map (fun x -> (String.replicate lvl "  ") + x)
    

let prettyPrint exp = 
    prettyPrintRec 0 exp
    |> String.concat System.Environment.NewLine