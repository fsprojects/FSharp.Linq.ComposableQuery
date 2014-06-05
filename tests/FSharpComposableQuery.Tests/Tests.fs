module FSharpComposableQuery.Tests

open FSharpComposableQuery
//open NUnit.Framework
open Microsoft.VisualStudio.TestTools.UnitTesting;

[<TestClass>]
type Tests() = 
//    new() = { }

    [<TestMethod>]
    member this.``hello returns 42``() =
      let result = 42
      printfn "%i" result
      Assert.AreEqual(42,result)