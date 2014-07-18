namespace System
open System.Reflection
open System.Runtime.CompilerServices

[<assembly: InternalsVisibleToAttribute("FSharpComposableQuery.Tests")>]
[<assembly: AssemblyTitleAttribute("FSharpComposableQuery")>]
[<assembly: AssemblyProductAttribute("FSharpComposableQuery")>]
[<assembly: AssemblyDescriptionAttribute("A Compositional, Safe Query Framework for F# Queries.")>]
[<assembly: AssemblyVersionAttribute("0.1.1")>]
[<assembly: AssemblyFileVersionAttribute("0.1.1")>]
()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.1.1"
