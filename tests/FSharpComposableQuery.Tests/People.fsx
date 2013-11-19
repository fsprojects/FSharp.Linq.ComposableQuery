#if INTERACTIVE
#r "FSharp.Data.TypeProviders.dll"
#r "System.Data.dll"
#r "System.Data.Linq.dll"
#r "FSharp.PowerPack.Linq.dll"

#load "Test.fsx"
#endif

open FSharpComposableQuery.Expr
open Test
open Microsoft.FSharp.Data.TypeProviders
open Microsoft.FSharp.Linq.QuotationEvaluation
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq
open System.Linq



[<Literal>]
let ConnectionString = 
  "Data Source=(localdb)\MyInstance;\
   Initial Catalog=MyPeople;
   Integrated Security=SSPI";;

type dbSchema = SqlDataConnection<ConnectionString>;;
let db = dbSchema.GetDataContext();;
//db.DataContext.Log <- System.Console.Out;;


type Couples = dbSchema.ServiceTypes.Couples
type CoupleR = {him:string; her:string}


let couples = db.DataContext.GetTable<Couples>()     
let addCouple (r : Couples) =          
  couples.InsertOnSubmit(r)

let addCouplesR (r:CoupleR) =
  let c = new Couples() in 
  c.Him <- r.him;
  c.Her <- r.her;
  addCouple(c);;

type People = dbSchema.ServiceTypes.People
type PeopleR = {name:string;age:int}

let people = db.DataContext.GetTable<People>()     
let addPeople (r : People) =     
    people.InsertOnSubmit(r)
let addPeopleR (r:PeopleR) =
  let p = new People() in 
  p.Age <- r.age;
  p.Name <- r.name;
  addPeople(p);;

let dropTables() = 
  ignore(db.DataContext.ExecuteCommand("DELETE FROM [MyPeople].[dbo].[Couples] WHERE 1=1"))
  ignore(db.DataContext.ExecuteCommand("DELETE FROM [MyPeople].[dbo].[People] WHERE 1=1"))
;;

// TODO: Random data generator

let idx = ref 0;;
let gensym str = let i = !idx in
                 let () = idx := i+1 in
                 str + "_" + i.ToString();;

let rand = new System.Random()
let randomArray(arr:'a array) = 
  let i = rand.Next(arr.Length) in
  (arr.[i])

let randomMaleName () = gensym (randomArray [|"alan";"bert";"charlie";"david";"edward"|]);;
let randomFemaleName() = gensym (randomArray [|"alice";"betty";"clara";"dora";"eve"|]);;
let randomAge () = rand.Next(18,80);;

let randomCouples n = List.map (fun _ -> {him=randomMaleName(); her=randomFemaleName()}) [1..n];;

let randomPeople couples = 
    List.concat (List.map (fun r -> [{name=r.him;age=randomAge()};{name=r.her;age=randomAge()}]) couples)

let addRandom n = 
  let couples = randomCouples n in
  List.iter (fun c -> addCouplesR c) couples
  db.DataContext.SubmitChanges()
  let people = randomPeople couples in
  List.iter (fun p -> addPeopleR p) people;
  db.DataContext.SubmitChanges();;




let printPeople x = Seq.iter (fun r -> printfn "%s %i" r.name r.age)    x            
let forcePeople x = Seq.iter (fun r -> ())    x            

type Result = {rname:string;diff:int}
let printResult x = Seq.iter (fun r -> printfn "%s %i" r.rname r.diff)    x            
let forceResult x = Seq.iter (fun r -> ())    x            

let differences = <@ seq {
              for c in db.Couples do
              for w in db.People do
              for m in db.People do
              if c.Her = w.Name && c.Him = m.Name && w.Age > m.Age 
              then yield {rname=w.Name; diff=w.Age - m.Age}
              } @>


let differences' = <@ query {
              for c in db.Couples do
              for w in db.People do
              for m in db.People do
              if c.Her = w.Name && c.Him = m.Name && w.Age > m.Age 
              then yield {rname=w.Name; diff=w.Age - m.Age}
            } @>

// example 1
let ex1 = differences
let ex1' = differences'
//testAll ex1 ex1' printResult
// SLOW timeAll ex1 ex1'  forceResult

let range = <@ fun (a:int) (b:int) -> 
  seq {
    for u in db.People do
    if a <= u.Age && u.Age < b 
    then yield {name=u.Name;age=u.Age}
    } @>

let range' = fun (a:int) (b:int) -> 
  query {
    for u in db.People do
    if a <= u.Age && u.Age < b 
    then yield {name=u.Name;age=u.Age}
    } 

// Example 2

let ex2 = <@ (%range) 30 40 @>
let ex2' = <@ range' 30 40 @> 

//testAll ex2 ex2' printPeople
//timeAll ex2 ex2' forcePeople


let satisfies:Expr<(int -> bool) -> seq<PeopleR>> = 
  <@ fun p -> seq { 
      for w in db.People do
      if p w.Age 
      then yield {name=w.Name;age=w.Age}
   } @>


let satisfies'  = 
 <@ fun p -> query { 
    for u in db.People do
    if p u.Age 
    then yield {name=u.Name;age=u.Age}
   } @>

// example 3

let ex3 = <@ (%satisfies) (fun x -> 30 <= x && x < 40) @>
let ex3' = <@  (%satisfies') (fun x -> 20 <= x && x < 30 ) @>
//testAll ex3 ex3' printPeople
//timeAll ex3 ex3'  forcePeople


// example 4

let ex4 = <@ (%satisfies) (fun x -> x % 2 = 0) @>
let ex4' = <@ (%satisfies') (fun x ->  x % 2 = 0 ) @>
//testAll ex4 ex4' printPeople
//timeAll ex4 ex4' forcePeople



let ageFromName = 
  <@ fun s -> seq{
        for u in db.People do 
        if s = u.Name then 
          yield u.Age } @>

let compose : Expr<string -> string -> seq<PeopleR>> = 
  <@ fun s t -> seq {
      for a in (%ageFromName) s do
      for b in (%ageFromName) t do 
      yield! (%range) a b
  } @>



let ageFromName' = 
  <@ fun s -> query {
        for u in db.People do 
        if s = u.Name then 
          yield u.Age } @>

let compose' : Expr<string -> string -> IQueryable<PeopleR>> = 
  <@ fun s t -> query {
      for a in (%ageFromName') s do
      for b in (%ageFromName') t do 
      yield! (%range) a b
  } @>


// example 5

let ex5 = <@ (%compose) "Eve" "Bob" @>
let ex5' = <@  (%compose') "Eve" "Bob"  @> 

//testAll ex5 ex5' printPeople
//timeAll ex5 ex5' forcePeople


type Predicate = 
  | Above of int
  | Below of int
  | And of Predicate * Predicate
  | Or of Predicate * Predicate
  | Not of Predicate

let t0 : Predicate = And (Above 20, Below 30)
let t1 : Predicate = Not(Or(Below 20, Above 30))

let rec eval(t:Predicate) : Expr<int -> bool> =
  match t with
  | Above n -> <@ fun x -> x >= n @>
  | Below n -> <@ fun x -> x < n @>
  | And (t1,t2) -> <@ fun x -> (%eval t1) x && (%eval t2) x @>
  | Or (t1,t2) -> <@ fun x -> (%eval t1) x || (%eval t2) x @>
  | Not (t0) -> <@ fun x -> not((%eval t0) x ) @>


// example 6
let ex6 = <@ (%satisfies) (%eval t0)@>
let ex6' = <@ (%satisfies') (%eval t0) @>
//testAll ex6 ex6' printPeople
//timeAll ex6 ex6' forcePeople

// example 7

let ex7 = <@ (%satisfies) (%eval t1)@>
let ex7' = <@ (%satisfies') (%eval t1) @>

//testAll ex7 ex7' printPeople
//timeAll ex7 ex7' forcePeople

let doBasicTest() = 
    timeAll ex1 ex1' forcePeople
    printfn "ex2"
    timeAll ex2 ex2' forcePeople
    printfn "ex3"
    timeAll ex3 ex3' forcePeople
    printfn "ex4"
    timeAll ex4 ex4' forcePeople
    printfn "ex5"
    timeAll ex5 ex5' forcePeople
    printfn "ex6"
    timeAll ex6 ex6' forcePeople
    printfn "ex7"
    timeAll ex7 ex7' forcePeople


let doTest n = 
    dropTables()
    addRandom n
    printfn "ex1"
    timeAll ex1 ex1' forcePeople
    printfn "ex2"
    timeAll ex2 ex2' forcePeople
    printfn "ex3"
    timeAll ex3 ex3' forcePeople
    printfn "ex4"
    timeAll ex4 ex4' forcePeople
    printfn "ex5"
    timeAll ex5 ex5' forcePeople
    printfn "ex6"
    timeAll ex6 ex6' forcePeople
    printfn "ex7"
    timeAll ex7 ex7' forcePeople


let doTest'()  =

    [("ex1",    timeAll' ex1 ex1' forcePeople);
     ("ex2",    timeAll' ex2 ex2' forcePeople);
     ("ex3",    timeAll' ex3 ex3' forcePeople);
     ("ex4",    timeAll' ex4 ex4' forcePeople);
     ("ex5",    timeAll' ex5 ex5' forcePeople);
     ("ex6",    timeAll' ex6 ex6' forcePeople);
     ("ex7",    timeAll' ex7 ex7' forcePeople)]