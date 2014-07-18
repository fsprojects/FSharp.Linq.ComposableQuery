namespace FSharpComposableQuery.Tests

open System
open System.Data.Linq.SqlClient
open System.Linq
open Microsoft.FSharp.Data.TypeProviders
open Microsoft.FSharp.Linq
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open NUnit.Framework

open FSharpComposableQuery

/// <summary>
/// Contains the modified queries from Microsoft's F# example expressions page. 
/// The queries here are further wrapped in quotations to allow for their evaluation in different contexts (see Utils.fs).  
/// <para>These tests require the schema from sql/simple.sql in a database referred to in app.config </para>
/// <para>The original queries can be found at http://msdn.microsoft.com/en-us/library/vstudio/hh225374.aspx </para>
/// </summary>
module Simple = 
    [<Literal>]
    let dbConfigPath = "data\\db.config"
    
    type internal schema = SqlDataConnection<ConnectionStringName="QueryConnectionString", ConfigFile=dbConfigPath>


    [<TestFixture>]
    type TestClass() = 

        let db = schema.GetDataContext()

        let data = [1; 5; 7; 11; 18; 21]

        let mutable idx = 0
        // Generates a unique tag for each consecutive query
        let tag s = 
            idx <- idx + 1
            printfn "Q%02d: %s" idx s
        
        [<Test>]
        member this.``contains query operator``() = 
            tag "contains query operator"
            let q =
              <@ query {
                    for student in db.Student do
                    select student.Age.Value
                    contains 11
                    } @>
            Utils.Run q


        [<Test>]
        member this.``count query operator``() = 
            tag "count query operator"
            let q =
              <@ query {
                    for student in db.Student do
                    select student
                    count
                    } @>
            Utils.Run q



        [<Test>]
        member this.``last query operator.``() = 
            tag "last query operator." 
            let q =
              <@ query {
                    for s in data do
                    sortBy s
                    last
                    } @>
            Utils.Run q


        [<Test>]
        member this.``lastOrDefault query operator.``() = 
            tag "lastOrDefault query operator." 
            let q =
              <@ query {
                        for number in data do
                        sortBy number
                        lastOrDefault
                        } @>
            Utils.Run q



        [<Test>]
        member this.``exactlyOne query operator.``() = 
            tag "exactlyOne query operator."
            let q =
              <@ query {
                    for student in db.Student do
                    where (student.StudentID = 1)
                    select student
                    exactlyOne
                    } @>
            Utils.Run q



        [<Test>]
        member this.``exactlyOneOrDefault query operator.``() = 
            tag "exactlyOneOrDefault query operator."
            let q =
              <@ query {
                    for student in db.Student do
                    where (student.StudentID = 1)
                    select student
                    exactlyOneOrDefault
                    } @>
            Utils.Run q



        [<Test>]
        member this.``headOrDefault query operator.``() = 
            tag "headOrDefault query operator."
            let q =
              <@ query {
                    for student in db.Student do
                    select student
                    headOrDefault
                    } @>
            Utils.Run q



        [<Test>]
        member this.``select query operator.``() = 
            tag "select query operator."
            let q =
              <@ query {
                    for (student) in db.Student do
                    select student
                    } @>
            Utils.Run q



        [<Test>]
        member this.``where query operator.``() = 
            tag "where query operator."
            let q =
              <@ query {
                    for student in db.Student do
                    where (student.StudentID > 4)
                    select student
                    } @>
            Utils.Run q


        [<Test>]
        member this.``minBy query operator.``() = 
            tag "minBy query operator."
            let q =
              <@ query {
                    for student in db.Student do
                    minBy student.StudentID
                } @>
            Utils.Run q



        [<Test>]
        member this.``maxBy query operator.``() = 
            tag "maxBy query operator."
            let q =
              <@ query {
                    for student in db.Student do
                    maxBy student.StudentID
                } @>
            Utils.Run q


    

        [<Test>]
        member this.``groupBy query operator.``() = 
            tag "groupBy query operator."
            let q = <@ query {
                for student in db.Student do
                groupBy student.Age into g
                select (g.Key, g.Count())
                } @>
            Utils.Run q



        [<Test>]
        member this.``sortBy query operator.``() = 
            tag "sortBy query operator."
            let q = <@ query {
                for student in db.Student do
                sortBy student.Name
                select student
            } @>
            Utils.Run q



        [<Test>]
        member this.``sortByDescending query operator.``() = 
            tag "sortByDescending query operator."
            let q = <@ query {
                for student in db.Student do
                sortByDescending student.Name
                select student
            } @>
            Utils.Run q



        [<Test>]
        member this.``thenBy query operator.``() = 
            tag "thenBy query operator."
            let q = <@ query {
                for student in db.Student do
                where student.Age.HasValue
                sortBy student.Age.Value
                thenBy student.Name
                select student
            } @>
            Utils.Run q



        [<Test>]
        member this.``thenByDescending query operator.``() = 
            tag "thenByDescending query operator."
            let q = <@ query {
                for student in db.Student do
                where student.Age.HasValue
                sortBy student.Age.Value
                thenByDescending student.Name
                select student
            } @>
            Utils.Run q



        [<Test>]
        member this.``groupValBy query operator.``() = 
            tag "groupValBy query operator."
            let q = <@ query {
                for student in db.Student do
                groupValBy student.Name student.Age into g
                select (g, g.Key, g.Count())
                } @>
            Utils.Run q



        [<Test>]
        member this.``sumByNullable query operator``() = 
            tag "sumByNullable query operator"
            let q = <@ query {
                for student in db.Student do
                sumByNullable student.Age
                } @>
            Utils.Run q



        [<Test>]
        member this.``minByNullable``() = 
            tag "minByNullable"
            let q = <@ query {
                for student in db.Student do
                minByNullable student.Age
                } @>
            Utils.Run q



        [<Test>]
        member this.``maxByNullable``() = 
            tag "maxByNullable"
            let q = <@ query {
                for student in db.Student do
                maxByNullable student.Age
                } @>
            Utils.Run q



        [<Test>]
        member this.``averageBy``() = 
            tag "averageBy"
            let q = <@ query {
                for student in db.Student do
                averageBy (float student.StudentID)
                } @>
            Utils.Run q



        [<Test>]
        member this.``averageByNullable``() = 
            tag "averageByNullable"
            let q = <@ query {
                for student in db.Student do
                averageByNullable (Nullable.float student.Age)
                } @>
            Utils.Run q



        [<Test>]
        member this.``find query operator``() = 
            tag "find query operator"
            let q = <@ query {
                for student in db.Student do
                find (student.Name = "Abercrombie, Kim")
            } @>
            Utils.Run q



        [<Test>]
        member this.``all query operator``() = 
            tag "all query operator"
            let q = <@ query {
                for student in db.Student do
                all (SqlMethods.Like(student.Name, "%,%"))
            } @>
            Utils.Run q



        [<Test>]
        member this.``head query operator``() = 
            tag "head query operator"
            let q = <@ query {
                for student in db.Student do
                head
                } @>
            Utils.Run q



        [<Test>]
        member this.``nth query operator``() = 
            tag "nth query operator"
            let q = <@ query {
                for numbers in data do
                nth 3
                } @>
            Utils.Run q



        [<Test>]
        member this.``skip query operator``() = 
            tag "skip query operator"
            let q = <@ query {
                for student in db.Student do
                skip 1
                } @>
            Utils.Run q



        [<Test>]
        member this.``skipWhile query operator``() = 
            tag "skipWhile query operator"
            let q = <@ query {
                for number in data do
                skipWhile (number < 3)
                select number
                } @>
            Utils.Run q



        [<Test>]
        member this.``sumBy query operator``() = 
            tag "sumBy query operator"
            let q = <@ query {
               for student in db.Student do
               sumBy student.StudentID
               } @>
            Utils.Run q



        [<Test>]
        member this.``take query operator``() = 
            tag "take query operator"
            let q = <@ query {
               for student in db.Student do
               select student
               take 2
               } @>
            Utils.Run q



        [<Test>]
        member this.``takeWhile query operator``() = 
            tag "takeWhile query operator"
            let q = <@ query {
                for number in data do
                takeWhile (number < 10)
                } @>
            Utils.Run q



        [<Test>]
        member this.``sortByNullable query operator``() = 
            tag "sortByNullable query operator"
            let q = <@ query {
                for student in db.Student do
                sortByNullable student.Age
                select student
            } @>
            Utils.Run q



        [<Test>]
        member this.``sortByNullableDescending query operator``() = 
            tag "sortByNullableDescending query operator"
            let q = <@ query {
                for student in db.Student do
                sortByNullableDescending student.Age
                select student
            } @>
            Utils.Run q



        [<Test>]
        member this.``thenByNullable query operator``() = 
            tag "thenByNullable query operator"
            let q = <@ query {
                for student in db.Student do
                sortBy student.Name
                thenByNullable student.Age
                select student
            } @>
            Utils.Run q



        [<Test>]
        member this.``thenByNullableDescending query operator``() = 
            tag "thenByNullableDescending query operator"
            let q = <@ query {
                for student in db.Student do
                sortBy student.Name
                thenByNullableDescending student.Age
                select student
            } @>
            Utils.Run q



        [<Test>]
        member this.``All students:``() = 
            tag "All students: "
            let q = <@ query {
                    for student in db.Student do
                    select student
                } @>
            Utils.Run q



        [<Test>]
        member this.``Count of students:``() = 
            tag "Count of students: "
            let q = <@ query {
                    for student in db.Student do        
                    count
                } @>
            Utils.Run q


        [<Test>]
        member this.``Exists.``() = 
            tag "Exists, native QueryBuilder."
            let q = <@ query {
                    for student in db.Student do
                    where (ExtraTopLevelOperators.query 
                                  { for courseSelection in db.CourseSelection do
                                    exists (courseSelection.StudentID = student.StudentID) })
                    select student } @>
            Utils.Run q

    


        [<Test>]
        member this.``Exists (bug).``() = 
            tag "Exists."
            let q = <@ query {
                    for student in db.Student do
                    where (query 
                                  { for courseSelection in db.CourseSelection do
                                    exists (courseSelection.StudentID = student.StudentID) })
                    select student } @>
            Utils.Run q


        [<Test>]
        member this.``Group by age and count``() = 
            tag "Group by age and count"
            let q = <@ query {
                    for n in db.Student do
                    groupBy n.Age into g
                    select (g.Key, g.Count())
            } @>
            Utils.Run q



        [<Test>]
        member this.``Group value by age.``() = 
            tag "Group value by age."
            let q = <@ query {
                    for n in db.Student do
                    groupValBy n.Age n.Age into g
                    select (g.Key, g.Count())
                } @>
            Utils.Run q



        [<Test>]
        member this.``Group students by age where age > 10.``() = 
            tag "Group students by age where age > 10."
            let q = <@ query {
                    for student in db.Student do
                    groupBy student.Age into g
                    where (g.Key.HasValue && g.Key.Value > 10)
                    select (g, g.Key)
            } @>
            Utils.Run q


        [<Test>]
        member this.``Group students by age and print counts of number of students at each age with more than 1 student.``() = 
            tag "Group students by age and print counts of number of students at each age with more than 1 student."
            let q = <@ query {
                    for student in db.Student do
                    groupBy student.Age into group
                    where (group.Count() > 1)
                    select (group.Key, group.Count())
            } @>
            Utils.Run q



        [<Test>]
        member this.``Group students by age and sum ages.``() = 
            tag "Group students by age and sum ages."
            let q = <@ query {
                    for student in db.Student do
                    groupBy student.Age into g        
                    let total = query { for student in g do sumByNullable student.Age }
                    select (g.Key, g.Count(), total)
            } @>
            Utils.Run q



        [<Test>]
        member this.``Group students by age and count number of students at each age, and display all with count > 1 in descending order of count.``() = 
            tag "Group students by age and count number of students at each age, and display all with count > 1 in descending order of count."
            let q = <@ query {
                    for student in db.Student do
                    groupBy student.Age into g
                    where (g.Count() > 1)        
                    sortByDescending (g.Count())
                    select (g.Key, g.Count())
            } @>
            Utils.Run q



        [<Test>]
        member this.``Select students from a set of IDs``() = 
            tag "Select students from a set of IDs"
            let idList = [1; 2; 5; 10]
            let idQuery = query { for id in idList do
                                   select id }
            let q = <@ query {
                    for student in db.Student do
                    where (idQuery.Contains(student.StudentID))
                    select student
                    } @>
            Utils.Run q



        [<Test>]
        member this.``Look for students with Name match _e%% pattern and take first two.``() = 
            tag "Look for students with Name match _e%% pattern and take first two."
            let q = <@ query {
                for student in db.Student do
                where (SqlMethods.Like( student.Name, "_e%") )
                select student
                take 2   
                } @>
            Utils.Run q



        [<Test>]
        member this.``Look for students with Name matching [abc]%% pattern.``() = 
            tag "Look for students with Name matching [abc]%% pattern."
            let q = <@ query {
                for student in db.Student do
                where (SqlMethods.Like( student.Name, "[abc]%") )
                select student  
                } @>
            Utils.Run q



        [<Test>]
        member this.``Look for students with name matching [^abc]%% pattern.``() = 
            tag "Look for students with name matching [^abc]%% pattern."
            let q = <@ query {
                for student in db.Student do
                where (SqlMethods.Like( student.Name, "[^abc]%") )
                select student  
                } @>
            Utils.Run q



        [<Test>]
        member this.``Look for students with name matching [^abc]%% pattern and select ID.``() = 
            tag "Look for students with name matching [^abc]%% pattern and select ID."
            let q = <@ query {
                for n in db.Student do
                where (SqlMethods.Like( n.Name, "[^abc]%") )
                select n.StudentID    
                } @>
            Utils.Run q



        [<Test>]
        member this.``Using Contains as a query filter.``() = 
            tag "Using Contains as a query filter."
            let q = <@ query {
                    for student in db.Student do
                    where (student.Name.Contains("a"))
                    select student
                } @>
            Utils.Run q



        [<Test>]
        member this.``Searching for names from a list.``() = 
            tag "Searching for names from a list."
            let names = [|"a";"b";"c"|]
            let q = <@ query {
                for student in db.Student do
                if names.Contains (student.Name) then select student } @>
            Utils.Run q


        [<Test>]
        member this.``Join Student and CourseSelection tables.``() = 
            tag "Join Student and CourseSelection tables."
            let q = <@ query {
                    for student in db.Student do 
                    join selection in db.CourseSelection 
                      on (student.StudentID = selection.StudentID)
                    select (student, selection)
                } @>
            Utils.Run q



        [<Test>]
        member this.``Left Join Student and CourseSelection tables.``() = 
            tag "Left Join Student and CourseSelection tables."
            let q = <@ query {
                for student in db.Student do
                leftOuterJoin selection in db.CourseSelection 
                  on (student.StudentID = selection.StudentID) into result
                for selection in result.DefaultIfEmpty() do
                select (student, selection)
                } @>
            Utils.Run q



        [<Test>]
        member this.``Join with count``() = 
            tag "Join with count"
            let q = <@ query {
                    for n in db.Student do 
                    join e in db.CourseSelection on (n.StudentID = e.StudentID)
                    count        
                } @>
            Utils.Run q



        [<Test>]
        member this.``Join with distinct.``() = 
            tag "Join with distinct."
            let q = <@ query {
                    for student in db.Student do 
                    join selection in db.CourseSelection on (student.StudentID = selection.StudentID)
                    distinct        
                } @>
            Utils.Run q



        [<Test>]
        member this.``Join with distinct and count.``() = 
            tag "Join with distinct and count."
            let q = <@ query {
                    for n in db.Student do 
                    join e in db.CourseSelection on (n.StudentID = e.StudentID)
                    distinct
                    count       
                } @>
            Utils.Run q



        [<Test>]
        member this.``Selecting students with age between 10 and 15.``() = 
            tag "Selecting students with age between 10 and 15."
            let q = <@ query {
                    for student in db.Student do
                    where (student.Age.Value >= 10 && student.Age.Value < 15)
                    select student
                } @>
            Utils.Run q



        [<Test>]
        member this.``Selecting students with age either 11 or 12.``() = 
            tag "Selecting students with age either 11 or 12."
            let q = <@ query {
                    for student in db.Student do
                    where (student.Age.Value = 11 || student.Age.Value = 12)
                    select student
                } @>
            Utils.Run q



        [<Test>]
        member this.``Selecting students in a certain age range and sorting.``() = 
            tag "Selecting students in a certain age range and sorting."
            let q = <@ query {
                    for n in db.Student do
                    where (n.Age.Value = 12 || n.Age.Value = 13)
                    sortByNullableDescending n.Age
                    select n
                } @>
            Utils.Run q



        [<Test>]
        member this.``Selecting students with certain ages, taking account of possibility of nulls.``() = 
            tag "Selecting students with certain ages, taking account of possibility of nulls."
            let q = <@ query {
                    for student in db.Student do
                    where ((student.Age.HasValue && student.Age.Value = 11) ||
                           (student.Age.HasValue && student.Age.Value = 12))
                    sortByDescending student.Name 
                    select student.Name
                    take 2
                } @>
            Utils.Run q

            
            
        [<Test>]
        member this.``Union of two queries.``() = 
            tag "Union of two queries."

            let query1 = <@ query {
                    for n in db.Student do
                    select (n.Name, n.Age)
                } @>

            let query2 = <@ query {
                    for n in db.LastStudent do
                    select (n.Name, n.Age)
                    } @>

            let q = <@ query { for n in (%query1).Union(%query2) do select n } @>
            Utils.Run q

        [<Test>]
        member this.``Union of two queries (enumerable)``() = 
            tag "Union of two queries (enumerable)"

            let la = [1;2;3;4]
            let query1 = <@ query { for n in la do yield n } @>

            let lb = [3;4;5;6]
            let query2 = <@ query { for n in lb do yield n } @>

            let q = <@ query { yield! (%query1).Union(%query2) } @>
            Utils.Run q



        [<Test>]
        member this.``Intersect of two queries.``() = 
            tag "Intersect of two queries."

            let query1 = <@ query { for n in db.Student do select (n.Name, n.Age) } @>
            let query2 = <@ query { for n in db.LastStudent do select (n.Name, n.Age) } @>

            let q = <@ query { yield! (%query1).Intersect(%query2) } @>

            Utils.Run q


        [<Test>]
        member this.``Using if statement to alter results for special value.``() = 
            tag "Using if statement to alter results for special value."
            let q = <@ query {
                    for student in db.Student do
                    select (if student.Age.HasValue && student.Age.Value = -1 then
                               (student.StudentID, System.Nullable<int>(100), student.Age)
                            else (student.StudentID, student.Age, student.Age))
                } @>
            Utils.Run q



        [<Test>]
        member this.``Using if statement to alter results special values.``() = 
            tag "Using if statement to alter results special values."
            let q = <@ query {
                    for student in db.Student do
                    select (if student.Age.HasValue && student.Age.Value = -1 then
                               (student.StudentID, System.Nullable<int>(100), student.Age)
                            elif student.Age.HasValue && student.Age.Value = 0 then
                                (student.StudentID, System.Nullable<int>(100), student.Age)
                            else (student.StudentID, student.Age, student.Age))
                } @>
            Utils.Run q




        [<Test>]
        member this.``Multiple table select.``() = 
            tag "Multiple table select."
            let q = <@ query {
                    for student in db.Student do
                    for course in db.Course do
                    select (student, course)
            } @>
            Utils.Run q



        [<Test>]
        member this.``Multiple Joins``() = 
            tag "Multiple Joins"
            let q = <@ query {
                for student in db.Student do
                join courseSelection in db.CourseSelection on
                    (student.StudentID = courseSelection.StudentID)
                join course in db.Course on
                      (courseSelection.CourseID = course.CourseID)
                select (student.Name, course.CourseName)
            } @>
            Utils.Run q



        [<Test>]
        member this.``Multiple Left Outer Joins``() = 
            tag "Multiple Left Outer Joins"
            let q = <@ query {
               for student in db.Student do
                leftOuterJoin courseSelection in db.CourseSelection 
                  on (student.StudentID = courseSelection.StudentID) into g1
                for courseSelection in g1.DefaultIfEmpty() do
                leftOuterJoin course in db.Course 
                  on (courseSelection.CourseID = course.CourseID) into g2
                for course in g2.DefaultIfEmpty() do
                select (student.Name, course.CourseName)
                } @>
            Utils.Run q

