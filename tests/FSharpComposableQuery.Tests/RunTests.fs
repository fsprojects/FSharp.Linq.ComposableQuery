module FSharpComposableQuery.RunTests

[<EntryPoint>]
let Main(args) =

    printfn "hi yo sup"
    
    QueryTests.doTest()

    System.Console.Read() |> ignore

    // main entry point return
    0