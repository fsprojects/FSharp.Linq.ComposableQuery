# FSharpComposableQuery
**(work in progress)**

A Compositional Query Framework for F# Queries, based on ["A Practical Theory of Language-Integrated Query" (ICFP 2013)](http://dl.acm.org/citation.cfm?id=2500586)


##Build

~~If you want to use the library simply proceed to the Installation section.~~

### Prerequisites
 - Visual Studio 2012 or later. 


To build, open a command prompt or a terminal window and type

    build
to execute the build script for your system (tested only under Windows). 
The default build target does not execute any unit tests since they require the presence of a properly configured SQL Server database. 
You can follow the instructions in the README.md file of the [FSharpComposableQuery.Tests](tests/FSharpComposableQuery.Tests) project on setting up the testing environment. 

You may get a _File does not exist_ error if you do not have MSBuild or MSTest in your current path. In such a case you can do one of the following:
 - Open VS Command Prompt from its shortcut or from inside Visual Studio
 - Add the respective directories with the executables manually to your PATH variable.  


## Installation

~~You can find the library on [NuGet](https://www.nuget.org/packages/FSharpComposableQuery).~~

As of 02.07.2014 the version available on NuGet is outdated. Please build the library manually if you intend to use it. 

## Usage

Check out the [tutorial](http://fsprojects.github.io/FSharp.Linq.Experimental.ComposableQuery/) for examples and an overview of the main features of the library. 
