namespace FSharpComposableQuery.Tests

open FSharpComposableQuery
open Microsoft.FSharp.Linq
open Microsoft.FSharp.Data.TypeProviders
open NUnit.Framework
open System.Linq

/// <summary>
/// Contains example queries and operations on the Organisation database.
/// The queries here are further wrapped in quotations to allow for their evaluation in different contexts (see Utils.fs).
/// <para>These tests require the schema from sql/organisation.sql in a database referred to in app.config </para>
/// </summary>
module Nested =
    type internal dbSchema = SqlDataConnection< ConnectionStringName="OrgConnectionString", ConfigFile="db.config" >
    

    // Schema declarations
    type internal Department = dbSchema.ServiceTypes.Departments

    // TypeProvider type abbreviations. 
    type internal Employee = dbSchema.ServiceTypes.Employees

    type internal Contact = dbSchema.ServiceTypes.Contacts

    type internal Task = dbSchema.ServiceTypes.Tasks
    
    //Nested type declarations
    type EmployeeTasks =
        { emp : string
          tasks : System.Linq.IQueryable<string> }

    type DepartmentEmployees =
        { dpt : string
          employees : System.Linq.IQueryable<EmployeeTasks> }

    type NestedOrg = System.Linq.IQueryable<DepartmentEmployees>

    let internal db = dbSchema.GetDataContext()

    [<TestFixture>]
    type TestClass() =

        [<Literal>]
        static let N_DEPARTMENTS = 40

        [<Literal>]
        static let N_EMPLOYEES = 5000

        [<Literal>]
        static let N_ABSTRACTION = 500

        //db tables
        static let departments = db.Departments
        static let employees = db.Employees
        static let contacts = db.Contacts
        static let tasks = db.Tasks

        // random data generator
        static let rand = new System.Random()
        static let mutable idx = 0

        static let genId str =
            idx <- idx + 1
            str + "_" + idx.ToString()

        static let randomArray (arr : 'a array) =
            let i = rand.Next(arr.Length)
            arr.[i]

        static let randomName() =
            randomArray [| "alan"; "bert"; "charlie"; "david"; "edward"; "alice"; "betty"; "clara"; "dora"; "eve" |]
            |> genId

        static let randomTask() = 
            randomArray [| "abstract"; "buy"; "call"; "dissemble"; "enthuse" |] 
            |> genId

        static let randomDepartment() = 
            randomArray [| "Sales"; "Research"; "Quality"; "Product" |] 
            |> genId

        /// <summary>
        /// Generates n uniquely named employees and distributes them uniformly across the given departments. 
        /// </summary>
        static let randomEmployees n depts = List.map (fun _ -> new Employee(Emp = randomName(), Dpt = randomArray depts)) [ 1..n ]

        /// <summary>
        /// Generates n uniquely named employees in each of the given departments. 
        /// </summary>
        static let randomEmployeesInEach n depts =
            depts
            |> Array.toList
            |> List.map (fun d -> randomEmployees n [| d |])
            |> List.concat

        /// <summary>
        /// Generates n uniquely named contacts and distributes them uniformly across the given departments. 
        /// </summary>
        static let randomContacts n depts =
            [ 1..n ]
            |> List.map (fun _ -> new Contact(Dpt = randomArray depts, Contact = randomName(), Client = rand.Next(2)))

        /// <summary>
        /// Generates 0 to 2 (inclusive) unique tasks for each of the given employees. 
        /// </summary>
        static let randomTasks emps =
            emps
            |> List.map (fun (r : Employee) ->
                             List.map (fun _ -> new Task(Emp = r.Emp, Tsk = randomTask())) [ 1..rand.Next(3) ])
            |> List.concat


        //database records update functions
        static let addContact (r : Contact) = contacts.InsertOnSubmit(r)

        static let addDept (dpt : string) = departments.InsertOnSubmit(new Department(Dpt = dpt))

        static let addEmployee (r : Employee) = employees.InsertOnSubmit(r)
        
        static let addTask (r : Task) = tasks.InsertOnSubmit(r)

        // Clears all relevant tables in the database.
        static let dropTables() =
            ignore (db.DataContext.ExecuteCommand("TRUNCATE TABLE [FCQ-Org].[dbo].[employees]"))
            ignore (db.DataContext.ExecuteCommand("TRUNCATE TABLE [FCQ-Org].[dbo].[tasks]"))
            ignore (db.DataContext.ExecuteCommand("TRUNCATE TABLE [FCQ-Org].[dbo].[departments]"))
            ignore (db.DataContext.ExecuteCommand("TRUNCATE TABLE [FCQ-Org].[dbo].[contacts]"))
            
        /// <summary>
        /// Creates a number of random departments and uniformly distributes the specified number of employees across them, 
        /// then updates the database with the new rows. 
        /// </summary>
        /// <param name="nDep">The number of departments to generate. </param>
        /// <param name="nEmp">The total number of employees to generate. </param>
        static let addRandom nDep nEmp =
            let depts = Array.map (ignore >> randomDepartment) [| 1..nDep |]
            Array.iter addDept depts
            db.DataContext.SubmitChanges()

            let employees = randomEmployees nEmp depts
            List.iter addEmployee employees
            db.DataContext.SubmitChanges()

            let contacts = randomContacts nEmp depts
            List.iter addContact contacts
            db.DataContext.SubmitChanges()
            
            let tasks = randomTasks employees
            List.iter addTask tasks
            db.DataContext.SubmitChanges()

        /// <summary>
        /// Creates a number of random departments and in each of them generates the specified number of employees,
        /// then updates the database with the new rows. 
        /// </summary>
        /// <param name="nDep">The number of departments to generate. </param>
        /// <param name="nEmp">The number of employees to generate in each department. </param>
        static let addRandomForEach nDep nEmp =
            let depts = Array.map (ignore >> randomDepartment) [| 1..nDep |]
            Array.iter addDept depts
            db.DataContext.SubmitChanges()

            // for each department generate n employees
            let employees = randomEmployeesInEach nEmp depts
            List.iter addEmployee employees
            db.DataContext.SubmitChanges()

            let contacts = randomContacts nEmp depts
            List.iter addContact contacts
            db.DataContext.SubmitChanges()

            let tasks = randomTasks employees
            List.iter addTask tasks
            db.DataContext.SubmitChanges()
            
        /// <summary>
        /// Creates a department named 'Abstraction' and generates a specified number of employees in it,
        /// each of whom can perform the task "abstract"
        /// <para/>
        /// </summary>
        /// <param name="nEmp">The number of employees to generate in the 'Abstraction' department. </param>
        static let addAbstractionDept nEmp =
            addDept "Abstraction"
            db.DataContext.SubmitChanges()

            let employees = randomEmployees nEmp [| "Abstraction" |]
            List.iter addEmployee employees
            db.DataContext.SubmitChanges()

            let tasks = randomTasks employees
            List.iter addTask tasks
            List.iter (fun (e : Employee) ->
                addTask (new Task(Emp = e.Emp, Tsk = "abstract"))) employees
            db.DataContext.SubmitChanges()


        (*
        Example 8 and 9 demonstrate the benefit of using intermediate nested structures. 
        Each of them evaluates to the same query but the way we formulate them is inherently different. 

        "List all departments where every employee can perform a given task t"
        *)

        // Example 8

        let expertiseNaive =
            <@ fun u ->
                query {
                    for d in db.Departments do
                        if not (query {
                                    for e in db.Employees do
                                        exists (e.Dpt = d.Dpt && not (query {
                                                                          for t in db.Tasks do
                                                                              exists (e.Emp = t.Emp && t.Tsk = u)
                                                                      }))
                                })
                        then yield d
                } @>

        let ex8 = <@ query { yield! (%expertiseNaive) "abstract" } @>


        // Example 9

        let nestedDb =
            <@ query {
                   for d in db.Departments do
                       yield { dpt = d.Dpt
                               employees =
                                   query {
                                       for e in db.Employees do
                                           if d.Dpt = e.Dpt then
                                               yield { emp = e.Emp
                                                       tasks =
                                                           query {
                                                               for t in db.Tasks do
                                                                   if t.Emp = e.Emp then yield t.Tsk
                                                           } }
                                   } }
               } @>

        let any() =
            <@ fun xs p ->
                query {
                    for x in xs do
                        exists (p x)
                } @>
                

        (* There are a number of ways to write each of the following queries *)

        let forallA() = <@ fun xs p -> not ((%any()) xs (fun x -> not(p x))) @>

        let forallB() = <@ fun xs p -> query { for x in xs do all(p x) } @>
        

        let containsA() = <@ fun xs u -> (%any()) xs (fun x -> x = u) @>

        let containsB() = <@ fun xs u -> not ((%forallA()) xs (fun x -> x <> u)) @>

        let containsC() = <@ fun xs u -> query { for x in xs do contains u } @>
        

        let expertise =
            <@ fun u ->
                query {
                    for d in (%nestedDb) do
                        if (%forallA()) (d.employees) (fun e -> (%containsA()) e.tasks u) then yield d.dpt
                } @>

        let ex9 = <@ query { yield! (%expertise) "abstract" } @>

        [<TestFixtureSetUp>]
        member public this.init() =
            printf
                "Nested: Adding %d departments, %d employees and additional %d people in the Abstraction department... "
                N_DEPARTMENTS N_EMPLOYEES N_ABSTRACTION
            dropTables()
            addRandom N_DEPARTMENTS N_EMPLOYEES
            addAbstractionDept N_ABSTRACTION
            printfn "done!"
            
        // This query evaluates, but it lazily constructs the result
        // by stitching SQL queries: one for every department and employee. 
        // Thus accessing even parts of the data is done by executing 
        // multiple queries instead of, possibly, one.  
        member this.test00() =
            let z = query { yield! (%nestedDb) }
            ()

        [<Test>]
        member this.test01() =
            printfn "%s" "ex8"
            Utils.Run ex8

        [<Test>]
        member this.test02() =
            printfn "%s" "ex9"
            Utils.Run ex9