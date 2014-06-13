FSharpComposableQuery Tests
===========================

Setting up the database
----------------------------------
1. Install Microsoft SQL Server. Tested with Microsoft SQL Server 2014 Express. 
2. Note the server and database name of the SQL server.
3. Edit the <connectionStrings> values in the App.config file in this directory so they point to the correct data source (server-name\db-name). You can use the provided App.config.sample file to start with. 
3. Execute the SQL scripts in the sql/ folder on the SQL Server
4. You should now be able to execute the tests using the build file. If you get a "file does not exist" error, make sure you run the script from VS Command Prompt or have MSBuild/MSTest in your path. 