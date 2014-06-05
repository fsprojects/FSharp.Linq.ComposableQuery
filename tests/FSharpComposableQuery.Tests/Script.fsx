//// Load the following files in order to run all of the tests.
//// Requires a database configures with tables of appropriate schemas.
//
//#load "Test.fsx"
//
//#load "People.fsx"
//#load "Nested.fsx"
//#load "XmlTest.fsx" 
//
//
//let results = People.doTest'() @ Nested.doTest'() @ XmlTest.doTest'()
//
//let latexify (msg,(fs2,fs3,ilinq,_,norm)) = 
//  printfn "%s & %.1f &  %.1f &  %.1f &  %.1f \\\\" msg fs2 fs3 ilinq norm
//
//let latexResults = Seq.iter latexify results
//
//
//
//
