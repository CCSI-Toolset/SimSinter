# SimSinter

**SimSinter for Aspen 7.3.2 or greater** Simulation interface for connecting Aspen Plus or ACM simulations to FOQUS and Turbine Gateway. Includes SinterConfigGUI.

## Getting Started
This software has been compiled and tested on Windows 7 professional

### Pre-requisites
Your environment must have the following tools installed.
The build has be tested with the following versions. Use other
versions at your own risk.

+ Git Bash for windows
+ [CMake 3.9](https://github.com/Kitware/CMake/releases/download/v3.19.2/cmake-3.19.2-win64-x64.msi)
+ Microsoft Visual Studio 11.0
+ Microsoft .NET v4.0.30319
+ NuGet Package Manager
+ [Wix Toolset v3.10] (https://wixtoolset.org/downloads/v3.10.4.4718/wix310.exe)

### Build and Test
After installing the tools above run the Git Bash program.
Executing the commands below with compile the source and 
run the tests.


```
git clone https://github.com/CCSI-Toolset/SimSinter.git
cd SimSinter
start make.bat

```

## Authors

* Jim Leek

See also the list of [contributors](../../contributors) who participated in this project.

## Development Practices

* Code development will be performed in a forked copy of the repo. Commits will not be 
  made directly to the repo. Developers will submit a pull request that is then merged
  by another team member, if another team member is available.
* Each pull request should contain only related modifications to a feature or bug fix.  
* Sensitive information (secret keys, usernames etc.) and configuration data 
  (e.g. database host port) should not be checked in to the repo.
* A practice of rebasing with the main repo should be used rather than merge commits.

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, 
see the [releases](../../releases) or [tags](../../tags) for this repository. 

## License & Copyright

See [LICENSE.md](LICENSE.md) file for details.
