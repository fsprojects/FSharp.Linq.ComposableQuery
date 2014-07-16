namespace FSharpComposableQuery.Tests

open System
open System.Data.Linq.SqlClient
open System.Linq
open Microsoft.FSharp.Data.TypeProviders
open Microsoft.FSharp.Linq
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.VisualStudio.TestTools.UnitTesting;

open FSharpComposableQuery

module QueryTests = 
    type internal schema = SqlDataConnection<ConnectionStringName="QueryConnectionString", ConfigFile=".\\App.config">

    [<TestClass>]
    type TestClass() = 
        inherit FSharpComposableQuery.Tests.TestClass()

        let db = schema.GetDataContext()


        let data = [1; 5; 7; 11; 18; 21]

        
        let printNullable (v:Nullable<'T>) =
            if (v.HasValue) then v.Value.ToString()
            else "NULL"


        [<TestMethod>]
        member this.``contains query operator``() = 
            printfn "Q%02d: %s" (this.tag()) "contains query operator"
            let result1 =
                query {
                    for student in db.Student do
                    select student.Age.Value
                    contains 11
                    }
            result1 |> printfn "Is at least one student age 11? %b" 


        [<TestMethod>]
        member this.``count query operator``() = 
            printfn "Q%02d: %s" (this.tag()) "count query operator"
            let result2 =
                query {
                    for student in db.Student do
                    select student
                    count
                    }

            result2 |> printfn "Number of students: %d" 


        [<TestMethod>]
        member this.``last query operator.``() = 
            printfn "Q%02d: %s" (this.tag()) "last query operator." 
            let result3 =
                query {
                    for s in data do
                    sortBy s
                    last
                    }
            printfn "Last: %d" result3


        [<TestMethod>]
        member this.``lastOrDefault query operator.``() = 
            printfn "Q%02d: %s" (this.tag()) "lastOrDefault query operator." 
            let result4 =
                query {
                        for number in data do
                        sortBy number
                        lastOrDefault
                        }
            result4 |> printfn "lastOrDefault: %d"


        [<TestMethod>]
        member this.``exactlyOne query operator.``() = 
            printfn "Q%02d: %s" (this.tag()) "exactlyOne query operator."
            let student2 =
                query {
                    for student in db.Student do
                    where (student.StudentID = 1)
                    select student
                    exactlyOne
                    }
            printfn "Student with StudentID = 1 is %s" student2.Name


        [<TestMethod>]
        member this.``exactlyOneOrDefault query operator.``() = 
            printfn "Q%02d: %s" (this.tag()) "exactlyOneOrDefault query operator."
            let student3 =
                query {
                    for student in db.Student do
                    where (student.StudentID = 1)
                    select student
                    exactlyOneOrDefault
                    }
            printfn "Student with StudentID = 1 is %s" student3.Name


        [<TestMethod>]
        member this.``headOrDefault query operator.``() = 
            printfn "Q%02d: %s" (this.tag()) "headOrDefault query operator."
            let student4 =
                query {
                    for student in db.Student do
                    select student
                    headOrDefault
                    }
            printfn "head student is %s" student4.Name


        [<TestMethod>]
        member this.``select query operator.``() = 
            printfn "Q%02d: %s" (this.tag()) "select query operator."
            let select5 = 
                query {
                    for (student) in db.Student do
                    select student
                    }
            select5 |> Seq.iter (fun (student) -> printfn "StudentID, Name: %d %s" student.StudentID student.Name)


        [<TestMethod>]
        member this.``where query operator.``() = 
            printfn "Q%02d: %s" (this.tag()) "where query operator."
            let select6 = 
                query {
                    for student in db.Student do
                    where (student.StudentID > 4)
                    select student
                    }
            select6 |> Seq.iter (fun student -> printfn "StudentID, Name: %d %s" student.StudentID student.Name)
            ignore 0

        [<TestMethod>]
        member this.``minBy query operator.``() = 
            printfn "Q%02d: %s" (this.tag()) "minBy query operator."
            let student5 =
                query {
                    for student in db.Student do
                    minBy student.StudentID
                }
            ignore 0


        [<TestMethod>]
        member this.``maxBy query operator.``() = 
            printfn "Q%02d: %s" (this.tag()) "maxBy query operator."
            let student6 =
                query {
                    for student in db.Student do
                    maxBy student.StudentID
                }
            ignore 0

    

        [<TestMethod>]
        member this.``groupBy query operator.``() = 
            printfn "Q%02d: %s" (this.tag()) "groupBy query operator."
            query {
                for student in db.Student do
                groupBy student.Age into g
                select (g.Key, g.Count())
                }
            |> Seq.iter (fun (age, count) -> printfn "Age: %s Count at that age: %d" (printNullable age) count)


        [<TestMethod>]
        member this.``sortBy query operator.``() = 
            printfn "Q%02d: %s" (this.tag()) "sortBy query operator."
            query {
                for student in db.Student do
                sortBy student.Name
                select student
            }
            |> Seq.iter (fun student -> printfn "StudentID, Name: %d %s" student.StudentID student.Name)


        [<TestMethod>]
        member this.``sortByDescending query operator.``() = 
            printfn "Q%02d: %s" (this.tag()) "sortByDescending query operator."
            query {
                for student in db.Student do
                sortByDescending student.Name
                select student
            }
            |> Seq.iter (fun student -> printfn "StudentID, Name: %d %s" student.StudentID student.Name)


        [<TestMethod>]
        member this.``thenBy query operator.``() = 
            printfn "Q%02d: %s" (this.tag()) "thenBy query operator."
            query {
                for student in db.Student do
                where student.Age.HasValue
                sortBy student.Age.Value
                thenBy student.Name
                select student
            }
            |> Seq.iter (fun student -> printfn "StudentID, Name: %d %s" student.Age.Value student.Name)


        [<TestMethod>]
        member this.``thenByDescending query operator.``() = 
            printfn "Q%02d: %s" (this.tag()) "thenByDescending query operator."
            query {
                for student in db.Student do
                where student.Age.HasValue
                sortBy student.Age.Value
                thenByDescending student.Name
                select student
            }
            |> Seq.iter (fun student -> printfn "StudentID, Name: %d %s" student.Age.Value student.Name)


        [<TestMethod>]
        member this.``groupValBy query operator.``() = 
            printfn "Q%02d: %s" (this.tag()) "groupValBy query operator."
            query {
                for student in db.Student do
                groupValBy student.Name student.Age into g
                select (g, g.Key, g.Count())
                }
            |> Seq.iter (fun (group, age, count) ->
                printfn "Age: %s Count at that age: %d" (printNullable age) count
                group |> Seq.iter (fun name -> printfn "Name: %s" name))


        [<TestMethod>]
        member this.``sumByNullable query operator``() = 
            printfn "Q%02d: %s" (this.tag()) "sumByNullable query operator"
            query {
                for student in db.Student do
                sumByNullable student.Age
                }
            |> (fun sum -> printfn "Sum of ages: %s" (printNullable sum))


        [<TestMethod>]
        member this.``minByNullable``() = 
            printfn "Q%02d: %s" (this.tag()) "minByNullable"
            query {
                for student in db.Student do
                minByNullable student.Age
                }
            |> (fun age -> printfn "Minimum age: %s" (printNullable age))


        [<TestMethod>]
        member this.``maxByNullable``() = 
            printfn "Q%02d: %s" (this.tag()) "maxByNullable"
            query {
                for student in db.Student do
                maxByNullable student.Age
                }
            |> (fun age -> printfn "Maximum age: %s" (printNullable age))


        [<TestMethod>]
        member this.``averageBy``() = 
            printfn "Q%02d: %s" (this.tag()) "averageBy"
            query {
                for student in db.Student do
                averageBy (float student.StudentID)
                }
            |> printfn "Average student ID: %f"


        [<TestMethod>]
        member this.``averageByNullable``() = 
            printfn "Q%02d: %s" (this.tag()) "averageByNullable"
            query {
                for student in db.Student do
                averageByNullable (Nullable.float student.Age)
                }
            |> (fun avg -> printfn "Average age: %s" (printNullable avg))


        [<TestMethod>]
        member this.``find query operator``() = 
            printfn "Q%02d: %s" (this.tag()) "find query operator"
            query {
                for student in db.Student do
                find (student.Name = "Abercrombie, Kim")
            }
            |> (fun student -> printfn "Found a match with StudentID = %d" student.StudentID)


        [<TestMethod>]
        member this.``all query operator``() = 
            printfn "Q%02d: %s" (this.tag()) "all query operator"
            query {
                for student in db.Student do
                all (SqlMethods.Like(student.Name, "%,%"))
            }
            |> printfn "Do all students have a comma in the name? %b"


        [<TestMethod>]
        member this.``head query operator``() = 
            printfn "Q%02d: %s" (this.tag()) "head query operator"
            query {
                for student in db.Student do
                head
                }
            |> (fun student -> printfn "Found the head student with StudentID = %d" student.StudentID)


        [<TestMethod>]
        member this.``nth query operator``() = 
            printfn "Q%02d: %s" (this.tag()) "nth query operator"
            query {
                for numbers in data do
                nth 3
                }
            |> printfn "Third number is %d"


        [<TestMethod>]
        member this.``skip query operator``() = 
            printfn "Q%02d: %s" (this.tag()) "skip query operator"
            query {
                for student in db.Student do
                skip 1
                }
            |> Seq.iter (fun student -> printfn "StudentID = %d" student.StudentID)


        [<TestMethod>]
        member this.``skipWhile query operator``() = 
            printfn "Q%02d: %s" (this.tag()) "skipWhile query operator"
            query {
                for number in data do
                skipWhile (number < 3)
                select number
                }
            |> Seq.iter (fun number -> printfn "Number = %d" number)


        [<TestMethod>]
        member this.``sumBy query operator``() = 
            printfn "Q%02d: %s" (this.tag()) "sumBy query operator"
            query {
               for student in db.Student do
               sumBy student.StudentID
               }
            |> printfn "Sum of student IDs: %d" 


        [<TestMethod>]
        member this.``take query operator``() = 
            printfn "Q%02d: %s" (this.tag()) "take query operator"
            query {
               for student in db.Student do
               select student
               take 2
               }
            |> Seq.iter (fun student -> printfn "StudentID = %d" student.StudentID)


        [<TestMethod>]
        member this.``takeWhile query operator``() = 
            printfn "Q%02d: %s" (this.tag()) "takeWhile query operator"
            query {
                for number in data do
                takeWhile (number < 10)
                }
            |> Seq.iter (fun number -> printfn "Number = %d" number)


        [<TestMethod>]
        member this.``sortByNullable query operator``() = 
            printfn "Q%02d: %s" (this.tag()) "sortByNullable query operator"
            query {
                for student in db.Student do
                sortByNullable student.Age
                select student
            }
            |> Seq.iter (fun student ->
                printfn "StudentID, Name, Age: %d %s %s" student.StudentID student.Name (printNullable student.Age))


        [<TestMethod>]
        member this.``sortByNullableDescending query operator``() = 
            printfn "Q%02d: %s" (this.tag()) "sortByNullableDescending query operator"
            query {
                for student in db.Student do
                sortByNullableDescending student.Age
                select student
            }
            |> Seq.iter (fun student ->
                printfn "StudentID, Name, Age: %d %s %s" student.StudentID student.Name (printNullable student.Age))


        [<TestMethod>]
        member this.``thenByNullable query operator``() = 
            printfn "Q%02d: %s" (this.tag()) "thenByNullable query operator"
            query {
                for student in db.Student do
                sortBy student.Name
                thenByNullable student.Age
                select student
            }
            |> Seq.iter (fun student ->
                printfn "StudentID, Name, Age: %d %s %s" student.StudentID student.Name (printNullable student.Age))


        [<TestMethod>]
        member this.``thenByNullableDescending query operator``() = 
            printfn "Q%02d: %s" (this.tag()) "thenByNullableDescending query operator"
            query {
                for student in db.Student do
                sortBy student.Name
                thenByNullableDescending student.Age
                select student
            }
            |> Seq.iter (fun student ->
                printfn "StudentID, Name, Age: %d %s %s" student.StudentID student.Name (printNullable student.Age))


        [<TestMethod>]
        member this.``All students:``() = 
            printfn "Q%02d: %s" (this.tag()) "All students: "
            query {
                    for student in db.Student do
                    select student
                }
                |> Seq.iter (fun student -> printfn "%s %d %s" student.Name student.StudentID (printNullable student.Age))


        [<TestMethod>]
        member this.``Count of students:``() = 
            printfn "Q%02d: %s" (this.tag()) "Count of students: "
            query {
                    for student in db.Student do        
                    count
                }
            |>  (fun count -> printfn "Student count: %d" count)




        (* This example is the same as above but works, because we use ExtraTopLevelOperators.query *)
        [<TestMethod>]
        member this.``Exists.``() = 
            printfn "Q%02d: %s" (this.tag()) "Exists."
            query {
                    for student in db.Student do
                    where (ExtraTopLevelOperators.query 
                                  { for courseSelection in db.CourseSelection do
                                    exists (courseSelection.StudentID = student.StudentID) })
                    select student }
            |> Seq.iter (fun student -> printfn "%A" student.Name)
    

        (* This example demonstrates the bug *)
        [<TestMethod>]
        member this.``Exists (bug).``() = 
            printfn "Q%02d: %s" (this.tag()) "Exists (bug)."
            query {
                    for student in db.Student do
                    where (query 
                                  { for courseSelection in db.CourseSelection do
                                    exists (courseSelection.StudentID = student.StudentID) })
                    select student }
            |> Seq.iter (fun student -> printfn "%A" student.Name)

        [<TestMethod>]
        member this.``Group by age and count``() = 
            printfn "Q%02d: %s" (this.tag()) "Group by age and count"
            query {
                    for n in db.Student do
                    groupBy n.Age into g
                    select (g.Key, g.Count())
            }
            |> Seq.iter (fun (age, count) -> printfn "%s %d" (printNullable age) count)


        [<TestMethod>]
        member this.``Group value by age.``() = 
            printfn "Q%02d: %s" (this.tag()) "Group value by age."
            query {
                    for n in db.Student do
                    groupValBy n.Age n.Age into g
                    select (g.Key, g.Count())
                }
            |> Seq.iter (fun (age, count) -> printfn "%s %d" (printNullable age) count)


    



        [<TestMethod>]
        member this.``Group students by age where age > 10.``() = 
            printfn "Q%02d: %s" (this.tag()) "Group students by age where age > 10."
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

        [<TestMethod>]
        member this.``Group students by age and print counts of number of students at each age with more than 1 student.``() = 
            printfn "Q%02d: %s" (this.tag()) "Group students by age and print counts of number of students at each age with more than 1 student."
            query {
                    for student in db.Student do
                    groupBy student.Age into group
                    where (group.Count() > 1)
                    select (group.Key, group.Count())
            }
            |> Seq.iter (fun (age, ageCount) ->
                 printfn "Age: %s Count: %d" (printNullable age) ageCount)


        [<TestMethod>]
        member this.``Group students by age and sum ages.``() = 
            printfn "Q%02d: %s" (this.tag()) "Group students by age and sum ages."
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


        [<TestMethod>]
        member this.``Group students by age and count number of students at each age, and display all with count > 1 in descending order of count.``() = 
            printfn "Q%02d: %s" (this.tag()) "Group students by age and count number of students at each age, and display all with count > 1 in descending order of count."
            query {
                    for student in db.Student do
                    groupBy student.Age into g
                    where (g.Count() > 1)        
                    sortByDescending (g.Count())
                    select (g.Key, g.Count())
            }
            |> Seq.iter (fun (age, myCount) ->
                printfn "Age: %s" (printNullable age)
                printfn "Count: %d" myCount)


        [<TestMethod>]
        member this.``Select students from a set of IDs``() = 
            printfn "Q%02d: %s" (this.tag()) "Select students from a set of IDs"
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


        [<TestMethod>]
        member this.``Look for students with Name match _e%% pattern and take first two.``() = 
            printfn "Q%02d: %s" (this.tag()) "Look for students with Name match _e%% pattern and take first two."
            query {
                for student in db.Student do
                where (SqlMethods.Like( student.Name, "_e%") )
                select student
                take 2   
                }
            |> Seq.iter (fun student -> printfn "%s" student.Name)


        [<TestMethod>]
        member this.``Look for students with Name matching [abc]%% pattern.``() = 
            printfn "Q%02d: %s" (this.tag()) "Look for students with Name matching [abc]%% pattern."
            query {
                for student in db.Student do
                where (SqlMethods.Like( student.Name, "[abc]%") )
                select student  
                }
            |> Seq.iter (fun student -> printfn "%s" student.Name)


        [<TestMethod>]
        member this.``Look for students with name matching [^abc]%% pattern.``() = 
            printfn "Q%02d: %s" (this.tag()) "Look for students with name matching [^abc]%% pattern."
            query {
                for student in db.Student do
                where (SqlMethods.Like( student.Name, "[^abc]%") )
                select student  
                }
            |> Seq.iter (fun student -> printfn "%s" student.Name)


        [<TestMethod>]
        member this.``Look for students with name matching [^abc]%% pattern and select ID.``() = 
            printfn "Q%02d: %s" (this.tag()) "Look for students with name matching [^abc]%% pattern and select ID."
            query {
                for n in db.Student do
                where (SqlMethods.Like( n.Name, "[^abc]%") )
                select n.StudentID    
                }
            |> Seq.iter (fun id -> printfn "%d" id)


        [<TestMethod>]
        member this.``Using Contains as a query filter.``() = 
            printfn "Q%02d: %s" (this.tag()) "Using Contains as a query filter."
            query {
                    for student in db.Student do
                    where (student.Name.Contains("a"))
                    select student
                }
            |> Seq.iter (fun student -> printfn "%s" student.Name)


        [<TestMethod>]
        member this.``Searching for names from a list.``() = 
            printfn "Q%02d: %s" (this.tag()) "Searching for names from a list."
            let names = [|"a";"b";"c"|]
            query {
                for student in db.Student do
                if names.Contains (student.Name) then select student }
            |> Seq.iter (fun student -> printfn "%s" student.Name)

    //     *
        [<TestMethod>]
        member this.``Join Student and CourseSelection tables.``() = 
            printfn "Q%02d: %s" (this.tag()) "Join Student and CourseSelection tables."
            query {
                    for student in db.Student do 
                    join selection in db.CourseSelection 
                      on (student.StudentID = selection.StudentID)
                    select (student, selection)
                }
            |> Seq.iter (fun (student, selection) -> printfn "%d %s %d" student.StudentID student.Name selection.CourseID)


        [<TestMethod>]
        member this.``Left Join Student and CourseSelection tables.``() = 
            printfn "Q%02d: %s" (this.tag()) "Left Join Student and CourseSelection tables."
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


        [<TestMethod>]
        member this.``Join with count``() = 
            printfn "Q%02d: %s" (this.tag()) "Join with count"
            query {
                    for n in db.Student do 
                    join e in db.CourseSelection on (n.StudentID = e.StudentID)
                    count        
                }
            |>  printfn "%d"


        [<TestMethod>]
        member this.``Join with distinct.``() = 
            printfn "Q%02d: %s" (this.tag()) "Join with distinct."
            query {
                    for student in db.Student do 
                    join selection in db.CourseSelection on (student.StudentID = selection.StudentID)
                    distinct        
                }
            |> Seq.iter (fun (student, selection) -> printfn "%s %d" student.Name selection.CourseID)


        [<TestMethod>]
        member this.``Join with distinct and count.``() = 
            printfn "Q%02d: %s" (this.tag()) "Join with distinct and count."
            query {
                    for n in db.Student do 
                    join e in db.CourseSelection on (n.StudentID = e.StudentID)
                    distinct
                    count       
                }
            |> printfn "%d"


        [<TestMethod>]
        member this.``Selecting students with age between 10 and 15.``() = 
            printfn "Q%02d: %s" (this.tag()) "Selecting students with age between 10 and 15."
            query {
                    for student in db.Student do
                    where (student.Age.Value >= 10 && student.Age.Value < 15)
                    select student
                }
            |> Seq.iter (fun student -> printfn "%s" student.Name)


        [<TestMethod>]
        member this.``Selecting students with age either 11 or 12.``() = 
            printfn "Q%02d: %s" (this.tag()) "Selecting students with age either 11 or 12."
            query {
                    for student in db.Student do
                    where (student.Age.Value = 11 || student.Age.Value = 12)
                    select student
                }
            |> Seq.iter (fun student -> printfn "%s" student.Name)


        [<TestMethod>]
        member this.``Selecting students in a certain age range and sorting.``() = 
            printfn "Q%02d: %s" (this.tag()) "Selecting students in a certain age range and sorting."
            query {
                    for n in db.Student do
                    where (n.Age.Value = 12 || n.Age.Value = 13)
                    sortByNullableDescending n.Age
                    select n
                }
            |> Seq.iter (fun student -> printfn "%s %s" student.Name (printNullable student.Age))


        [<TestMethod>]
        member this.``Selecting students with certain ages, taking account of possibility of nulls.``() = 
            printfn "Q%02d: %s" (this.tag()) "Selecting students with certain ages, taking account of possibility of nulls."
            query {
                    for student in db.Student do
                    where ((student.Age.HasValue && student.Age.Value = 11) ||
                           (student.Age.HasValue && student.Age.Value = 12))
                    sortByDescending student.Name 
                    select student.Name
                    take 2
                }
            |> Seq.iter (fun name -> printfn "%s" name)


        [<TestMethod>]
        member this.``Union of two queries.``() = 
            printfn "Q%02d: %s" (this.tag()) "Union of two queries."

            let query1 = query {
                    for n in db.Student do
                    select (n.Name, n.Age)
                }

            let query2 = query {
                    for n in db.LastStudent do
                    select (n.Name, n.Age)
                    }

            query2.Union (query1)
            |> Seq.iter (fun (name, age) -> printfn "%s %s" name (printNullable age))


        [<TestMethod>]
        member this.``Intersect of two queries.``() = 
            printfn "Q%02d: %s" (this.tag()) "Intersect of two queries."
            let query1 = query {
                    for n in db.Student do
                    select (n.Name, n.Age)
                }

            let query2 = query {
                    for n in db.LastStudent do
                    select (n.Name, n.Age)
                    }

            query1.Intersect(query2)
            |> Seq.iter (fun (name, age) -> printfn "%s %s" name (printNullable age))


        [<TestMethod>]
        member this.``Using if statement to alter results for special value.``() = 
            printfn "Q%02d: %s" (this.tag()) "Using if statement to alter results for special value."
            query {
                    for student in db.Student do
                    select (if student.Age.HasValue && student.Age.Value = -1 then
                               (student.StudentID, System.Nullable<int>(100), student.Age)
                            else (student.StudentID, student.Age, student.Age))
                }
            |> Seq.iter (fun (id, value, age) -> printfn "%d %s %s" id (printNullable value) (printNullable age))


        [<TestMethod>]
        member this.``Using if statement to alter results special values.``() = 
            printfn "Q%02d: %s" (this.tag()) "Using if statement to alter results special values."
            query {
                    for student in db.Student do
                    select (if student.Age.HasValue && student.Age.Value = -1 then
                               (student.StudentID, System.Nullable<int>(100), student.Age)
                            elif student.Age.HasValue && student.Age.Value = 0 then
                                (student.StudentID, System.Nullable<int>(100), student.Age)
                            else (student.StudentID, student.Age, student.Age))
                }
            |> Seq.iter (fun (id, value, age) -> printfn "%d %s %s" id (printNullable value) (printNullable age))



        [<TestMethod>]
        member this.``Multiple table select.``() = 
            printfn "Q%02d: %s" (this.tag()) "Multiple table select."
            query {
                    for student in db.Student do
                    for course in db.Course do
                    select (student, course)
            }
            |> Seq.iteri (fun index (student, course) ->
                if (index = 0) then printfn "StudentID Name Age CourseID CourseName"
                printfn "%d %s %s %d %s" student.StudentID student.Name (printNullable student.Age) course.CourseID course.CourseName)


        [<TestMethod>]
        member this.``Multiple Joins``() = 
            printfn "Q%02d: %s" (this.tag()) "Multiple Joins"
            query {
                for student in db.Student do
                join courseSelection in db.CourseSelection on
                    (student.StudentID = courseSelection.StudentID)
                join course in db.Course on
                      (courseSelection.CourseID = course.CourseID)
                select (student.Name, course.CourseName)
                }
                |> Seq.iter (fun (studentName, courseName) -> printfn "%s %s" studentName courseName)


        [<TestMethod>]
        member this.``Multiple Left Outer Joins``() = 
            printfn "Q%02d: %s" (this.tag()) "Multiple Left Outer Joins"
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
