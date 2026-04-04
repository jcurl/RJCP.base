# SHFB Documentation <!-- omit in toc -->

## 1. Building

### 1.1. Preconditions

The following tools must be installed prior:

- You must have Visual Studio 2022, or 2026 installed (for the MSBuild system).
  If you can build the project binaries, then you should have the build tools
  properly installed.
- Install SHFB 2026.3.29. Other versions may not work. Earlier versions will not
  work. There was a refactoring of the plugin libraries (used to rework the MAML
  output).

### 1.2. Quick Build

The easiest way to build the documentation, is from the project `rjcp.base` and
build from there.

```sh
git rj build -c preview --build --doc
```

This relies on the tool [README](../README.md) in the section "Using RJ BUILD".

### 1.3. 1.3 Manual Build

The manual build steps require more effort and is automated through the `git rj`
tool that is written.

1. Build the project

   ```sh
   dotnet build -c Release RJCP.Framework.sln /p:CheckEolTargetFramework=false
   ```

2. Build the plugins

   ```sh
   dotnet build -c Release RJCP.Sandcastle.Plugin.sln
   ```

3. Build the documentation.

   ```sh
   "C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\msbuild.exe" RJCP.Documentation.sln /t:Rebuild /verbosity:minimal /p:Configuration=Release /nologo
   ```

## 2. Project Files

There are a number of project files:

- `RJCP.Documentation.shfbproj` - the main project file. Must list all the
  components. The lead framework is .NET 10.0 (8.0, 6.0). All the components
  target these frameworks.
- `RJCP.Documentation.net48.shfbproj` - list of all projects targetting .NET
  Framework 4.8.
- `RJCP.Documentation.net462.shfbproj` - list of all projects targetting .NET
  Framework 4.6.2.
- `RJCP.Documentation.net40.shfbproj` - list of all projects targetting .NET
  Framework 4.0.

Naturally, projects that build for .NET 4.0 will work on all up to .NET 4.8.1,
and so on for the other projects. Project documentation will show that the API
is valid for the target framework that it is built for.

There are a number of override files, that are needed for SHFB 2026.3.29
(required with introduction of
[v2025.12.18.0](https://ewsoftware.github.io/SHFB/html/v2025.12.18.0.htm)).
These files support building different release types from the top directory with
`git rj build -c <release>`. This in turn loads `.gitrjbuild` to load in one of
the following files when building a project:

- `override.dev.proj` - Specifies the output path `distribute\dev`.
- `override.devsn.proj` - Specifies the output path `distribute\devsn`.
- `override.preview.proj` - Specifies the output path `distribute\preview`.
- `override.release.proj` - Specifies the output path `distribute\release`.

## 3. Plugin

A SandCastle 2026.3.29 plugin is written that fixes the table of contents for
MSHA/MSHC outputs. When building documentation, the plugin must first be built.
Generally on release, a copy of the Documentation build is provided on the
`RJCP.base` repository.

Recommended is to use the `git rj` tool part of this repository:

```sh
$ git rj build --doc
```

Manual builds require the plugin for Sandcastle (built automatically above):

```sh
$ dotnet build -c Debug RJCP.Sandcastle.Plugin.sln
Restore complete (1.1s)
  RJCP.Sandcastle.Plugin.HelpId succeeded (2.3s) → RJCP.Sandcastle.Plugin\HelpId\bin\Debug\net48\RJCP.Sandcastle.Plugin.HelpId.dll

$ dotnet build -c Release RJCP.Sandcastle.Plugin.sln
Restore complete (1.1s)
  RJCP.Sandcastle.Plugin.HelpId succeeded (0.7s) → RJCP.Sandcastle.Plugin\HelpId\bin\Release\netstandard2.0\RJCP.Sandcastle.Plugin.HelpId.dll
```
