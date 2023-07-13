REM SimSinter make.bat
REM
REM This make file is a reproduction of how SimSinter was being
REM built on Jenkins at LBNL.  It has been tested on:
REM    - 32 bit Windows 7 installation (required)
REM
REM Pre-requisites:
REM   - CMake 3.9
REM   - Microsoft Vistual 11.0
REM   - Microsoft .NET v4.030319
REM   - NuGet Package Manager
REM   - Wix Toolset v3.10

REM Copy installer files to local directory
REM - this didn't work - copy "C:\Program Files\Common files\Merge Modules\Microsoft_VC110_CRT_x86.msm" Master\CCSIUnitsInstaller
REM - this didn't work - copy "C:\Program Files\Common files\Merge Modules\Microsoft_VC110_ATL_x86.msm" Master\CCSIUnitsInstaller
copy "C:\Program Files (x86)\Common Files\Merge Modules\Microsoft_VC110_CRT_x86.msm" Master\CCSIUnitsInstaller
copy "C:\Program Files (x86)\Common Files\Merge Modules\Microsoft_VC110_ATL_x86.msm" Master\CCSIUnitsInstaller

REM Compile the UC2 udunits2 library
cd Master\UC2\udunits2
cmake clean .
cmake --build . --config Release
copy expat\Release\expat.dll C:\Windows\expat.dll
copy lib\Release\udunits2.dll C:\Windows\udunits2.dll
cd ..\..\..

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
