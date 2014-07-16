FSharpComposableQuery Tests
===========================

Setting up the environment
-----------------------
1. Install a recent version of Microsoft SQL Server. The version used while developing was Microsoft SQL Server 2014 Express. 
2. Note the server and database name of the newly installed SQL server.
3. Edit the `<connectionStrings>` values in the App.config file in this directory so they point to the correct data source (server-name\db-name). You can use the provided App.config.sample file as an example. 
3. Execute the SQL scripts from the [sql](/sql) folder on the SQL Server using e.g. SQL Server Management Studio
4. You should now be able to run the tests using the target "All". 

Running the tests
-----------------
A test is considered passed as long as the QueryBuilder produced a valid output query and it was executed without error. 

The results of the ComposableQueryBuilder can also be compared against the output of the native one and both query builders can be benchmarked from within the Visual Studio project by modifying the value of the `Utils.RunMode` variable to either run and raise exceptions (the default mode used with MSTest), compare and print, or time and print. 

Please note that there are some issues with the result comparison feature (issue #2) and some queries can not be evaluated with the F# 3.0 default QueryBuilder - the first column of test results - which is totally expected. 