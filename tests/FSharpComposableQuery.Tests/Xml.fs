namespace FSharpComposableQuery.Tests

open Microsoft.FSharp.Data.TypeProviders
open Microsoft.FSharp.Quotations
open Microsoft.VisualStudio.TestTools.UnitTesting
open System.Linq
open System.Xml.Linq
open FSharpComposableQuery

/// <summary>
/// Contains example queries and operations on the Xml database. 
/// The queries here are further wrapped in quotations to allow for their evaluation in different contexts (see Utils.fs).  
/// <para>These tests require the schema from sql/xml.sql in a database referred to in app.config </para>
/// </summary>
module Xml =
    [<Literal>]
    let xmlPath = "data\movies.xml"
    
    let basicXml = XElement.Parse "<a id='1'><b><c>foo</c></b><d><e/><f/></d></a>"

    type internal schema = SqlDataConnection< ConnectionStringName="XmlConnectionString", ConfigFile=".\\App.config" >

    type internal Data = schema.ServiceTypes.Data

    type internal Text = schema.ServiceTypes.Text

    type internal Attribute = schema.ServiceTypes.Attribute

    type Axis =
        | Self
        | Child
        | Descendant
        | DescendantOrSelf
        | Following
        | FollowingSibling
        | Rev of Axis
        
    type Path =
        | Seq of Path * Path
        | Axis of Axis
        | Name of string
        | Filter of Path

    let internal db = schema.GetDataContext()
    let internal data = db.Data
    let internal text = db.Text
    let internal attributes = db.Attribute

    // XML document loading/shredding
    let mutable idx = 0

    let new_id() =
        let i = idx
        idx <- i + 1
        i

    let rec traverseXml entry parent i (node : XNode) =
        match node with
        | :? XElement as xml ->
            let id = new_id()
            let j = Seq.iter (traverseAttribute entry id) (xml.Attributes())
            let j = traverseChildren entry id (i + 1) (xml.Nodes())
            let d = new Data()
            d.Name <- xml.Name.ToString()
            d.ID <- id
            d.Entry <- entry
            d.Pre <- i
            d.Post <- j
            d.Parent <- parent
            data.InsertOnSubmit(d)
            data.Context.SubmitChanges()
            j + 1
        | :? XText as xtext ->
            let id = new_id()
            let d = new Data()
            d.Name <- "#text"
            d.ID <- id
            d.Entry <- entry
            d.Pre <- i
            d.Post <- i
            d.Parent <- parent
            data.InsertOnSubmit(d)
            data.Context.SubmitChanges()
            let t = new Text()
            t.ID <- id
            t.Value <- xtext.Value
            text.InsertOnSubmit(t)
            i + 1
        | _ -> i

    and traverseChildren entry parent i (xmls) = Seq.fold (traverseXml entry parent) i xmls

    and traverseAttribute entry parent att =
        let a = new Attribute()
        a.Element <- parent
        a.Name <- att.Name.LocalName
        a.Value <- att.Value
        attributes.InsertOnSubmit(a)

    let insertXml entry xml =
        let root_id = new_id()
        let j = traverseXml entry root_id 1 xml
        let d = new Data()
        d.ID <- root_id
        d.Entry <- entry
        d.Pre <- 0
        d.Post <- j
        d.Parent <- -1
        d.Name <- "#document"
        data.InsertOnSubmit(d)
        data.Context.SubmitChanges()

    let loadXml entry (filename : string) =
        let xml = XElement.Load(filename)
        insertXml entry xml

    // Clears all relevant tables in the database. 
    let dropTables() =
        ignore (db.DataContext.ExecuteCommand("TRUNCATE TABLE [MyXml].[dbo].[Attribute]"))
        ignore (db.DataContext.ExecuteCommand("TRUNCATE TABLE [MyXml].[dbo].[Text]"))
        ignore (db.DataContext.ExecuteCommand("TRUNCATE TABLE [MyXml].[dbo].[Data]"))

    let loadBasicXml() =
        dropTables()
        insertXml 0 basicXml

    let rec internal axisPred' axis =
        match axis with
        | Self -> <@ fun (row1 : Data) (row2 : Data) -> row1.ID = row2.ID @>
        | Child -> <@ fun (row1 : Data) (row2 : Data) -> row1.ID = row2.Parent @>
        | Descendant -> <@ fun (row1 : Data) (row2 : Data) -> row1.Pre < row2.Pre && row2.Post < row1.Post @>
        | DescendantOrSelf -> <@ fun (row1 : Data) (row2 : Data) -> row1.Pre <= row2.Pre && row2.Post <= row1.Post @>
        | Following -> <@ fun (row1 : Data) (row2 : Data) -> row1.Post < row2.Pre @>
        | FollowingSibling -> <@ fun (row1 : Data) (row2 : Data) -> row1.Post < row2.Pre && row1.Parent = row2.Parent @>
        | Rev axis -> <@ fun row1 row2 -> (%axisPred' axis) row2 row1 @>

    let internal axisPred axis =
        <@ fun (row1 : Data) (row2 : Data) -> row1.Entry = row2.Entry && (%(axisPred' axis)) row1 row2 @>

    let (./.) p1 p2 = Seq(p1, p2)
    let child = Axis Child
    let self = Axis Self
    let descendant = Axis Descendant
    let descendant_or_self = Axis DescendantOrSelf
    let parent = Axis(Rev Child)
    let ancestor = Axis(Rev Descendant)
    let ancestor_or_self = Axis(Rev DescendantOrSelf)
    let following = Axis Following
    let followingsibling = Axis FollowingSibling
    let preceding = Axis(Rev Following)
    let precedingsibling = Axis(Rev FollowingSibling)
    let (.%.) path name = Seq(path, Name name)
    let (.^.) path1 path2 = Seq(path1, Filter(path2))

    let internal pathQuery data (path) =
        let rec pathQ path =
            match path with
            | Seq(p1, p2) ->
                <@ fun row1 row3 ->
                    query {
                        for row2 in %data do
                            exists ((%(pathQ p1)) row1 row2 && (%(pathQ p2)) row2 row3)
                    } @>
            | Axis ax -> <@ fun (row : Data) (row' : Data) -> ((%(axisPred ax)) row row') @>
            | Name name -> <@ fun (row : Data) (row' : Data) -> row.Name = name && row.ID = row'.ID @>
            | Filter p ->
                <@ fun (row : Data) (row' : Data) ->
                    row.ID = row'.ID && query {
                                            for row'' in %data do
                                                exists ((%(pathQ p)) row row'')
                                        } @>
        <@ fun row1 ->
            query {
                for row2 in %data do
                    if (%pathQ path) row1 row2 then yield row2
            } @>

    
    /// <summary>
    /// Translates a path p to a query that returns each node matching p, starting from the root. 
    /// </summary>
    /// <param name="rootId">The id of the root node. </param>
    /// <param name="data">The XML document to run the query on. </param>
    /// <param name="p">The path to construct the query from. </param>
    let internal xpath rootId data p =
        <@ query {
               for root in %data do
                   for row' in (%(pathQuery data p)) root do
                       if (root.Parent = -1 && root.Entry = rootId) then yield row'.ID
           } @>

    
    let xp0 = child ./. child //                                                        /*/*
    let xp1 = descendant ./. parent //                                                  //*/parent::*
    let xp2 = descendant ./. (Filter(followingsibling .%. "dirn")) //                   //*[following-sibling::d]
    let xp3 = descendant .%. "year" ./. Filter(ancestor ./. preceding .%. "dir") //     //f[ancestor::*/preceding::b]

    [<TestClass>]
    [<DeploymentItem("data", "data")>]  //requires "data/movies.xml"
    type TestClass() =

        [<ClassInitialize>]
        static member init (c : TestContext) =
            printf "Xml: Parsing file %A... " xmlPath
            dropTables()
            loadXml 0 xmlPath
            printfn "done!"

        [<TestMethod>]
        member this.test01() =
            printfn "%s" "xp0"
            xp0
            |> xpath 0 <@ data @>
            |> Utils.Run

        [<TestMethod>]
        member this.test02() =
            printfn "%s" "xp1"
            xp1
            |> xpath 0 <@ data @>
            |> Utils.Run

        [<TestMethod>]
        member this.test03() =
            printfn "%s" "xp2"
            xp2
            |> xpath 0 <@ data @>
            |> Utils.Run

        [<TestMethod>]
        member this.test04() =
            printfn "%s" "xp3"
            xp3
            |> xpath 0 <@ data @>
            |> Utils.Run