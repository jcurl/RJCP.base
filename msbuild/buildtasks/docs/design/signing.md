# Design Documentation - Signing <!-- omit in toc -->

- [1. Microsoft Build SDK](#1-microsoft-build-sdk)
  - [1.1. Results from 'dotnet build' on Windows](#11-results-from-dotnet-build-on-windows)

## 1. Microsoft Build SDK

This section covers important information used to write the task for signing a
file. Important is to determine which files should be signed.

### 1.1. Results from 'dotnet build' on Windows

The `.target` file should figure out how to automatically sign a target project
when included. If the user doesn't want this, then they should not use the
target, and provide it themselves (it appears relatively simple to add your own
target to sign using the task).

For the test, the following target was created:

```xml
  <Target Name="X509SignAuthenticodeObj" AfterTargets="Compile"
          Condition="'$(OS)' == 'Windows_NT' and '$(X509SigningCert)' != ''">
      <Message Text="                TargetExt = $(TargetExt)" />
      <Message Text="                       OS = $(OS)" />
      <Message Text="               OutputType = $(OutputType)" />
      <Message Text="TargetFrameworkIdentifier = $(TargetFrameworkIdentifier)" />
      <Message Text="   TargetFrameworkVersion = $(TargetFrameworkVersion)" />
      <Message Text="               UseAppHost = $(UseAppHost)" />
  </Target>
```

The `$(OS)` variable has the value `Windows_NT`.

| .NET Framework Moniker | OutputType | TargetExt | TargetFrameworkIdentifier | TargetFrameworkVersion | DLL | EXE | UseAppHost |
|------------------------|------------|-----------|---------------------------|------------------------|-----|-----|------------|
| net50                  | Library    | .dll      | .NETCoreApp               | v5.0                   |  X  |     | ''         |
|                        | Exe        | .dll      |                           |                        |  X  |  X  | true       |
|                        | WinExe     | .dll      |                           |                        |  X  |  X  | true       |
| netcoreapp3.1          | Library    | .dll      | .NETCoreApp               | v3.1                   |  X  |     | ''         |
|                        | Exe        | .dll      |                           |                        |  X  |  X  | true       |
| netcoreapp3.0          | Library    | .dll      | .NETCoreApp               | v3.0                   |  X  |     | ''         |
|                        | Exe        | .dll      |                           |                        |  X  |  X  | true       |
| netcoreapp2.2          | Library    | .dll      | .NETCoreApp               | v2.2                   |  X  |     | ''         |
|                        | Exe        | .dll      |                           |                        |  X  |     | false      |
| netcoreapp2.1          | Library    | .dll      | .NETCoreApp               | v2.1                   |  X  |     | ''         |
|                        | Exe        | .dll      |                           |                        |  X  |     | false      |
| netcoreapp2.0          | Library    | .dll      | .NETCoreApp               | v2.0                   |  X  |     | ''         |
|                        | Exe        | .dll      |                           |                        |  X  |     | false      |
| netstandard2.1         | Library    | .dll      | .NETStandard              | v2.1                   |  X  |     | ''         |
|                        | Exe        | .dll      |                           |                        |  X  |     | false      |
| netstandard2.0         | Library    | .dll      | .NETStandard              | v2.0                   |  X  |     | ''         |
|                        | Exe        | .dll      |                           |                        |  X  |     | false      |
| netstandard1.9 (1)     | Library    | .dll      | .NETStandard              | v1.9                   |  X  |     | ''         |
|                        | Exe        | .dll      |                           |                        |  X  |     | false      |
| netstandard1.3         | Library    | .dll      | .NETStandard              | v1.3                   |  X  |     | ''         |
|                        | Exe        | .dll      |                           |                        |  X  |     | false      |
| net48                  | Library    | .dll      | .NETFramework             | v4.8                   |  X  |     | ''         |
|                        | Exe        | .exe      |                           |                        |     |  X  | ''         |
|                        | WinExe     | .exe      |                           |                        |     |  X  | ''         |
| net45                  | Library    | .dll      | .NETFramework             | v4.5                   |  X  |     | ''         |
|                        | Exe        | .exe      |                           |                        |     |  X  | ''         |
| net40                  | Library    | .dll      | .NETFramework             | v4.0                   |  X  |     | ''         |
|                        | Exe        | .exe      |                           |                        |     |  X  | ''         |

* (1) - Even though .NETStandard1.9 isn't provided by Microsoft, we see that the
  framework version is defined and that software compiles. This says we should
  check ranges used in the .NET Framework Version, and not specific comparisons.

When checking the output of the build, we see that `_CreateAppHost` looks for
and copies the file `apphost.exe` which appears to be the executable for .NET
Core programs. The `Compile` target depends on `_CreateAppHost`. So we can sign
the binaries after `Compile`.
