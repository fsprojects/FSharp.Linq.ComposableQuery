namespace FSharpComposableQuery

open FSharpComposableQuery.Common
open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Quotations
open System.Diagnostics
open System.Linq

type internal Debug() = 

    //init log
    static let logFile = sprintf "Log %s.txt" (System.DateTime.Now.ToString("yyy-MM-dd HH_mm_ss"))
    static do if System.IO.File.Exists(logFile) then System.IO.File.Delete(logFile)
    
    static let printObj (o:obj) = 
        let ty = o.GetType()
        if ty = typedefof<Expr> then
            (o :?> Expr).ToString(false)
        else
            let hash = o.GetHashCode() % 100
            sprintf "%s_%d" ty.Name hash

    static let printOp (x:Op) = 
        match x with
            | Plus -> "+"
            | Minus -> "-"
            | Times -> "*"
            | Div -> "/"
            | Mod -> "%"
            | Equal -> "=="
            | Nequal -> "!="
            | Leq -> "<="
            | Lt -> "<"
            | Geq -> ">="
            | Gt -> ">"
            | And -> "&&"
            | Or -> "||"
            | Concat -> "@"
            | Like -> "Like" // SQL
            | Not -> "Not" // unary
            | Neg -> "Neg" // unary

    static member prettyPrint exp = 
        let concat = String.concat System.Environment.NewLine
        let rec prettyPrintRec(lvl : int) (exp : Exp) : string List = 
            match exp with
                | IntC x -> [string x]
                | BoolC b -> [string b]
                | StringC s -> ["\"" + s + "\""]
                | Unit -> ["null"]
                | Tuple(_tty, es) -> ["Tuple:"] @ List.concat (List.map (prettyPrintRec 1) es)
                | Proj(e, i) -> (prettyPrintRec 0 e) @ ["  .Elem(" + i.ToString() + ")"]
                | IfThenElse(e, e1, (Empty _)) -> ["if"] @ (prettyPrintRec 1 e) @ ["then"] @ (prettyPrintRec 1 e1)
                | IfThenElse(e, e1, e2) -> ["if"] @ (prettyPrintRec 1 e) @ ["then"] @ (prettyPrintRec 1 e1) @ ["else"] @ (prettyPrintRec 1 e2)
                | EVar x -> ["(" + x.Name + " : " + x.Type.Name + ")"]
                | ELet(x, e1, e2) -> ["Let " + x.Name + " = "] @ (prettyPrintRec 1 e1) @ (prettyPrintRec 0 e2)
                | BinOp(e1, binop, e2) -> 
                    [(concat (prettyPrintRec 0 e1)) + " " + (printOp binop) + " " + (concat (prettyPrintRec 0 e2))]
                | UnOp(unop, e) -> [printOp unop] @ (prettyPrintRec 1 e)
                | Field(e, l) -> [(concat (prettyPrintRec 0 e)) + "." + l.name]
                | Record(_, r) -> ["{"] @ (List.concat (List.map (fun (f,e) -> ([" " + f.name + " = "] @ (prettyPrintRec 1 e))) r)) @ ["}"]
                | Lam(x, e) -> ["fun " + x.Name + " -> "] @ prettyPrintRec 1 e
                | App(e1, e2) -> ["App " + (String.concat "; " (prettyPrintRec 0 e2)) + " to:"] @ prettyPrintRec 1 e1
                | Empty _ -> ["Empty"]
                | Singleton e -> ["yield"] @ prettyPrintRec 1 e
                | Comp(e1, x, e2) -> ["foreach var " + (x.Name) + " in:"] @ (prettyPrintRec 1 e2) @ ["do"] @ (prettyPrintRec 1 e1)
                | Exists(e) -> ["Exists:"] @ prettyPrintRec 1 e
                | Table(e, ty) -> ["Table<" + ty.Name.ToString() + "> = "; "  " + e.ToString(false)]
                | Unknown(unk, _, eopt, args) -> 
                    let otxt = (concat << List.concat << List.map (prettyPrintRec 0)) (Option.toList eopt)
                    let argstxt = 
                        match args.IsEmpty with
                        | true -> []
                        | false -> (List.reduce (fun s l -> s @ [","] @ l) << List.map (prettyPrintRec 1)) args
                    match unk with
                        | UnknownCall mi ->  [otxt + "." + mi.Name + "("] @ argstxt @ [")"]
                        | UnknownValueCall mi -> ["query.RunValue( <@ " + otxt + "." + mi.Name + "("] @ argstxt @ [") @> )"]
                        | UnknownNew ci -> [otxt + ".Create_" + ci.Name + "("] @ argstxt @ [")"]
                        | UnknownRef (Patterns.Value (o,_)) -> [printObj o]
                        | UnknownRef (o) -> [printObj o]
                | Union(e1, e2) -> prettyPrintRec 0 e1 @ [ "  U" ] @ prettyPrintRec 0 e2
                | RunAsEnumerable(e1, ty) -> ["query.Run[Enumerable<" + ty.Name + ">]"] @ prettyPrintRec 1 e1
                | RunAsQueryable(e1, ty) -> ["query.Run[Queryable<" + ty.Name + ">]"] @ prettyPrintRec 1 e1
                | Quote(e1) -> ["<@ "] @ prettyPrintRec 1 e1 @ [" @>"]
                | Source(eTy, sTy, e1) -> 
                    ["query.Source<" + eTy.Name + ", " + sTy.Name + ">"] 
                    @ prettyPrintRec 1 e1
                | _ -> failwith "prettyPrint: unhandled case"
            |> List.map (fun x -> (String.replicate lvl "  ") + x)

        prettyPrintRec 1 exp
        |> concat
    
    [<Conditional("DEBUG")>]    //omitted if compiled in Release
    static member printfn f = 
        Printf.kprintf (fun s -> System.IO.File.AppendAllText(logFile, s + System.Environment.NewLine)) f

