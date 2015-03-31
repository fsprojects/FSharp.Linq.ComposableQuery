namespace FSharpComposableQuery.Tests

open NUnit.Framework;
open System
open System.Reflection


module RunTests = 
    
    // Returns all methods (public or not) of the given type t possessing a specific attribute. 
    // The extraFlags argument must specify whether to retrieve instance, static or both types of methods.
    let getMethods extraFlags att (t : Type) = 
        t.GetMethods(BindingFlags.Public ||| BindingFlags.NonPublic ||| extraFlags)
        |> Array.filter (fun mi -> not << Array.isEmpty <| mi.GetCustomAttributes(att, true))

    // Returns all the static methods tagged with the ClassInitialize attribute in the given System.Type
    let getInitMethods t = getMethods BindingFlags.Static typedefof<TestFixtureSetUpAttribute> t

    // Returns all the instance methods tagged with the TestMethod attribute in the given System.Type
    let getTestMethods t = getMethods BindingFlags.Instance typedefof<TestAttribute> t
    
    // invoke class initializers
    // TODO: these should take a TestContext parameter.
    let initTests (o : obj) =  
        for m in getInitMethods (o.GetType()) do
            m.Invoke(o, [|null|]) |> ignore
            
    // invoke test methods
    // other types of methods (e.g. test initializers) are not invoked
    // as they are not currently used in any of the tests
    let runTests o = 
        for m in getTestMethods (o.GetType()) do
            m.Invoke(o, null) |> ignore



    let delimiter = ("=" + (String.replicate 10 "-="))
    let printHeader fmt = 
        Printf.kprintf
            (fun s ->
                printfn "%s" delimiter
                printfn "%s" s
                printfn "%s" delimiter) fmt
            
    let newTests() : obj list = 
        [
//            (new FSharpComposableQuery.Tests.NorthwindTests.TestClass())
            (new FSharpComposableQuery.Tests.Simple.TestClass())
            (new FSharpComposableQuery.Tests.People.TestClass())
            (new FSharpComposableQuery.Tests.Nested.TestClass())
            (new FSharpComposableQuery.Tests.Xml.TestClass()) 
            (new FSharpComposableQuery.Tests.TPCH.TestClass()) 
        ]

    [<EntryPoint>]
    let Main(args) =
    
        printHeader "Setting up database tables"
        List.iter initTests (newTests())

        printHeader "Comparing result values (%s, %s, %s)" "F# 3.0" "TLinq" "Match"
        Utils.RunMode <- UtilsMode.CompPrint
        List.iter runTests (newTests())

        printHeader "Mean execution time (%s, %s)" "F# 3.0" "TLinq"
        Utils.RunMode <- UtilsMode.TimePrint
        List.iter runTests (newTests())
        
        printHeader "Done!"
        Console.Read()


        