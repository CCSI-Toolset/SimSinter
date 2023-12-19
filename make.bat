REM SimSinter make.bat
REM
REM The entirety of this is not verified to work.

REM Get SimSinter dependencies
REM    nuget.exe must be in the PATH 
nuget.exe restore Master 

REM Compile the SimSinter project
REM    MSBuild.exe must be in the PATH
MSBuild.exe /t:Clean /p:Configuration=Release Master\SimSinter.sln  
MSBuild.exe /p:Configuration=Release Master\SimSinter.sln 

REM Run the Tests
REM    MSTest.exe must be in the PATH

REM Delete the previous test results
if EXIST SimSinter-Build.trx. (
  del SimSinter-Build.trx 
)
MSTest.exe /resultsfile:SimSinter-Build.trx /testcontainer:Master\SinterRegressionTests\bin\Release\SinterRegressionTests.dll
