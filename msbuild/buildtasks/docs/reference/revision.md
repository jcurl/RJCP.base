# Advanced Usage Documentation <!-- omit in toc -->

This documentation covers how to use the Tasks defined in the
`RJCP.MSBuildTasks.dll` file directly.

This information may change with newer versions of this library. If you use this
and something breaks, refer to this documentation to identify necessary updates
required to your project.

- [1. Using this assembly within MSBuild](#1-using-this-assembly-within-msbuild)
- [2. Version Processing](#2-version-processing)
- [3. Revision Control](#3-revision-control)
  - [3.1. Getting Revision Control Information](#31-getting-revision-control-information)
  - [3.2. Strict Control](#32-strict-control)

## 1. Using this assembly within MSBuild

There are two different tasks suitable for this workload

```xml
<UsingTask TaskName="RJCP.MSBuildTasks.RevisionControl"
           AssemblyFile="..\buildtasks\bin\Debug\netstandard2.1\RJCP.MSBuildTasks.dll" />
<UsingTask TaskName="RJCP.MSBuildTasks.SemVer"
           AssemblyFile="..\buildtasks\bin\Debug\netstandard2.1\RJCP.MSBuildTasks.dll" />
```

## 2. Version Processing

Revision control handles version processing most of the time. The `SemVer` task
can take a version and break it up into its constituents.

```xml
  <Target Name="MyTask" >
    <SemVer Version="$(Version)">
      <Output TaskParameter="VersionBase" PropertyName="VersionBase" />
      <Output TaskParameter="VersionSuffix" PropertyName="VersionSuffix" />
      <Output TaskParameter="VersionMeta" PropertyName="VersionMeta" />
    </SemVer>
  </Target>
```

This takes a version `x.y.z.b-suffix+meta` and breaks it up into three parts:

- `VersionBase` becomes `x.y.z.b`, or `x.y.z` if b == 0;
- `VersionSuffix` becomes `suffix` without the hyphen;
- `VersionMeta` becomes `meta` without the plus.

## 3. Revision Control

### 3.1. Getting Revision Control Information

The task on obtaining information about revision control is meant to only obtain
information relevant for the revision control system. Processing that data
should be done by the target so it is easily customizable.

```xml
    <RevisionControl Type="$(RevisionControl)" Path="$(MSBuildProjectDirectory)"
                     Label="$(RevisionControlLabel)" Strict="$(RevisionControlStrict)">
      <Output TaskParameter="RevisionControlType" PropertyName="RevisionControlType" />
      <Output TaskParameter="RevisionControlBranch" PropertyName="RevisionControlBranch" />
      <Output TaskParameter="RevisionControlCommit" PropertyName="RevisionControlCommit" />
      <Output TaskParameter="RevisionControlCommitShort" PropertyName="RevisionControlCommitShort" />
      <Output TaskParameter="RevisionControlDateTime" PropertyName="RevisionControlDateTime" />
      <Output TaskParameter="RevisionControlDirty" PropertyName="RevisionControlDirty" />
      <Output TaskParameter="RevisionControlTagged" PropertyName="RevisionControlTagged" />
      <Output TaskParameter="RevisionControlHost" PropertyName="RevisionControlHost" />
      <Output TaskParameter="RevisionControlUser" PropertyName="RevisionControlUser" />
    </RevisionControl>
```

Inputs to the task are:

- `Type` - The type of revision control. Supported at this time is `git`.
- `Path` - The directory path which to get information for. The example above is
  the directory where the project file is. Files outside of this project won't
  affect the results of revision control.
- `Label` - If not empty, specifies that a check for this label should be made
- `Strict` - If `yes`, `true`, `enabled`, then a warning is raised if the
  revision control is dirty or the label doesn't match. This can be used to warn
  (or stop compilation) if the filesystem is dirty. Useful for release builds
  and extra sanity checks.

Outputs are details of the revision control. The meaning of these parameters are
already defined in [Functional Description](../revision.md).

### 3.2. Strict Control

If building in `strict` mode, then this can be used to determine if the contents
of the repository has changed since it was labelled. This allows minor changes
outside of the directory of the sources with new commits, and if the git diff
between the label and the current version haven't changed, the build is
considered to be unmodified.

A second mode exists, which is used to override builds. If you have multiple
projects in the same repository, the strict mode will check that there are no
changes to each repository. If there are changes however, and the user has
manually compared the changes to ensure that there is no impact to the sources
built, then an *override* can be created, that ignores the fact that there is a
difference.

You should not ignore differences when:
- Code really has changed; or
- Dependencies might have changed.

This is used in the RJCP project to handle the generation of NuGet packages. If
there are minor changes to the build tools, or to editor files, then this should
not require a new release of a NuGet package.

To create an override file:

From the root directory (where the .gitrjbuild file is that describes the config
for the `git rj` commands), create a new file called `.rjbuild`. This is a
simple INI file that has the contents

```ini
[git-overrides]
<hash> = label1,label2
```

Each key/value pair must be unique. The hash is the current HEAD of the
repository being built. Get this by looking at the HEAD for the repository. It
is the GIT SHA-1 hash.

The values are the possible labels that can be overridden. If the current HEAD
contains an entry matching the label being built, then differences are ignored
and not reported as a warning. This can allow builds (with warn as error) to
continue, because you, the user, have already confirmed that there are no code
changes and everything is still compatible.

As soon as there is a new commit on the repository, then the hash of HEAD
changes, and the override is effectively removed, making the override very
specific for the current commit.
