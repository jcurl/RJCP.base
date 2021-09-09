# Multiple Frameworks

The NuGet package is designed that it can be run from a `dotnet` environment
(e.g. on Linux), as well as from within Visual Studio (the full .NET framework
v4.8).

## The NuGet Structure

The structure of the NuGet package is:

* build\
  * RJCP.MSBuildTasks.props
  * RJCP.MSBuildTasks.targets
* buildMultiTargeting\
  * RJCP.MSBuildTasks.props
  * RJCP.MSBuildTasks.targets
* lib\
  * net40
    * _._
  * netstandard1.0
    * _._
* tools\
  * RJCP.MSBuildTasks.props
  * RJCP.MSBuildTasks.targets
  * net48\
    * RJCP.MSBuildTasks.dll
  * netstandard2.1
    * RJCP.MSBuildTasks.dll

The `.props` and `.targets` file in the folders `build` and
`buildMultiTargeting` are just simple scripts that redirect to the same files in
the `tools` folder. The main logic is in the `tools` folder.

## The Target

### The Right Framework

On Visual Studio 2019, the .NET 4.8 build must be loaded, while using `dotnet`,
the .NET Core version should be used (as on Linux, it is unlikely that Mono
would be installed). This is part of the `tools\RJCP.MSBuildTasks.targets` file.

Trying to use the .NETStandard 2.1 only on Visual Studio would result in the following error:

> Error MSB4062 The "RJCP.MSBuildTasks.RevisionControl" task could not be loaded
> from the assembly
> C:\Users\jcurl\.nuget\packages\rjcp.msbuildtasks\0.2.0\build\RJCP.MSBuildTasks.dll.
> Could not load file or assembly 'netstandard, Version=2.1.0.0,
> Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51' or one of its dependencies.
> The system cannot find the file specified. Confirm that the <UsingTask>
> declaration is correct, that the assembly and all its dependencies are
> available, and that the task contains a public class that implements
> Microsoft.Build.Framework.ITask. RJCP.Core.Datastructures
> (RJCP.Framework\RJCP.Core.Datastructures\RJCP.Core.Datastructures)
> C:\Users\jcurl\.nuget\packages\rjcp.msbuildtasks\0.2.0\build\RJCP.MSBuildTasks.targets
> 47

Thus the following code snippet chooses the correct .NET build.

```xml
  <PropertyGroup Condition="'$(MSBuildRuntimeType)' == 'Full' Or '$(MSBuildRuntimeType)' == 'Mono'">
    <_RJCP_MSBuildTasksAssembly>$(MSBuildThisFileDirectory)net48/RJCP.MSBuildTasks.dll</_RJCP_MSBuildTasksAssembly>
  </PropertyGroup>

  <PropertyGroup>
    <_RJCP_MSBuildTasksAssembly Condition="'$(_RJCP_MSBuildTasksAssembly)' == ''">$(MSBuildThisFileDirectory)netstandard2.1/RJCP.MSBuildTasks.dll</_RJCP_MSBuildTasksAssembly>
  </PropertyGroup>
```

Then when the tasks are loaded later, it uses the correct file.

```xml
  <UsingTask TaskName="RJCP.MSBuildTasks.X509SignAuthenticode" AssemblyFile="$(_RJCP_MSBuildTasksAssembly)" />
  <UsingTask TaskName="RJCP.MSBuildTasks.RevisionControl" AssemblyFile="$(_RJCP_MSBuildTasksAssembly)" />
  <UsingTask TaskName="RJCP.MSBuildTasks.SemVer" AssemblyFile="$(_RJCP_MSBuildTasksAssembly)" />
  <UsingTask TaskName="RJCP.MSBuildTasks.RevisionControlClearCache" AssemblyFile="$(_RJCP_MSBuildTasksAssembly)" />
```
