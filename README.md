# FSharpComposableQuery
**(work in progress)**

A Compositional Query Framework for F# Queries, based on ["A Practical Theory of Language-Integrated Query" (ICFP 2013)](http://dl.acm.org/citation.cfm?id=2500586)


##Building

~~If you would like to use the library simply proceed to the Installation section.~~

### Prerequisites
 - Visual Studio 2012 or later. 


To build, simply type

    build
in a command prompt or terminal. Currently tested only under Windows. 

If you get a _File does not exist_ error make sure you can run MSBuild and MSTest from the console (i.e. you have them in your PATH variable). In the case you cannot, either use Visual Studio Command Prompt, open a cmd from within Visual Studio, or add the respective directories manually to your PATH variable.  

By default the build script executes all unit tests after compilation. Please note they are expected to fail unless you follow the instructions in the README.md file of the [FSharpComposableQuery.Tests](tests/FSharpComposableQuery.Tests) project on setting up the testing environment. 

## Installation

~~You can find the library on [NuGet](https://www.nuget.org/packages/FSharpComposableQuery).~~

As of 02.07.14 the version available on NuGet is outdated. Please build the library manually 

## Usage

Check out the [tutorial](http://fsprojects.github.io/FSharp.Linq.Experimental.ComposableQuery/) for examples and an overview of the main features of the library. 
