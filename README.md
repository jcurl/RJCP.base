# RJCP Framework

The RJCP Framework is a collection of libraries for use with other projects.

## Getting Started

### The Target Framework

The target frameworks are the Desktop version of .NET 4.0 to .NET 4.8. There is
no plan to migrate to `dotnet` at this time, as my tooling doesn't support this
(yet) and this is non-paid work.

### Documentation

Documentation is generally created using the Mark Down format. Look throught the
directories for files ending with the extension `.md`.

For instructions on building, see `README.RjBuild.md`.

### Compiling

Generally, the projects can be loaded and compiled with Visual Studio 2019. I've
written some specialized tooling called `RjBuild`, that adds extra checks on top
of the solution files, creating NuGet packages, checking dependencies and
automatically running test cases.

### NuGet Packages

The output of the build in this repository are project binaries and NuGet
packages. You can use these NuGet packages in your own repositories. You
should *not* upload them to NuGet. If there will ever be a local NuGet server,
that information will be made available in the SCMP. Until then, get them from
SVN (see the SCMP) and use them in your project.

## Questions

Questions about this project may be sent to the author at `Jason Curl
<jcurl@arcor.de>.` Please be aware, this is non-paid work, and an answer very
much depends on free time.
