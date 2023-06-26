# Target Software and Unit Testing <!-- omit in toc -->

The libraries in this project target various frameworks, from .NET 4.0 to .NET
4.8 and .NET Standard 2.1 for .NET Core applications.

- [1. Multiple Target Support](#1-multiple-target-support)
  - [1.1. Building for .NET 4.0](#11-building-for-net-40)
- [2. Testing Multiple Targets](#2-testing-multiple-targets)
  - [2.1. Testing Legacy .NET 4.0 to 4.6.1](#21-testing-legacy-net-40-to-461)
  - [2.2. Considerations for Multitarget Support](#22-considerations-for-multitarget-support)
- [3. Operating System Support](#3-operating-system-support)

## 1. Multiple Target Support

The feature that enables multiple targets is the C#
[TargetFrameworks](https://learn.microsoft.com/en-us/dotnet/standard/frameworks)
feature available in .NET Core SDK build projects.

### 1.1. Building for .NET 4.0

Visual Studio 2022 and later now omit the SDKs for .NET 4.0 to .NET 4.5.1. To
get these targets, one must install Visual Studio 2019, or similar work loads
that contain the target libraries.

So long as there is an available platform to build for .NET 4.0 and later, this
project will continue to do so. In the case that it is not possible to build all
targets within a single invocation (especially when generating the NuGet
packages), support will have to (unfortunately) drop.

## 2. Testing Multiple Targets

Generally, for each target that the library sets, there is a test project for
that target. Usually this is simple, the `TargetFrameworks` as set for the
library project is the same as in the test project.

### 2.1. Testing Legacy .NET 4.0 to 4.6.1

Since upgrading to `Microsoft.NET.Test.Sdk` v17.4.0 or later, and also
`NUnit3TestAdapter` v4.4.0 or later, test projects targetting .NET 4.0 to .NET
4.6.1 are not executed. The libraries and the test projects are still built
however.

The goal is still to keep current the SDK and Test Runners. This makes testing
of packages earlier than .NET 4.6.2 more challenging.

This project will continue to target for .NET 4.0. To test .NET 4.0 libraries, a
separate test project is required (they'll be called `RJCP.XXX.Legacy.csproj`).
This separate test project is near identical to the main test project, but is
specifically set up to target .NET 4.8 and import the library for .NET 4.0. For
testing purposes it is not ideal, but sufficient. Reasoning is that .NET 4.0 is
not installed, but .NET 4.8 on modern machines which is a highly compatible
replacement for .NET 4.0.

This allows to use the most recent version of the `Microsoft.NET.Test.Sdk` and
`NUnit3TestAdapter`.

This work was done as part of my internal work tracking DOTNET-827 (search the
commits with this `Issue:` field for details).

### 2.2. Considerations for Multitarget Support

The decision of the project is therefore that:

* .NET 4.0, if suitable, is a target. This can be used by platforms that can run
  .NET 4.0, such as Windows 2003, Windows XP, ReactOS, etc. This has the name
  "Legacy" in this project. The rationale is that if trivial to support, this
  platform should still be built for.
* The next targetted version of .NET is the supported version of .NET, at this
  point in time, it is .NET 4.6.2. Therefore, all other versions (.NET 4.5 to
  4.6.1) are removed. The rationale is that newer platforms, such as Windows 7
  can run .NET Framework software up to .NET 4.8.1.
* Where possible, .NET Standard 2.1 should be targetting for .NET Core. Only if
  it is absolutely necessary, will a newer version of the .NET Core framework
  (e.g. .NET 6 LTS, or later) be targetted.

This decision reduces the effort required to still support .NET 4.0 by not
supporting other unsupported versions of .NET framework. All other versions of
.NET framework will continue to be used.

## 3. Operating System Support

The libraries will primarily target Windows Operating Systems. Linux support is
a worthwhile, but a secondary goal.

The main problem supporting Linux OSes is the availability of the .NET
Frameworks. Ubuntu 18.04 supported through Mono, .NET 4.0 - 4.8, as well as .NET
Core 2.1 and 3.1. Unfortunately, Mono does not appear to be targetting newer
versions of Ubuntu or Debian (their documentation on installation is out of
date, thus indicating that the intention of targetting Ubuntu 20.04 or later is
limited). By all means, there are workarounds and ways to install, but raises
the bar required for users, likely that it won't be used by users on newer
platforms.

Older versions of .NET Core (e.g. 2.1 or 3.1) are not deployed on newer versions
of Ubuntu, and compatibility between Linux distributions is not as good as
Windows.

Generally, the built targets will run on Linux, but testing is limited, as it is
not so easy to run the build scripts on Linux.
