# RJCP Framework

The RJCP Framework is a collection of libraries for use with other projects.

## Getting Started

### The Target Framework

The target frameworks are the Desktop version of .NET (version 4.0 to 4.8) and
.NET Core (.NET Standard 2.1 and .NET Core App 3.1 for unit tests).

### Documentation

Documentation is generally created using the Mark Down format. Look through the
directories for files ending with the extension `.md`.

## Compiling

### Preconditions

Before compiling, you'll need .NET Core installed of course. Special
requirements include:

* Update your NuGet repository path to include `RJCP.MSBuildTasks.nupkg`

When performing release builds on Windows:

* Ensure that `signtool.exe` is in your path for release builds;
* The file `signcert.crt` is the certificate that is sought for in the
  certificate store of Windows. Update this file to match the signing
  certificate to use (it's the public portion only). Ensure to import the
  private portion certificate separately.
* If strongname signing is enabled, ensure that the file can be found
  (`rjcp.snk`).

### Using RJ BUILD

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
  * When this flag is not used, the default configuration is `dev`.
  * When this flag is used, the default configuration is `release`.

You can run this on Windows or Linux, the specific command provided are slightly
different to support development on both platforms. Releases are only supported
on Windows.

#### Configurations when Building

The `git rj build` command also accespts the option `-c CONFIG` which can
compile for a particular purpose. The configurations are defined in the
`.gitrjbuild` file, which is a JSON file. At this time, there are four different
configuration files:

* `dev`: The default configuration used with `git rj build`. It builds with `-c
  Debug` on the command line, and enables extra debug options (such as checking
  for overflow, useful for unit testing).
* `release`: The default configuration used with `git rj build --release`. It
  builds with `-c Release`, enables authenticode signing (with timestamping) and
  strong-name signing. This is the configuration that should be used for
  creating releases. The project files that use `RJCP.MSBuildTasks` also do
  extra checks against the revision control system.
* `devsn`: The same as `dev`, but enables strong-name signing. This is useful
  for checking that code works as expected (because `InternalsVisibleTo` by
  default won't work with strong-name signing without some changes to the source
  to provide the public token, or to remove `InternalsVisibleTo`).
* `preview`: This is the same as `release`, but doesn't do strong-name signing
  or authenticode signing. This is useful to run all unit tests in release mode
  before performing a release.

Rule of thumb:

* Development should use `git rj build` which uses `--config dev` by default.
* If modifying a project that uses InternalsVisibleTo, also run `--config devsn`
  to make sure it should properly sign when run in release mode, without having
  to perform labelling, etc.
* When doing a release
  * Run first with `git rj build --config preview --release` and fix all the
    labelling. This doesn't do authenticode, so can be slightly faster (or no
    internet connection is needed). This doesn't strong-sign because it might be
    valuable at this stage running more tests that have access to
    `InternalsVisibleTo`.
  * Final bild with `git rj build --release`.

### NuGet Packages

The output of the build in this repository are project binaries and NuGet
packages. You can use these NuGet packages in your own repositories. You should
*not* upload them to NuGet.

## Questions

Questions about this project may be sent to the author at `Jason Curl
<jcurl@arcor.de>.` Please be aware, this is non-paid work, and an answer very
much depends on free time.
