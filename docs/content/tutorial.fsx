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

#r "FSharpComposableQuery.dll"
#r "FSharp.Data.TypeProviders.dll"
#r "System.Data.dll"
#r "System.Data.Linq.dll"

open Microsoft.FSharp.Data.TypeProviders
open FSharpComposableQuery

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
In addition, more queries and query compositions work.  We illustrate this through several examples below.

*)

(** 

Parameterizing a query
----------------------

LINQ already supports queries with parameters, as long as those parameters are of base type.  For example, 
to obntain the set fo people with age between two bounds, you can create a query parameterized by two integers:

*)

[<Literal>]
let ConnectionString = 
  "Data Source=(localdb)\MyInstance;\
   Initial Catalog=MyPeople;
   Integrated Security=SSPI";;

type dbSchema = SqlDataConnection<ConnectionString>;;
let db = dbSchema.GetDataContext();;

type People = dbSchema.ServiceTypes.People
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

let ex2 = query { for x in (%range) 30 40 do yield x}


(** The reason is that the first approach only works if the parameters are constants of base type;
the second is more flexible.  *)

(** 

The RunQuery operator 
---------------------

It's a little awkward to evaluate composite queries due to the fact that we can't splice them directly into 
the query brackets: this typechecks, but doesn't do the right thing.

*)

let wrongEx2 : System.Linq.IQueryable<PeopleR> = query { (%range) 30 40 }

(**

To simplify the use of composite queries, we define the following convenience operation that 
evaluates a quoted query result directly:

*)

let runQuery q = query { for x in (%q) do yield x }

(** 

Then, we can equivalently just do: 

*)

let ex2again = runQuery <@ (%range) 30 40 @>


(** 

Building queries using query combinators
----------------------------------------

You can build queries up using other query combinators as follows:

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

(** We could have defined this more directly as a single parameterized query as follows: *)

let composeMonolithic = 
  <@ fun s t -> query {
      for u1 in db.People do
      for u2 in db.People do 
      for u in db.People do
      if s = u1.Name && t = u2.Name && u1.Age <= u.Age && u.Age < u2.Age then
        yield {name=u.Name;age=u.Age}
  } @>

(**

But this involves some code duplication and renaming, and if the subquery we use to find a person's age from their name were to
change, then we would have to change both places --- easy to get wrong.  This way of building the query up from 
components leads to more maintainable code.  

Moreover, this small query is easy enough to compose in your head, but as the size of the query and number of tables 
involved grows, keeping the different parameters and constraints straight becomes more tedious and error-prone.  This
way of defining a query such as compose means that the logical meaning of the query is easy to see by reading the code, 
and the FSharpComposableQuery library does the work of normalizing the query to a form that an SQL database can evaluate
efficiently.

*)

(**

Higher-order parameters 
-----------------------

FSharpComposableQuery lifts the restriction that the parameters to a query have to be of base type: instead,
they can be higher-order functions.  
(Actually, F# does handle some higher-order functions, but FSharpComposableQuery provides a stronger guarantee
of good behavior.)

For example, we can define a query combinator that takes a function 

*)

let satisfies  = 
 <@ fun p -> query { 
    for u in db.People do
    if p u.Age 
    then yield {name=u.Name;age=u.Age}
   } @>

let ex3 = runQuery <@ (%satisfies) (fun x -> 20 <= x && x < 30 ) @>

let ex4 = runQuery <@ (%satisfies) (fun x ->  x % 2 = 0 ) @>

(** 
This is subject to some side-conditions: basically, the function you pass into a higher-order query combinator
may only perform operations that are sensible on the database; recursion and side-effects such as printing are not allowed,
and will result in a run-time error. *)

let wrong1 = runQuery <@ (%satisfies) (fun age -> printfn "%d" age; true) @>

let rec even n = if n = 0 then true
                 else if n = 1 then false
                 else even(n-2)
let wrong2 = runQuery <@ (%satisfies) even @>

(** 
Note that wrong2 is morally equivalent to ex4 above (provided ages are nonnegative), but is not allowed.  The library
is not smart enough to determine that the parameter passed into satisfies is equivalent to an operation that
can be performed on the database (using modular arithmetic); you have to do this yourself.

*)


(** 

Building queries using recursion 
--------------------------------

Although recursion is not allowed *within* a query, you can still use recursion to *build* a query.

Consider the following datatype defining some Boolean predicates on ages:

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

let ex6 = runQuery <@ (%satisfies) (%eval t0) @>

let ex7 = runQuery <@ (%satisfies) (%eval t1) @>

(** Why is this allowed, even though eval is recursive?  
Again, notice that although the (%eval t0) computes a quotation .  This all happens before satisfies starts 
working on the 

Had we instead tried it in this, simpler way:
*)

let rec wrongEval t x =
  match t with
  | Above n -> x >= n
  | Below n -> x < n
  | And (t1,t2) -> wrongEval t1 x && wrongEval t2 x
  | Or (t1,t2) -> wrongEval t1 x || wrongEval t2 x 
  | Not (t0) -> not(wrongEval t0 x)

let wrongEx6 = runQuery <@ (%satisfies) (wrongEval t1) @>

(** then we would run into the same problem as before, because we would be trying to run satisfies on quoted
  code containing recursive calls, which is not allowed.  *)
