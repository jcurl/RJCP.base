# Revision Control <!-- omit in toc -->

The task for revision control can query the current build folder for information
about the revision control system. This information can then be used by the .NET
SDK project file to modify or add metadata based on this information.

- [1. Usage](#1-usage)
- [2. Preconditions](#2-preconditions)
- [3. Generating a Version](#3-generating-a-version)
- [4. Customizing the Version](#4-customizing-the-version)
  - [4.1. Performing Actions Immediately Before the Revision Control Check](#41-performing-actions-immediately-before-the-revision-control-check)
  - [4.2. Performing Actions Immediately After the Revision Control Check](#42-performing-actions-immediately-after-the-revision-control-check)
    - [4.2.1. NuSpec Properties](#421-nuspec-properties)
- [5. Known Problems](#5-known-problems)
  - [5.1. Caching](#51-caching)

## 1. Usage

You will need to have a command line version of `git` installed. On Windows,
this can be done by GIT for Windows, and on Linux it should be in the system
path. The `git` tool is queried for relevant information as a new process, and
the output of that process is queried for information.

To enable the functionality after including the tasks into your project:

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
  <RevisionControl>git</RevisionControl>
  <RevisionControlLabel>release/v$(Version)</RevisionControlLabel>
</PropertyGroup>

<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <RevisionControlStrict>true</RevisionControlStrict>
</PropertyGroup>
```

The `Version` is set up by your own project. The `RevisionControl` tells the
system that this project is being managed in a `git` repository, and so `git`
tools are required. If this property is not set, the functionality provided by
this task is disabled. That means projects by default will not query or use the
revision control system.

The `RevisionControlLabel` can help to report if the current directory where the
project file is has been modified since the label.

The default behaviour of the task is to then modify the `Version` property so
that:

* To warn the user when building in strict mode, if the software is dirty
  (unchecked in changes), or if the current commit is different to the current
  label.
* Extend the version suffix as required automatically for incremental builds.
* Provide a `SourceRevisionInformationVersion` based on the current commit,
  which shouldn't affect the SemVer versioning scheme
* Update the version of the package if `IsPackable`.

## 2. Preconditions

Make sure you have `git` installed on your system and it is in your path when
you build. The `git` tool is searched for in the following locations:

* The system `%PATH%` variable
* `%LOCALAPPDATA%\Programs\Git\bin\git.exe`
* `%ProgramFiles%\Git\bin\git.exe`
* `%ProgramFiles(X86)%\bin\git.exe`

## 3. Generating a Version

When the property `RevisionControl` is set, the functionality is enabled. It
will check for details about the directory where the current project file is
kept.

The `Version` property is modified unless all of the following are true:

* The `Configuration` is `Release`
* The repository is labelled and matches precisely `RevisionControlLabel`, and
  the current commit is identical for the directory and below for the current
  project (items above the directory are ignored, allowing multiple projects in
  a single mono-repository).
* There are no changes pending for commit (staged) and no changes to files being
  tracked by revision control.

If any of the above are not met, the `Version` is modified.

* If there is no version suffix, the new version becomes
  `$(Version)-alpha.$(RevisionControlDateTime)`.
* If there is a version suffix, the new version becomes
  `$(VersionPrefix)-$(VersionSuffix).$(RevisionControlDateTime)`.

For specific examples:

* Let's say the version is `1.0.0`. The build is run in the `Debug`
  configuration. The new version will be `1.0.0-alpha.20210903T191723`.
* Let's say the version is `1.0.0-Preview`. The build is run in the `Debug`
  configuration. The new version will be `1.0.0-Preview.20210903T191723`.
* Let's say the version is `1.0.0`, the current commit is labelled with
  `release/v1.0.0`, no files are modified. Building with `Release` will not
  modify the version and it remains at `1.0.0`.
* Let's say the version is `1.0.0-Preview`, the current commit is labelled with
  `release/v1.0.0-Preview`, no files are modified. Building with `Release` will
  not modify the version and it will remain at `1.0.0-Preview`.

The property `SourceRevisionId` if not already set, is assigned the short commit
from git.

## 4. Customizing the Version

The rules for renaming the version might not work for every project. To execute
your own task after version control information is obtained, refer to the next
sections on examples for customization.

When NuGet projects are consumed, the `RJCP.MSBuildTasks.props` file is normally
included first, with the `RJCP.MSBuidlTasks.target` file included last.

### 4.1. Performing Actions Immediately Before the Revision Control Check

```xml
<Target Name="BeforeRevisionControlCheck" BeforeTargets="CoreRevisionControlCheck">
  <Message Importance="High" Text="BeforeRevisionControlCheck" />
</Target>
```

### 4.2. Performing Actions Immediately After the Revision Control Check

There are a number of use cases where executing a task after revision control
information is obtained. Some examples are:

* Use custom rules (not the ones defined by this project) for rewriting the
  `Version` and `PackageVersion` property. Even though the `Version` property is
  modified, it can be reconstructed from the following properties:
  * `RevisionControlVersionBase`
  * `RevisionControlVersionSuffix`
  * `RevisionControlVersionMeta`
* Providing properties for a NuSpec file

```xml
<Target Name="AfterRevisionControlCheck" AfterTargets="CoreRevisionControlCheck">
  <Message Importance="High" Text="AfterRevisionControlCheck" />
  <Message Importance="High" Text=" Configuration                = $(Configuration)" />
  <Message Importance="High" Text=" TargetFramework              = $(TargetFramework)" />
  <Message Importance="High" Text=" RevisionControlType          = $(RevisionControlType)" />
  <Message Importance="High" Text=" RevisionControlBranch        = $(RevisionControlBranch)" />
  <Message Importance="High" Text=" RevisionControlCommit        = $(RevisionControlCommit)" />
  <Message Importance="High" Text=" RevisionControlCommitShort   = $(RevisionControlCommitShort)" />
  <Message Importance="High" Text=" RevisionControlDateTime      = $(RevisionControlDateTime)" />
  <Message Importance="High" Text=" RevisionControlDirty         = $(RevisionControlDirty)" />
  <Message Importance="High" Text=" RevisionControlTagged        = $(RevisionControlTagged)" />
  <Message Importance="High" Text=" RevisionControlHost          = $(RevisionControlHost)" />
  <Message Importance="High" Text=" RevisionControlUser          = $(RevisionControlUser)" />
  <Message Importance="High" Text=" RevisionControlVersionBase   = $(RevisionControlVersionBase)" />
  <Message Importance="High" Text=" RevisionControlVersionSuffix = $(RevisionControlVersionSuffix)" />
  <Message Importance="High" Text=" RevisionControlVersionMeta   = $(RevisionControlVersionMeta)" />
</Target>
```

A descripton of the properties set by `CoreRevisionControlCheck` are:

* `RevisionControlType`: The revision control system identified. Supported is
  `git`.
* `RevisionControlBranch`: Identifies the branch. For example, you could add
  logic in `AfterRevisionControlCheck` to ensure that only the `master` branch
  is allowed for release builds.
* `RevisionControlCommit`: The last commit (not the same as the current commit).
  The last commit ensures a constant value unless there is new history in the
  path where the project file is kept.
* `RevisionControlCommitShort`: A short version of the commit, useful for
  `SourceRevisionId`.
* `RevisionControlDateTime`: A ISO8601 Date/Time similar format that is based on
  the last commit. This is useful for preprelease builds to ensure a constant
  version that increases over time.
* `RevisionControlDirty`: Is `False` if there are no unstaged or staged commits,
  `True` otherwise. Files that are in the directory but not explicitly added to
  the revision control system are ignored - so if a file is forgotten to be
  added, it won't be detected as dirty.
* `RevisionControlTagged`: Is `True` if `RevisionControlLabel` is provided, and
  the current commit contents are identical to the tag for the path of the
  project.
* `RevisionControlHost`: A shortcut to the hostname of the computer.
* `RevisionControlUser`: A short-cut to the user name of the currently logged in
  user.

#### 4.2.1. NuSpec Properties

As the task `CoreRevisionControlCheck` modifies the `Version` property, it's
important that the property, like below, is set after the
`CoreRevisionControlCheck`.

```xml
<NuSpecProperties>id=$(AssemblyTitle);version=$(PackageVersion);authors=$(Authors);description=$(Description)</NuSpecProperties>
```

If this property is set at the top, in the project wide properties, it will be
an incorrect version.

## 5. Known Problems

### 5.1. Caching

The task in use caches the results of the GIT commands between invocations.
Normally MSBuild is started and it closes when builds are finished. If MSBuild
is started and remains persistent, it may be possible that updates to the
revision control system are not noticed. You should disable MSBuild being
resident in memory.

A common issue occurs when using Visual Studio Code, that the properties of the
version information remains constant between builds, even though new commits
have been made and committed. To over come this, close Visual Studio and redo
the build from the command line.
