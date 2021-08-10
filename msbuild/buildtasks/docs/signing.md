# Signing Executables

Signing executables only works for Windows, as it requires the native
`signtool.exe` available on Windows. This task will not execute on Linux.

Add to your project the section:

```xml
  <PropertyGroup Condition=" '$(Configuration)' == 'Release'">
    <X509SigningCert>signcert.crt</X509SigningCert>
    <X509TimeStampUri>http://timestamp.digicert.com</X509TimeStampUri>
  </PropertyGroup>
```

In this case, the signing certificate should be in the same location as your
`.csproj` file, and it's called `signcert.crt`. It will be signed only for
releases.

## Preconditions before building

Signing only works on Windows. Ensure that the `signtool.exe` is in your path.
The signing certificate with the private key should already be in your
certificate store. From the certificate store it's easy to export only the
public portion to your project.

## Overriding the Certificate

For a Continuous Server, or a final build system, you might need to use a
different certificate. In this case you would have the certificate installed in
the certificate store that the private key can't be exported, and then override
the certificate hash with:

```cmd
dotnet build -c Release /p:X509SigningCert=C:\MyCert.crt
```
