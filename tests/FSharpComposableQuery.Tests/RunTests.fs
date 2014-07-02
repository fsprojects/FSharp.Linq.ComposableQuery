namespace FSharpComposableQuery.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting;
open System
open System.Reflection


module RunTests = 
    
    // Returns all methods (public or not) of the given type t possessing a specific attribute. 
    // The extraFlags argument must specify whether to retrieve instance, static or both types of methods.
    let getMethods extraFlags att (t : Type) = 
        t.GetMethods(BindingFlags.Public ||| BindingFlags.NonPublic ||| extraFlags)
        |> Array.filter (fun mi -> not << Array.isEmpty <| mi.GetCustomAttributes(att, true))

    // Returns all the static methods tagged with the ClassInitialize attribute in the given System.Type
    let getInitMethods t = getMethods BindingFlags.Static typedefof<ClassInitializeAttribute> t

    // Returns all the instance methods tagged with the TestMethod attribute in the given System.Type
    let getTestMethods t = getMethods BindingFlags.Instance typedefof<TestMethodAttribute> t
    
    // invoke class initializers
    // TODO: these take a TestContext parameter.
    let initTests (o : obj) =  
        for m in getInitMethods (o.GetType()) do
            m.Invoke(o, [|null|]) |> ignore
            
    // invoke test methods
    // other types of methods (e.g. test initializers) are not invoked
    // as they are not currently used in any of the tests
    let runTests (o : obj) = 
        for m in getTestMethods (o.GetType()) do
            m.Invoke(o, null) |> ignore



    let delimiter = ("=" + (String.replicate 10 "-="))
    let printHeader fmt = 
        Printf.kprintf
            (fun s ->
                printfn "%s" delimiter
                printfn "%s" s
                printfn "%s" delimiter) fmt
            

    let tests : TestClass list = [
        (new FSharpComposableQuery.Tests.Simple.TestClass())
        (new FSharpComposableQuery.Tests.People.TestClass())
        (new FSharpComposableQuery.Tests.Nested.TestClass())
        (new FSharpComposableQuery.Tests.Xml.TestClass()) 
        ]

    [<EntryPoint>]
    let Main(args) =

        //init tables
        printHeader "Setting up database tables"
        List.iter initTests tests

        //compare results
        Utils.RunMode <- UtilsMode.CompPrint
        printHeader "Comparing result values (%s, %s, %s)" "F# 3.0" "TLinq" "Match"
        List.iter runTests tests

        //run benchmarks
        Utils.RunMode <- UtilsMode.TimePrint
        printHeader "Mean execution time (%s, %s)" "F# 3.0" "TLinq"
        List.iter runTests tests
        
        printHeader "Done!"
        Console.Read()


        