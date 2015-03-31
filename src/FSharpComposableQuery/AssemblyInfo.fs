namespace System
open System.Reflection
open System.Runtime.CompilerServices

[<assembly: InternalsVisibleToAttribute("FSharpComposableQuery.Tests")>]
[<assembly: AssemblyTitleAttribute("FSharpComposableQuery")>]
[<assembly: AssemblyProductAttribute("FSharpComposableQuery")>]
[<assembly: AssemblyDescriptionAttribute("A Compositional, Safe Query Framework for Dynamic F# Queries.")>]
[<assembly: AssemblyVersionAttribute("1.0.4")>]
[<assembly: AssemblyFileVersionAttribute("1.0.4")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0.4"
