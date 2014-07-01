namespace FSharpComposableQuery.Tests

open System

type TestClass() = 

    static let mutable tag = 0


    member this.tagQuery (?txt:string) = 
        tag <- tag + 1
        match txt with
        | Some(s) -> 
            printfn "Q%02d %s" tag s
        | None -> 
            printfn "Q%02d " tag