namespace FSharpComposableQuery.Tests


open FSharpComposableQuery
open FSharpComposableQuery.TestUtils
open Microsoft.FSharp.Linq
open Microsoft.FSharp.Data.TypeProviders
open Microsoft.VisualStudio.TestTools.UnitTesting;
open System.Linq


module Nested = 

    type internal dbSchema = SqlDataConnection<ConnectionStringName="OrgConnectionString", ConfigFile=".\\App.config">

    type internal Departments = dbSchema.ServiceTypes.Departments
    type internal Employees = dbSchema.ServiceTypes.Employees
    type internal Contacts = dbSchema.ServiceTypes.Contacts
    type internal Tasks = dbSchema.ServiceTypes.Tasks
    type internal Department = {dpt:string}
    
    type internal Employee = {dpt:string;emp:string}
    type internal Task = {emp:string;tsk:string}
    type internal Contact = {dpt:string;contact:string;client:int}
    type internal Org = { departments: Department list;
                    employees: Employee  list;
                    tasks: Task list}


    type EmployeeTasks = {emp:string; tasks : seq<string>}
    type DepartmentEmployees = {dpt : string; employees: seq<EmployeeTasks> }
    type NestedOrg = seq<DepartmentEmployees>

    type EmployeeTasks' = {emp:string; tasks : System.Linq.IQueryable<string>}
    type DepartmentEmployees' = {dpt : string; employees: System.Linq.IQueryable<EmployeeTasks'> }
    type NestedOrg' = System.Linq.IQueryable<DepartmentEmployees'>
    
    let internal db = dbSchema.GetDataContext()
    
    [<TestClass>]
    type TestClass() = 
        inherit Tests.TestClass()

        
        static let departments = db.Departments
        static let employees = db.Employees
        static let contacts = db.Contacts
        static let tasks = db.Tasks

        static let printDepts x = 
            Seq.iter (fun (d:Department) -> printfn "%s" d.dpt) x
    
        static let forceDepts x = 
            Seq.iter (fun _ -> ()) x
    
        static let printEmp (e:Employees) = 
            printfn "Name:L %s Dept: %s Salary: %d" e.Emp e.Dpt e.Salary

        static let printEmps x = 
            Seq.iter printEmp x


        // random data generator
        static let rand = new System.Random()

        static let mutable idx = 0
        static let genId str = 
            idx <- idx + 1
            str + "_" + idx.ToString()
    
        static let randomArray(arr:'a array) = 
            let i = rand.Next(arr.Length) in
                (arr.[i])

        static let randomName() = 
            randomArray [|"alan";"bert";"charlie";"david";"edward";"alice";"betty";"clara";"dora";"eve"|]
            |> genId

        static let randomTask() = 
            randomArray [|"abstract";"buy";"call";"dissemble";"enthuse"|]
            |> genId

        static let randomDepartment() = 
            randomArray [|"Sales";"Research";"Quality";"Product"|]
            |> genId

        static let randomSalary () = 
            rand.Next(10000,1000000)

        static let randomEmployees n depts = 
            [1..n]
            |> List.map (fun _ -> {emp=randomName();dpt=randomArray depts}:Employee)

        static let randomEmployeesInDepartments depts n = 
            List.concat (List.map (fun d -> randomEmployees n [|d|]) (Array.toList depts))

        static let randomContacts n depts = 
            List.map (fun _ -> {dpt=randomArray depts;contact=randomName();client=rand.Next(2)}) [1..n]

        static let randomTasks emps = 
            List.concat (List.map (fun (r:Employee) -> List.map (fun _ -> {emp=r.emp;tsk=randomTask()}) [1..rand.Next(3)]) emps)


        //database records update functions

        static let addContacts(r:Contacts) =
            contacts.InsertOnSubmit(r)

        static let addContactR (c:Contact) =
            let p = new Contacts()
            p.Dpt <- c.dpt;
            p.Contact <- c.contact
            p.Client <- c.client
            addContacts(p)
  
        static let addDept (dpt : string) =     
            let d = new Departments()
            d.Dpt <- dpt;
            departments.InsertOnSubmit(d)

        static let addEmployees (r : Employees) =     
            employees.InsertOnSubmit(r)

        static let addEmpR (emp:Employee) =
            let p = new Employees()
            p.Emp <- emp.emp
            p.Dpt <- emp.dpt
            addEmployees(p)

        static let addTasks (r : Tasks) =     
            tasks.InsertOnSubmit(r)

        static let addTaskR (t:Task) =
            let p = new Tasks()
            p.Emp <- t.emp
            p.Tsk <- t.tsk
            addTasks(p)
  
        static let dropTables() = 
            ignore(db.DataContext.ExecuteCommand("DELETE FROM [organisation].[dbo].[employees] WHERE 1=1"))
            ignore(db.DataContext.ExecuteCommand("DELETE FROM [organisation].[dbo].[tasks] WHERE 1=1"))
            ignore(db.DataContext.ExecuteCommand("DELETE FROM [organisation].[dbo].[departments] WHERE 1=1"))
            ignore(db.DataContext.ExecuteCommand("DELETE FROM [organisation].[dbo].[contacts] WHERE 1=1"))


        static let addRandom ds n = 
            let depts = Array.map (fun _ -> (randomDepartment())) [|1..ds|] in
            Array.iter (fun p -> addDept p) depts;
            db.DataContext.SubmitChanges();

            let employees = randomEmployees n depts in
            List.iter (fun p -> addEmpR p) employees;
            db.DataContext.SubmitChanges();

            let contacts = randomContacts n depts in
            List.iter addContactR contacts
            db.DataContext.SubmitChanges();

            let tasks = randomTasks employees in
            List.iter (fun c -> addTaskR c) tasks;
            db.DataContext.SubmitChanges()
        

        static let addRandomDepartments ds n = 
            let depts = Array.map (fun _ -> (randomDepartment())) [|1..ds|] in
            Array.iter (fun p -> addDept p) depts;
            db.DataContext.SubmitChanges();
      
            // for each department generate up to n  employees
            let employees = randomEmployeesInDepartments depts n in
            List.iter (fun p -> addEmpR p) employees;
            db.DataContext.SubmitChanges();

            let contacts = randomContacts n depts in
            List.iter addContactR contacts
            db.DataContext.SubmitChanges();

            let tasks = randomTasks employees in
            List.iter (fun c -> addTaskR c) tasks;
            db.DataContext.SubmitChanges()


        static let addAbstractionDept n = 
            let employees = randomEmployees n [|"Abstraction"|] in
            List.iter (fun p -> addEmpR p) employees;
            db.DataContext.SubmitChanges();
            let tasks = randomTasks employees in
            List.iter (fun c -> addTaskR c) tasks;
            List.iter (fun (e:Employee) -> addTaskR {emp=e.emp;tsk="abstract"}) employees;
            db.DataContext.SubmitChanges()


        // Example 8

        let exists() = <@ fun xs -> Seq.exists (fun _ -> true) xs @>

        let expertiseNaive =
          <@ fun u -> seq {
            for d in db.Departments do 
            if not((%exists ()) (seq {for e in db.Employees do 
                                      if e.Dpt = d.Dpt && 
                                         not ((%exists()) (seq {for t in db.Tasks do
                                                                if e.Emp = t.Emp && t.Tsk = u
                                                                then yield ()
                                                            }
                                                     )
                                          )
                                      then yield ()
                               }
                          )
                   )
            then yield {Department.dpt=d.Dpt}
          } @>

        let expertiseNaive' = 
          <@ fun u -> query {
            for d in db.Departments do 
            if not(query {
                for e in db.Employees do 
                    exists(e.Dpt = d.Dpt &&
                        not (query {for t in db.Tasks do
                                    exists(e.Emp = t.Emp && t.Tsk = u)
                                }
                            )
                        )
                })
            then yield {Department.dpt=d.Dpt}
          } @>

        let ex8 = <@ (%expertiseNaive) "abstract"@>

        let ex8' = <@ (%expertiseNaive') "abstract" @>

        let nestedOrg:Quotations.Expr<seq<DepartmentEmployees>> = 
            <@ seq { 
            for d in db.Departments do
            yield {dpt = d.Dpt; 
                   employees = 
                    seq {
                       for e in db.Employees do
                       if d.Dpt = e.Dpt
                       then yield {emp=e.Emp;
                                   tasks= seq {
                                       for t in db.Tasks do
                                       if t.Emp = e.Emp 
                                       then yield t.Tsk
                                     }
                                   }
                     }
                  }
          } @>



        let any() = 
          <@ fun xs -> fun p -> Seq.exists p xs @>

        let all () = 
          <@ fun xs -> fun p -> not(Seq.exists (fun x -> not (p(x))) xs) @>

        let contains() = 
          <@ fun xs -> fun u -> (%any()) xs (fun x -> x = u) @>

        let expertise = 
          <@ fun u -> seq {
            for d in (%nestedOrg) do 
            if (%all()) (d.employees) (fun e -> (%contains()) e.tasks u)
            then yield {Department.dpt=d.dpt}
            } @>



        let nestedOrg'  = 
          query { 
            for d in db.Departments do
            yield {dpt = d.Dpt; 
                   employees= query {
                       for e in db.Employees do
                       if d.Dpt = e.Dpt
                       then yield {emp=e.Emp;
                                   tasks= query {
                                       for t in db.Tasks do
                                       if t.Emp = e.Emp 
                                       then yield t.Tsk
                                     }
                                   }
                     }
                  }
          }



        let any'() = 
          <@ fun xs -> fun p -> query { for x in xs do exists (p xs) } @>

        let all' () = 
          <@ fun xs -> fun p -> not(query {for x in xs do exists (not (p(x)))}) @>

        let contains'() = 
          <@ fun xs -> fun u -> (%any()) xs (fun x -> x = u) @>

        let expertise' =
          <@ fun u -> query {
            for d in (nestedOrg') do 
            if (%all'()) (d.employees) (fun e -> (%contains'()) e.tasks u)
            then yield {Department.dpt=d.dpt}
            } @>


        let printAny xs = 
            Seq.iter (fun x -> printfn "%A" x) xs

    
        // Example 9

        let ex9 = <@ (%expertise)  "abstract" @>
        let ex9' = <@ (%expertise') "abstract" @>

        let doBasicTest() = 
            printfn "ex8"
            timeAll ex8 ex8' (Seq.iter (fun _ -> ()))
            printfn "ex9"
            timeAll ex9 ex9' (Seq.iter (fun _ -> ()))

        let doTest d n = 
            printfn "[Departments %d, employees %d]" d n
            dropTables()
            addRandom d n
            printfn "ex8"
            timeAll ex8 ex8' (Seq.iter (fun _ -> ()))
            printfn "ex9"
            timeAll ex9 ex9' (Seq.iter (fun _ -> ()))
    
        let doTestAbstraction d n a = 
            printfn "[Departments %d, employees %d, abstraction %d]" d (n+a) a
            dropTables()
            addRandom d n
            addAbstractionDept a
            printfn "ex8"
            timeAll ex8 ex8' (Seq.iter (fun _ -> ()))
            printfn "ex9"
            timeAll ex9 ex9' (Seq.iter (fun _ -> ()))
    


        let doTestSweep () = 
          let deptSizes = [4;40;400] in
          let abstractions = [0;10;100;1000] in
          let employees = [5000] in
          Seq.iter (fun (x,y,z) -> if z <> 0 then doTestAbstraction x y z else doTest x y) 
                   (seq {for x in deptSizes do
                         for y in employees do
                         for z in abstractions do
                         yield(x,y,z)})

        let tabify (d,n,t1,t2) = 
            printfn "%d \t%d \t%f \t%f" d n t1 t2


        let doTest'()  =

            [("ex8",    timeAll' ex8 ex8' forceDepts);
             ("ex9",    timeAll' ex9 ex9' forceDepts);]
         
        let forceSeq = Seq.iter ignore

        [<TestMethod>]
        member this.testEx8() = 
             this.tagQuery "ex8"
             timeAll ex8 ex8' forceSeq

        [<TestMethod>]
        member this.testEx9() = 
             this.tagQuery "ex9"
             timeAll ex9 ex9' forceSeq

        (*
        let doScaleTest d n = 
            dropTables()
            addRandomDepartments d n
            let (),t1 = timeFS3' ex9' forceDepts
            let (),t2 = timePLinq' ex9 forceDepts
            tabify (d,n,t1,t2)
            (d,n,t1,t2)
    

        let doScaleAbsTest d n = 
            dropTables()
            addRandomDepartments (d-1) n
            addAbstractionDept n
            let (),t1 = timeFS3' ex9' forceDepts
            let (),t2 = timePLinq' ex9 forceDepts
            tabify (d,n,t1,t2)
            (d,n,t1,t2)
    

        let results d n k = 
            List.map (fun d -> List.map (fun _ -> doScaleTest (4*d) n) [1..k]) [1..d]

        *)