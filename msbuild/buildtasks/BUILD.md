# Building and Testing the RJCP.MSBuildTasks  <!-- omit in toc -->

- [1. Building](#1-building)
- [2. Packaging](#2-packaging)
- [3. Testing](#3-testing)
  - [3.1. Unit Tests](#31-unit-tests)
  - [3.2. Integration Tests](#32-integration-tests)
- [4. Signing Certificates](#4-signing-certificates)

## 1. Building

These instructions show how to build the `RJCP.MSBuildTasks` project and how to
create a NuGet package that can then be used for inclusion in your own project.

1. Build the `Debug` package first. This builds the preconditions that are then
   required for building the `Release` package. In particular, the `Debug` build
   is then used to sign the `Release` build.

   ```cmd
   cd msbuild\buildtasks\buildtasks
   dotnet build
   ```

2. Build the `Release` package. The `RJCP.MSBuildTasks.csproj` contains
   instructions to sign the release package with the certificate that matches
   the thumbprint `signcert.crt` in the same folder. This is my public
   certificate which I use for the releases.

   Ensure that `signtool.exe` is in your path. If you're using PowerShell,
   adding it to the path might be a command such as (assuming you've installed
   the Windows Kit):

   You of course have `git` installed, because the repository we're building
   from is in git.

   ```cmd
   $Env:Path += ";C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64"
   ```

   Then build the release version:

   ```cmd
   dotnet build -c Release
   ```

   If you don't have the certificate with the private key installed in your
   certificate store, you'll likely end up with the error:

   > C:\Users\jcurl\Documents\Programming\rjcp.base\msbuild\buildtasks\buildtasks\RJCP.MSBuildTasks.csproj(86,5): error : SignTool failed with exit code 1.
   > C:\Users\jcurl\Documents\Programming\rjcp.base\msbuild\buildtasks\buildtasks\RJCP.MSBuildTasks.csproj(86,5): error : STDERR: SignTool Error: No certificates were found that met all the given criteria.

   To sign with your own certificate, overriding the one in this project, ensure
   that you've imported the private certificate (the certificate with the
   private key used for signing) into the certificate store. Save the public
   certificate in the `buildtasks` directory (e.g. it's called
   `mysigncert.crt`). Then run the build signing with your code signing
   certificate.

   ```cmd
   dotnet build -c Release /p:X509SigningCert=c:\full\path\to\mysigncert.crt
   ```

## 2. Packaging

Packaging is done with the `dotnet pack -c Release` command. Packaging the debug
version is disabled. Ensure you've build the debug version first, which is used
to sign the build task using authenticode with itself.

The details for the NuGet file are obtained from `RJCP.MSBuildTasks.nuspec`,
information used in the `RJCP.MSBuildTasks.csproj` file are ignored.

## 3. Testing

There are two types of tests - the unit tests and the integration tests.

### 3.1. Unit Tests

The unit tests depends on `RJCP.DLL.CodeQuality`, which uses the NuGet package
`RJCP.MSBuildTasks.nupkg` provided here. This is why you might not be able to
build and run the tests in a single step.

1. First build the `RJCP.MSBuildTasks.0.2.0.nupkg` library and put in a local
   feed. The instructions for doing this are provided if the NuGet package isn't
   already available.
2. To run the unit tests, which simulates the task and execution of the
   `signtool.exe`, execute the command from where the `RJCP.MSBuildTasks.sln`
   file is kept:

   ```cmd
   cd msbuild\buildtasks\buildtaskstest
   dotnet test
   ```

Please be sure to run the unit tests under Windows. Other environments, such as
MSYS2 might have similar commands, like `timeout.exe` that have different
behaviour under PowerShell for Windows.

### 3.2. Integration Tests

Integration tests are run as part of doing the release build. The integration
test is quite simply that the `RJCP.MSBuildTasks.csproj` file has instructions
to use tasks from the Debug build, and to execute the task while building
itself.

## 4. Signing Certificates

You'll need to get a certificate and import this into the Certificate store. My
signing certificate is from the volunteer certificate authority CACert. The root
certificates must be installed into your trusted root.

The public/private key pair is usually a file called `certificate.p12` or
`certificate.pfx`. This contains the public and the private key portion. Double
clicking on this will ask to install the certificate, and will ask you for the
password. It's recommended that the private key portion is not exportable.

From the Certificate Store (run `certmgr.msc` from the Windows command prompt),
find the certificate you imported under `Personal` and export the public key
portion to disk. This is the public key portion, that is safe to include in your
repositories (but do not ever submit the private key portion, the `.p12` or
`.pfx` in your repository).
