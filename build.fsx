// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#r @"packages/FAKE/tools/FakeLib.dll"
open Fake 
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open System

// --------------------------------------------------------------------------------------
// START TODO: Provide project-specific details below
// --------------------------------------------------------------------------------------

// Information about the project are used
//  - for version and project name in generated AssemblyInfo file
//  - by the generated NuGet package 
//  - to run tests and to publish documentation on GitHub gh-pages
//  - for documentation, you also need to edit info in "docs/tools/generate.fsx"

// The name of the project 
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "FSharpComposableQuery"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "A Compositional, Safe Query Framework for Dynamic F# Queries."

// Longer description of the project
// (used as a description for NuGet package; line breaks are automatically cleaned up)
let description = """
  A Compositional, Safe Query Framework for Dynamic F# Queries
  """
// List of author names (for NuGet package)
let authors = [ "James Cheney"; "Sam Lindley"; "Yordan Stoyanov" ]
// Tags for your project (for NuGet package)
let tags = "F# fsharp LINQ SQL database data dynamic query"

// File system information 
// Pattern specifying all library files (projects or solutions)
let libraryReferences  = !! "src/*/*.fsproj"
// Pattern specifying all test files (projects or solutions)
let testReferences = !! "tests/*/*.fsproj"
// The output directory
let buildDir = "./bin/"


// Pattern specifying assemblies to be tested using MSTest
let testAssemblies = !! "bin/FSharpComposableQuery*Tests*.exe"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted 
let gitHome = "https://github.com/fsprojects"
// The name of the project on GitHub
let gitName = "FSharpComposableQuery"

// --------------------------------------------------------------------------------------
// END TODO: The rest of the file includes standard build steps 
// --------------------------------------------------------------------------------------

// Read additional information from the release notes document
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let release = parseReleaseNotes (IO.File.ReadAllLines "RELEASE_NOTES.md")

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" (fun _ ->
  let fileName = "src/" + project + "/AssemblyInfo.fs"
  CreateFSharpAssemblyInfo fileName
      [ Attribute.InternalsVisibleTo "FSharpComposableQuery.Tests"
        Attribute.Title project
        Attribute.Product project
        Attribute.Description summary
        Attribute.Version release.AssemblyVersion
        Attribute.FileVersion release.AssemblyVersion ] 
)

// --------------------------------------------------------------------------------------
// Clean build results & restore NuGet packages

Target "RestorePackages" (fun _ ->
    !! "./**/packages.config"
    |> Seq.iter (RestorePackage (fun p -> { p with ToolPath = "./.nuget/NuGet.exe" }))
)

Target "Clean" (fun _ ->
    CleanDirs [buildDir; "temp"]
)

Target "CleanDocs" (fun _ ->
    CleanDirs ["docs/output"]
)

// --------------------------------------------------------------------------------------
// Build library

Target "Build" (fun _ ->
    let props = [("DocumentationFile", project + ".XML")]   //explicitly generate XML documentation
    MSBuildReleaseExt buildDir props "Rebuild" libraryReferences
    |> Log "Build-Output: "
)

// --------------------------------------------------------------------------------------
// Build tests and library

Target "BuildTest" (fun _ ->
    MSBuildRelease buildDir "Rebuild" testReferences
    |> Log "BuildTest-Output: "
)

// --------------------------------------------------------------------------------------
// Run unit tests using test runner & kill test runner when complete

Target "RunTests" (fun _ ->
    let nunitVersion = GetPackageVersion "packages" "NUnit.Runners"
    let nunitPath = sprintf "packages/NUnit.Runners.%s/Tools" nunitVersion
    ActivateFinalTarget "CloseTestRunner"

    testAssemblies
    |> NUnit (fun p ->
        { p with
            ToolPath = nunitPath
            DisableShadowCopy = true
            TimeOut = TimeSpan.FromMinutes 20.
            OutputFile = "TestResults.xml" })
)

FinalTarget "CloseTestRunner" (fun _ ->
    ProcessHelper.killProcess "nunit-agent.exe"
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target "NuGet" (fun _ ->
    // Format the description to fit on a single line (remove \r\n and double-spaces)
    let description = description.Replace("\r", "")
                                 .Replace("\n", "")
                                 .Replace("  ", " ")
    let nugetPath = ".nuget/nuget.exe"
    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = release.NugetVersion
            ReleaseNotes = String.Join(Environment.NewLine, release.Notes)
            Tags = tags
            OutputPath = "bin"
            ToolPath = nugetPath
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [] })
        ("nuget/" + project + ".nuspec")
)

// --------------------------------------------------------------------------------------
// Generate the documentation

Target "GenerateDocs" (fun _ ->
    executeFSIWithArgs "docs/tools" "generate.fsx" ["--define:RELEASE"] [] |> ignore
)

// --------------------------------------------------------------------------------------
// Release Scripts

Target "ReleaseDocs" (fun _ ->
    let tempDocsDir = "temp/gh-pages"
    CleanDir tempDocsDir
    Repository.cloneSingleBranch "" (gitHome + "/" + gitName + ".git") "gh-pages" tempDocsDir

    Repository.fullclean tempDocsDir
    CopyRecursive "docs/output" tempDocsDir true |> tracefn "%A"
    StageAll tempDocsDir
    Commit tempDocsDir (sprintf "Update generated documentation for version %s" release.NugetVersion)
    Branches.push tempDocsDir
)

Target "Release" DoNothing

Target "All" DoNothing

// --------------------------------------------------------------------------------------
// Run 'Build' target by default. Invoke 'build <Target>' to override

"Clean" ==> "RestorePackages" ==> "AssemblyInfo" ==> "Build"
"AssemblyInfo" ==> "BuildTest" ==> "RunTests" 
"CleanDocs" ==> "GenerateDocs" ==> "ReleaseDocs" 
"Build" ==> "RunTests" ==> "GenerateDocs" ==> "All"
"RunTests" ==> "NuGet" ==> "Release"

RunTargetOrDefault "Build"
