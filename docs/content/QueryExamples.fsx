(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"


(**
Lots of sample queries
========================

This file collects the sample queries from the Visual Studio documentation for F#.  The original queries can be found here: 

 * [FSharp Query Expressions](http://msdn.microsoft.com/en-us/library/vstudio/hh225374.aspx)

*)
#r "FSharp.Data.TypeProviders.dll"
#r "System.Data.dll"
#r "System.Data.Linq.dll"
#r "FSharp.PowerPack.Linq.dll"
#r "FSharpComposableQuery.dll"

open System
open System.Data.Linq.SqlClient
open System.Linq

open Microsoft.FSharp.Data.TypeProviders
open Microsoft.FSharp.Linq

open FSharpComposableQuery

(** Query examples  *)

(**
We assume a locally configured SQL Srever database server with the database MyDatabase populated as shown 
on [this page](http://msdn.microsoft.com/en-us/library/vstudio/hh225374.aspx).  All of the queries run correctly
with FSharpComposableQuery and produce the same answers as the main implementation of F# query.  This 
is because FSharpComposableQuery leaves query operations it doesn't recognize in place.

*)

[<Literal>]
let ConnectionString = 
  "Data Source=(localdb)\MyInstance;\
   Initial Catalog=MyDatabase;
   Integrated Security=SSPI"

type schema = SqlDataConnection<ConnectionString>

let db = schema.GetDataContext()
//db.DataContext.Log <- System.Console.Out

let student = db.Student

let data = [1; 5; 7; 11; 18; 21]

type Nullable<'T when 'T : ( new : unit -> 'T) and 'T : struct and 'T :> ValueType > with
    member this.Print() =
        if (this.HasValue) then this.Value.ToString()
        else "NULL"

let testQuery f = f query query

// Convenience copies of failing tests


// Start
(**  contains query operator
*)
query {
        for student in db.Student do
        select student.Age.Value
        contains 11
        }|> printfn "Is at least one student age 11? %b" 


(**
count query operator
*)
query {
        for student in db.Student do
        select student
        count
        } |> printfn "Number of students: %d" 


(**
last query operator.
*)
query {
        for s in data do
        sortBy s
        last
        } 
|> printfn "Last: %d" 


(**
lastOrDefault query operator.
*)
query {
            for number in data do
            sortBy number
            lastOrDefault
            }  
|> printfn "lastOrDefault: %d"

(**
exactlyOne query operator
*)
let student2 =
    query {
        for student in db.Student do
        where (student.StudentID = 1)
        select student
        exactlyOne
        }
printfn "Student with StudentID = 1 is %s" student2.Name

(**
exactlyOneOrDefault query operator
*)
let student3 =
    query {
        for student in db.Student do
        where (student.StudentID = 1)
        select student
        exactlyOneOrDefault
        }
printfn "Student with StudentID = 1 is %s" student3.Name

(**
headOrDefault query operator
*)
let student4 =
    query {
        for student in db.Student do
        select student
        headOrDefault
        }
printfn "head student is %s" student4.Name

(**
select query operator
*)
query {
        for (student:schema.ServiceTypes.Student) in db.Student do
        select student
        }
|> Seq.iter (fun (student:schema.ServiceTypes.Student) -> printfn "StudentID, Name: %d %s" student.StudentID student.Name)

(**
where query operator
*)
query {
        for student in db.Student do
        where (student.StudentID > 4)
        select student
        }
|> Seq.iter (fun student -> printfn "StudentID, Name: %d %s" student.StudentID student.Name)

(**
minBy query operator
*)
let student5 =
    query {
        for student in db.Student do
        minBy student.StudentID
    }
(**
maxBy query operator
*)
let student6 =
    query {
        for student in db.Student do
        maxBy student.StudentID
    }
    
(**
groupBy query operator.
*)
query {
    for student in db.Student do
    groupBy student.Age into g
    select (g.Key, g.Count())
    }
|> Seq.iter (fun (age, count) -> printfn "Age: %s Count at that age: %d" (age.Print()) count)

(**
sortBy query operator
*)
query {
    for student in db.Student do
    sortBy student.Name
    select student
}
|> Seq.iter (fun student -> printfn "StudentID, Name: %d %s" student.StudentID student.Name)

(**
sortByDescending query operator
*)
query {
    for student in db.Student do
    sortByDescending student.Name
    select student
}
|> Seq.iter (fun student -> printfn "StudentID, Name: %d %s" student.StudentID student.Name)

(**
thenBy query operator
*)
query {
    for student in db.Student do
    where student.Age.HasValue
    sortBy student.Age.Value
    thenBy student.Name
    select student
}
|> Seq.iter (fun student -> printfn "StudentID, Name: %d %s" student.Age.Value student.Name)

(**
thenByDescending query operator
*)
query {
    for student in db.Student do
    where student.Age.HasValue
    sortBy student.Age.Value
    thenByDescending student.Name
    select student
}
|> Seq.iter (fun student -> printfn "StudentID, Name: %d %s" student.Age.Value student.Name)

(**
groupValBy query operator
*)
query {
    for student in db.Student do
    groupValBy student.Name student.Age into g
    select (g, g.Key, g.Count())
    }
|> Seq.iter (fun (group, age, count) ->
    printfn "Age: %s Count at that age: %d" (age.Print()) count
    group |> Seq.iter (fun name -> printfn "Name: %s" name))

(**
 sumByNullable query operator
*)
query {
    for student in db.Student do
    sumByNullable student.Age
    }
|> (fun sum -> printfn "Sum of ages: %s" (sum.Print()))

(**
 minByNullable
*)
query {
    for student in db.Student do
    minByNullable student.Age
    }
|> (fun age -> printfn "Minimum age: %s" (age.Print()))

(**
 maxByNullable
*)
query {
    for student in db.Student do
    maxByNullable student.Age
    }
|> (fun age -> printfn "Maximum age: %s" (age.Print()))

(**
 averageBy
*)
query {
    for student in db.Student do
    averageBy (float student.StudentID)
    }
|> printfn "Average student ID: %f"

(**
 averageByNullable
*)
query {
    for student in db.Student do
    averageByNullable (Nullable.float student.Age)
    }
|> (fun avg -> printfn "Average age: %s" (avg.Print()))

(**
 find query operator
*)
query {
    for student in db.Student do
    find (student.Name = "Abercrombie, Kim")
}
|> (fun student -> printfn "Found a match with StudentID = %d" student.StudentID)


(**
 all query operator
*)
query {
    for student in db.Student do
    all (SqlMethods.Like(student.Name, "%,%"))
}
|> printfn "Do all students have a comma in the name? %b"


(**
 head query operator
*)
query {
    for student in db.Student do
    head
    }
|> (fun student -> printfn "Found the head student with StudentID = %d" student.StudentID)

(**
 nth query operator
*)
query {
    for numbers in data do
    nth 3
    }
|> printfn "Third number is %d"

(**
 skip query operator
*)
query {
    for student in db.Student do
    skip 1
    }
|> Seq.iter (fun student -> printfn "StudentID = %d" student.StudentID)

(**
 skipWhile query operator
*)
query {
    for number in data do
    skipWhile (number < 3)
    select number
    }
|> Seq.iter (fun number -> printfn "Number = %d" number)


(**
 sumBy query operator
*)
query {
   for student in db.Student do
   sumBy student.StudentID
   }
|> printfn "Sum of student IDs: %d" 

(**
 take query operator
*)
query {
   for student in db.Student do
   select student
   take 2
   }
|> Seq.iter (fun student -> printfn "StudentID = %d" student.StudentID)

(**
 takeWhile query operator
*)
query {
    for number in data do
    takeWhile (number < 10)
    }
|> Seq.iter (fun number -> printfn "Number = %d" number)

(**
 sortByNullable query operator
*)
query {
    for student in db.Student do
    sortByNullable student.Age
    select student
}
|> Seq.iter (fun student ->
    printfn "StudentID, Name, Age: %d %s %s" student.StudentID student.Name (student.Age.Print()))

(**
 sortByNullableDescending query operator
*)
query {
    for student in db.Student do
    sortByNullableDescending student.Age
    select student
}
|> Seq.iter (fun student ->
    printfn "StudentID, Name, Age: %d %s %s" student.StudentID student.Name (student.Age.Print()))

(**
 thenByNullable query operator
*)
query {
    for student in db.Student do
    sortBy student.Name
    thenByNullable student.Age
    select student
}
|> Seq.iter (fun student ->
    printfn "StudentID, Name, Age: %d %s %s" student.StudentID student.Name (student.Age.Print()))

(**
 thenByNullableDescending query operator
*)
query {
    for student in db.Student do
    sortBy student.Name
    thenByNullableDescending student.Age
    select student
}
|> Seq.iter (fun student ->
    printfn "StudentID, Name, Age: %d %s %s" student.StudentID student.Name (student.Age.Print()))


(**
All students: 
*)
query {
        for student in db.Student do
        select student
    }
    |> Seq.iter (fun student -> printfn "%s %d %s" student.Name student.StudentID (student.Age.Print()))


(**
Count of students: 
*)
query {
        for student in db.Student do        
        count
    }
|>  (fun count -> printfn "Student count: %d" count)

(**
Exists
*)
query {
        for student in db.Student do
        where (ExtraTopLevelOperators.query 
                      { for courseSelection in db.CourseSelection do
                        exists (courseSelection.StudentID = student.StudentID) })
        select student }
|> Seq.iter (fun student -> printfn "%A" student.Name)


(**
 Group by age and count
*)
query {
        for n in db.Student do
        groupBy n.Age into g
        select (g.Key, g.Count())
}
|> Seq.iter (fun (age, count) -> printfn "%s %d" (age.Print()) count)

(**
 Group value by age
*)
query {
        for n in db.Student do
        groupValBy n.Age n.Age into g
        select (g.Key, g.Count())
    }
|> Seq.iter (fun (age, count) -> printfn "%s %d" (age.Print()) count)


    



(**
Group students by age where age > 10
*)
query {
        for student in db.Student do
        groupBy student.Age into g
        where (g.Key.HasValue && g.Key.Value > 10)
        select (g, g.Key)
}
|> Seq.iter (fun (students, age) ->
    printfn "Age: %s" (age.Value.ToString())
    students
    |> Seq.iter (fun student -> printfn "%s" student.Name))

(**
Group students by age and print counts of number of students at each age with more than 1 student
*)
query {
        for student in db.Student do
        groupBy student.Age into group
        where (group.Count() > 1)
        select (group.Key, group.Count())
}
|> Seq.iter (fun (age, ageCount) ->
     printfn "Age: %s Count: %d" (age.Print()) ageCount)

(**
Group students by age and sum ages
*)
query {
        for student in db.Student do
        groupBy student.Age into g        
        let total = query { for student in g do sumByNullable student.Age }
        select (g.Key, g.Count(), total)
}
|> Seq.iter (fun (age, count, total) ->
    printfn "Age: %d" (age.GetValueOrDefault())
    printfn "Count: %d" count
    printfn "Total years: %s" (total.ToString()))


(**
Group students by age and count number of students at each age, and display all with count > 1 in descending order of count
*)
query {
        for student in db.Student do
        groupBy student.Age into g
        where (g.Count() > 1)        
        sortByDescending (g.Count())
        select (g.Key, g.Count())
}
|> Seq.iter (fun (age, myCount) ->
    printfn "Age: %s" (age.Print())
    printfn "Count: %d" myCount)

(**
 Select students from a set of IDs
*)
let idList = [1; 2; 5; 10]
let idQuery = query { for id in idList do
                       select id }
query {
        for student in db.Student do
        where (idQuery.Contains(student.StudentID))
        select student
        }
|> Seq.iter (fun student ->
    printfn "Name: %s" student.Name)

(**
Look for students with Name match _e%% pattern and take first two
*)
query {
    for student in db.Student do
    where (SqlMethods.Like( student.Name, "_e%") )
    select student
    take 2   
    }
|> Seq.iter (fun student -> printfn "%s" student.Name)

(**
Look for students with Name matching [abc]%% pattern
*)
query {
    for student in db.Student do
    where (SqlMethods.Like( student.Name, "[abc]%") )
    select student  
    }
|> Seq.iter (fun student -> printfn "%s" student.Name)

(**
Look for students with name matching [^abc]%% pattern
*)
query {
    for student in db.Student do
    where (SqlMethods.Like( student.Name, "[^abc]%") )
    select student  
    }
|> Seq.iter (fun student -> printfn "%s" student.Name)

(**
Look for students with name matching [^abc]%% pattern and select ID
*)
query {
    for n in db.Student do
    where (SqlMethods.Like( n.Name, "[^abc]%") )
    select n.StudentID    
    }
|> Seq.iter (fun id -> printfn "%d" id)

(**
 Using Contains as a query filter
*)
query {
        for student in db.Student do
        where (student.Name.Contains("a"))
        select student
    }
|> Seq.iter (fun student -> printfn "%s" student.Name)


(**
Searching for names from a list
*)
let names = [|"a";"b";"c"|]
query {
    for student in db.Student do
    if names.Contains (student.Name) then select student }
|> Seq.iter (fun student -> printfn "%s" student.Name)

(**
Join Student and CourseSelection tables
*)
query {
        for student in db.Student do 
        join selection in db.CourseSelection 
          on (student.StudentID = selection.StudentID)
        select (student, selection)
    }
|> Seq.iter (fun (student, selection) -> printfn "%d %s %d" student.StudentID student.Name selection.CourseID)

(**
Left Join Student and CourseSelection tables
*)
query {
    for student in db.Student do
    leftOuterJoin selection in db.CourseSelection 
      on (student.StudentID = selection.StudentID) into result
    for selection in result.DefaultIfEmpty() do
    select (student, selection)
    }
|> Seq.iter (fun (student, selection) ->
    let selectionID, studentID, courseID =
        match selection with
        | null -> "NULL", "NULL", "NULL"
        | sel -> (sel.ID.ToString(), sel.StudentID.ToString(), sel.CourseID.ToString())
    printfn "%d %s %d %s %s %s" student.StudentID student.Name (student.Age.GetValueOrDefault()) selectionID studentID courseID)

(**
Join with count
*)
query {
        for n in db.Student do 
        join e in db.CourseSelection on (n.StudentID = e.StudentID)
        count        
    }
|>  printfn "%d"

(**
 Join with distinct
*)
query {
        for student in db.Student do 
        join selection in db.CourseSelection on (student.StudentID = selection.StudentID)
        distinct        
    }
|> Seq.iter (fun (student, selection) -> printfn "%s %d" student.Name selection.CourseID)

(**
 Join with distinct and count
*)
query {
        for n in db.Student do 
        join e in db.CourseSelection on (n.StudentID = e.StudentID)
        distinct
        count       
    }
|> printfn "%d"


(**
 Selecting students with age between 10 and 15
*)
query {
        for student in db.Student do
        where (student.Age.Value >= 10 && student.Age.Value < 15)
        select student
    }
|> Seq.iter (fun student -> printfn "%s" student.Name)

(**
 Selecting students with age either 11 or 12
*)
query {
        for student in db.Student do
        where (student.Age.Value = 11 || student.Age.Value = 12)
        select student
    }
|> Seq.iter (fun student -> printfn "%s" student.Name)

(**
 Selecting students in a certain age range and sorting
*)
query {
        for n in db.Student do
        where (n.Age.Value = 12 || n.Age.Value = 13)
        sortByNullableDescending n.Age
        select n
    }
|> Seq.iter (fun student -> printfn "%s %s" student.Name (student.Age.Print()))

(**
 Selecting students with certain ages, taking account of possibility of nulls
*)
query {
        for student in db.Student do
        where ((student.Age.HasValue && student.Age.Value = 11) ||
               (student.Age.HasValue && student.Age.Value = 12))
        sortByDescending student.Name 
        select student.Name
        take 2
    }
|> Seq.iter (fun name -> printfn "%s" name)


(**
 Union of two queries
*)
module Queries =
    let query1 = query {
            for n in db.Student do
            select (n.Name, n.Age)
        }

    let query2 = query {
            for n in db.LastStudent do
            select (n.Name, n.Age)
            }

    query2.Union (query1)
    |> Seq.iter (fun (name, age) -> printfn "%s %s" name (age.Print()))

(**
 Intersect of two queries
*)
module Queries2 =
    let query1 = query {
           for n in db.Student do
           select (n.Name, n.Age)
        }

    let query2 = query {
            for n in db.LastStudent do
            select (n.Name, n.Age)
            }

    query1.Intersect(query2)
    |> Seq.iter (fun (name, age) -> printfn "%s %s" name (age.Print()))

(**
 Using if statement to alter results for special value
*)
query {
        for student in db.Student do
        select (if student.Age.HasValue && student.Age.Value = -1 then
                   (student.StudentID, System.Nullable<int>(100), student.Age)
                else (student.StudentID, student.Age, student.Age))
    }
|> Seq.iter (fun (id, value, age) -> printfn "%d %s %s" id (value.Print()) (age.Print()))

(**
 Using if statement to alter results special values
*)
query {
        for student in db.Student do
        select (if student.Age.HasValue && student.Age.Value = -1 then
                   (student.StudentID, System.Nullable<int>(100), student.Age)
                elif student.Age.HasValue && student.Age.Value = 0 then
                    (student.StudentID, System.Nullable<int>(100), student.Age)
                else (student.StudentID, student.Age, student.Age))
    }
|> Seq.iter (fun (id, value, age) -> printfn "%d %s %s" id (value.Print()) (age.Print()))


(**
Multiple table select
*)
query {
        for student in db.Student do
        for course in db.Course do
        select (student, course)
}
|> Seq.iteri (fun index (student, course) ->
    if (index = 0) then printfn "StudentID Name Age CourseID CourseName"
    printfn "%d %s %s %d %s" student.StudentID student.Name (student.Age.Print()) course.CourseID course.CourseName)

(**
Multiple Joins
*)
query {
    for student in db.Student do
    join courseSelection in db.CourseSelection on
        (student.StudentID = courseSelection.StudentID)
    join course in db.Course on
          (courseSelection.CourseID = course.CourseID)
    select (student.Name, course.CourseName)
    }
    |> Seq.iter (fun (studentName, courseName) -> printfn "%s %s" studentName courseName)

(**
Multiple Left Outer Joins
*)
query {
    for student in db.Student do
    leftOuterJoin courseSelection in db.CourseSelection 
      on (student.StudentID = courseSelection.StudentID) into g1
    for courseSelection in g1.DefaultIfEmpty() do
    leftOuterJoin course in db.Course 
      on (courseSelection.CourseID = course.CourseID) into g2
    for course in g2.DefaultIfEmpty() do
    select (student.Name, course.CourseName)
    }
|> Seq.iter (fun (studentName, courseName) -> printfn "%s %s" studentName courseName)

