namespace FSharpComposableQuery.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting;
open System


module RunTests = 
    let getTestMethods (t : Type) = 
        t.GetMethods() 
        |> Array.filter (fun m -> (not << Array.isEmpty <| m.GetCustomAttributes(typedefof<TestMethodAttribute>, true)))

    let runTests (o : obj) = 
        for t in getTestMethods (o.GetType()) do
            t.Invoke(o, null) |> ignore

    [<EntryPoint>]
    let Main(args) =
    
        new FSharpComposableQuery.Tests.QueryTests()
        |> runTests

        new FSharpComposableQuery.Tests.NestedTests()
        |> runTests
    
        Console.WriteLine("-" + (String.replicate 10 "=-"))
        Console.WriteLine("Done!")
        Console.Read()
