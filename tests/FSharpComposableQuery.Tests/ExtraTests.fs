namespace FSharpComposableQuery.Tests

open System
open System.Linq
open FSharpComposableQuery
open Microsoft.FSharp.Quotations

module ExtraTests = 

    let private simpleDb = Simple.schema.GetDataContext()
    type simpleTy = Simple.schema.ServiceTypes

    let data = [1..10]
    
    let private qVals = 
        [|
            <@ fun (age:int) -> query { for s in simpleDb.Student do all(s.Age.HasValue) } @>
            <@ fun (age:int) -> query { for s in simpleDb.Student do contains(simpleDb.Student.First()) } @>
            <@ fun (age:int) -> query { for s in simpleDb.Student do exists(s.Age.HasValue) } @>
        |]

    let private qEnum = <@ fun id -> query { for i in data do if i < id then select i } @>
    let private qQuery = <@ fun (age:int) -> query { for s in simpleDb.Student do if not (s.Age.Equals(age)) then yield s } @>


    let private qValueUseValue i = <@ query { for s in simpleDb.Student do if (s.Age.HasValue = ((%qVals.[i]) 15)) then count } @>
    let private qQueryUseQuery = <@ query { for s in ((%qQuery) 16) do yield s } @>

    let private qValueUseQuery = <@ query { for s in ((%qQuery) 16) do count } @>
    let private qQueryUseValue i = <@ query { for s in simpleDb.Student do if (s.Age.HasValue = ((%qVals.[i]) 15)) then yield s } @>


    let private nStudents = <@ fun (src:Data.Linq.Table<simpleTy.Student>) -> query { for y in src do count } @>
    
    let private existsName = <@ fun n -> query { for y in simpleDb.Student do exists(y.Name = n) } @>
    
    let private findUnique = <@ fun (src:Data.Linq.Table<simpleTy.Student>) -> query {
                for z in src do
                    if not ((%existsName) z.Name) then
                        yield z
        } @>

    

    let RunExtraTests() = 
        let aq = Utils.Run <@ query { yield! (%qQuery) 25 } @>
        
        let avv = Array.map (fun i -> Utils.Run (qValueUseValue i)) [|0..qVals.Length-1|]
        let aqq = Utils.Run <@ query { yield! (%qQueryUseQuery) } @>

        let avq = Utils.Run qValueUseQuery
        let aqv = Array.map (fun i -> Utils.Run <@ query { yield! (%qQueryUseValue i) } @>) [|0..qVals.Length-1|]

        ()