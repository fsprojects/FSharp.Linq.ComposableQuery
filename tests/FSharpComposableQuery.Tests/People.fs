namespace FSharpComposableQuery.Tests

open Microsoft.FSharp.Data.TypeProviders
open Microsoft.FSharp.Quotations
open System.Linq
open Microsoft.VisualStudio.TestTools.UnitTesting;



module People = 
    
    [<Literal>]
    let N_COUPLES = 5000

    type internal dbSchemaPeople = SqlDataConnection<ConnectionStringName="PeopleConnectionString", ConfigFile=".\\App.config">

    type internal Couples = dbSchemaPeople.ServiceTypes.Couples
    type internal CoupleR = {him:string; her:string}

    type internal People = dbSchemaPeople.ServiceTypes.People
    type internal PeopleR = {name:string;age:int}
    type internal Result = {rname:string;diff:int}

    let internal db = dbSchemaPeople.GetDataContext()

    // used in example 6
    type internal Predicate = 
        | Above of int
        | Below of int
        | And of Predicate * Predicate
        | Or of Predicate * Predicate
        | Not of Predicate

    [<TestClass>]
    type TestClass() =
        inherit FSharpComposableQuery.Tests.TestClass()

        static let couples = db.DataContext.GetTable<Couples>()
        static let people = db.DataContext.GetTable<People>()

        static let addCoupleR (r:CoupleR) =
          let c = new Couples() in 
          c.Him <- r.him;
          c.Her <- r.her;  
          couples.InsertOnSubmit(c)

        static let addPeopleR (r:PeopleR) =
          let p = new People() in 
          p.Age <- r.age
          p.Name <- r.name
          people.InsertOnSubmit(p)

        static let dropTables() = 
          ignore(db.DataContext.ExecuteCommand("DELETE FROM [MyPeople].[dbo].[Couples] WHERE 1=1"))
          ignore(db.DataContext.ExecuteCommand("DELETE FROM [MyPeople].[dbo].[People] WHERE 1=1"))
    

        // TODO: Random data generator

        static let mutable idx = 0
        static let gensym str = 
            idx <- idx + 1
            str + "_" + idx.ToString()

        static let rand = new System.Random()
        static let randomArray(arr:'a array) = 
          let i = rand.Next(arr.Length) in
          (arr.[i])

        static let randomMaleName () = gensym (randomArray [|"alan";"bert";"charlie";"david";"edward"|])
        static let randomFemaleName() = gensym (randomArray [|"alice";"betty";"clara";"dora";"eve"|])
        static let randomAge () = rand.Next(18,80)

        static let  randomCouples n = List.map (fun _ -> {him=randomMaleName(); her=randomFemaleName()}) [1..n]

        static let randomPeople couples = 
            List.concat (List.map (fun r -> [{name=r.him;age=randomAge()};{name=r.her;age=randomAge()}]) couples)

        static let addRandom n = 
          let couples = randomCouples n
          List.iter addCoupleR couples
          db.DataContext.SubmitChanges()

          let people = randomPeople couples
          List.iter addPeopleR people;
          db.DataContext.SubmitChanges()



        // Example 1

        let differences = <@ query {
                    for c in db.Couples do
                    for w in db.People do
                    for m in db.People do
                        if c.Her = w.Name && c.Him = m.Name && w.Age > m.Age 
                        then yield {rname=w.Name; diff=w.Age - m.Age}
                    } @>

        let ex1 = differences


        // Example 2

        let rangeSimple = fun (a:int) (b:int) -> 
          query {
            for u in db.People do
                if a <= u.Age && u.Age < b 
                then yield {name=u.Name;age=u.Age}
            }   

        let ex2 = <@ query { yield! rangeSimple 30 40 } @> 


        // Example 3, 4

        let satisfies  = 
         <@ fun p -> query { 
            for u in db.People do
                if p u.Age 
                then yield {name=u.Name;age=u.Age}
           } @>

        let ex3 = <@ query { yield! (%satisfies) (fun x -> 20 <= x && x < 30 ) } @>

        let ex4 = <@ query { yield! (%satisfies) (fun x ->  x % 2 = 0 ) } @>



        // Example 5
        
        let range = <@ fun (a:int) (b:int) -> 
          query {
            for u in db.People do
            if a <= u.Age && u.Age < b 
            then yield {name=u.Name;age=u.Age}
            } @>


        let ageFromName = 
          <@ fun s -> query {
             for u in db.People do 
                if s = u.Name 
                then yield u.Age } @>

        let compose : Expr<string -> string -> IQueryable<PeopleR>> = 
          <@ fun s t -> query {
              for a in (%ageFromName) s do
              for b in (%ageFromName) t do 
                yield! (%range) a b
          } @>

        let ex5 = <@ query { yield! (%compose) "Eve" "Bob" } @> 


        // Example 6, 7

        let rec eval(t:Predicate) : Expr<int -> bool> =
          match t with
          | Above n -> <@ fun x -> x >= n @>
          | Below n -> <@ fun x -> x < n @>
          | And (t1,t2) -> <@ fun x -> (%eval t1) x && (%eval t2) x @>
          | Or (t1,t2) -> <@ fun x -> (%eval t1) x || (%eval t2) x @>
          | Not (t0) -> <@ fun x -> not((%eval t0) x ) @>
          

        let ex6 = <@ query { yield! (%satisfies) (%eval (And (Above 20, Below 30))) } @>

        
        let t7 = eval (Not(Or(Below 20, Above 30)))
        
        let ex7 = <@ query { yield! (%satisfies) (%t7) } @>



        [<ClassInitialize>]
        static member init (c:TestContext) = 
            printf "People: Adding %d couples... " N_COUPLES
            dropTables()
            addRandom N_COUPLES
            printfn "done!"

        [<TestMethod>]
        member this.testEx1() = 
            printfn "%s" "ex1"
            Utils.Run ex1

        [<TestMethod>]
        member this.testEx2() = 
            printfn "%s" "ex2"
            Utils.Run ex2

        [<TestMethod>]
        member this.testEx3() = 
            printfn "%s" "ex3"
            Utils.Run ex3

        [<TestMethod>]
        member this.testEx4() = 
            printfn "%s" "ex4"
            Utils.Run ex4

        [<TestMethod>]
        member this.testEx5() = 
            printfn "%s" "ex5"
            Utils.Run ex5
            
        [<TestMethod>]
        member this.testEx6() = 
            printfn "%s" "ex6"
            Utils.Run ex6
            
        [<TestMethod>]
        member this.testEx7() = 
            printfn "%s" "ex7"
            Utils.Run ex7