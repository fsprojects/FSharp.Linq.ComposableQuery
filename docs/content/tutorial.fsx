(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

#if INTERACTIVE     // reference required libraries when run as a script
#r "FSharpComposableQuery.dll"
#r "FSharp.Data.TypeProviders.dll"
#r "System.Data.Linq.dll"
#endif

open FSharpComposableQuery
open Microsoft.FSharp.Data.TypeProviders

(**

FSharpComposableQuery Tutorial
=================================

Basic Queries
-----------------------

For instruction on how to obtain and install the **FSharpComposableQuery** library, 
check out the [main page](./index.html). 

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


Advanced Queries
----------------------

In addition, more queries and techniques work as compared to the default `Microsoft.FSharp.Core.QueryBuilder`.

We illustrate this through several examples.


### Parameterizing a query

LINQ already supports queries with parameters, as long as those parameters are of base type. For example, 
to obtain the set of people with age in a given interval, you can create a query parameterized by two integers:

*)
// Connect to the SQL data source
type dbPeople = SqlDataConnection<ConnectionStringName="PeopleConnectionString", ConfigFile="db.config">
let db = dbPeople.GetDataContext()

type Person = dbPeople.ServiceTypes.People

let range1 = fun (a:int) (b:int) -> 
  query {
    for u in db.People do
    if a <= u.Age && u.Age < b 
    then yield u
    } 

let ex1 = range1 30 40

(**
However, doing it this way is not especially reusable, and we recommend doing it this way instead:
*)

let range = <@ fun (a:int) (b:int) -> 
  query {
    for u in db.People do
    if a <= u.Age && u.Age < b 
    then yield u
    }  @>
    
let ex2 = query { yield! (%range) 30 40 }


(** 
The reason is that the first approach only works if the parameters are of base type;
the second is more flexible. 


### A note on evaluation

As you may have noticed from the previous example we used a slightly different method of evaluating the query.

If we simply wrote:
*)

let ex2wrong : System.Linq.IQueryable<Person> = query { (%range) 30 40 }

(**
The resulting query would still type-check, compile and execute fine, but the result variable will always contain 
the empty sequence. This happens as the `QueryBuilder` uses the reserved keywords `yield` (for a single item) and 
`yield!` (for collections of items) to add results to the output. As the result of `(%range) a b` is not explicitly 
returned by either, it gets discarded instead. 

To use composite queries properly, we should simply return the results of a query using either keyword as follows:
*)

let ex2again = query { yield! (%range) 30 40 }

let ex2again' = query { for x in (%range) 30 40 do yield x }     // same as ex2again

(** 

What about queries which return a single value?

Such queries include operations that return base or record types, such as the `count`, `last` and `averageBy` methods. 
Here is an example query which counts the number of people in a given age range:
*)
let countRange = <@ fun (a:int) (b:int) -> 
    query { 
        for u in (%range) a b do 
        count 
    } @>

(**
Suppose we wanted to find the number of people in their thirties. Knowing that `countRange` returns a single value 
(in this case an `int`) we could try to passing its result to the `yield` method as follows:
*)
let countWrong = query { yield (%countRange) 30 40 }

(**
This solution is far from perfect though: the variable `countWrong` is of type `IQueryable<int>` when we would actually 
expect it to be of type `int`. This happens as `yield` returns the item as part of a collection. 

Now, _we_ know there is _exactly_ one item in this sequence, so we could access it using any of the methods in the `Seq` 
module or the standard LINQ extensions. 
Fortunately, there is an easier, built-in way to evaluate such expressions, which is to simply pass them to the 
`query.Run` method:
*)

let countCorrect = query.Run <@ (%countRange) 30 40 @>

(** 
This way the resulting variable `countCorrect` is of the expected type `int`. 


### Building queries using query combinators

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
                yield u
    } @>

(**
We can see this solution is far from ideal: there is code duplication, renaming of variables, and it may be hard
to spot the overall structure.  Moreover, while this query is easy enough to compose in one's head, keeping 
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

Although one can construct similar queries using the default F# builder, the problem is that the generated SQL query will
be quite inefficient. The role of the **FSharpComposableQuery** library in the evaluation of this query is to normalize it to
a form at least as efficient as the flat query above. In fact, all queries which have an equivalent flat form get reduced 
to it as part of the normalisation procedure.  


Higher-order parameters 
-----------------------

**FSharpComposableQuery** lifts the intuitive restriction that the parameters to a query have to be of base type: instead,
they can be higher-order functions. It is not that F# fails to handle higher-order functions, but FSharpComposableQuery 
provides a stronger guarantee of good behavior. 

For example, we can define the following query combinator that gets all people whose age matches the argument predicate:
*)

let satisfies  = 
 <@ fun p -> query { 
    for u in db.People do
    if p u.Age 
    then yield u
   } @>

(**
We can then use it to find all people in their thirties, or all people with even age:
*)

let ex3 = query.Run <@ (%satisfies) (fun x -> 20 <= x && x < 30 ) @>

let ex4 = query.Run <@ (%satisfies) (fun x -> x % 2 = 0 ) @>

(** 
This is subject to some side-conditions: basically, the function you pass into a higher-order query combinator
may only perform operations that are sensible on the database; recursion and side-effects such as printing are not allowed,
and will result in a run-time error. 
*)

let wrong1 = query.Run <@ (%satisfies) (fun age -> printfn "%d" age; true) @>

let rec even n = if n = 0 then true         // only works for positive n
                 else if n = 1 then false
                 else even(n-2)

let wrong2 = query.Run <@ (%satisfies) even @>

(** 
Note that `wrong2` is morally equivalent to `ex4` above, provided ages are nonnegative, but is not allowed. The library
is not smart enough to determine that the parameter passed into `satisfies` is equivalent to an operation that
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

(** 
For example, we can define the "in their 30s" predicate two different ways as follows:
*)

let t0 : Predicate = And (Above 30, Below 40)
let t1 : Predicate = Not(Or(Below 30, Above 40))

(** 
We can define an evaluator that takes a predicate and produces a *parameterized query* as follows:
*)

let rec eval(t) =
  match t with
  | Above n -> <@ fun x -> x >= n @>
  | Below n -> <@ fun x -> x < n @>
  | And (t1,t2) -> <@ fun x -> (%eval t1) x && (%eval t2) x @>
  | Or (t1,t2) -> <@ fun x -> (%eval t1) x || (%eval t2) x @>
  | Not (t0) -> <@ fun x -> not((%eval t0) x) @>

(** 
Notice that given a predicate t, the return value of this function is a quoted function that takes an integer
and returns a boolean.  Moreover, all of the operations we used are Boolean or arithmetic comparisons that any
database can handle in queries.

So, we can plug the predicate obtained from evaluation into the `satisfies` query combinator, as follows:
*)

let ex6 = query.Run <@ (%satisfies) (%eval t0) @>

let ex7 = query.Run <@ (%satisfies) (%eval t1) @>

(** 
Why is this allowed, even though the `eval` function is recursive?  

Again, notice that although `(%eval t)` evaluates recursively to a quotation, all of this happens before it is passed 
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

let ex6wrong = query.Run <@ (%satisfies) (wrongEval t1) @>

(** 
then we would run into the same problem as before, because we would be trying to run `satisfies` on quoted code containing 
recursive calls, which is not allowed.  
  

Queries over nested structures
-------------------------------

Even though the input data source and the output SQL query must be flat (i.e. tables of base types), 
we can make use of intermediate structures to represent the data and queries in a more readable fashion. 
The problem is that most often such queries are incredibly inefficient as compared to the 'simple version, 
their runtime bounded by the amount of records in the tables. 

**FSharpComposableQuery** guarantees us that _any_ query which can be rewritten in such a way
as to generate a single SQL query, _will_ be rewritten to it, as part of the normalisation process. 
It thus allows us to use nested structures without having to worry about any significant performance penalties. 

For this example consider the simple database schema `Org` of an organisation 
with tables listing departments, the employees and their tasks:
  
    type Org = 
        {   departments : { dpt : string }                list;
            employees   : { dpt : string; emp : string }  list;
            tasks       : { emp : string; tsk : string }  list; }

Instead of declaring the type manually we will use the LINQ-to-SQL `TypeProvider` to get these directly from the database tables:
*)

type internal dbSchema = SqlDataConnection< ConnectionStringName="OrgConnectionString", ConfigFile="db.config" >

let internal orgDb = dbSchema.GetDataContext()

type internal Department = dbSchema.ServiceTypes.Departments
type internal Employee = dbSchema.ServiceTypes.Employees
type internal Contact = dbSchema.ServiceTypes.Contacts
type internal Task = dbSchema.ServiceTypes.Tasks

(**
As an example we are going to demonstrate a query which finds the departments where every employee can perform 
a given task. 

The following parameterised query accomplishes this in a way similar to how one would write it in SQL:
*)

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

let internal ex8 = query { yield! (%expertiseNaive) "abstract" }

(**
Note that even though the query is efficient and fairly readable, considering the given representation,
it is a single monolithic block which makes it difficult to read and maintain.  


We will now show how the use of a nested data structure can help us formulate more readable queries. 

To accomplish this we will first convert the existing relational structure to a nested one, where each 
_department_ contains a list of _employees_, and each _employee_ contains a list of _tasks_. 
Since these types are different to the types used in the relational schema, the `TypeProvider` is 
of no help here and we have to define them manually. 

The resulting type `NestedOrg` is shown below:
*)

type EmployeeTasks =
    { emp : string; tasks : System.Linq.IQueryable<string> }

type DepartmentEmployees =
    { dpt : string; employees : System.Linq.IQueryable<EmployeeTasks> }

type NestedOrg = System.Linq.IQueryable<DepartmentEmployees>

(**
Note the use of `IQueryable<'T>` instead of `seq` or `list` as it is the type all LINQ database operations
work with and return. 


We can convert the source tables to the second format as follows:
*)

let nestedDb =
    <@ query {
            for d in orgDb.Departments do                   // for each department d
                yield { dpt = d.Dpt
                        employees =
                            query {
                                for e in orgDb.Employees do // add employees working in it
                                    if d.Dpt = e.Dpt then
                                        yield { emp = e.Emp
                                                tasks =     // each with the tasks assigned to him
                                                    query {
                                                        for t in orgDb.Tasks do
                                                            if t.Emp = e.Emp then 
                                                                yield t.Tsk
                                                    } }
                            } }
        } @>

(**
Even though we can evaluate the expression above using `query.Run` and directly query its contents,
each such query will surely generate a multitude of SQL queries - possibly one for every department and employee we
have to go through. 

In most cases though, the queries we are interested in return "simple" data in the form of rows 
(or even a single value). Usually these queries can be written by hand in such a way as to generate a single, 
efficient SQL query; doing so also means one has to use the 


and such queries can be rewritten in a way as to generate a single output SQL query. 
Note that we cannot evaluate `query.Run { yield! (%nestedOrg) }` because `query.Run` requires a flat argument,
and the return type of `nestedOrg` is nested. However, this nested structure becomes useful when used to 
formulate other queries. 


For convenience we will now declare several higher-order queries. 
Note how we can easily express consecutive queries in terms of previous ones. 

The first one takes a predicate and a list and returns whether the predicate holds for any item in the list:
*)
open Microsoft.FSharp.Quotations
open System.Linq

let any() = <@ fun xs p -> query { for x in xs do exists (p x) } @>

(**
The second takes a predicate and a list and returns whether the predicate holds for all items in the list:
*)

let forallA() = <@ fun xs p -> not ((%any()) xs (fun x -> not(p x))) @>

let forallB() = <@ fun xs p -> query { for x in xs do all(p x) } @>

(**
The third one takes a value and a list and returns whether the value occurs in the list:
*)
let containsA() = <@ fun xs u -> (%any()) xs (fun x -> x = u) @>

let containsB() = <@ fun xs u -> not ((%forallA()) xs (fun x -> x <> u)) @>

let containsC() = <@ fun xs u -> query { for x in xs do contains u } @>

(**
The above queries, although defined in different terms, all produce the same efficient SQL output 
when used with the **FSharpComposableQuery** library. 

We can now define and execute a query, equivalent to `expertiseNaive`, using those three operators and the nested structure
we created earlier:
*)

let expertise =
    <@ fun u ->
        query {
            for d in (%nestedDb) do
                if (%forallA()) (d.employees) (fun e -> (%containsA()) e.tasks u) then yield d.dpt
        } @>

let ex9 = query { yield! (%expertise) "abstract" }

(**
yields the same query as in the previous example (8). 

Note that it is still possible to run these queries using the default QueryBuilder.
It is simply that such queries operating on nested types may and will cause the default QueryBuilder to output a multitude
of SQL queries roughly proportional to the amount of rows in the tables involved since it does not try
to simplify (normalise) the expressions inside the query. 

This is what the **FSharpComposableQuery** library accomplishes in such cases. 
It provides a strong guarantee on the number of generated SQL queries: it always outputs a single SQL query for all inputs
which can be transformed to such a form. 

You can observe the difference in the execution time of `ex9` as parsed by the default and the **FSharpComposableQuery** 
builders by running the tests from the `Nested.fs` file in the Tests project. In the case of 5000 employees and 500 
departments the query as executed by the first builder takes significantly more time than the second. 

*)