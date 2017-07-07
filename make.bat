REM SimSinter make.bat


cmake clean
cmake --build Master\UC2\udunits2 --config Release


REM TODO figure out path to nuget
"C:\Program Files\NuGet\nuget.exe" restore Master 

REM TODO figure out path to MSBUILD
"C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" /t:Clean Master\SimSinter.sln  
"C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" /p:Configuration=Release Master\SimSinter.sln 

REM TODO figure out path to MSTest
rm SimSinter-Build.trx
"C:\Program Files\Microsoft Visual Studio 11.0\Common7\IDE\MSTest.exe" /resultsfile:SimSinter-Build.trx /test:SinterRegressionTests.SinterInputTests.ParseVariableTest /testcontainer:C:\Jenkins\workspace\SimSinter\Master\SinterRegressionTests\bin\Release\SinterRegressionTests.dll
