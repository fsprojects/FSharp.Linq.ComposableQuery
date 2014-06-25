namespace FSharpComposableQuery.Tests


type TestClass() = 

    static let mutable tag = 0

    member this.tagQuery (?txt) = 
        match txt with
        | Some(s) -> 
            printfn "Q%d: %A" tag s
        | None -> 
            printfn "Q%d:" tag
        tag <- tag + 1
