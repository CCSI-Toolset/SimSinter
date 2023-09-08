REM SimSinter make.bat
REM
REM The entirety of this is not verified to work.
REM This is the recommended/easiest way to build the udunits2 library that is required by the main SimSinter solution.
REM

REM Compile the UC2 udunits2 library
cd Master\UC2\udunits2
mkdir build
cd build
cmake ..
MSBuild.exe /t:Clean /p:Configuration=Release UDUnits.sln
MSBuild.exe /p:Configuration=Release UDUnits.sln

REM back to top of code base
cd ..\..\..\..

REM Get SimSinter dependencies
REM    nuget.exe must be in the PATH 
nuget.exe restore Master 

REM Compile the SimSinter project
REM    MSBuild.exe must be in the PATH
MSBuild.exe /t:Clean /p:Configuration=Release Master\SimSinter.sln  
MSBuild.exe /p:Configuration=Release Master\SimSinter.sln 

REM Run the Tests
REM    MSTest.exe must be in the PATH
REM    udunits2.dll and expat.dll created by the udunits build need to be in the PATH for the tests to run

REM Delete the previous test results
if EXIST SimSinter-Build.trx. (
  del SimSinter-Build.trx 
)
MSTest.exe /resultsfile:SimSinter-Build.trx /testcontainer:Master\SinterRegressionTests\bin\Release\SinterRegressionTests.dll
