namespace FSharpComposableQuery.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting;
open System
open System.Reflection

module RunTests = 
    
    let getInitMethod (t : Type) =
        let m = t.GetMethods(BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Static)
        m
        |> Array.filter (fun m -> (not << Array.isEmpty <| m.GetCustomAttributes(typedefof<ClassInitializeAttribute>, true)))

    let getTestMethods (t : Type) = 
        let mis = t.GetMethods(BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance)
        Array.filter (fun (m:MethodInfo) -> (not << Array.isEmpty <| m.GetCustomAttributes(typedefof<TestMethodAttribute>, true))) mis

    let runTests (o : obj) = 
        let t = o.GetType()
        for m in getInitMethod t do
            m.Invoke(o, [|null|]) |> ignore
        for m in getTestMethods t do
            m.Invoke(o, null) |> ignore

    [<EntryPoint>]
    let Main(args) =
    
        new FSharpComposableQuery.Tests.Simple.TestClass()
        |> runTests

        (new FSharpComposableQuery.Tests.People.TestClass())
        |> runTests
        
        (new FSharpComposableQuery.Tests.Nested.TestClass())
        |> runTests

        new FSharpComposableQuery.Tests.Xml.TestClass()
        |> runTests

        Console.WriteLine("-" + (String.replicate 10 "=-"))
        Console.WriteLine("Done!")
        Console.Read()


