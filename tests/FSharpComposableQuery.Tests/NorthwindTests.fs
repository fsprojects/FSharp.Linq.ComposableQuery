namespace FSharpComposableQuery.Tests

open Microsoft.FSharp.Data.TypeProviders;
open NUnit.Framework;

module NorthwindTests =
    type Northwind = ODataService<"http://services.odata.org/Northwind/Northwind.svc">
    let db = Northwind.GetDataContext()

    // Some tests to compare 

    let dbQuery =  FSharpComposableQuery.TopLevelValues.query

    [<TestFixture>]
    type TestClass() = 
        [<Test>]
        member x.queryCustomers () = 
            // A query expression.
            let _ = query { for x in db.Customers do
                            yield x }
            ()

        [<Test>]
        member x.dbQueryCustomers () = 
            // A query expression.
            let _ = dbQuery { for x in db.Customers do
                              yield x }
            ()

        [<Test>]
        member x.queryInvoices () = 
            // A query expression.
            let _ = query { for x in db.Invoices do
                            yield x }
            ()

        [<Test>]
        member x.dbQueryInvoices () = 
            let query2 = dbQuery { for x in db.Invoices do
                                   yield x }
            ()

