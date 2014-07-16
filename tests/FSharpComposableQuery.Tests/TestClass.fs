namespace FSharpComposableQuery.Tests

open System

/// <summary>
/// A base test class to provide static query tagging
/// </summary>
type TestClass() = 

    let mutable idx = 0
    
    /// <summary>
    /// Generates a unique tag for this class instance.
    /// </summary>
    member this.tag() = 
        idx <- idx + 1
        idx
