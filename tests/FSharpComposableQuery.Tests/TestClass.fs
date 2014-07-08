namespace FSharpComposableQuery.Tests

open System

/// <summary>
/// A base test class to provide static query tagging
/// </summary>
type TestClass() = 

    static let mutable tag = 0

    /// <summary>
    /// Attaches an unique integer tag to the given string and then prints it. 
    /// </summary>
    /// <param name="txt"></param>
    member this.tagQuery (?txt:string) = 
        tag <- tag + 1
        match txt with
        | Some(s) -> 
            printfn "Q%02d %s" tag s
        | None -> 
            printfn "Q%02d " tag