@echo off
cls
if not exist packages\FAKE\tools\Fake.exe (
  .nuget\nuget.exe install FAKE -OutputDirectory packages -ExcludeVersion
)
set platform=Any CPU
packages\FAKE\tools\FAKE.exe build.fsx %*
pause
