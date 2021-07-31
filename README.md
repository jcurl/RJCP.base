# RJCP Framework

The RJCP Framework is a collection of libraries for use with other projects.

## Getting Started

### The Target Framework

The target frameworks are the Desktop version of .NET (version 4.0 to 4.8) and
.NET Core (.NET Standard 2.1 and .NET Core App 3.1 for unit tests).

### Documentation

Documentation is generally created using the Mark Down format. Look through the
directories for files ending with the extension `.md`.

### Compiling

Generally, the projects can be loaded and compiled with Visual Studio 2019. It
uses the `dotnet` tool for building.

To compile from the command line, the `git-rj.py` command provides support.

```cmd
git rj build
```

The options:

* `git rj build --clean` - run the clean target with `dotnet clean`
* `git rj build --build` - build only in debug mode with `dotnet build`
* `git rj build --test` - run test cases with `dotnet test`
* `git rj build --pack` - create NuGet packages with `dotnet pack`
* `git rj build --doc` - create help files with msbuild and Sandcastle
  * You must have built prior in release mode with `dotnet build -c Release`
  * To build the documentation
    * HTML Help Compiler 1.4 installation
    * SHFB 2020.3.6.0 or later
    * Visual Studio Build Tools (msbuild)
* `git rj build --release` - Build, test, package and create documentation in
  release mode.

You can run this on Windows or Linux, the specific command provided are slightly
different to support development on both platforms. Releases are only supported
on Windows.

### NuGet Packages

The output of the build in this repository are project binaries and NuGet
packages. You can use these NuGet packages in your own repositories. You
should *not* upload them to NuGet.

## Questions

Questions about this project may be sent to the author at `Jason Curl
<jcurl@arcor.de>.` Please be aware, this is non-paid work, and an answer very
much depends on free time.
