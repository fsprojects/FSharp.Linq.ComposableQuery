namespace FSharpComposableQuery.Tests

open System
open System.Linq
open FSharpComposableQuery

module ExtraTests = 

    let private simpleDb = Simple.schema.GetDataContext()
    type simpleTy = Simple.schema.ServiceTypes

    let data = [1..10]

    let private qValue = <@ fun (age:int) -> query { for s in simpleDb.Student do head } @>
    let private qEnum = <@ fun id -> query { for i in data do if i < id then select i } @>
    let private qQuery = <@ fun (age:int) -> query { for s in simpleDb.Student do if not (s.Age.Equals(age)) then yield s } @>


    let private qValueUseValue = <@ query { for s in simpleDb.Student do if (s = ((%qValue) 15)) then count } @>
    let private qQueryUseQuery = <@ query { for s in ((%qQuery) 16) do yield s } @>

    let private qValueUseQuery = <@ query { for s in ((%qQuery) 16) do count } @>
    let private qQueryUseValue = <@ query { for s in simpleDb.Student do if (s = ((%qValue) 15)) then yield s } @>

    let private qValValSimple = <@ query
        {
            for s in simpleDb.Student do
            for u in simpleDb.Student do
            where(not(u.Age.Equals(15)) = s.Age.HasValue)
            select s
            count
        } @>

    let private nStudents = <@ fun (src:Data.Linq.Table<simpleTy.Student>) -> query { for y in src do count } @>
    
    let private existsName = <@ fun n -> query { for y in simpleDb.Student do exists(y.Name = n) } @>
    
    let private findMatching = <@ fun (src:Data.Linq.Table<simpleTy.Student>) -> query {
                for z in src do
                    if not ((%existsName) z.Name) then
                        yield z
        } @>

    

    let RunExtraTests() = 
        let av = Utils.Run <@ ((%qValue) 16) @>
        let aq = Utils.Run <@ query { yield! (%qQuery) 25 } @>

        let avv = Utils.Run qValueUseValue
        let aqq = Utils.Run <@ query { yield! (%qQueryUseQuery) } @>

        let avq = Utils.Run qValueUseQuery
        let aqv = Utils.Run <@ query { yield! (%qQueryUseValue) } @>

        ()