# Buid Tasks for RJCP Framework Components <!-- omit in toc -->

These Build Tasks are intended to support building the RJCP Framework Library.

- [Buid Tasks for RJCP Framework Components](#buid-tasks-for-rjcp-framework-components)
  - [1. Using this assembly within MSBuild](#1-using-this-assembly-within-msbuild)
  - [2. Certificates](#2-certificates)
    - [2.1. New Task: X509ThumbPrint](#21-new-task-x509thumbprint)
    - [2.2. New Task: X509SignAuthenticode](#22-new-task-x509signauthenticode)

## 1. Using this assembly within MSBuild

Your MSBuild should reference the assembly providing these tasks.

```xml
<UsingTask TaskName="RJCP.MSBuildTasks.X509SignAuthenticode"
           AssemblyFile="..\buildtasks\bin\Debug\netstandard2.1\RJCP.MSBuildTasks.dll" />
```

## 2. Certificates

### 2.1. New Task: X509ThumbPrint

The `X509ThumbPrint` extracts a SHA1 thumb print from an existing public
certificate. The extracted thumb print can then be used by other tools, such as
the Sign task by Microsoft, which in the background uses the `signtool.exe` tool
to sign binaries.

This is intended to make it easier to exchange signing certificates, by
providing the X509 public certificate that should be used (of course, the
certificate with the private key should be available for `signtool.exe` to sign
against).

```xml
  <UsingTask TaskName="RJCP.MSBuildTasks.X509ThumbPrint"
             AssemblyFile="..\buildtasks\bin\Debug\netstandard2.1\RJCP.MSBuildTasks.dll" />

  <Target Name="X509ThumbPrint">
    <PropertyGroup>
      <CodeSignCert>02EAAE_CodeSign.crt</CodeSignCert>
    </PropertyGroup>

    <X509ThumbPrint CertPath="$(CodeSignCert)">
      <Output TaskParameter="ThumbPrint" PropertyName="X509ThumbPrint"/>
    </X509ThumbPrint>

    <Message Text="Certificate Hash = $(X509ThumbPrint)" />
  </Target>
```

### 2.2. New Task: X509SignAuthenticode

The `X509SignAuthenticode` extends the existing Microsoft `SignFile` task. It
completely reimplements the task to:

* Search for `signtool.exe` in the current path (when using on .NET Core
  projects, it would only use the `signtool.exe` in the current path), where it
  would be much more convenient if it would use the path, where the Windows SDK
  is installed.
* Instead of providing a SHA1 hash, provide the file name of the public
  certificate to get the SHA1 hash.

The principle is it is easier for a user to remember and confirm the certificate
that should be used for signing by looking at the certificate file, than trying
to identify from the hash what certificate is needed (especially if it is not
installed in the certificate store).

The task is a wrapper around the `signtool.exe` tool. The task itself does not
sign using a PFX file. The code signing certificate must already be installed in
the Windows certificate store. The public certificate on the file system should
be the same one from the PFX file. Typically after installing the PFX file in
the certificate store, you can export the certificate again without the private
key.

```xml
  <UsingTask TaskName="RJCP.MSBuildTasks.X509SignAuthenticode"
             AssemblyFile="..\buildtasks\bin\Debug\netstandard2.1\RJCP.MSBuildTasks.dll" />

  <Target Name="X509SignAuthenticode">
    <PropertyGroup>
      <CodeSignCert>02EAAE_CodeSign.crt</CodeSignCert>
    </PropertyGroup>

    <X509SignAuthenticode CertPath="$(CodeSignCert)"
                          TimeStampUri="http://timestamp.digicert.com"
                          InputAssembly="$(TargetDir)$(TargetFileName)" />
  </Target>
```

The parameters supported are:

* `CertPath` - Provide the path to where the public portion of the code signing
  certificate is stored. This file is only used to obtain the SHA1 hash when
  executing `signtool.exe`, which it obtains from the Certificate Store. This
  file must not have a password, nor a private key.
* `InputAssembly` - The name of the file that should be signed with
  `signtool.exe`.
* `TimeStampUri` (Optional) - A URI which is given to `signtool.exe` to use for
  signing with a secure timestamp server. Examples might be:
  * http://timestamp.digicert.com
  * http://timestamp.verisign.com/scripts/timstamp.dll
* `CertificateStoreName` (Optional) - The name of the certificate store where
  `signtool.exe` should look for the certificate. The default is `My`.
* `CertificateStoreLocation` (Options) - The location of the store. Possible
  values are:
  * `CurrentUser` (the default)
  * `LocalMachine`

It then takes the arguments, and executes the command `signtool.exe`. The
command executed for the example above would be:

```cmd
signtool.exe sign /fd sha256 /sha1 <thumbprint> /tr http://timestamp.digicert.com/
```
