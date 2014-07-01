namespace FSharpComposableQuery

open FSharpComposableQuery.Common
open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Quotations
open System.Diagnostics

type internal Debug() = 

    //setup log
    static let logFile = "Log.txt"
    static do System.IO.File.Delete(logFile)


    //Example of a Debug-conditional method. 
    //Calls to it get replaced by nops when compiling in 'Release'
    static member prettyPrint exp = 
    

        let printObj (o:obj) = 
            let hash = string (o.GetHashCode())
            let len = hash.Length
            o.GetType().Name + hash.[(len - 2)..(len-1)]

        let printOp (x:Op) = 
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

        let concat = 
            String.concat System.Environment.NewLine

        let rec prettyPrintRec(lvl : int) (exp : Exp) : string List = 
            match exp with
                | IntC x -> [string x]
                | BoolC b -> [string b]
                | StringC s -> [s]
                | Unit -> ["null"]
                | Tuple(_tty, es) -> ["Tuple:"] @ List.concat (List.map (prettyPrintRec 1) es)
                | Proj(e, i) -> (prettyPrintRec 0 e) @ ["  .Elem(" + i.ToString() + ")"]
                | IfThenElse(e, e1, (Empty _)) -> ["if"] @ (prettyPrintRec 1 e) @ ["then"] @ (prettyPrintRec 1 e1)
                | IfThenElse(e, e1, e2) -> ["if"] @ (prettyPrintRec 1 e) @ ["then"] @ (prettyPrintRec 1 e1) @ ["else"] @ (prettyPrintRec 1 e2)
                | EVar x -> [x.Name]
                | ELet(x, e1, e2) -> ["Let " + x.Name + " = "] @ (prettyPrintRec 1 e1) @ (prettyPrintRec 0 e2)
                | BinOp(e1, binop, e2) -> 
                    [(concat (prettyPrintRec 0 e1)) + " " + (printOp binop) + " " + (concat (prettyPrintRec 0 e2))]
                | UnOp(unop, e) -> [printOp unop] @ (prettyPrintRec 1 e)
                | Field(e, l) -> [(concat (prettyPrintRec 0 e)) + "." + l.name]
                | Record(rty, r) -> ["{"] @ (List.concat (List.map (fun (f,e) -> ([" " + f.name + " = "] @ (prettyPrintRec 1 e))) r)) @ ["}"]
                | Lam(x, e) -> prettyPrintRec 0 e
                | App(e1, e2) -> prettyPrintRec 0 e1
                | Empty ty -> ["Empty"]
                | Singleton e -> ["yield"] @ prettyPrintRec 1 e
                | Comp(e1, x, e2) -> ["foreach var " + (x.Name) + " in:"] @ (prettyPrintRec 1 e2) @ ["do"] @ (prettyPrintRec 1 e1)
                | Exists(e) -> ["Exists:"] @ prettyPrintRec 1 e
                (*| Table(e,ty) -> sourceExp( e,ty)*)
                | Table(e, _ty) -> ["Table:"] //@ prettyPrintRec 1 (toExp e)
                | Unknown(unk, _, eopt, args) -> 
                    let otxt = (concat << List.concat << List.map (prettyPrintRec 0)) (Option.toList eopt)
                    let argstxt = 
                        match args.IsEmpty with
                        | true -> []
                        | false -> (List.reduce (fun s l -> s @ [","] @ l) << List.map (prettyPrintRec 1)) args
                    match unk with
                        | UnknownCall mi ->  [otxt + "." + mi.Name + "("] @ argstxt @ [")"]
                        | UnknownNew ci -> [otxt + ".Create_" + ci.Name + "("] @ argstxt @ [")"]
                        | UnknownRef (Patterns.Value (o,_)) -> ["(Ref " + (printObj o) + ")"]
                        | UnknownRef (o) -> [printObj o]
                        | UnknownQuote -> [otxt + "<@ "] @ argstxt @ [" @>"]
                | Union(e1, e2) -> prettyPrintRec 0 e1 @ [ "  U" ] @ prettyPrintRec 0 e2
                | _ -> raise NYI
            |> List.map (fun x -> (String.replicate lvl "  ") + x)

        prettyPrintRec 1 exp
        |> concat
    
    [<Conditional("DEBUG")>]
    static member printfn(f,s) = 
        System.IO.File.AppendAllText(logFile, (sprintf f s) + System.Environment.NewLine)

