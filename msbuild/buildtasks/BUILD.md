# Building and Testing the RJCP.MSBuildTasks  <!-- omit in toc -->

- [1. Building](#1-building)
- [2. Packaging](#2-packaging)
- [3. Testing](#3-testing)
  - [3.1. Unit Tests](#31-unit-tests)
  - [3.2. Integration Tests](#32-integration-tests)
- [4. Debugging in an Integrated Environment](#4-debugging-in-an-integrated-environment)
  - [4.1. Automation of Build Sequence Steps](#41-automation-of-build-sequence-steps)
  - [4.2. Logging](#42-logging)
- [5. Signing Certificates](#5-signing-certificates)

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

   > C:\...\rjcp.base\msbuild\buildtasks\buildtasks\RJCP.MSBuildTasks.csproj(86,5): error : SignTool failed with exit code 1.
   > C:\...\rjcp.base\msbuild\buildtasks\buildtasks\RJCP.MSBuildTasks.csproj(86,5): error : STDERR: SignTool Error: No certificates were found that met all the given criteria.

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

1. First build the `RJCP.MSBuildTasks.0.2.3.nupkg` library and put in a local
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

## 4. Debugging in an Integrated Environment

In case of sporadic errors in your build environment, we might need to debug
through the case of interactive printing.

The simplest way is to build this project with the debug information in case of
an error. Copy the output assembly to the place of the NuGet package repository
on your system. NuGet won't overwrite your test binary. It's more onerous but
might be easier to reproduce than trying through unit tests. Once the problem is
understood, then additional unit tests for this package can be added.

### 4.1. Automation of Build Sequence Steps

The following steps have been implemented in the script `debug.sh` for Linux.
The sequence of steps is the same for Windows, but would need different commands
(or the GUI to copy files).

1. Build the debug version, a precondition from building this release version.
   This step is only needed once just to enable the release version.

   Ensure that you've added at least new files to git, else they'll be removed!

   ```sh
   export NUGET_PACKAGE=$HOME/.nuget/packages/rjcp.msbuildtasks/0.2.3

   git clean -xfd
   git rj clean
   pkill -f dotnet
   rm -rf ${NUGET_PACKAGE}
   cd msbuild/buildtasks/buildtasks
   dotnet build
   ```

2. Build the release version. The `RJCP.MSBuildTasks.csproj` specifically
   references the just build debug version.

   We stop any instance of `dotnet` running in case assemblies are cached. Any
   previous debug version of `RJCP.MSBuildTasks.dll` is removed from
   `${NUGET_PACKAGE}` to make sure a clean build on the dependencies against the
   official (but still buggy) version.

   Then the file is copied to the location of the package cache, that a full
   build can use.

   ```sh
   pkill -f dotnet
   rm -rf ${NUGET_PACKAGE}
   dotnet build -c Release
   pkill -f dotnet
   cp ./buildtasks/bin/Release/netstandard2.1/RJCP.MSBuildTasks.dll ${NUGET_PACKAGE}/tools/netstandard2.1
   ```

3. From your own project, build it and try to reproduce the issue with the
   instrumented version.

   ```sh
   git rj build --build
   ```

   or if the problem must be reproduced, a long command line for endurance
   testing can be created:

   ```sh
   i=1; while (true); do echo "===== Run $i"; git rj build --build || break; ((i++)); done
   ```

   This was used to do endurance testing for a sporadic issue with process
   spawning and results.

On Windows the following commands are useful:

- Home Directory
  - Linux: `$HOME`
  - Windows `%USERPROFILE%`
- Killing all processes
  - Linux: `pkill -f dotnet`
  - Windows: `taskkill /f /im dotnet.exe`

### 4.2. Logging

If more intense logging is needed, it's recommended to log to a file. While not
checked in, this is a simple logging class that can be used. It only writes to
the file on `Flush`, which should be done at the end of the MSBuild Task that is
being tested.

```csharp
// #define CONSOLE
namespace RJCP.MSBuildTasks.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class FileLog
    {
        private readonly List<DateTime> m_Time = new List<DateTime>();
        private readonly List<string> m_Log = new List<string>();

        private static readonly object s_Lock = new object();
        private static FileLog s_Instance;

        public static FileLog Instance
        {
            get
            {
                if (s_Instance == null) {
                    lock (s_Lock) {
                        if (s_Instance == null) {
                            s_Instance = new FileLog();
                        }
                    }
                }
                return s_Instance;
            }
        }

        private readonly object m_LogLock = new object();

        public void Log(string message)
        {
#if CONSOLE
            DateTime now = DateTime.Now;
            Console.WriteLine($"{now:HH':'mm':'ss'.'fff}: {message}");
#else
            lock (m_LogLock) {
                m_Time.Add(DateTime.Now);
                m_Log.Add(message);
            }
#endif
        }

        public void Flush()
        {
#if !CONSOLE
            lock (m_LogLock) {
                using (FileStream fs = new FileStream("/tmp/msbuildtasks.txt", FileMode.Append, FileAccess.Write, FileShare.Read))
                using (StreamWriter sw = new StreamWriter(fs)) {
                    for (int i = 0; i < m_Log.Count; i++) {
                        sw.WriteLine($"{m_Time[i]:HH':'mm':'ss'.'fff}: {m_Log[i]}");
                    }
                    m_Time.Clear();
                    m_Log.Clear();
                    sw.Flush();
                    fs.Flush();
                }
            }
#endif
        }
    }
}
```

## 5. Signing Certificates

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
