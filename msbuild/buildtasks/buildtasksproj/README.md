# MSBuildTasks Test Application

The purpose of this project is to include the MSBuildTasks into the project and
test that it works interactively with MSBuild.

## Building and Testing

Building this example works with the `dotnet` tools. The
`RJCP.BuildTasksProj.csproj` file is configured to use the .NET Standard 2.1
(.NET Core) library, and so will not work properly within the Visual Studio IDE
(it requires the .NET 4.8 version). You can easily change the contents of the
project to use the other library. Please note, that the final project will
automatically choose the correct library based on the `MSBuildRuntimeType`.

First, build the `buildtasks` project separately, from the root directory.

```sh
dotnet build -c debug RJCP.MSBuildTasks.sln
```

This puts the resulting binary in
`buildtasks\bin\Debug\netstandard2.1\RJCP.MSBuidlTasks.dll`.

### Certificate Thumbprint

The test loads the certificate and obtains the thumbprint.

```sh
dotnet build /t:GetX509ThumbPrint -v=diag > build.txt
```

```text
21:38:30.673   1:7>Target "GetX509ThumbPrint: (TargetId:40)" in project "C:\Users\jcurl\Documents\Programming\rjcp.base\msbuild\buildtasks\buildtasksproj\RJCP.BuildTasksProj.csproj" (entry point):
                   Using "X509ThumbPrint" task from assembly "C:\Users\jcurl\Documents\Programming\rjcp.base\msbuild\buildtasks\buildtasksproj\..\buildtasks\bin\Debug\netstandard2.1\RJCP.MSBuildTasks.dll".
                   Task "X509ThumbPrint" (TaskId:34)
                     Task Parameter:CertPath=02EAAE_CodeSign.crt (TaskId:34)
                     Certificate file 02EAAE_CodeSign.crt to be used (TaskId:34)
                   Done executing task "X509ThumbPrint". (TaskId:34)
```
