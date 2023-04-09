# Build Tasks for RJCP Framework Components <!-- omit in toc -->

These Build Tasks are intended to support building the RJCP Framework Library.

- [1. Functionality Provided by this Package](#1-functionality-provided-by-this-package)
  - [1.1. Tested Environments](#11-tested-environments)
- [2. Using this Library via NuGet](#2-using-this-library-via-nuget)
- [3. Functionality Provided](#3-functionality-provided)
- [4. Reference Documentation](#4-reference-documentation)
- [5. Design Documentation](#5-design-documentation)

## 1. Functionality Provided by this Package

This package has only one extra task for usage at this time, to automate signing
of executables on Windows.

### 1.1. Tested Environments

This library has been tested to be consumed with Visual Studio 2019 installed,
imported into the newer .NET SDK Project format.

## 2. Using this Library via NuGet

The simplest way to consume this project is to include the NuGet package in your
own project. This imports the target file and will automatically sign your
binary on build or publish using the .NET SDK package format.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp2.1</TargetFramework>
    <X509SigningCert>signcert.crt</X509SigningCert>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="RJCP.MSBuildTasks" Version="0.2.3" />
  </ItemGroup>

</Project>
```

## 3. Functionality Provided

- [Signing (Windows only)](docs/signing.md)
- [Revision Control](docs/revision.md)

## 4. Reference Documentation

- [Signing](docs/reference/signing.md)
- [Revision Control](docs/revision.md)

## 5. Design Documentation

- [Signing](docs/design/signing.md)
- [Revision Control](docs/design/revision.md)
