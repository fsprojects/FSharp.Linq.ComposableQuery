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


module Simple = 
    type internal schema = SqlDataConnection<ConnectionStringName="QueryConnectionString", ConfigFile=".\\App.config">


    [<TestClass>]
    type TestClass() = 
        inherit FSharpComposableQuery.Tests.TestClass()

        let db = schema.GetDataContext()

        let data = [1; 5; 7; 11; 18; 21]

        
        let printNullable (v:Nullable<'T>) =
            if (v.HasValue) then v.Value.ToString()
            else "NULL"

        [<Literal>]
        static let invalidResult = "Results don't match!"
        
//        let q = query {
//            for x in 
//                query { yield! db.Student }
//                do yield x }
//            
//        let q2 = query {
//            for x in ( for y in db.Student do yield y)
//                do yield x }

        [<TestMethod>]
        member this.``contains query operator``() = 
            this.tagQuery "contains query operator"
            let q =
              <@ query {
                    for student in db.Student do
                    select student.Age.Value
                    contains 11
                    } @>
            Utils.Run q


        [<TestMethod>]
        member this.``count query operator``() = 
            this.tagQuery "count query operator"
            let q =
              <@ query {
                    for student in db.Student do
                    select student
                    count
                    } @>
            Utils.Run q



        [<TestMethod>]
        member this.``last query operator.``() = 
            this.tagQuery "last query operator." 
            let q =
              <@ query {
                    for s in data do
                    sortBy s
                    last
                    } @>
            Utils.Run q


        [<TestMethod>]
        member this.``lastOrDefault query operator.``() = 
            this.tagQuery "lastOrDefault query operator." 
            let q =
              <@ query {
                        for number in data do
                        sortBy number
                        lastOrDefault
                        } @>
            Utils.Run q



        [<TestMethod>]
        member this.``exactlyOne query operator.``() = 
            this.tagQuery "exactlyOne query operator."
            let q =
              <@ query {
                    for student in db.Student do
                    where (student.StudentID = 1)
                    select student
                    exactlyOne
                    } @>
            Utils.Run q



        [<TestMethod>]
        member this.``exactlyOneOrDefault query operator.``() = 
            this.tagQuery "exactlyOneOrDefault query operator."
            let q =
              <@ query {
                    for student in db.Student do
                    where (student.StudentID = 1)
                    select student
                    exactlyOneOrDefault
                    } @>
            Utils.Run q



        [<TestMethod>]
        member this.``headOrDefault query operator.``() = 
            this.tagQuery "headOrDefault query operator."
            let q =
              <@ query {
                    for student in db.Student do
                    select student
                    headOrDefault
                    } @>
            Utils.Run q



        [<TestMethod>]
        member this.``select query operator.``() = 
            this.tagQuery "select query operator."
            let q =
              <@ query {
                    for (student) in db.Student do
                    select student
                    } @>
            Utils.Run q



        [<TestMethod>]
        member this.``where query operator.``() = 
            this.tagQuery "where query operator."
            let q =
              <@ query {
                    for student in db.Student do
                    where (student.StudentID > 4)
                    select student
                    } @>
            Utils.Run q


        [<TestMethod>]
        member this.``minBy query operator.``() = 
            this.tagQuery "minBy query operator."
            let q =
              <@ query {
                    for student in db.Student do
                    minBy student.StudentID
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``maxBy query operator.``() = 
            this.tagQuery "maxBy query operator."
            let q =
              <@ query {
                    for student in db.Student do
                    maxBy student.StudentID
                } @>
            Utils.Run q


    

        [<TestMethod>]
        member this.``groupBy query operator.``() = 
            this.tagQuery "groupBy query operator."
            let q = <@ query {
                for student in db.Student do
                groupBy student.Age into g
                select (g.Key, g.Count())
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``sortBy query operator.``() = 
            this.tagQuery "sortBy query operator."
            let q = <@ query {
                for student in db.Student do
                sortBy student.Name
                select student
            } @>
            Utils.Run q



        [<TestMethod>]
        member this.``sortByDescending query operator.``() = 
            this.tagQuery "sortByDescending query operator."
            let q = <@ query {
                for student in db.Student do
                sortByDescending student.Name
                select student
            } @>
            Utils.Run q



        [<TestMethod>]
        member this.``thenBy query operator.``() = 
            this.tagQuery "thenBy query operator."
            let q = <@ query {
                for student in db.Student do
                where student.Age.HasValue
                sortBy student.Age.Value
                thenBy student.Name
                select student
            } @>
            Utils.Run q



        [<TestMethod>]
        member this.``thenByDescending query operator.``() = 
            this.tagQuery "thenByDescending query operator."
            let q = <@ query {
                for student in db.Student do
                where student.Age.HasValue
                sortBy student.Age.Value
                thenByDescending student.Name
                select student
            } @>
            Utils.Run q



        [<TestMethod>]
        member this.``groupValBy query operator.``() = 
            this.tagQuery "groupValBy query operator."
            let q = <@ query {
                for student in db.Student do
                groupValBy student.Name student.Age into g
                select (g, g.Key, g.Count())
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``sumByNullable query operator``() = 
            this.tagQuery "sumByNullable query operator"
            let q = <@ query {
                for student in db.Student do
                sumByNullable student.Age
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``minByNullable``() = 
            this.tagQuery "minByNullable"
            let q = <@ query {
                for student in db.Student do
                minByNullable student.Age
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``maxByNullable``() = 
            this.tagQuery "maxByNullable"
            let q = <@ query {
                for student in db.Student do
                maxByNullable student.Age
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``averageBy``() = 
            this.tagQuery "averageBy"
            let q = <@ query {
                for student in db.Student do
                averageBy (float student.StudentID)
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``averageByNullable``() = 
            this.tagQuery "averageByNullable"
            let q = <@ query {
                for student in db.Student do
                averageByNullable (Nullable.float student.Age)
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``find query operator``() = 
            this.tagQuery "find query operator"
            let q = <@ query {
                for student in db.Student do
                find (student.Name = "Abercrombie, Kim")
            } @>
            Utils.Run q



        [<TestMethod>]
        member this.``all query operator``() = 
            this.tagQuery "all query operator"
            let q = <@ query {
                for student in db.Student do
                all (SqlMethods.Like(student.Name, "%,%"))
            } @>
            Utils.Run q



        [<TestMethod>]
        member this.``head query operator``() = 
            this.tagQuery "head query operator"
            let q = <@ query {
                for student in db.Student do
                head
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``nth query operator``() = 
            this.tagQuery "nth query operator"
            let q = <@ query {
                for numbers in data do
                nth 3
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``skip query operator``() = 
            this.tagQuery "skip query operator"
            let q = <@ query {
                for student in db.Student do
                skip 1
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``skipWhile query operator``() = 
            this.tagQuery "skipWhile query operator"
            let q = <@ query {
                for number in data do
                skipWhile (number < 3)
                select number
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``sumBy query operator``() = 
            this.tagQuery "sumBy query operator"
            let q = <@ query {
               for student in db.Student do
               sumBy student.StudentID
               } @>
            Utils.Run q



        [<TestMethod>]
        member this.``take query operator``() = 
            this.tagQuery "take query operator"
            let q = <@ query {
               for student in db.Student do
               select student
               take 2
               } @>
            Utils.Run q



        [<TestMethod>]
        member this.``takeWhile query operator``() = 
            this.tagQuery "takeWhile query operator"
            let q = <@ query {
                for number in data do
                takeWhile (number < 10)
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``sortByNullable query operator``() = 
            this.tagQuery "sortByNullable query operator"
            let q = <@ query {
                for student in db.Student do
                sortByNullable student.Age
                select student
            } @>
            Utils.Run q



        [<TestMethod>]
        member this.``sortByNullableDescending query operator``() = 
            this.tagQuery "sortByNullableDescending query operator"
            let q = <@ query {
                for student in db.Student do
                sortByNullableDescending student.Age
                select student
            } @>
            Utils.Run q



        [<TestMethod>]
        member this.``thenByNullable query operator``() = 
            this.tagQuery "thenByNullable query operator"
            let q = <@ query {
                for student in db.Student do
                sortBy student.Name
                thenByNullable student.Age
                select student
            } @>
            Utils.Run q



        [<TestMethod>]
        member this.``thenByNullableDescending query operator``() = 
            this.tagQuery "thenByNullableDescending query operator"
            let q = <@ query {
                for student in db.Student do
                sortBy student.Name
                thenByNullableDescending student.Age
                select student
            } @>
            Utils.Run q



        [<TestMethod>]
        member this.``All students:``() = 
            this.tagQuery "All students: "
            let q = <@ query {
                    for student in db.Student do
                    select student
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Count of students:``() = 
            this.tagQuery "Count of students: "
            let q = <@ query {
                    for student in db.Student do        
                    count
                } @>
            Utils.Run q





        (* This example is the same as above but works, because we use ExtraTopLevelOperators.query *)
        [<TestMethod>]
        member this.``Exists.``() = 
            this.tagQuery "Exists."
            let q = <@ query {
                    for student in db.Student do
                    where (ExtraTopLevelOperators.query 
                                  { for courseSelection in db.CourseSelection do
                                    exists (courseSelection.StudentID = student.StudentID) })
                    select student } @>
            Utils.Run q

    

        (* This example demonstrates the bug *)
        [<TestMethod>]
        member this.``Exists (bug).``() = 
            this.tagQuery "Exists (bug)."
            let q = <@ query {
                    for student in db.Student do
                    where (query 
                                  { for courseSelection in db.CourseSelection do
                                    exists (courseSelection.StudentID = student.StudentID) })
                    select student } @>
            Utils.Run q


        [<TestMethod>]
        member this.``Group by age and count``() = 
            this.tagQuery "Group by age and count"
            let q = <@ query {
                    for n in db.Student do
                    groupBy n.Age into g
                    select (g.Key, g.Count())
            } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Group value by age.``() = 
            this.tagQuery "Group value by age."
            let q = <@ query {
                    for n in db.Student do
                    groupValBy n.Age n.Age into g
                    select (g.Key, g.Count())
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Group students by age where age > 10.``() = 
            this.tagQuery "Group students by age where age > 10."
            let q = <@ query {
                    for student in db.Student do
                    groupBy student.Age into g
                    where (g.Key.HasValue && g.Key.Value > 10)
                    select (g, g.Key)
            } @>
            Utils.Run q


        [<TestMethod>]
        member this.``Group students by age and print counts of number of students at each age with more than 1 student.``() = 
            this.tagQuery "Group students by age and print counts of number of students at each age with more than 1 student."
            let q = <@ query {
                    for student in db.Student do
                    groupBy student.Age into group
                    where (group.Count() > 1)
                    select (group.Key, group.Count())
            } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Group students by age and sum ages.``() = 
            this.tagQuery "Group students by age and sum ages."
            let q = <@ query {
                    for student in db.Student do
                    groupBy student.Age into g        
                    let total = query { for student in g do sumByNullable student.Age }
                    select (g.Key, g.Count(), total)
            } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Group students by age and count number of students at each age, and display all with count > 1 in descending order of count.``() = 
            this.tagQuery "Group students by age and count number of students at each age, and display all with count > 1 in descending order of count."
            let q = <@ query {
                    for student in db.Student do
                    groupBy student.Age into g
                    where (g.Count() > 1)        
                    sortByDescending (g.Count())
                    select (g.Key, g.Count())
            } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Select students from a set of IDs``() = 
            this.tagQuery "Select students from a set of IDs"
            let idList = [1; 2; 5; 10]
            let idQuery = query { for id in idList do
                                   select id }
            let q = <@ query {
                    for student in db.Student do
                    where (idQuery.Contains(student.StudentID))
                    select student
                    } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Look for students with Name match _e%% pattern and take first two.``() = 
            this.tagQuery "Look for students with Name match _e%% pattern and take first two."
            let q = <@ query {
                for student in db.Student do
                where (SqlMethods.Like( student.Name, "_e%") )
                select student
                take 2   
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Look for students with Name matching [abc]%% pattern.``() = 
            this.tagQuery "Look for students with Name matching [abc]%% pattern."
            let q = <@ query {
                for student in db.Student do
                where (SqlMethods.Like( student.Name, "[abc]%") )
                select student  
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Look for students with name matching [^abc]%% pattern.``() = 
            this.tagQuery "Look for students with name matching [^abc]%% pattern."
            let q = <@ query {
                for student in db.Student do
                where (SqlMethods.Like( student.Name, "[^abc]%") )
                select student  
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Look for students with name matching [^abc]%% pattern and select ID.``() = 
            this.tagQuery "Look for students with name matching [^abc]%% pattern and select ID."
            let q = <@ query {
                for n in db.Student do
                where (SqlMethods.Like( n.Name, "[^abc]%") )
                select n.StudentID    
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Using Contains as a query filter.``() = 
            this.tagQuery "Using Contains as a query filter."
            let q = <@ query {
                    for student in db.Student do
                    where (student.Name.Contains("a"))
                    select student
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Searching for names from a list.``() = 
            this.tagQuery "Searching for names from a list."
            let names = [|"a";"b";"c"|]
            let q = <@ query {
                for student in db.Student do
                if names.Contains (student.Name) then select student } @>
            Utils.Run q


    //     *
        [<TestMethod>]
        member this.``Join Student and CourseSelection tables.``() = 
            this.tagQuery "Join Student and CourseSelection tables."
            let q = <@ query {
                    for student in db.Student do 
                    join selection in db.CourseSelection 
                      on (student.StudentID = selection.StudentID)
                    select (student, selection)
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Left Join Student and CourseSelection tables.``() = 
            this.tagQuery "Left Join Student and CourseSelection tables."
            let q = <@ query {
                for student in db.Student do
                leftOuterJoin selection in db.CourseSelection 
                  on (student.StudentID = selection.StudentID) into result
                for selection in result.DefaultIfEmpty() do
                select (student, selection)
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Join with count``() = 
            this.tagQuery "Join with count"
            let q = <@ query {
                    for n in db.Student do 
                    join e in db.CourseSelection on (n.StudentID = e.StudentID)
                    count        
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Join with distinct.``() = 
            this.tagQuery "Join with distinct."
            let q = <@ query {
                    for student in db.Student do 
                    join selection in db.CourseSelection on (student.StudentID = selection.StudentID)
                    distinct        
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Join with distinct and count.``() = 
            this.tagQuery "Join with distinct and count."
            let q = <@ query {
                    for n in db.Student do 
                    join e in db.CourseSelection on (n.StudentID = e.StudentID)
                    distinct
                    count       
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Selecting students with age between 10 and 15.``() = 
            this.tagQuery "Selecting students with age between 10 and 15."
            let q = <@ query {
                    for student in db.Student do
                    where (student.Age.Value >= 10 && student.Age.Value < 15)
                    select student
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Selecting students with age either 11 or 12.``() = 
            this.tagQuery "Selecting students with age either 11 or 12."
            let q = <@ query {
                    for student in db.Student do
                    where (student.Age.Value = 11 || student.Age.Value = 12)
                    select student
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Selecting students in a certain age range and sorting.``() = 
            this.tagQuery "Selecting students in a certain age range and sorting."
            let q = <@ query {
                    for n in db.Student do
                    where (n.Age.Value = 12 || n.Age.Value = 13)
                    sortByNullableDescending n.Age
                    select n
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Selecting students with certain ages, taking account of possibility of nulls.``() = 
            this.tagQuery "Selecting students with certain ages, taking account of possibility of nulls."
            let q = <@ query {
                    for student in db.Student do
                    where ((student.Age.HasValue && student.Age.Value = 11) ||
                           (student.Age.HasValue && student.Age.Value = 12))
                    sortByDescending student.Name 
                    select student.Name
                    take 2
                } @>
            Utils.Run q

            
            
        [<TestMethod>]
        member this.``Union of two queries.``() = 
            this.tagQuery "Union of two queries."

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

        [<TestMethod>]
        member this.``Union of two queries (enumerable)``() = 
            this.tagQuery "Union of two queries (enumerable)"

            let la = [1;2;3;4]
            let query1 = <@ query { for n in la do yield n } @>

            let lb = [3;4;5;6]
            let query2 = <@ query { for n in lb do yield n } @>

            let q = <@ query { yield! (%query1).Union(%query2) } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Intersect of two queries.``() = 
            this.tagQuery "Intersect of two queries."

            let query1 = <@ query { for n in db.Student do select (n.Name, n.Age) } @>
            let query2 = <@ query { for n in db.LastStudent do select (n.Name, n.Age) } @>

            let q = <@ query { yield! (%query1).Intersect(%query2) } @>

            Utils.Run q


        [<TestMethod>]
        member this.``Using if statement to alter results for special value.``() = 
            this.tagQuery "Using if statement to alter results for special value."
            let q = <@ query {
                    for student in db.Student do
                    select (if student.Age.HasValue && student.Age.Value = -1 then
                               (student.StudentID, System.Nullable<int>(100), student.Age)
                            else (student.StudentID, student.Age, student.Age))
                } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Using if statement to alter results special values.``() = 
            this.tagQuery "Using if statement to alter results special values."
            let q = <@ query {
                    for student in db.Student do
                    select (if student.Age.HasValue && student.Age.Value = -1 then
                               (student.StudentID, System.Nullable<int>(100), student.Age)
                            elif student.Age.HasValue && student.Age.Value = 0 then
                                (student.StudentID, System.Nullable<int>(100), student.Age)
                            else (student.StudentID, student.Age, student.Age))
                } @>
            Utils.Run q




        [<TestMethod>]
        member this.``Multiple table select.``() = 
            this.tagQuery "Multiple table select."
            let q = <@ query {
                    for student in db.Student do
                    for course in db.Course do
                    select (student, course)
            } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Multiple Joins``() = 
            this.tagQuery "Multiple Joins"
            let q = <@ query {
                for student in db.Student do
                join courseSelection in db.CourseSelection on
                    (student.StudentID = courseSelection.StudentID)
                join course in db.Course on
                      (courseSelection.CourseID = course.CourseID)
                select (student.Name, course.CourseName)
            } @>
            Utils.Run q



        [<TestMethod>]
        member this.``Multiple Left Outer Joins``() = 
            this.tagQuery "Multiple Left Outer Joins"
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

