#I "../../bin"
#r "System.Data.dll"
#r "FSharp.Data.TypeProviders.dll"
#r "System.Data.Linq.dll"
#r "FSharpComposableQuery.dll"

open System
open System.Data
open System.Data.Linq
open System.Linq
open Microsoft.FSharp.Data.TypeProviders

let dbquery = FSharpComposableQuery.TopLevelValues.query

[<Literal>]
let compileTimeConnectionString = "Data Source=.;Initial Catalog=FCQ-Brands;Integrated Security=SSPI;"
type SqlToBrandDb = SqlDataConnection<compileTimeConnectionString>
let dbContext () =
    let connectionString = compileTimeConnectionString
    let db = SqlToBrandDb.GetDataContext(connectionString)
    db.DataContext.Log <- System.Console.Out
    db

type dbt = SqlToBrandDb.ServiceTypes
fsi.PrintLength <- 1000
fsi.AddPrinter (fun (x : dbt.UserAuth) -> x.UserName)
fsi.AddPrinter (fun (x : dbt.Brand_User) -> x.BrandBrand.Name)

let force : IQueryable<dbt.UserAuth * dbt.Brand_User> -> unit = Seq.iter (fun _ -> ())
let show : IQueryable<dbt.UserAuth * dbt.Brand_User> -> _ = fun xs ->
    xs
    |> Seq.map (fun (a, b) ->
        match a,b with
        | null, null -> "(null, null)"
        | null, b -> sprintf "(null, %s)" b.BrandBrand.Name
        | a, null -> sprintf "(%s, null)" a.UserName
        | a, b -> sprintf "(%s, %s)" a.UserName b.BrandBrand.Name)
    |> List.ofSeq

let showUA : IQueryable<dbt.UserAuth> -> _ = fun xs ->
    xs
    |> Seq.map (fun a ->
        match a with
        | null -> "null"
        | a -> a.UserName)
    |> List.ofSeq

let getAdmins (brand : string) f = using <| dbContext () <| fun db ->
    let ofBrandUserAuth = <@ fun (b : string) ->
        if b = null then query {for a in db.UserAuth do select a} else
            query {
                for a in db.UserAuth do
                join un in db.UserName on
                    (a.Id = un.AuthId)
                join bu in db.Brand_User on
                    (un.Id = bu.User)
                where (bu.BrandBrand.Name = b)
                select a
            } @>

    <@ fun b -> query {
        for a in (%ofBrandUserAuth) b do
        select a
    } @>
    |> fun q -> query {yield! (%q) brand}
    |> f

getAdmins null showUA 
getAdmins "Zimtam" showUA

// The standard query gives the following results ...
//> 
//SELECT [t0].[Id], [t0].[UserName], [t0].[Email], [t0].[PrimaryEmail], [t0].[PhoneNumber], [t0].[FirstName], [t0].[LastName], [t0].[DisplayName], [t0].[Company], [t0].[BirthDate], [t0].[BirthDateRaw], [t0].[Address], [t0].[Address2], [t0].[City], [t0].[State], [t0].[Country], [t0].[Culture], [t0].[FullName], [t0].[Gender], [t0].[Language], [t0].[MailAddress], [t0].[Nickname], [t0].[PostalCode], [t0].[TimeZone], [t0].[Salt], [t0].[PasswordHash], [t0].[DigestHa1Hash], [t0].[Roles], [t0].[Permissions], [t0].[CreatedDate], [t0].[ModifiedDate], [t0].[InvalidLoginAttempts], [t0].[LastLoginAttempt], [t0].[LockedDate], [t0].[RecoveryToken], [t0].[RefId], [t0].[RefIdStr], [t0].[Meta]
//FROM [dbo].[UserAuth] AS [t0]
//INNER JOIN [dbo].[UserName] AS [t1] ON [t0].[Id] = [t1].[AuthId]
//INNER JOIN [dbo].[Brand|User] AS [t2] ON [t1].[Id] = [t2].[User]
//INNER JOIN [dbo].[Brand] AS [t3] ON [t3].[Id] = [t2].[Brand]
//WHERE [t3].[Name] = @p0
//-- @p0: Input NVarChar (Size = 4000; Prec = 0; Scale = 0) [Icefan]
//-- Context: SqlProvider(Sql2008) Model: AttributedMetaModel Build: 4.0.30319.18408
//
//val it : string list = []
//> 
//SELECT [t0].[Id], [t0].[UserName], [t0].[Email], [t0].[PrimaryEmail], [t0].[PhoneNumber], [t0].[FirstName], [t0].[LastName], [t0].[DisplayName], [t0].[Company], [t0].[BirthDate], [t0].[BirthDateRaw], [t0].[Address], [t0].[Address2], [t0].[City], [t0].[State], [t0].[Country], [t0].[Culture], [t0].[FullName], [t0].[Gender], [t0].[Language], [t0].[MailAddress], [t0].[Nickname], [t0].[PostalCode], [t0].[TimeZone], [t0].[Salt], [t0].[PasswordHash], [t0].[DigestHa1Hash], [t0].[Roles], [t0].[Permissions], [t0].[CreatedDate], [t0].[ModifiedDate], [t0].[InvalidLoginAttempts], [t0].[LastLoginAttempt], [t0].[LockedDate], [t0].[RecoveryToken], [t0].[RefId], [t0].[RefIdStr], [t0].[Meta]
//FROM [dbo].[UserAuth] AS [t0]
//-- Context: SqlProvider(Sql2008) Model: AttributedMetaModel Build: 4.0.30319.18408
//
//val it : string list = ["Aa"; "Bb"; "Cc"]
//> 
//SELECT [t0].[Id], [t0].[UserName], [t0].[Email], [t0].[PrimaryEmail], [t0].[PhoneNumber], [t0].[FirstName], [t0].[LastName], [t0].[DisplayName], [t0].[Company], [t0].[BirthDate], [t0].[BirthDateRaw], [t0].[Address], [t0].[Address2], [t0].[City], [t0].[State], [t0].[Country], [t0].[Culture], [t0].[FullName], [t0].[Gender], [t0].[Language], [t0].[MailAddress], [t0].[Nickname], [t0].[PostalCode], [t0].[TimeZone], [t0].[Salt], [t0].[PasswordHash], [t0].[DigestHa1Hash], [t0].[Roles], [t0].[Permissions], [t0].[CreatedDate], [t0].[ModifiedDate], [t0].[InvalidLoginAttempts], [t0].[LastLoginAttempt], [t0].[LockedDate], [t0].[RecoveryToken], [t0].[RefId], [t0].[RefIdStr], [t0].[Meta]
//FROM [dbo].[UserAuth] AS [t0]
//INNER JOIN [dbo].[UserName] AS [t1] ON [t0].[Id] = [t1].[AuthId]
//INNER JOIN [dbo].[Brand|User] AS [t2] ON [t1].[Id] = [t2].[User]
//INNER JOIN [dbo].[Brand] AS [t3] ON [t3].[Id] = [t2].[Brand]
//WHERE [t3].[Name] = @p0
//-- @p0: Input NVarChar (Size = 4000; Prec = 0; Scale = 0) [Zimtam]
//-- Context: SqlProvider(Sql2008) Model: AttributedMetaModel Build: 4.0.30319.18408
//
//val it : string list = ["Bb"; "Cc"]

// The following fails ...
(*
let getAdmins' (brand : string) f = using <| dbContext () <| fun db ->
    let ofBrandUserAuth = <@ fun (b : string) ->
        if b = null then dbquery {for a in db.UserAuth do select a} else
            dbquery {
                for a in db.UserAuth do
                join un in db.UserName on
                    (a.Id = un.AuthId)
                join bu in db.Brand_User on
                    (un.Id = bu.User)
                where (bu.BrandBrand.Name = b)
                select a
            } @>

    <@ fun b -> dbquery {
        for a in (%ofBrandUserAuth) b do
        select a
    } @>
    |> fun q -> dbquery {yield! (%q) brand}
    |> f

getAdmins' null showUA 
getAdmins' "Zimtam" showUA
*)

// The error from dbquery is ...
//System.InvalidOperationException: System.Linq.IQueryable`1[System.Object] is not a GenericTypeDefinition. MakeGenericType may only be called on a type for which Type.IsGenericTypeDefinition is true.
//   at System.RuntimeType.MakeGenericType(Type[] instantiation)
//   at FSharpComposableQuery.QueryImpl.QueryBuilder.getType(Exp exp)
//   at FSharpComposableQuery.QueryImpl.QueryBuilder.getType(Exp exp)
//   at FSharpComposableQuery.QueryImpl.QueryBuilder.reduce(Exp exp)
//   at FSharpComposableQuery.QueryImpl.QueryBuilder.reduce(Exp exp)
//   at FSharpComposableQuery.QueryImpl.QueryBuilder.reduce(Exp exp)
//   at FSharpComposableQuery.QueryImpl.QueryBuilder.reduce(Exp exp)
//   at FSharpComposableQuery.QueryImpl.QueryBuilder.reduce(Exp exp)
//   at FSharpComposableQuery.QueryImpl.QueryBuilder.reduce(Exp exp)
//   at FSharpComposableQuery.QueryImpl.QueryBuilder.reduce(Exp exp)
//   at FSharpComposableQuery.QueryImpl.QueryBuilder.reduce(Exp exp)
//   at FSharpComposableQuery.QueryImpl.QueryBuilder.reduce(Exp exp)
//   at FSharpComposableQuery.QueryImpl.QueryBuilder.reduce(Exp exp)
//   at FSharpComposableQuery.QueryImpl.QueryBuilder.reduce(Exp exp)
//   at FSharpComposableQuery.QueryImpl.QueryBuilder.reduce(Exp exp)
//   at FSharpComposableQuery.QueryImpl.QueryBuilder.reduce(Exp exp)
//   at FSharpComposableQuery.QueryImpl.QueryBuilder.Norm[T](FSharpExpr`1 expr)
//   at FSharpComposableQuery.QueryImpl.QueryBuilder.Run[T](FSharpExpr`1 q)
//   at FSI_0003.getAdmins'@73.Invoke(FCQ_Brands db) in .\FSharp.Linq.ComposableQuery\tests\FCQ-Brands\FCQ-Brands.fsx:line 86
//   at Microsoft.FSharp.Core.Operators.Using[T,TResult](T resource, FSharpFunc`2 action)
//   at <StartupCode$FSI_0004>.$FSI_0004.main@()
//Stopped due to error