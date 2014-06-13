module FSharpComposableQuery.RunTests

open Microsoft.VisualStudio.TestTools.UnitTesting;
open System

let getTests (t : Type) = 
    t.GetMethods() 
    |> Array.filter (fun m -> (not << Array.isEmpty <| m.GetCustomAttributes(typedefof<TestMethodAttribute>, true)))

let runTests (o : obj) = 
    for t in getTests (o.GetType()) do
        t.Invoke(o, null) |> ignore

[<EntryPoint>]
let Main(args) =
    
    new FSharpComposableQuery.QueryTests.QueryTests()
    |> runTests

    new FSharpComposableQuery.NestedTests.NestedTests()
    |> runTests

    Console.WriteLine("-" + (String.replicate 10 "=-"))
    Console.WriteLine("Done!")
    Console.Read()
