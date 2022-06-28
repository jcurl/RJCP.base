# RJCP Framework <!-- omit in toc -->

The RJCP Framework is a collection of libraries for use with other projects.

## 1. Getting Started

### 1.1. The Target Framework

The target frameworks are the Desktop version of .NET (version 4.0 to 4.8) and
.NET Core (.NET Standard 2.1 and .NET Core App 3.1 for unit tests).

### 1.2. Documentation

Documentation is generally created using the Mark Down format. Look through the
directories for files ending with the extension `.md`.

## 2. Compiling

### 2.1. Frameworks

The tools in this collection target the frameworks:

* .NET 4.0
* .NET 4.5 and later, up to .NET 4.8
* .NET Core Standard 2.1, .NET Core 3.1

#### 2.1.1. Linux

To get .NET 4.0 SDK on Linux, install from
[Mono](https://www.mono-project.com/download/stable/).

You should install [.NET Core 3.1
SDK](https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu) for your
Operating System.

If your Operating System doesn't support .NET Core 3.1 (e.g. Ubuntu 22.04 LTS
only supports .NET Core 6 and later), you should install the version that is
supported. Th .NET Core 6 SDK is tested on Ubuntu 22.04 to still build .NET Core
3.1 binaries. To run those binaries without the .NET 3.1 runtime, set the
environment variable:

```sh
export DOTNET_ROLL_FORWARD=LatestMajor
```

This will tell the runtime to use the most up to date runtime available.

### 2.2. Preconditions

When performing release builds on Windows:

* Ensure that `signtool.exe` is in your path for release builds;
* The file `signcert.crt` is the certificate that is sought for in the
  certificate store of Windows. Update this file to match the signing
  certificate to use (it's the public portion only). Ensure to import the
  private portion certificate separately.
* If strongname signing is enabled, ensure that the file can be found
  (`rjcp.snk`).

### 2.3. Checking Out

This repository, `RJCP.base`, is the main repository that defines the directory
structure, and through submodules, the configuration management. You don't need
this repository to build, but it can help.

If you check out a submodule directly, assuming the dependent submodules are in
the correct paths, you can build local debug builds with the `dotnet build`
command. This builds for all target frameworks, including .NET Desktop.

This repository contains the global solution file, which is needed when
compiling with Visual Studio 2019. If you load a solution file from a repository
from a submodule, you should first build it with the `dotnet build` command,
that builds properly all dependencies, then the solution for that submodule
should also generally build. Loading an individual solution file can make it
faster for development.

Once you've checked out this repository:

```cmd
git config user.email "myemail@email.com"
git config user.name "John Smith"
```

Add the `git-rj.py` tool to the path. The implementation is done using Python
3.9, so you should have this, or a compatible version installed. There is a
small script written in bash, called `git-rj` that should be executable next to
it, that can simplify running the python script from Windows, MSys for Windows
(GIT bash), or Linux.

From the base directory of this repository:

```cmd
export PATH=$PATH:`pwd`/configmgr
```

Finally, initialize the repository, which will check out all the submodules and
configure them for you:

```cmd
git rj init
```

### 2.4. Using RJ BUILD

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

#### 2.4.1. Configurations when Building

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

### 2.5. Further GIT RJ help

You can run `git rj help` to get information. See the file
[README.gitrj](configmgr/README.gitrj.md) for more help.

### 2.6. NuGet Packages

The output of the build in this repository are project binaries and NuGet
packages. You can use these NuGet packages in your own repositories. You should
*not* upload them to NuGet.

## 3. Questions

Questions about this project may be sent to the author at `Jason Curl
<jcurl@arcor.de>.` Please be aware, this is non-paid work, and an answer very
much depends on free time.
