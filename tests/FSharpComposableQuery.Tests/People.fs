namespace FSharpComposableQuery.Tests

open Microsoft.FSharp.Data.TypeProviders
open Microsoft.FSharp.Quotations
open System.Linq
open NUnit.Framework

/// <summary>
/// Contains example queries and operations on the People database. 
/// The queries here are further wrapped in quotations to allow for their evaluation in different contexts (see Utils.fs).  
/// <para>These tests require the schema from sql/people.sql in a database referred to in app.config </para>
/// </summary>
module People =

    [<Literal>]
    let internal N_COUPLES = 5000
    [<Literal>]
    let dbConfigPath = "db.config"
    
    type internal dbSchemaPeople = SqlDataConnection< ConnectionStringName="PeopleConnectionString", ConfigFile=dbConfigPath>
    type internal Couple = dbSchemaPeople.ServiceTypes.Couples

    type internal Person = dbSchemaPeople.ServiceTypes.People

    // Used in example 1
    type internal Result = { Name : string; Diff : int }

    let internal db = dbSchemaPeople.GetDataContext()

    // Used in example 6
    type internal Predicate =
        | Above of int
        | Below of int
        | And of Predicate * Predicate
        | Or of Predicate * Predicate
        | Not of Predicate

    [<TestFixture>]
    type TestClass() =
        static let couples = db.DataContext.GetTable<Couple>()
        static let people = db.DataContext.GetTable<Person>()

        // db table manipulation

        // Adds the given CoupleR object as a row in the database. 
        static let addCoupleR (c : Couple) =
            couples.InsertOnSubmit(c)
            
        // Adds the given PeopleR object as a row in the database. 
        static let addPeopleR (p : Person) =
            people.InsertOnSubmit(p)

        // Clears all relevant tables in the database. 
        static let dropTables() =
            ignore (db.DataContext.ExecuteCommand("TRUNCATE TABLE [FCQ-People].[dbo].[Couples]"))
            ignore (db.DataContext.ExecuteCommand("TRUNCATE TABLE [FCQ-People].[dbo].[People]"))

            
        static let rnd = new System.Random()

        static let mutable idx = 0

        // Appends a unique tag to the given string. 
        static let addTag str =
            idx <- idx + 1
            str + "_" + idx.ToString()

        // Picks a random element from the given array. 
        static let pickRandom (arr : _ array) =
            let i = rnd.Next(arr.Length)
            arr.[i]

        static let maleNames = [| "alan"; "bert"; "charlie"; "david"; "edward" |]
        static let femaleNames = [| "alice"; "betty"; "clara"; "dora"; "eve" |]

        static let randomAge() = rnd.Next(18, 80)
        static let randomMaleName() = (pickRandom >> addTag) maleNames
        static let randomFemaleName() = (pickRandom >> addTag) femaleNames
        static let randomCouple() = new Couple(Him = randomMaleName(), Her = randomFemaleName())
        static let randomCouples n = List.map (ignore >> randomCouple) [ 1..n ]

        static let randomPersons (c:Couple) = 
              [ new Person(Name = c.Him, Age = randomAge()) 
                new Person(Name = c.Her, Age = randomAge()) ]

        static let randomPeople = (List.map randomPersons >> List.concat)

        // Generates n random couples (and the corresponding people) records and then adds them to the database. 
        static let addRandom n =
            let couples = randomCouples n
            List.iter addCoupleR couples
            db.DataContext.SubmitChanges()

            let people = randomPeople couples
            List.iter addPeopleR people
            db.DataContext.SubmitChanges()

        // Example 1
        let differences =
            <@ query {
                for c in db.Couples do
                for w in db.People do
                for m in db.People do
                    if c.Her = w.Name && c.Him = m.Name && w.Age > m.Age then
                        yield { Name = w.Name
                                Diff = w.Age - m.Age }
               } @>

        let ex1 = differences

        // Example 2
        let rangeSimple =
            fun (a : int) (b : int) ->
                query {
                    for u in db.People do
                        if a <= u.Age && u.Age < b then
                            yield u
                }

        let ex2 = <@ query { yield! rangeSimple 30 40 } @>

        // Example 3, 4
        let satisfies =
            <@ fun p ->
                query {
                    for u in db.People do
                        if p u.Age then
                            yield u
                } @>

        let ex3 = <@ query { yield! (%satisfies) (fun x -> 20 <= x && x < 30) } @>

        let ex4 = <@ query { yield! (%satisfies) (fun x -> x % 2 = 0) } @>

        // Example 5
        let range =
            <@ fun (a : int) (b : int) ->
                query {
                    for u in db.People do
                        if a <= u.Age && u.Age < b then
                            yield u
                } @>

        let ageFromName =
            <@ fun s ->
                query {
                    for u in db.People do
                        if s = u.Name then yield u.Age
                } @>

        let compose : Expr<string -> string -> IQueryable<Person>> =
            <@ fun s t ->
                query {
                    for a in (%ageFromName) s do
                        for b in (%ageFromName) t do
                            yield! (%range) a b
                } @>

        let ex5 = <@ query { yield! (%compose) "Eve" "Bob" } @>

        // Example 6, 7
        let rec eval (t : Predicate) : Expr<int -> bool> =
            match t with
            | Above n -> <@ fun x -> x >= n @>
            | Below n -> <@ fun x -> x < n @>
            | And(t1, t2) -> <@ fun x -> (%eval t1) x && (%eval t2) x @>
            | Or(t1, t2) -> <@ fun x -> (%eval t1) x || (%eval t2) x @>
            | Not(t0) -> <@ fun x -> not ((%eval t0) x) @>

        let ex6 = <@ query { yield! (%satisfies) (%eval (And(Above 20, Below 30))) } @>

        let ex7 = <@ query { yield! (%satisfies) (%eval (Not(Or(Below 20, Above 30)))) } @>

        let testYieldFrom' = 
              <@ query { for u in db.People do
                         if 1 <= 0 then
                          yield! (query {yield u}) }@>
        
        let testYieldFrom2' = 
              <@ query { for u in db.People do
                         if 1 <= 0 then
                          yield! (query {for u in db.People do 
                                         where (1 <= 0) 
                                         yield u}) }@>

        [<TestFixtureSetUp>]
        member public this.init() =
            printf "People: Adding %d couples... " N_COUPLES
            dropTables()
            addRandom N_COUPLES
            printfn "done! (%d people; %d couples)" (people.Count()) (couples.Count())

        [<Test>]
        member this.test01() =
            printfn "%s" "ex1"
            Utils.Run ex1

        [<Test>]
        member this.test02() =
            printfn "%s" "ex2"
            Utils.Run ex2

        [<Test>]
        member this.test03() =
            printfn "%s" "ex3"
            Utils.Run ex3

        [<Test>]
        member this.test04() =
            printfn "%s" "ex4"
            Utils.Run ex4

        [<Test>]
        member this.test05() =
            printfn "%s" "ex5"
            Utils.Run ex5

        [<Test>]
        member this.test06() =
            printfn "%s" "ex6"
            Utils.Run ex6

        [<Test>]
        member this.test07() =
            printfn "%s" "ex7"
            Utils.Run ex7

        [<Test>]
        member this.test000() = 
            printfn "%s" "testYieldFrom"
            Utils.Run testYieldFrom' 

        [<Test>]
        member this.test001() = 
            printfn "%s" "testYieldFrom2"
            Utils.Run testYieldFrom2' 
