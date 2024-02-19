# Conversion to .NET Core 6.0 <!-- omit in toc -->

- [1. Task List for the Upgrade](#1-task-list-for-the-upgrade)
- [2. Upgrading from .NET Standard 2.1 to .NET 6.0](#2-upgrading-from-net-standard-21-to-net-60)
- [3. Review of Repositories](#3-review-of-repositories)
- [4. Observations on Changes](#4-observations-on-changes)
  - [4.1. Project File Updates](#41-project-file-updates)
  - [4.2. Conditional Includes](#42-conditional-includes)
  - [4.3. Changes in Project Dependencies](#43-changes-in-project-dependencies)
  - [4.4. New Informational Diagnostics](#44-new-informational-diagnostics)
- [5. C# Language Upgrade](#5-c-language-upgrade)

## 1. Task List for the Upgrade

- Tasks
  - [x] No warnings NETSDK1138
  - [x] No warnings NU1702
  - [x] Create ticket for checking all SafeHandle implementations needed .NET
    Framework. DOTNET-937.
  - [x] Create ticket CA1416 (Callsite). Should find best way to suppress, and
    add our own warnings to code that isn't fully Windows. DOTNET-938.
  - [x] Create ticket CommandLine CS0649. DOTNET-939.
  - [X] Check for `SecurityPermission` and remove. DOTNET-940.
  - [X] Build release
  - [X] Fix netcoreapp -f and test on Linux.
  - [X] Review all projects.
    - [x] Should have no NETStandard anywhere.
    - [X] Check whitespace.
  - [x] Check for all `#if` guards. Should have no NETSTANDARD anywhere.
  - [X] Update RollForward to Major
  - [X] Run Benchmarks if it should be a DLL or an EXE
    - Must be an EXE, else it doesn't run.
  - [X] Run Benchmarks if it should be RollForward or not

## 2. Upgrading from .NET Standard 2.1 to .NET 6.0

The [.NET Standard
documentation](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-1)
covers:

> ### When to target net8.0 vs. netstandard
>
> For existing code that targets netstandard, there's no need to change the TFM
> to net8.0 or a later TFM. .NET 8 implements .NET Standard 2.1 and earlier. The
> only reason to retarget from .NET Standard to .NET 8+ would be to gain access
> to more runtime features, language features, or APIs. For example, in order to
> use C# 9, you need to target .NET 5 or a later version. You can multitarget
> .NET 8 and .NET Standard to get access to newer features and still have your
> library available to other .NET implementations.
>
> Here are some guidelines for new code for .NET 5+:
>
> - App components
>
>   If you're using libraries to break down an application into several
>   components, we recommend you target net8.0. For simplicity, it's best to
>   keep all projects that make up your application on the same version of .NET.
>   Then you can assume the same BCL features everywhere.
>
> - Reusable libraries
>
>   If you're building reusable libraries that you plan to ship on NuGet,
>   consider the trade-off between reach and available feature set. .NET
>   Standard 2.0 is the latest version that's supported by .NET Framework, so it
>   gives good reach with a fairly large feature set. We don't recommend
>   targeting .NET Standard 1.x, as you'd limit the available feature set for a
>   minimal increase in reach.
>
>   If you don't need to support .NET Framework, you could go with .NET Standard
>   2.1 or .NET 8. We recommend you skip .NET Standard 2.1 and go straight to
>   .NET 8. Most widely used libraries will multi-target for both .NET Standard
>   2.0 and .NET 5+. Supporting .NET Standard 2.0 gives you the most reach,
>   while supporting .NET 5+ ensures you can leverage the latest platform
>   features for customers that are already on .NET 5+.

As some of the libraries have dependencies on the .NET Framework, all .NET
Standard 2.1 libraries will be upgraded to .NET 6.0 as well.

## 3. Review of Repositories

Approximate order based on dependencies:

- ✓ RJCP.IO.Buffer
- ✓ RJCP.Core.Datastructures
- ✓ RJCP.Core.Text
- ✓ RJCP.Threading
- ✓ RJCP.Diagnostics.Trace
- ✓ RJCP.Core.Xml (Datastructures)
- ✓ RJCP.Environment
  - ✓ RJCP.Environment.Version (Path)
- ✓ RJCP.IO.Path (Datastructure, Environment)
- ✓ RJCP.Core.CommandLine (Environment)
- ✓ RJCP.CodeQuality (Envrironment)
- ✓ RJCP.Diagnostics.CrashReporter (Environment, Trace)
- ✓ RJCP.IO.Device (Environment, Trace)
- ✓ RJCP.Diagnostics.CpuId (Environment)
- ✓ RJCP.IO.VolumeDeviceInfo (Environment)
- ✓ RJCP.SerialPortStream (BufferIO, Trace, DeviceMgr)
  - ✓ RJCP.SerialPortStream.Virtual (BufferIO)
- ✓ RJCP.Diagnostics.Log (Datastructures, Trace)
  - ✓ RJCP.Diagnostics.Log.Dlt (Log, Datastructures, Text, Xml)
  - ✓ DltDump (Log, Log.DLT, CommandLine, Environment, CrashReporter, Trace,
    SerialPortStream, Path)

| Repository       | Component                              | Current FW     | Upgraded FW    | RollForward | Deps |
| ---------------- | -------------------------------------- | -------------- | -------------- | ----------- | ---- |
| base             | RJCP.MSBuildTasks                      | netstandard2.1 | netstandard2.1 | -           | -    |
| base             | RJCP.MSBuildProj                       | netcoreapp3.1  | net60          | ✓           | -    |
| bufferio         | RJCP.IO.Buffer.dll                     | netstandard2.1 | net60          | -           | -    |
| bufferio         | RJCP.IO.BufferTest.dll                 | netcoreapp3.1  | net60          | -           | -    |
| datastructures   | RJCP.Core.Datastructures.dll           | netstandard2.1 | net60          | -           | -    |
| datastructures   | RJCP.Core.DatastructuresTest.dll       | netcoreapp3.1  | net60          | -           | -    |
| datastructures   | RJCP.Core.DatastructuresBenchmark.exe  | netcoreapp3.1  | net60          | ✓           | -    |
| environment      | RJCP.Core.Environment.dll              | netstandard2.1 | net60          | -           | ✓    |
| environment      | RJCP.Core.EnvironmentTest.dll          | netcoreapp3.1  | net60          | -           | -    |
| path             | RJCP.IO.Path.dll                       | netstandard2.1 | net60          | -           | -    |
| path             | RJCP.IO.PathTest.dll                   | netcoreapp3.1  | net60          | -           | -    |
| path             | RJCP.IO.PathBenchmark.exe              | netcoreapp3.1  | net60          | ✓           | -    |
| path             | FileInfo.exe                           | netcoreapp3.1  | net60          | ✓           | -    |
| path             | FileInfoCheck.exe                      | netcoreapp3.1  | net60          | ✓           | -    |
| environment      | RJCP.Core.Environment.Version.dll      | netstandard2.1 | net60          | -           | ✓    |
| environment      | RJCP.Core.Environment.VersionTest.dll  | netcoreapp3.1  | net60          | -           | -    |
| environment      | NetVersion.exe                         | netcoreapp3.1  | net60          | ✓           | -    |
| environment      | WinVersion.exxe                        | netcoreapp3.1  | net60          | ✓           | -    |
| commandline      | RJCP.Core.CommandLine.dll              | netstandard2.1 | net60          | -           | -    |
| commandline      | RJCP.Core.CommandLineTest.dll          | netcoreapp3.1  | net60          | -           | -    |
| nunitextensions  | RJCP.CodeQuality.dll                   | netstandard2.1 | net60          | -           | ✓    |
| nunitextensions  | RJCP.CodeQualityTests.dll              | netcoreapp3.1  | net60          | -           | -    |
| cpuid            | RJCP.Diagnostics.CpuId.dll             | netstandard2.1 | net60          | -           | -    |
| cpuid            | RJCP.Diagnostics.CpuIdTest.dll         | netcoreapp3.1  | net60          | -           | -    |
| cpuid            | CpuIdCon.exe                           | netcoreapp3.1  | net60          | ✓           | -    |
| cpuid            | CpuIdWin.exe                           | netcoreapp3.1  | net60-windows  | ✓           | -    |
| trace            | RJCP.Diagnostics.Trace.dll             | netstandard2.1 | net60          | -           | ✓    |
| trace            | RJCP.Diagnostics.TraceTest.dll         | netcoreapp3.1  | net60          | -           | ✓    |
| crashreporter    | RJCP.Diagnostics.CrashReporter.dll     | netstandard2.1 | net60          | -           | ✓    |
| crashreporter    | RJCP.Diagnostics.CrashReporterTest.dll | netcoreapp3.1  | net60          | -           | -    |
| crashreporter    | CrashReportApp.exe                     | netcoreapp3.1  | net60          | ✓           | -    |
| devicemgr        | RJCP.IO.Device.dll                     | netstandard2.1 | net60          | -           | ✓    |
| devicemgr        | RJCP.IO.DeviceTest.dll                 | netcoreapp3.1  | net60          | -           | ✓    |
| devicemgr        | DeviceInfoDump.exe                     | netcoreapp3.1  | net60          | ✓           | ✓    |
| log              | RJCP.Diagnostics.Log.dll               | netstandard2.1 | net60          | -           | -    |
| log              | RJCP.Diagnostics.LogTest.dll           | netcoreapp3.1  | net60          | -           | -    |
| log              | RJCP.Diagnostics.Log.Dlt.dll           | netstandard2.1 | net60          | -           | -    |
| log              | RJCP.Diagnostics.Log.DltTest.dll       | netcoreapp3.1  | net60          | -           | -    |
| log              | RJCP.Diagnostics.Log.DltBenchmark.exe  | netcoreapp3.1  | net60          | ✓           | -    |
| log              | DltDump.exe                            | netcoreapp3.1  | net60          | ✓           | ✓    |
| log              | DltDumpBenchmark.exe                   | netcoreapp3.1  | net60          | ✓           | -    |
| log              | DltDumpTest.dll                        | netcoreapp3.1  | net60          | -           | -    |
| log              | DltUdpReceive.exe                      | netcoreapp3.1  | net60          | ✓           | -    |
| serialportstream | RJCP.SerialPortStream.dll              | netstandard2.1 | net60          | -           | ✓    |
| serialportstream | RJCP.SerialPortStreamTest.dll          | netcoreapp3.1  | net60          | -           | ✓    |
| serialportstream | RJCP.SerialPortStream.Virtual.dll      | netstandard2.1 | net60          | -           | -    |
| serialportstream | RJCP.SerialPortStream.VirtualTest.dll  | netcoreapp3.1  | net60          | -           | -    |
| serialportstream | BufferBytesTest.exe                    | netstandard2.1 | net60          | ✓           | ✓    |
| serialportstream | RJCP.SerialPortStreamManualTest.dll    | netcoreapp3.1  | net60          | -           | ✓    |
| serialportstream | RJCP.SerialPortStreamNativeTest.dll    | netcoreapp3.1  | net60          | -           | ✓    |
| text             | RJCP.Core.Text.dll                     | netstandard2.1 | net60          | -           | -    |
| text             | RJCP.Core.TextTest.dll                 | netcoreapp3.1  | net60          | -           | -    |
| text             | RJCP.Core.TextBenchmark.exe            | netcoreapp3.1  | net60          | ✓           | -    |
| text             | IEEE754TableGen.dll                    | netcoreapp3.1  | net60          | ✓           | -    |
| thread           | RJCP.Threading.dll                     | netstandard2.1 | net60          | -           | -    |
| thread           | RJCP.ThreadingTest.dll                 | netcoreapp3.1  | net60          | -           | -    |
| thread           | RJCP.Threading.TaskBenchmark.exe       | netcoreapp3.1  | net60          | ✓           | -    |
| volumedevinfo    | RJCP.IO.VolumeDeviceInfo.dll           | netstandard2.1 | net60          | -           | -    |
| volumedevinfo    | RJCP.IO.VolumeDeviceInfoTest.dll       | netcoreapp3.1  | net60          | -           | -    |
| volumedevinfo    | VolumeInfo.exe                         | netcoreapp3.1  | net60          | ✓           | -    |
| xml              | RJCP.Core.Xml.dll                      | netstandard2.1 | net60          | -           | -    |
| xml              | RJCP.Core.XmlTest.dll                  | netcoreapp3.1  | net60          | -           | -    |

The `RJCP.MSBuildTasks` remains to be compatible with all .NET Frameworks (it is
not deprecated). As such, there is no need to rebuild and package a new version.

## 4. Observations on Changes

### 4.1. Project File Updates

The following changes should be made to all projects:

- Change `netstandard2.1` and `netcoreapp3.1` to `net60`.
- Remove unneeded whitespace
- Update conditional includes, and use the `TargetFrameworkIdentifier` where
  possible.
- Update dependencies to .NET 6.0. Test what happens when they're removed (if it
  compiles, we don't need it).
- Update year to 2024.

### 4.2. Conditional Includes

The following can be used

- `NETCOREAPP` for .NET Core 3.1 and later include .NET 6.0
- `NET5_0_OR_GREATER`
- `NET6_0`
- `NET6_0_OR_GREATER`

While the `#if NETCOREAPP` can remain, I opted to move to `NET6_0_OR_GREATER`
for readability. Even if `NETCOREAPP` is a more generic term, the latter
indicates clearly the software is no longer tested with the older framework
versions or with .NET Standard 2.1.

### 4.3. Changes in Project Dependencies

Ensure to update the dependencies with:

```xml
<ItemGroup Condition="'$(TargetFrameworkIdentifier)' == '.NETCoreApp'">
</ItemGroup>

<ItemGroup Condition="'$(TargetFrameworkIdentifier)' == '.NETFramework'">
</itemGroup>

<ItemGroup Condition="$([MSBuild]::IsTargetFrameworkCompatible('$(TargetFramework)', 'net462'))">
</ItemGroup>
```

Upgrades for components:

```xml
<ItemGroup Condition="'$(TargetFrameworkIdentifier)' == '.NETCoreApp'">
  <PackageReference Include="Microsoft.Extensions.Configuration" Version="6.0.1" />
  <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="6.0.0" />
  <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="6.0.0" />
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="6.0.1" />
  <PackageReference Include="Microsoft.Extensions.Logging" Version="6.0.0" />
  <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="6.0.4" />
  <PackageReference Include="Microsoft.Extensions.Logging.Configuration" Version="6.0.0" />
  <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="6.0.0" />
  <PackageReference Include="System.Configuration.ConfigurationManager" Version="6.0.1" />
</ItemGroup>
```

Components that can be removed:

```xml
<ItemGroup Condition="'$(TargetFrameworkIdentifier)' == '.NETCoreApp'">
  <PackageReference Include="System.Collections.Specialized" Version="4.3.0" />
  <PackageReference Include="System.Diagnostics.FileVersionInfo" Version="4.3.0" />
  <PackageReference Include="System.Diagnostics.TraceSource" Version="4.3.0" />
  <PackageReference Include="System.Threading.Overlapped" Version="4.3.0" />
  <PackageReference Include="System.Threading.Thread" Version="4.3.0" />
  <PackageReference Include="System.Threading.ThreadPool" Version="4.3.0" />
  <PackageReference Include="Microsoft.Win32.Registry" Version="4.7.0" />
</ItemGroup>
```

### 4.4. New Informational Diagnostics

- IDE0078: Use Pattern Matching
- IDE0083: Use Pattern Matching
- IDE0090: 'new' expression can be simplified
- IDE0150: Null check can be clarified
  - Use `myvar is not null` instead of `myvar is object`
  - C# 9.0 should be used. Would this allow us for .NET 4.6.2 to use the new style?
- CA1510: Use 'ArgumentNullException.ThrowIfNull' instead of explicitly throwing
  a new exception instance
- CA1416 (Environment, CrashReporter): This callsite is reachable on all
  platforms. 'Registry.xxx' is only supported on: 'Windows'.
  - How to suppress this? I know the code is OK. But ignoring the error is
    probably not the best thing to do.
  - MS suggests to use `!OperatingSystem.IsWindows()` or similar. See
    [CA1416(]https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1416).
- CA1837: Use 'Environment.ProcessId' instead of
  'Process.GetCurrentProcess().Id'
- CS0649 (CommandLine): Field 'OptionShortUnder.Under' is never assigned to, and
  will always have its default value false (for all frameworks)
- SYSLIB0003 (CommandLine): 'SecurityPermissionAttribute' is obsolete: 'Code
  Access Security is not supported or honored by the runtime.'
  - Code was removed and is consistent with all other exception programming.
- SYSLIB0004 (Path): 'ReliabilityContractAttribute' is obsolete. Should guard
  these.
  - Should check for all other instances of SafeHandle, as this is likely
    incorrectly implemented for .NET Framework in other places (even if not a
    problem for .NET Core)
- SYSLIB0012 (CrashReporter): 'Assembly.CodeBase' is obsolete:
  'Assembly.CodeBase and Assembly.EscapedCodeBase are only included for .NET
  Framework compatibility. Use Assembly.Location.'

## 5. C# Language Upgrade

All projects were updated for the language C# 10. This allows for many style
cleanups (not `Span<T>` unfortunately), and many of the IDE findings could be
removed. The new library `RJCP.Core.SysCompat.csproj` was created to fill in
some missing gaps (even if diagnostics not run, it helped in having to avoid
using a lot of messy conditional defines in code). Some new features could be
enabled, even for .NET 4.0 that didn't exist before.

See the library notes for more information.
