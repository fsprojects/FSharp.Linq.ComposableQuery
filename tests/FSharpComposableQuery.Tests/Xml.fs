namespace FSharpComposableQuery.Tests

open FSharpComposableQuery.TestUtils
open Microsoft.FSharp.Data.TypeProviders
open Microsoft.FSharp.Quotations
open System.Linq
open Microsoft.VisualStudio.TestTools.UnitTesting;
open System.Xml.Linq

    
module Xml = 


    type internal schema = SqlDataConnection<ConnectionStringName="XmlConnectionString", ConfigFile=".\\App.config">
    type internal Data = schema.ServiceTypes.Data
    type internal Text = schema.ServiceTypes.Text
    type internal Attribute = schema.ServiceTypes.Attribute

    let internal db = schema.GetDataContext()

    let internal data = db.Data

    let internal text = db.Text

    let internal attributes = db.Attribute


    // XML document loading/shredding
    let idx = ref 0
    let new_id() = let i = !idx in
                   idx := i+1
                   i

    let rec traverseXml entry parent i (node:XNode) = 
        match node with 
          | :? XElement as xml -> 
              let id = new_id() in
              let j = Seq.iter (traverseAttribute entry id) (xml.Attributes()) in
              let j = traverseChildren entry id (i+1) (xml.Nodes()) in
              let d = new Data() in
              d.Name <- xml.Name.ToString();
              d.ID <- id;
              d.Entry <- entry;
              d.Pre <- i;
              d.Post <- j;
              d.Parent <- parent;
              data.InsertOnSubmit(d);
              j+1
          | :? XText as xtext -> 
              let id = new_id() in
              let d = new Data() in
              d.Name <- "#text";
              d.ID <- id;
              d.Entry <- entry;
              d.Pre <- i;
              d.Post <- i;
              d.Parent <- parent;
              data.InsertOnSubmit(d);
              let t = new Text() in
              t.ID <- id;
              t.Value <- xtext.Value;
              text.InsertOnSubmit(t);
              i+1
           | _ -> i

    and traverseChildren entry parent i (xmls) = 
          Seq.fold (traverseXml entry parent) i xmls

    and traverseAttribute entry parent att = 
        let a = new Attribute() in
        a.Element <- parent;
        a.Name <- att.Name.LocalName;
        a.Value <- att.Value;
        attributes.InsertOnSubmit(a)
    
    

    let insertXml entry xml = 
      let root_id = new_id() in 
      let j = traverseXml entry root_id 1 xml;
      let d = new Data() in 
      d.ID <- root_id;
      d.Entry <- entry;
      d.Pre <- 0;
      d.Post <- j;
      d.Parent <- -1;
      d.Name <- "#document";
      data.InsertOnSubmit(d);
      data.Context.SubmitChanges()

    let loadXml entry (filename:string) = 
      let xml = XElement.Load(filename) 
      in insertXml entry xml

    let dropTables() = 
      ignore(db.DataContext.ExecuteCommand("DELETE FROM [MyXml].[dbo].[Attribute] WHERE 1=1"))
      ignore(db.DataContext.ExecuteCommand("DELETE FROM [MyXml].[dbo].[Text] WHERE 1=1"))
      ignore(db.DataContext.ExecuteCommand("DELETE FROM [MyXml].[dbo].[Data] WHERE 1=1"))
    

    let defaultXml = XElement.Parse "<a id='1'><b><c>foo</c></b><d><e/><f/></d></a>"

    let loadBasicXml() =
        dropTables()
        insertXml 0 defaultXml



    type Axis = Self
              | Child 
              | Descendant
              | DescendantOrSelf
              | Following
              | FollowingSibling
              | Rev of Axis
    //          | Preceding 
    //          | PrecedingSibling
    //          | Parent 
    //          | Ancestor
    //          | AncestorOrSelf


    let rec internal axisPred' axis = 
      match axis with 
          Self             -> <@ fun (row1:Data) (row2:Data) -> row1.ID = row2.ID @>
        | Child            -> <@ fun (row1:Data) (row2:Data) -> row1.ID = row2.Parent @>
        | Descendant       -> <@ fun (row1:Data) (row2:Data) -> row1.Pre < row2.Pre && row2.Post < row1.Post @>
        | DescendantOrSelf -> <@ fun (row1:Data) (row2:Data) -> row1.Pre <= row2.Pre && row2.Post <= row1.Post @>
        | Following        -> <@ fun (row1:Data) (row2:Data) -> row1.Post < row2.Pre @>
        | FollowingSibling -> <@ fun (row1:Data) (row2:Data) -> row1.Post < row2.Pre && row1.Parent = row2.Parent @>
        | Rev axis -> <@ fun row1 row2 -> (%axisPred' axis) row2 row1 @>
    //    | Parent           -> <@ fun (row1:Data) (row2:Data) -> row1.Parent = row2.ID @>
    //    | Ancestor         -> <@ fun (row1:Data) (row2:Data) -> row2.Pre < row1.Pre && row1.Post < row2.Post @>
    //    | AncestorOrSelf   -> <@ fun (row1:Data) (row2:Data) -> row2.Pre <= row1.Pre && row1.Post <= row2.Post @>
    //    | Preceding        -> <@ fun (row1:Data) (row2:Data) -> row2.Pre < row1.Pre @>
    //    | PrecedingSibling -> <@ fun (row1:Data) (row2:Data) -> row2.Pre < row1.Pre && row1.Parent = row2.Parent @>

    let internal axisPred axis = <@ fun  (row1:Data) (row2:Data) -> row1.Entry = row2.Entry && (%(axisPred' axis)) row1 row2 @>

    type Path = Seq of Path * Path
              | Axis of Axis 
              | Name of string
              | Filter of Path

    let internal pathQ data path = 
      let rec pathQ' path = 
          match path with 
            Seq (p1,p2) -> <@ fun row -> (%pathQ' p1) row 
                                         |> Seq.collect(fun row' -> (%(pathQ' p2)) row')  @>
          | Axis ax -> <@ fun (row:Data) -> Seq.filter (fun (row':Data) -> row.Entry = row'.Entry && (%(axisPred ax)) row row') %data @>
          | Name name -> <@ fun (row:Data) -> if row.Name = name then Seq.singleton row else Seq.empty @>
          | Filter p -> <@ fun (row:Data) -> if Seq.exists (fun _ -> true) ((%(pathQ' p)) row) then Seq.singleton row else Seq.empty @>
      in pathQ' path

    let internal pathQ2 data path = 
      let rec pathQ' path = 
          match path with 
            Seq (p1,p2) -> 
            <@ fun row -> seq   { for row1 in (%(pathQ' p1)) row do 
                                  for row2 in (%(pathQ' p2)) row1 do
                                  yield row2 } @>
          | Axis ax -> 
            <@ fun row -> seq   { for (row':Data) in %data do 
                                         if ((%(axisPred ax)) row row')
                                         then yield row' } @>
          | Name name -> <@ fun row -> seq {if row.Name = name then yield row } @>
          | Filter p -> <@ fun row -> seq {if Seq.exists (fun _ -> true) ((%pathQ' p) row) 
                                                  then yield row } @>
      in pathQ' path

    let internal pathP data path = 
      let rec pathP' path = 
          match path with 
            Seq (p1,p2)   -> <@ fun row1 row3 -> 
              Seq.exists (fun row2 -> 
                  (%pathP' p1) row1 row2 && (%pathP' p2) row2 row3) 
                %data  @>
          | Axis ax       -> axisPred ax
          | Name name -> <@ fun row1 row2 ->  
              row1.Name = name  && row1.ID = row2.ID @>
          | Filter p      -> <@ fun row1 row2 -> 
              row1.ID = row2.ID && 
              Seq.exists (fun row3 -> (%pathP' p) row1 row3) %data @>
      in <@ fun row1 -> seq {for row2 in %data do if (%pathP' path) row1 row2 then yield row2 } @>

    let internal pathP2 data path = 
      let rec pathP' path = 
          match path with 
            Seq (p1,p2)   -> <@ fun (row1:Data, row3:Data) -> 
              Seq.exists (fun row2 -> 
                  (%pathP' p1) (row1, row2) && (%pathP' p2) (row2, row3)) 
                %data  @>
          | Axis ax       -> <@ fun (row1,row2) -> (%axisPred ax) row1 row2 @>
          | Name name -> <@ fun (row1, row2) ->  
              row1.Name = name  && row1.ID = row2.ID @>
          | Filter p      -> <@ fun (row1, row2) -> 
              row1.ID = row2.ID && 
              Seq.exists (fun row3 -> (%pathP' p) (row1, row3)) %data @>
      in <@ fun row1 -> seq {for row2 in %data do if (%pathP' path) (row1, row2) then yield row2 } @>


    // /a == Seq (Axis Child, Name "a")
    // //a == Seq(Axis DescendantOrSelf, (Seq (Axis Child, Name "a")))

    let (./.) p1 p2 = Seq(p1,p2)

    let child = Axis Child
    let self = Axis Self
    let descendant = Axis  Descendant
    let descendant_or_self = Axis DescendantOrSelf
    let parent = Axis (Rev Child)
    let ancestor = Axis (Rev Descendant)
    let ancestor_or_self = Axis (Rev DescendantOrSelf)
    let following = Axis Following
    let followingsibling = Axis FollowingSibling
    let preceding = Axis (Rev Following)
    let precedingsibling = Axis (Rev FollowingSibling)

    let (.%.) path name = Seq(path,Name name)
    let (.^.) path1 path2 = Seq(path1,Filter(path2))



    let internal pathQuery data (path) = 
      let rec pathQ' path = 
          match path with 
            Seq (p1,p2) -> 
            <@ fun row -> query { for row1 in (%(pathQ' p1)) row do 
                                  for row2 in (%(pathQ' p2)) row1 do
                                  yield row2 } @>
          | Axis ax -> 
            <@ fun (row:Data) -> query { for (row':Data) in %data do 
                                         where ((%(axisPred ax)) row row')
                                         select row' } @>
          | Name name -> <@ fun (row:Data) -> query {if row.Name = name then yield row } @>
          | Filter p -> <@ fun (row:Data) -> query {if query{for row' in ((%(pathQ' p)) row) do exists true }
                                                    then yield row } @>
      in pathQ' path

    let internal pathQuery' data (path) = 
      let rec pathQ' path = 
          match path with 
            Seq (p1,p2) -> 
            <@ fun row -> query { for row1 in (%(pathQ' p1)) row do 
                                  for row2 in (%(pathQ' p2)) row1 do
                                  yield row2 } @>
          | Axis ax -> 
            <@ fun (row:Data) -> query { for (row':Data) in %data do 
                                         where ((%(axisPred ax)) row row')
                                         select row' } @>
          | Name name -> <@ fun (row:Data) -> query {where (row.Name = name)
                                                     yield row } @>
          | Filter p -> <@ fun (row:Data) -> query {where (query{for row' in ((%(pathQ' p)) row) do exists true}) 
                                                    yield row } @>
      in pathQ' path


    let internal pathQuery2 data (path) = 
      let rec pathQ2 path = 
          match path with 
            Seq (p1,p2) -> 
            <@ fun row1 row3 -> query { for row2 in %data do
                                        exists ((%(pathQ2 p1)) row1 row2 && (%(pathQ2 p2)) row2 row3) 
                                      } @>
          | Axis ax -> 
            <@ fun (row:Data) (row':Data) -> ((%(axisPred ax)) row row') @>
          | Name name -> <@ fun (row:Data) (row':Data) -> row.Name = name && row.ID = row'.ID  @>
          | Filter p -> <@ fun (row:Data) (row':Data) -> 
                                 row.ID = row'.ID && 
                                 query{for row'' in %data do exists ((%(pathQ2 p)) row row'') } @>
      in <@ fun row1 -> query {for row2 in %data do if (%pathQ2 path) row1 row2 then yield row2 } @>




    let internal xpath i data path = <@ seq { 
        for row in %data do 
            for row' in (%(pathP2 data path)) row do
                if (row.Parent = -1 && row.Entry = i) 
                    then yield row'.ID} @>


    let internal printRow (row:Data) = printfn "ID:%d Name:%s Parent:%d Pre:%d Post:%d" row.ID row.Name row.Parent row.Pre row.Post
    let internal printRows x = x |> Seq.iter printRow
    let forceRows x = x |> Seq.iter (fun _ -> ())

    let internal xpath' i data path = <@ query { 
        for row in %data do 
            for row' in (%(pathQuery2 data path)) row do 
                if (row.Parent = -1 && row.Entry = i) 
                    then yield row'.ID} @>

    let countRows xs = printfn "%d" (Seq.length xs)
    let testXPath xp = 
      testAll (xpath 0 <@data@> xp) (xpath' 0 <@data@> xp) countRows

    let timeXPath xp = 
      timeAll (xpath 0 <@data@> xp) (xpath' 0 <@data@> xp) forceRows
    let timeXPath' xp = 
      timeAll' (xpath 0 <@data@> xp) (xpath' 0 <@data@> xp) forceRows

    //let simfs2 xp = timeFS2 "FSharp 2.0 (NF)" (xpath 0 <@data@> xp |> nf_expr) countRows
    let simfs3 xp = timeFS3 "FSharp 3.0 (NF)" (xpath' 0 <@data@> xp) countRows
    let simPLinqq xp = testPLinqQ "PLinqQ" (xpath' 0 <@data@> xp) countRows

    let xp0 = child ./. child


    let xp1 = descendant ./. parent


    let xp2 = descendant ./. (Filter (followingsibling .%. "dirn"))


    let xp3 = descendant .%. "year" ./. Filter(ancestor ./. preceding .%. "dir")

    let doBasicTest() = 
        printfn "Populating db"
        loadBasicXml()

        printfn "xp0"
        //testXPath xp0
        timeXPath xp0
        printfn "xp1"
        //testXPath xp1
        timeXPath xp1
        printfn "xp2"
        //testXPath xp2
        timeXPath xp2
        printfn "xp3"
        //testXPath xp3
        timeXPath xp3



    let doTest'()  =

        [("xp0",    timeXPath' xp0);
         ("xp1",    timeXPath' xp1);
         ("xp2",    timeXPath' xp2);
         ("xp3",    timeXPath' xp3)]

    [<TestClass>]
    type TestClass() = 
        inherit FSharpComposableQuery.Tests.TestClass()
        
        [<ClassInitialize>]
        static member init (c:TestContext) = 
            dropTables()
            insertXml 0 defaultXml  //simple tests

        [<TestMethod>]
        member this.testXp0() = 
            this.tagQuery "xp0"
            timeXPath xp0
        
        [<TestMethod>]
        member this.testXp1() = 
            this.tagQuery "xp1"
            timeXPath xp1
        
        [<TestMethod>]
        member this.testXp2() = 
            this.tagQuery "xp2"
            timeXPath xp2
        
        [<TestMethod>]
        member this.testXp3() = 
            this.tagQuery "xp3"
            timeXPath xp3