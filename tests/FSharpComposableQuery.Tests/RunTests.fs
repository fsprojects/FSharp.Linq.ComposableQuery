namespace FSharpComposableQuery.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting;
open System
open System.Reflection

module RunTests = 
    
    // Gets all methods in the given type t with the given attribute. 
    // The extraFlags variable should specify whether to retrieve instance or static methods 
    let getMethods extraFlags att (t : Type) = 
        t.GetMethods(BindingFlags.Public ||| BindingFlags.NonPublic ||| extraFlags)
        |> Array.filter (fun mi -> not << Array.isEmpty <| mi.GetCustomAttributes(att, true))

    // Gets all the static methods tagged with the ClassInitialize attribute in the given System.Type
    let getInitMethods t = getMethods BindingFlags.Static typedefof<ClassInitializeAttribute> t

    // Gets all the instance methods tagged with the TestMethod attribute in the given System.Type
    let getTestMethods t = getMethods BindingFlags.Instance typedefof<TestMethodAttribute> t

    let initTests (o : obj) = 
        let t = o.GetType()
        
        // invoke class initializers
        // TODO: these take a TestContext parameter. 
        for m in getInitMethods t do
            m.Invoke(o, [|null|]) |> ignore

    let runTests (o : obj) = 
        let t = o.GetType()

        // invoke test methods
        for m in getTestMethods t do
            m.Invoke(o, null) |> ignore

        // other types of methods (e.g. test initializers) are not invoked
        // as they are not currently used in any of the tests

    let tests : TestClass list = [
        (new FSharpComposableQuery.Tests.Simple.TestClass())
        (new FSharpComposableQuery.Tests.People.TestClass())
        (new FSharpComposableQuery.Tests.Nested.TestClass())
        (new FSharpComposableQuery.Tests.Xml.TestClass()) 
        ]


    let delimiter = ("-" + (String.replicate 10 "=-"))

    [<EntryPoint>]
    let Main(args) =

        List.iter initTests tests
 
        //compare results
        printfn "%s" delimiter
        printfn "Comparing result values (%s, %s, %s)" "F# 3.0" "TLinq" "Match"
        printfn "%s" delimiter

        Utils.RunMode <- UtilsMode.CompPrint
        List.iter runTests tests

        //run benchmarks
        printfn "%s" delimiter
        printfn "Mean execution time (%s, %s)" "F# 3.0" "TLinq"
        printfn "%s" delimiter

        Utils.RunMode <- UtilsMode.TimePrint
        List.iter runTests tests
        

        printfn "%s" delimiter
        printfn "Done!"
        Console.Read()


