# RjBuild

The tool `RjBuild` is a custom tool written in C# to support and enhance
building and testing of the HELIOS project. It brings the following benefits:

* Multitargetting builds for a single source, by automating the build process.
* Parallel execution of test suites for improved performance.
* Incremental test execution based on the GIT revision control system.

## Minimum Requirements

### Development Machines

For building on a development machine:

* Visual Studio 2019.
* For Unit Testing, you will need to have .NET 3.5 and NUnit 2.6.4 installed.
  For Windows 8 or later, go to the Control Panel item "Windows Features" and
  enable .NET 2.0 and .NET 3.5SP1.
* Ensure that you have the SDKs for .NET installed. It might vary, run the build
  script and then install the SDKs as required. At the time of writing this
  documentation, the following SDKs are required.
  * .NET 4.0
  * .NET 4.5

Start the appropriate Solution File with Visual Studio. Visual Studio Code is
not supported for building using MSBuild.

### Command Line Build (and useful for CI, and VSCode)

Installation Tools required

* Subversion Command Line Tools, and in the PATH
* Visual Studio 2019 (version 16.9 or compatible). See next section for
  Installing Visual Studio Build Tools.
* .NET 4.0 and .NET 4.5 SDK Installation
* .NET 4.8 runtime installed to run `RjBuild.exe`. This is already provided by
  Windows 10 v1903 and later.
* .NET 3.5SP1 fully patched. This is required for NUnit 2.6.4
* NUnit 2.6.4 for some older test cases installed into the standard location.
* The correct code signing certificate, or modify the `rjcp.rjproj` to
  remove/change the code signing certificate hash.

To build the documentation

* HTML Help Compiler 1.4 installation
* SHFB 2020.3.6.0

When everything is installed, just run `build.bat` on Windows. If you're using
Visual Studio Code, use the default build task that also runs `build.bat`.

#### Installing Visual Studio Build Tools

Install the build tools and the .NET SDKs. It is possible to have Build Tools
installed which is a different / older version of the Visual Studio IDE
developer tools. Take note of the version that `RjBuild.exe` requires. Thus you
can use the newest Visual Studio Enterprise edition, and still build with an
older compatible Build Tools (note, to download older versions, you should
create an offline installation first, or you'll need a MSDN subscription from
microsoft to download the non-latest version)

* MSBuild installed (usually as part of VS2019)

  * One can install the `vs_buildtools.exe`, so that MSBuild is normally
    installed in

    `C:\Program Files (x86)\Microsoft Visual
    Studio\2019\BuildTools\MSBuild\Current\Bin\msbuild.exe`

  * You can download the Visual Studio 2019 Build Tools from Microsoft's
    servers. Find the Visual Studio 2019 Build Tools installer (Current), and
    download the build tools with the command:

    `vs_buildtools.exe --layout offline --add
    Microsoft.VisualStudio.Workload.MSBuildTool --lang en-US`

  * Install it quickly across multiple computers, copy the offline installation
    and start with the command

    `vs_buildtools.exe -p`

* The .NET 4.5 SDK

* The .NET 4.0 SDK must be on the build machine. There is no installer. Usually
  it must be installed along side of Visual Studio and adding the .NET 4.0 SDK.
  The easiest way without installing a complete development environment is to
  copy the reference assemblies from a development machine to a CI Server:

  `c:\Program Files (x86)\Reference
  Assemblies\Microsoft\Framework\.NETFramework\v4.0`

#### Visual Studio Versions

The `RjBuild.exe` tool is built against NuGet packages of Visual Studio (the GAC
versions only work with Visual Studio 2015 and earlier). Occasionally Microsoft
push updates for Visual Studio that will break RjBuild (this happened for Visual
Studio 16.4 and Visual Studio 16.9). That means upgrading Visual Studio may
break the build system.

The recommendation is to install a known version of Visual Studio Build Tools
along side the IDE version of Visual Studio (such as Community, Professional or
Enterprise).

#### Code Signing

When using `build.bat` to build the system, it will perform code signing on the
generated executables. This requires the code sign certificate and private key
to be in the Computers keystore. If it isn't, some components will fail
building.

This problem does not occur when using Visual Studio, as the project files by
default have the code signing hash not defined. Modify the `rjcp.rjproj` file to
provide the correct codesigning hash, or remove it, to allow building.

#### Parallel Execution of Test Suites

Test suites are now executed in parallel based on the number of CPUs in the
system.

Because test suites may now be run in parallel (note, parallel test case
execution is up to the test runner itself, not the RjBuild tool), one must be
careful about the global resources required when running a test suite.

Examples of global resources that may prevent test cases running in parallel
are:

* TCP sockets. No two test cases should try and listen on the same socket
  simultaneously. Further, clients generally use TCP listeners that may only
  exist for the duration of the test case.
* Serial Ports. A test machine generally has a system configuration that a test
  case uses. No two test case should use the same global resource
  simultaneously. It might be one succeeds, the other fail, or both fail, or
  test results are undefined.
