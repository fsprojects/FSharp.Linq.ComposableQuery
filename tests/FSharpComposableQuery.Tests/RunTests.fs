module FSharpComposableQuery.RunTests

open Microsoft.VisualStudio.TestTools.UnitTesting;
open System

let getTests (t : Type) = 
    t.GetMethods()
    |> Array.filter (fun m -> 
        m.GetCustomAttributes(typedefof<TestMethodAttribute>, true)
        |> Array.isEmpty
        |> not)

let runTests (o : obj) = 
    for t in getTests (o.GetType()) do
        t.Invoke(o, null)
        |> ignore

[<EntryPoint>]
let Main(args) =
    
    new FSharpComposableQuery.QueryTests.QueryTests()
        |> runTests

    System.Console.Read()
