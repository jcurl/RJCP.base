# RjBuild

The tool `RjBuild` is a custom tool written in C# to support and enhance
building and testing of the HELIOS project. It brings the following benefits:

* Multitargetting builds for a single source, by automating the build process.
* Parallel execution of test suites for improved performance.
* Incremental test execution based on revision control system.

## Minimum Requirements

Common Dependencies, that may not be on your computer.

* For computers earlier than Microsoft Windows 10 v1903, install .NET 4.8.
  `RjBuild.exe` is built against this version of the framework.
* For Unit Testing, you will need to have .NET 3.5 and NUnit 2.6.4 installed.
  For Windows 8 or later, go to the Control Panel item "Windows Features" and
  enable .NET 2.0 and .NET 3.5SP1.
* NUnit3 test cases are supported also, and require no special installation,
  only that the projects reference the correct libraries.

### Development Machines

For building on a development machine, just install Visual Studio 2019.

* Ensure that you have the SDKs for .NET installed. It might vary, run the build
  script and then install the SDKs as required. At the time of writing this
  documentation, the following SDKs are required.
  * .NET 4.0
  * .NET 4.5

If you have an older version of Visual Studio, you can install just the build
tools, as in the next section for setting up for a CI server.

### Continuous Integration Servers

For building on a CI, you will need to have:

* MSBuild installed (usually as part of VS2019)
  * One can install the `vs_buildtools.exe`, so that MSBuild is normally
    installed in

    `C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\msbuild.exe`

  * You can download the Visual Studio 2019 Build Tools from Microsoft's
    servers. Find the Visual Studio 2019 Build Tools installer (Current), and
    download the build tools with the command:

    `vs_buildtools.exe --layout offline --add Microsoft.VisualStudio.Workload.MSBuildTool --lang en-US`

  * Install it quickly across multiple computers, copy the offline installation
    and start with the command

    `vs_buildtools.exe -p`

* The .NET 4.5 SDK and others are needed for builds to succeed:

  * The .NET 4.0 SDK must be on the build machine. There is no installer,
    without installing Visual Studio with .NET 4.0. The easiest way without
    installing a complete development environment is to copy the reference
    assemblies from a development machine to a CI Server:

    `c:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0`

## Parallel Execution of Test Suites

Test suites are now executed in parallel based on the number of CPUs in the
system.

Because test suites may now be run in parallel (note, parallel test case
execution is up to the test runner itself, not the RjBuild tool), one must be
careful about the global resources required when running a test suite.

Examples of global resources that may prevent test cases running in parallel are:

* TCP sockets. No two test cases should try and listen on the same socket
  simultaneously. Further, clients generally use TCP listeners that may only
  exist for the duration of the test case.
* Serial Ports. A test machine generally has a system configuration that a test
  case uses. No two test case should use the same global resource
  simultaneously. It might be one succeeds, the other fail, or both fail, or
  test results are undefined.
