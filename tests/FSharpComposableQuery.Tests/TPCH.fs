﻿namespace FSharpComposableQuery.TPCH

open Microsoft.FSharp.Data.TypeProviders
open Microsoft.FSharp.Quotations
open System.Linq
open NUnit.Framework

/// <summary>
/// Contains example queries and operations on the People database. 
/// The queries here are further wrapped in quotations to allow for their evaluation in different contexts (see Utils.fs).  
/// <para>These tests require the schema from sql/people.sql in a database referred to in app.config </para>
/// </summary>
module People =

    
    [<Literal>]
    let dbConfigPath = "db.config"
    
    type internal dbSchemaPeople = SqlDataConnection< ConnectionStringName="TPCHConnectionString", ConfigFile=dbConfigPath>

    type internal Customer = dbSchemaPeople.ServiceTypes.Customer
    type internal Lineitem = dbSchemaPeople.ServiceTypes.Lineitem
    type internal Nation = dbSchemaPeople.ServiceTypes.Nation
    type internal Orders = dbSchemaPeople.ServiceTypes.Orders
    type internal Part = dbSchemaPeople.ServiceTypes.Part
    type internal Partsupp = dbSchemaPeople.ServiceTypes.Partsupp
    type internal Region = dbSchemaPeople.ServiceTypes.Region
    type internal Supplier = dbSchemaPeople.ServiceTypes.Supplier

    let internal db = dbSchemaPeople.GetDataContext()


    [<TestFixture>]
    type TestClass() =
        static let customers = db.DataContext.GetTable<Customer>()
        static let lineitem = db.DataContext.GetTable<Lineitem>()
        static let nation = db.DataContext.GetTable<Nation>()
        static let orders = db.DataContext.GetTable<Orders>()
        static let part = db.DataContext.GetTable<Part>()
        static let partsupp = db.DataContext.GetTable<Partsupp>()
        static let region = db.DataContext.GetTable<Region>()
        static let supplier = db.DataContext.GetTable<Supplier>()
        static let customers = db.DataContext.GetTable<Customer>()

        /// helper: emptiness test
        static let empty = <@ fun xs -> not (query {for x in xs do exists (true)}) @>
        /// helper: contains 
        static let rec contains xs = 
            match xs with 
              [] -> <@ fun x -> false @>
            | y::ys -> <@ fun x -> x = y || (%contains ys) y @> 

        let q1 delta = 
          let date = (new System.DateTime(1998,12,01)).AddDays(-delta) in
          query { for l in db.Lineitem do 
                  where (l.L_ShipDate <= date)
                  groupBy (l.L_ReturnFlag, l.L_LineStatus) into g
                  sortBy (g.Key)
                  let sum_qty = g.Sum(fun l -> l.L_Quantity) in
                  let sum_base_price = g.Sum(fun l -> l.L_ExtendedPrice) in
                  let sum_disc_price = g.Sum(fun l -> (decimal(1) - l.L_Discount) * l.L_ExtendedPrice) in
                  let sum_charge = g.Sum(fun l -> (decimal(1) + l.L_Tax) * (decimal(1) - l.L_Discount) * l.L_ExtendedPrice) in
                  let avg_qty = g.Average(fun l -> l.L_Quantity) in 
                  let avg_price = g.Average(fun l -> l.L_ExtendedPrice) in
                  let avg_disc = g.Average(fun l -> l.L_Discount) in 
                  select (g.Key,sum_qty,sum_base_price,sum_disc_price,sum_charge,avg_qty,avg_price,avg_disc, g.Count) }


        
        let q22 countries = 
            let avgBalance = 
                <@ fun (cs : IQueryable<Customer>) -> 
                    query {for c in cs do 
                           where (c.C_AcctBal > decimal(0)) 
                           averageBy (c.C_AcctBal)} @> in
            let ordersOf = 
                <@ fun (c : Customer) ->
                    query { for o in db.Orders do 
                            where (o.O_CustKey = c.C_CustKey)
                            select o } @> in
           
            let potentialCustomers = 
                <@ fun (cs : IQueryable<Customer>) ->
                    query { for c in cs do
                            where (c.C_AcctBal > (%avgBalance) cs && (%empty) ((%ordersOf) c))  
                            select c
                            }  @> in 
            let countryCodeOf = 
                <@ fun (c : Customer) -> c.C_Phone.Substring(0,2) @> in
            let livesIn countries = 
                <@ fun (c:Customer) -> (%contains countries) c @>
            let pots = <@ (%potentialCustomers) (query { for c in db.Customer do 
                                                         where ((%livesIn countries) c)
                                                         select c}) @> in 
            query { for p in (%pots) do 
                    groupBy ((%countryCodeOf) p) into g
                    sortBy (g.Key)
                    let total = g.Sum(fun c -> c.C_AcctBal) in 
                    select(g.Key, g.Count(), total)
                    }

