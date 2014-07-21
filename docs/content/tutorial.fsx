(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"


(**

Introducing FSharpComposableQuery
=================================

(__work in progress__)

Compositional Query Framework for F# Queries, based on 
[A Practical Theory of Language-Integrated Query (ICFP 2013)](http://dl.acm.org/citation.cfm?id=2500586).  

*)

(**

Referencing the library
-----------------------
*)

#if INTERACTIVE
#r "FSharpComposableQuery.dll"
#r "FSharp.Data.TypeProviders.dll"
#r "System.Data.Linq.dll"
#endif

open FSharpComposableQuery
open Microsoft.FSharp.Data.TypeProviders

(**
All existing F# database and in-memory queries should work as normal. For example:
*)

let data = [1; 5; 7; 11; 18; 21]
let lastNumberInSortedList =
    query {
        for s in data do
        sortBy s
        last
    }

    
(**
In addition, more queries and query compositions work. We illustrate this through several examples below.

Parameterizing a query
----------------------

LINQ already supports queries with parameters, as long as those parameters are of base type. For example, 
to obtain the set of people with age in a given interval, you can create a query parameterized by two integers:

*)
type dbPeople = SqlDataConnection<ConnectionStringName="PeopleConnectionString", ConfigFile="db.config">
let db = dbPeople.GetDataContext()

type People = dbPeople.ServiceTypes.People
type PeopleR = {name:string;age:int}

let range1 = fun (a:int) (b:int) -> 
  query {
    for u in db.People do
    if a <= u.Age && u.Age < b 
    then yield {name=u.Name;age=u.Age}
    } 

let ex1 = range1 30 40

(**
However, doing it this way is not especially reusable, and we recommend doing it this way instead:
*)

let range = <@ fun (a:int) (b:int) -> 
  query {
    for u in db.People do
    if a <= u.Age && u.Age < b 
    then yield {name=u.Name;age=u.Age}
    }  @>
    
let qRangeA = query { yield! (%range) 30 40 }

let qRangeB = query { for x in (%range) 30 40 do yield x}  //equivalent to ex1a


(** The reason is that the first approach only works if the parameters are of base type;
the second is more flexible. 


A note on evaluation
---------------------
As you may have noticed we used a slightly different method of evaluating the query above.
This happens as we can't splice the query directly into the query brackets - the following 
type-checks the arguments but doesn't do the right thing:
*)

let qRangeWrong : System.Linq.IQueryable<PeopleR> = query { (%range) 30 40 }

(**
This happens since the result of (%range) is not explicitly returned in the outer query and gets discarded instead. 

To properly use composite queries, we can instead explicitly return the results of a query with the "yield!" keyword as follows:
*)

let ex2correct = query { yield! (%range) 30 40 }

(** 

If, however, a query returns a single value, we cannot use this method - the "yield!" method works only on queries 
which return rows (collections of base type). We could return the single value using the "yield" keyword, but then the 
resulting variable will be a collection of exactly one item - instead of just that item.  

In such cases you should pass the query expression to the `query.Run` method instead, which will evaluate it as a value:
*) 

// Counts the number of people in the given age range. 
let countRange = <@ fun (a:int) (b:int) -> 
    query { 
        for u in (%range) a b do 
        count 
    } @>


let countCorrectA = query.Run <@ (%countRange) 30 40 @>   // Counts the number of people in their thirties. 

let countCorrectB' = <@ (%countRange) 30 40 @>
let countCorrectB = query.Run countCorrectB'              // Counts the number of people in their thirties.

(** 

Building queries using query combinators
----------------------------------------

We can also build queries using other query combinators. Suppose we want to find the people who are older than 
a given person and at the same time younger than another. 

The naive way of doing this, using a single parameterized query, would be:
*)

let composeMonolithic = 
    <@ fun s t -> query {
        for u1 in db.People do
        for u2 in db.People do 
        for u in db.People do
            if s = u1.Name && t = u2.Name && u1.Age <= u.Age && u.Age < u2.Age then
                yield {name=u.Name;age=u.Age}
    } @>

(**
We can see this solution is far from perfect: there is code duplication, renaming of variables, and it may be hard
to spot the overall structure of the code.  Moreover, while this query is easy enough to compose in one's head, keeping 
track of the different parameters and constraints becomes more tedious and error-prone as the size and number of tables 
involved in a query grows.  

Compare the previous example to the following one:
*)

let ageFromName = 
  <@ fun s -> query {
        for u in db.People do 
        if s = u.Name then 
          yield u.Age } @>

let compose = 
  <@ fun s t -> query {
      for a in (%ageFromName) s do
      for b in (%ageFromName) t do 
      yield! (%range) a b
  } @>

(**
This way of defining a query exemplifies the logical meaning of the query, and makes it much easier to understand its 
purpose from the code. 

Although one can construct similar queries using the default F# builder, the problem is that the 
generated SQL query may be far from perfect. The role of the FSharpComposableQuery library 
in the evaluation of this query is to normalize it to such a form which can then be evaluated as 
efficiently as the flat query above. In fact, all composite queries which have an equivalent flat 
form get reduced to it as part of the normalisation procedure.  


Higher-order parameters 
-----------------------

FSharpComposableQuery lifts the restriction that the parameters to a query have to be of base type: instead,
they can be higher-order functions.  
(Actually, F# does handle some higher-order functions, but FSharpComposableQuery provides a stronger guarantee of good behavior.)

For example, we can define the following query combinator that gets all people whose age matches the argument predicate:
*)

let satisfies  = 
 <@ fun p -> query { 
    for u in db.People do
    if p u.Age 
    then yield {name=u.Name;age=u.Age}
   } @>

(**
We can then use it to find all people in their thirties, or all people with even age:
*)

let ex3 = query.Run <@ (%satisfies) (fun x -> 20 <= x && x < 30 ) @>

let ex4 = query.Run <@ (%satisfies) (fun x -> x % 2 = 0 ) @>

(** 
This is subject to some side-conditions: basically, the function you pass into a higher-order query combinator
may only perform operations that are sensible on the database; recursion and side-effects such as printing are not allowed,
and will result in a run-time error. *)

let wrong1 = query.Run <@ (%satisfies) (fun age -> printfn "%d" age; true) @>

let rec even n = if n = 0 then true
                 else if n = 1 then false
                 else even(n-2)
let wrong2 = query.Run <@ (%satisfies) even @>

(** 
Note that wrong2 is morally equivalent to ex4 above (provided ages are nonnegative), but is not allowed.  The library
is not smart enough to determine that the parameter passed into satisfies is equivalent to an operation that
can be performed on the database (using modular arithmetic); you would have to do this yourself.


Building queries dynamically (using recursion)
--------------------------------

Although recursion is not allowed *within* a query, you can still use recursion to *build* a query.

Consider the following data type defining some Boolean predicates on ages:

*)

type Predicate = 
  | Above of int
  | Below of int
  | And of Predicate * Predicate
  | Or of Predicate * Predicate
  | Not of Predicate

(** For example, we can define the "in their 30s" predicate two different ways as follows:

*)

let t0 : Predicate = And (Above 30, Below 40)
let t1 : Predicate = Not(Or(Below 30, Above 40))

(** We can define an evaluator that takes a predicate and produces a *parameterized query* as follows:
*)

let rec eval(t) =
  match t with
  | Above n -> <@ fun x -> x >= n @>
  | Below n -> <@ fun x -> x < n @>
  | And (t1,t2) -> <@ fun x -> (%eval t1) x && (%eval t2) x @>
  | Or (t1,t2) -> <@ fun x -> (%eval t1) x || (%eval t2) x @>
  | Not (t0) -> <@ fun x -> not((%eval t0) x) @>

(** Notice that given a predicate t, the return value of this function is a quoted function that takes an integer
and returns a boolean.  Moreover, all of the operations we used are Boolean or arithmetic comparisons that any
database can handle in queries.
*)

(** So, we can plug the predicate obtained from evaluation into the satisfies query combinator, as follows:
*)

let ex6 = query.Run <@ (%satisfies) (%eval t0) @>

let ex7 = query.Run <@ (%satisfies) (%eval t1) @>

(** 
Why is this allowed, even though the eval function is recursive?  
Again, notice that although (%eval t) evaluates recursively to a quotation, all of this happens before it is passed 
as an argument to the satisfies query.  

Had we instead tried it in this, simpler way:
*)

let rec wrongEval t x =
  match t with
  | Above n -> x >= n
  | Below n -> x < n
  | And (t1,t2) -> wrongEval t1 x && wrongEval t2 x
  | Or (t1,t2) -> wrongEval t1 x || wrongEval t2 x 
  | Not (t0) -> not(wrongEval t0 x)

let wrongEx6 = query.Run <@ (%satisfies) (wrongEval t1) @>

(** 
then we would run into the same problem as before, because we would be trying to run satisfies on quoted code containing 
recursive calls, which is not allowed.  
  

Queries over nested structures
--------------------------------
Consider a simple database schema of an organisation `Org` with tables listing departments, the employees and their tasks:
  
  *)
  
type internal orgSchema = SqlDataConnection< ConnectionStringName="OrgConnectionString", ConfigFile="db.config" >


(**
The following parameterised query finds departments where every employee can perform a given task `u`:

*)
let internal orgDb = orgSchema.GetDataContext()

let internal expertiseNaive =
    <@ fun u ->
        query {
            for d in orgDb.Departments do
                if not (query {
                            for e in orgDb.Employees do
                                exists (e.Dpt = d.Dpt && not (query {
                                                                    for t in orgDb.Tasks do
                                                                        exists (e.Emp = t.Emp && t.Tsk = u)
                                                                }))
                        })
                then yield d
        } @>

let internal ex8 = <@ query { yield! (%expertiseNaive) "abstract" } @>

(**
Note that, again, queries constructed in such a way are harder to read and maintain. We will now use a nested 
data structure to help us formulate a more readable equivalent. 

To do so, we would first convert the existing relational structure to a nested one where each _department_ contains
a list of _employees_, and each _employee_ contains a list of _tasks_. The resulting type `NestedOrg` is shown below:
*)
type EmployeeTasks =
    { emp : string; tasks : System.Linq.IQueryable<string> }

type DepartmentEmployees =
    { dpt : string; employees : System.Linq.IQueryable<EmployeeTasks> }

type NestedOrg = System.Linq.IQueryable<DepartmentEmployees>

(**

We can convert the initial representation into the second as follows:
*)

let nestedOrg =
    <@ query {
            for d in orgDb.Departments do
                yield { dpt = d.Dpt                         //for each department d
                        employees =
                            query {
                                for e in orgDb.Employees do    //add employees working in it
                                    if d.Dpt = e.Dpt then
                                        yield { emp = e.Emp
                                                tasks =     //with their assigned tasks
                                                    query {
                                                        for t in orgDb.Tasks do
                                                            if t.Emp = e.Emp then 
                                                                yield t.Tsk
                                                    } }
                            } }
        } @>

(**

Note that we cannot evaluate `query.Run { yield! (%nestedOrg) }` because `query.Run` requires a flat argument,
and the return type of `nestedOrg` is nested. However, this nested structure becomes convenient when used to 
formulate other queries. 


For convenience we declare several higher-order queries. The first one takes a predicate and a list and returns 
whether the predicate holds for any item in the list:
*)

let any =
    <@ fun xs p ->
        query {
            for x in xs do
                exists (p x)
        } @>

(**
The second takes a predicate and a list and returns whether the predicate holds for all items in the list:
*)

let all' = //clashes with default method name

    <@ fun xs p ->
        not (query {
                    for x in xs do
                        exists (not (p (x)))
                }) @>
                

(**
The third one takes a value and a list and returns whether the value occurs in the list:
*)
let contains' = //clashes with default method name
    <@ fun xs u -> (%any) xs (fun x -> x = u) @>

(**
We can now define a query, equivalent to `expertiseNaive`, using these three operators and the nested structure
we created:
*)

let expertise =
    <@ fun u ->
        query {
            for d in (%nestedOrg) do
                if (%all') (d.employees) (fun e -> (%contains') e.tasks u) then yield d.dpt
        } @>

(**

Executing the query
*)

let ex9 = <@ query { yield! (%expertise) "abstract" } @>

(**
yields the same query as the previous example (8). 

Note that it is still possible to run these queries using the default QueryBuilder.
It is simply that such queries operating on nested types may (and will?) cause the default QueryBuilder to output a multitude
of SQL queries roughly proportional to the amount of rows in the tables (?). 

The role of the FSharpComposableQuery library in this case is to provide a strong guarantee on the number of generated SQL queries. 
More specifically, it guarantees to output a single SQL query for all input queries which can be transformed to such a query. 

*)