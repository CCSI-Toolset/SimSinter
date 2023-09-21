# Development Environment

The current recommended development environment is:
- Visual Studio Community Edition 2022
    - Desktop .NET
    - Desktop C++
- WIS4 plugin for VS

In order to build the software also needed are (verified versions):
- Windows Server 2022
- Aspen v14
- Office 2021

# Building SimSinter

1. udunits2 is a pre-requisite that needs to be built first. 
The SimSinter solution is dependant on the artifacts (udunits2.dll and expat.dll) generated from this. 
The solution will work with modifications if the following steps are taken:
    1. From the package root navigate to the udunits2 build directory:
    ```
    cd Master\UC2\udunits2
    mkdir build
    cd build
    ```
    2. Run cmake to configure the build and create VS solutions.
    ```
    cmake ..
    ```
    3. Build the UDUnits solution (either in Visual Studio or with the following).
    ```
    MSBuild.exe /t:Clean /p:Configuration=Release UDUnits.sln
    MSBuild.exe /p:Configuration=Release UDUnits.sln
    ```
2. Open the SimSinter solution in Visual Studio and build your Configuration.

# Running tests

The SimSinter solution includes a suite of regression tests. We recommend running them in Visual Studio with the Debug configuration. 
You will need to make modifications for other setups.

- If the ACM executions tests fail fast with an error related to the input files this may be related to an ACM default preference. https://esupport.aspentech.com/S_Article?id=000098760 provides the solution.
