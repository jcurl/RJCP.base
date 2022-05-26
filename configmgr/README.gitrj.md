# GIT Helper <!-- omit in toc -->

The helper tool, `git-rj`, is to assist issuing similar commands to multiple
repositories (submodules) at once. It supports the following:

* `git rj version`: Get the version of the script, python and git
* `git rj help`: Get basic help information
* `git rj init`: Initialize the submodules, checkout the default branch and pull
* `git rj pull`: Pull to the HEAD for the current branch for all repositories
* `git rj fetch`: Fetch from all repositories
* `git rj status`: Get the status for all repositories
* `git rj clean`: Cleans all repositories
* `git rj cobr`: Check out a branch on all repositories
* `git rj shbr`: Show all branches on the repositories
* `git rj rmbr`: Remove branches from the repositories
* `git rj build`: Build from the root of the repository
* `git rj perf`: Run some microbenchmarking tools

Table of Contents

- [1. Introduction](#1-introduction)
  - [1.1. Requirements](#11-requirements)
  - [1.2. Configuring Submodules](#12-configuring-submodules)
  - [1.3. Tested Versions](#13-tested-versions)
  - [1.4. The Bootstrapper Script](#14-the-bootstrapper-script)
    - [1.4.1. Linking under Linux](#141-linking-under-linux)
    - [1.4.2. Linking under Windows](#142-linking-under-windows)
  - [1.5. Checking the Version](#15-checking-the-version)
  - [1.6. Getting Help](#16-getting-help)
- [2. General Usage for GIT Repository Management](#2-general-usage-for-git-repository-management)
  - [2.1. Initializing the Repository](#21-initializing-the-repository)
    - [2.1.1. Resetting the Repository to a Known State](#211-resetting-the-repository-to-a-known-state)
  - [2.2. Updating (Pulling) the Repositories to the Latest Commit](#22-updating-pulling-the-repositories-to-the-latest-commit)
  - [2.3. Fetching from all Repositories](#23-fetching-from-all-repositories)
  - [2.4. Cleaning from all Sub-modules](#24-cleaning-from-all-sub-modules)
  - [2.5. Get the Status](#25-get-the-status)
    - [2.5.1. Modified Repository](#251-modified-repository)
    - [2.5.2. Tracking Branches](#252-tracking-branches)
    - [2.5.3. Local Branch Push Required to Server](#253-local-branch-push-required-to-server)
    - [2.5.4. Rebase Required to Destination Branch](#254-rebase-required-to-destination-branch)
    - [2.5.5. Commits Ahead or Behind between the Local and Destination Branches](#255-commits-ahead-or-behind-between-the-local-and-destination-branches)
    - [2.5.6. A Word about Branches and Remotes](#256-a-word-about-branches-and-remotes)
    - [2.5.7. The Destination Branch Selection Method](#257-the-destination-branch-selection-method)
  - [2.6. Checking out a branch](#26-checking-out-a-branch)
    - [2.6.1. Use Case: Feature on the master branch](#261-use-case-feature-on-the-master-branch)
    - [2.6.2. Use Case: Feature Development on a Development Branch](#262-use-case-feature-development-on-a-development-branch)
    - [2.6.3. Managing a Hot-Fix Branch](#263-managing-a-hot-fix-branch)
  - [2.7. Showing all the Branches](#27-showing-all-the-branches)
  - [2.8. Removing Branches](#28-removing-branches)
    - [2.8.1. 2.8.1.Pruning](#281-281pruning)
    - [2.8.2. Removing Local and Remote Branches](#282-removing-local-and-remote-branches)
- [3. Automating Builds](#3-automating-builds)
  - [3.1. Building in Developer Mode](#31-building-in-developer-mode)
  - [3.2. Build in Release Mode](#32-build-in-release-mode)
  - [3.3. The .gitrjbuild Configuration File](#33-the-gitrjbuild-configuration-file)
    - [3.3.1. The "dev" and "release" section](#331-the-dev-and-release-section)
    - [3.3.2. Expansion Variables](#332-expansion-variables)
- [4. Performance Testing](#4-performance-testing)
  - [4.1. Running Performance Tests](#41-running-performance-tests)
  - [4.2. Defining a Performance Test](#42-defining-a-performance-test)
  - [4.3. Executing a Specific Performance Tests](#43-executing-a-specific-performance-tests)
  - [4.4. Printing the Last Results](#44-printing-the-last-results)

## 1. Introduction

The `git-rj` script simplifies common tasks for git repositories having many
submodules. It uses Python 3, which allows rapid prototyping while being
maintainable. Python has advanced over time and also provides very useful
features. Threading functionality is used in this script to execute git commands
in parallel to boost performance, especially on Windows.

It also serves as an entry point to building the software in the repository,
simplifying common commands. Its use for building is optional and is indicated
by the presence of the `.gitrjbuild` file.

Documentation is written for the version of the script as of version
1.0-alpha.20210731.1

### 1.1. Requirements

The script requires installation of Python 3.7.x or later. Git 2.13 or later
should be used.

From the git shell, add the script to your path, e.g.

```sh
export PATH=$PATH:`pwd`/configmgr
```

### 1.2. Configuring Submodules

When you add a submodule, a file `.gitmodules` is added to your repository. By
default, this file contains two entries, something similar to

```text
[submodule "framework/mymodule"]
    path = framework/mymodule
    url = ../mymodule.git
```

The `.gitmodules` file should be configured to specify the default branch for
each submodule.

```text
[submodule "framework/mymodule"]
    path = framework/mymodule
    url = ../mymodule.git
    branch = master
    ignore = untracked
```

### 1.3. Tested Versions

The script was developed on Windows with Python 3.9.0 and Git for Windows
2.13.2-windows.

The script works on Ubuntu 18.04, with Python 3.6.9 and Git 2.17.1

### 1.4. The Bootstrapper Script

The entry script `git-rj` is a bootstrapper script that allows compatibility
between working with Git Bash for Windows, Cmder for Windows and Linux. It has
the job of testing the python interpreter for version 3.x, and then starting the
script.

To put the script in the path, you can:

* Add the `configmgr` path from the repository into your path; or
* Create a link from your `bin` directory to the `configmgr/git-rj` shell
  script.

#### 1.4.1. Linking under Linux

Under Linux, one can link directly to the Python script, but the name must strip
the `.py` extension for it to be used as `git rj command`. This will avoid the
usage of the bootstrapper script for a slight improvement in performance.

```sh
ln -s `pwd`/configmgr/git-rj.py ~/bin/git-rj
```

#### 1.4.2. Linking under Windows

Under Windows, the bootstrapper script is required. The Python script is written
to run under Linux and Windows, requiring `python3` to work. When installing
Python 3 for Windows, only the binary `python.exe` is available. Unfortunately,
the script requests `python3` and so won't work. We can't change it to `python`,
as then it wouldn't work on Linux. This is why the bootstrapper script is
written, to find the correct python on both OSes in the supported environments.

The easiest, is to just add the location of the executable to the system path.

If desired, a *SYMLINK* under Windows can be made from `%USERPROFILE%\bin` to
the shell script using the `mklink` command (it must be run as administrator to
make a *SYMLINK*).

```text
dir
 Volume in drive C has no label.
 Volume Serial Number is 1EAB-A5B6

 Directory of %USERPROFILE%\bin

15/10/2020  18:37    <DIR>          .
15/10/2020  18:37    <DIR>          ..
15/10/2020  18:37    <SYMLINK>      git-rj [C:\Users\jcurl\Documents\Programming\NETFW\rjcp.base\configmgr\git-rj]
               4 File(s)        106,657 bytes
               2 Dir(s)  34,121,879,552 bytes free
```

The bootstrapper script looks if the `git-rj.py` exists in the local same
location as `git-rj` (e.g. `%USERPROFILE%\bin`). If it isn't found, the tool
`readlink` is used to see if it is a symbolic link, and it's searched for there.
Finally, if it isn't found, then it just runs `python git-rj.py`, but will
likely fail if it gets that far.

### 1.5. Checking the Version

You can check the versions by running:

```sh
git rj version
```

```text
git rj
  Version: 1.0-alpha.20210731.1
  Python: 3.9.0-final
  GIT: git version 2.13.2.windows.1
```

### 1.6. Getting Help

To get help in general, use the command

```sh
git rj help
```

To get help for a specific command, issue that command and use the option `-h`.

```sh
git rj status -h
```

## 2. General Usage for GIT Repository Management

All the subcommands support the option `-h` to get help information on that
subcommand and options that can be used.

For example:

```sh
git rj init -h
```

```text
usage: git rj init [-h] [-i] [-c] [-b] [-p] [-f]

Initialize the modules for usage. This command, when given with no options,
will initialize submodules (with git submodule updat --init), apply the top
level configuration on all repositories (taking your current user name and
email address), and check out to the default branch for each submodule. It
will then do a fast-forward only pull (with a force fetch update)

optional arguments:
  -h, --help      show this help message and exit
  -i, --init      Initialize the submodules. This will check out the exact
                  commit.
  -c, --config    Update the git configuration for the base module and all
                  submodules.
  -b, --checkout  Check out the default branch, as given in the .gitmodules
                  file.
  -p, --pull      Pull after the default branch is checked out, using fast-
                  forward only, and force updating the refs with the fetch.
  -f, --force     Applies the force option, which at this time causes a forced
                  checkout, overwriting changes when checking out the branch.
```

All commands must be generally run at the base (the project super tree). Not
doing so will result in the error:

```sh
git rj pull
```

```text
Error: 'git rj pull':
 Not at the top level repository.
```

### 2.1. Initializing the Repository

Usually, when checking out the base repository, it still needs to be configured,
and submodules need to be initialized. Run without any options:

```sh
git checkout ssh://bitbucket/myrepo.git
cd mjyrepo.git
git rj init
```

the command will:

* Check that the git configuration `user.name` and `user.email` are set. If not,
  the user will be warned and asked to set them.
* Initialize the submodules with the command `git submodule update --init`. This
  will check out the commit. Often this is in detached mode and no branch is
  selected at this time.
* Initialize the configuration to some sane defaults. This includes copying the
  user name and email address to the submodules.
* Switch to the default branch, as given in the `.gitmodules` file for that
  repository. If this isn't changed, it's usually `master`.
* Pull to the HEAD for the branch that is checked out.

There are options to select if only a subset of these operations should be
performed. Typically, the first time the command is executed, it should be run
without any options.

The options are:

* `--init`: Initialize the submodules
* `--config`: Apply configuration settings to the submodules
* `--checkout`: Check out the default branch, as given in the `.gitmodules`
  file.
* `--pull`: Pull all submodules.
* `--force`: Apply the `--force` command check checking out, and pulling. This
  can be used to help reset the state of the repository to a fresh check out.

#### 2.1.1. Resetting the Repository to a Known State

If all repositories are in a development state that can be discarded (e.g. the
changes required were already merged on a different machine), and the
repositories should be brought to the most recent state reflecting that on the
git server, then one doesn't need to reinitialize everything, just check out the
default branch and pull. For this, use the command

```sh
git rj init -bpf
```

This will check out the default branch (`-b`), pull (`-p`) and discard any local
changes (`-f`).

### 2.2. Updating (Pulling) the Repositories to the Latest Commit

As software is developed within a team, or across multiple computers, one needs
to get the latest version of the software for the current branch.

```sh
git rj pull
```

It will simply pull the base repository (without recursing into the submodules),
and then execute in parallel for all submodules also a `git pull`. The script
runs the update in parallel, one for each repository.

To discard all local changes, run:

```sh
git rj pull --force
```

### 2.3. Fetching from all Repositories

To fetch updates for all repositories, run the command:

```sh
git rj fetch [--force]
```

The option `--force` is passed to the `git fetch` command for each repository

### 2.4. Cleaning from all Sub-modules

To remove all build files, untracked directories and revert all changes for all
repositories (which is useful to run before doing a clean build), run the
command:

```sh
git rj clean
```

This doesn't clean the base module (so that changes to the base are not
affected). To also clean the base repository, run

```sh
git rj clean --all
```

### 2.5. Get the Status

With multiple repositories, one needs a compact form to check the status, if a
repository is modified, if there is a tracking branch, if the repository needs
to be pushed.

Get a quick overview of the current module and the immediate submodules with the
command

```sh
git rj status [--long]
```

It will query various details, and for each immediate submodule

```text
[MTPR] module/path   07ad378112a (commits: a / b) [branch -> remote]
```

The long version replies slightly differently

```text
[-T--] module/path                              07ad378112a100f7b38997ebcb1b37b4513caadd (commits: 0 / 0) [master -> origin/master]
```

A brief description of the information provided on each line:

* The first is a set of codes.
  * `M` or `-`: Indicates if the repository is dirty (modified).
  * `T` or `t` or `-`: Indicates if the local branch is being tracked. `T`
    indicates the branch is being tracked, and the remote exists locally. `t`
    indicates the branch is being tracked, but the remote can't be found
    locally. `-` indicates that the branch is not being tracked.
  * `P`: Indicates if the local branch has been modified with reference to the
    remote. This can only be obtained if the current branch is being tracked.
  * `R`: Indicates if the current branch must be rebased, because the default
    branch on the remote has advanced since this branch was created (there's a
    fork).
* Then the module path is shown. If the path is too long to fit, it is truncated
  so that the most right paths are shown as much as possible.
* A hash of the current commit. This is useful to copy the current configuration
  of the repositories.
* The number of commits with reference to the default remote branch is shown
  * `a` is the number of commits the current local branch is ahead of the
    default remote (e.g. how many commits ahead `feature/mytopic` is to
    `origin/master`).
  * `b` is the number of commits the current local branch is behind the default
    remote (e.g. how many commits the `origin/master` is ahead of the base of
    the current local branch`). If this value is non-zero, a rebase or a merge
    is required.
* Then the name of the local branches being referenced:
  * The first `branch` is the local branch name, be it `master` or
    `feature/mytopic`.
  * The second `remote` is the default branch, given in `.gitmodule` for the
    repository, mapped to the default remote (e.g. `origin/master`).

#### 2.5.1. Modified Repository

If the `M` flag is set, go to the module and run `git status`, to determine what
changes are present. The changes may be unstaged, or staged.

It's useful to know if any of the repositories are in a modified state before
switching branches, or building and testing the software.

#### 2.5.2. Tracking Branches

When a local branch is first created using `git checkout -b feature/mytopic`,
there is no tracking branch. This is shown with `-`.

```text
[----] framework/datastructures       178c1ef0f11 (commits: 1 / 0) [test -> origin/master]
```

When the branch is pushed, there might be a remote branch, but it must be set
explicitly to track. That is, the following command will push, but not set up
the tracking branch

```sh
git push origin test
```

Then getting the status still results in

```text
[----] framework/datastructures       178c1ef0f11 (commits: 1 / 0) [test -> origin/master]
```

To set the branch for tracking, one must use the command

```sh
git push --set-upstream origin test
```

Then the status shows that the branch is being tracked

```text
[-T--] framework/datastructures       178c1ef0f11 (commits: 1 / 0) [test -> origin/master]
```

Finally, if the branch is remotely merged and the tracking branch is removed, so
that when pulled the remotes are pruned, the result will be

```text
[-t--] framework/datastructures       178c1ef0f11 (commits: 1 / 0) [test -> origin/master]
```

This lets you know that the branch you're no longer has the remote present. You
must do a fetch first that will prune all branches that no longer exist on the
server, such as

```sh
git rj fetch --all
```

#### 2.5.3. Local Branch Push Required to Server

If the `P` flag is set, then there is a local tracking branch, and the current
commit differs to the remote commit. This indicates a push is required (which
may also need a rebase or a merge as appropriately), or a pull is required. It
lets you know that the content on the git server differs to the local content.

If a push or a pull is required, additional information is obtained and printed
immediately below:

```text
[-TP-] framework/datastructures       8cdaf06d25f (commits: 0 / 0) [bugfix/issuex -> origin/master]
   Local Branch: bugfix/helios-1661 (by 0 commit)
   Tracking Branch: origin/bugfix/helios-1661 (by 1 commits)
```

This shows that the branch `bugfix/issuex` on the server is 1 commit ahead of
the local repository (so that someone else has committed). Had it shown that the
local branch is ahead, this indicates that a pull is required.

This may be useful when fetching all repositories and then individually checking
for any activity with the current work status of the local repositories.

#### 2.5.4. Rebase Required to Destination Branch

If the `R` flag is set, then local branch (e.g. `feature/mytopic`) and the
destination branch (e.g. `origin/master`) have diverged, so that merging the
local branch to the destination branch cannot be done with a fast-forward
branch. Typically this is resolved by a merge commit, or rebasing the local
branch on the destination branch first. The most common use case for this is to
indicate that remote changes have been made on the destination branch that are
not present on this branch (and may require rebasing and then testing).

#### 2.5.5. Commits Ahead or Behind between the Local and Destination Branches

The field `(commits: a / b)` indicate how many commits the local branch is ahead
(`a`). Some projects prefer to squash commits on a feature branch to a single
commit before pushing. Those with more than 1 commit need a squash.

The number of commits behind `b` indicate how far the destination branch (not
the tracking branch for the current local branch) has changed. This can be
indicative during development of how many changes have been made.

Thus these values are a reference to the changes of the local branch to the
destination branch.

The last part of the status `[branch -> remote]` provides the names of those
branches, which the commit counts are referring to.

#### 2.5.6. A Word about Branches and Remotes

The `.gitmodules` should contain information about the default branch for the
submodule. For the current base repository, the default branch is always
`master`.

Each branch may have its own remote when tracking is set up. Each remote is set
for tracking by `git push --set-upstream <remote> <branch>`, or when it's
checked out from the remote.

This allows the default branch `master` may point to the remote `origin`, and
the current development branch `bugfix/issuex` may point to `company`. The
example shows below that the default remote for the destination branch is
`origin`.

```text
(commits: 0 / 0) [bugfix/issuex -> origin/master]
```

The fact that `origin/master` and `company/master` may differ is ignored, by
only using the *default* remote as configured, usually by the first pull and
initialization (but may be changed using git commands).

Likewise, the user may create a branch from the local `master`, and then decide
to push that branch, setting the upstream remote to a company server remote
called `company`. The `P` flag relies on the tracking branch given for the local
branch that is currently checked out, and will show only the information between
the local branch and the feature branch pushed to the different remote.

```text
[-TP-] framework/datastructures       4d79aecb3651 (commits: 0 / 0) [bugfix/issuex -> origin/master]
   Local Branch: bugfix/issuex (by 1 commit)
   Tracking Branch: company/bugfix/issuex (by 0 commits)
```

That means, the `origin/master` has not advanced since the commit
`bugfix/issuex` was created locally, and that the current tracking branch for
`bugfix/issuex` points to the remote `company` and must be pushed. This makes it
quite fine to push local changes still for review to a local server, to then
push the reviewed changes to an upstream (public) server.

#### 2.5.7. The Destination Branch Selection Method

The destination branch is obtained from the file `.gitmodules` with the config
option `branch`.

If the branch exists locally, it's tracking branch is used as a reference if it
exists, else the local branch is used.

Otherwise, there's no local branch, then the remote `origin` is used as
preference, unless there only a single remote, in which case that is used. If
there is no `origin` remote, and there is more than one remote defined, no
branch is used as a reference, as it's ambiguous which branch to refer to, one
should check out locally the branch from the remote which should be used.

If that's confusing, here's a table. Let's say that the default branch name is
`foo`.

| local branch exists  | origin | second | third | Destination Branch |
| -------------------- | ------ | ------ | ----- | ------------------ |
| No                   | No     | No     | No    | None               |
| No                   | Yes    | -      | -     | origin/foo         |
| No                   | No     | Yes    | No    | second/foo         |
| No                   | No     | Yes    | Yes   | None               |
| YES, no tracking     | -      | -      | -     | foo                |
| YES, tracking origin | No     | -      | -     | foo                |
| YES, tracking origin | Yes    | -      | -     | origin/foo         |
| YES, tracking second | -      | No     | -     | foo                |
| YES, tracking second | -      | Yes    | -     | second/foo         |

If no destination branch is found, the output shows the branch name in the
`.gitsubmodules` file (`foo` in this example), and the commits ahead and behind
will be empty.

```text
(commits: - / -) [master -> bugfix/issuex]
```

This says that the `.gitmodules` file has `bugfix/issuex` present, but:

* there is no local branch that exists, and;
  * no remote branch that has been fetched of that name; or
  * or multiple remotes exist with that name and none have the remote `origin`.

### 2.6. Checking out a branch

When developing a feature, multiple repositories might need to be modified all
simultaneously. The checkout command makes it easy to check out a branch on all
repositories simultaneously, and if that branch doesn't exist, then check out
the default branch (as given in the `.gitmodules` file).

```sh
git rj cobr [--force] [branch]
```

If no branch is given, then all branches in the immediate submodules are checked
out to the branch given by the `.gitmodules` default branch as it is currently
provided in the base repository. The base repository is not touched when no
branch is given, so you can check out what is required first, or change
`.gitmodules` for testing.

If a branch name is given, the base branch is checked out first. Then all
repositories are checked out to the branch also specified. If the branch
specified on the command line doesn't exist in the base repository, the base
repository is not changed.

Once the base repository is checked out, the `.gitmodules` file describes what
the default branches of all submodules should be. If the branches exist in the
submodule, it is checked out, otherwise the default branch given in the
`.gitmodules` file is checked out. If the branch in the `.gitmodules` file
doesn't exist, then the default branch `master` is checked out.

If the option `--force` is given, all local changes are lost, including the base
branch, if a branch was specified. This supports working on long running
development branches (like release branches and development branches).

To check out all submodules as per the current `.gitmodules` file. This is
usually to change to the current position of the long running branch.

```sh
git rj cobr
```

If you want to switch to a feature branch and pull to the latest, discarding all
local changes:

```sh
git rj cobr -f feature/abc
git rj pull
```

The ultimate effect is to check out a branch (such as a branch in work), and if
not defined, default to a second branch (such as a development or release
branch).

#### 2.6.1. Use Case: Feature on the master branch

Often, just working from the `master` branch for all repositories, a feature is
added to a library, and an application should be changed at the same time to use
that new feature (the library change might also break the application if they
aren't changed in unison).

The `.gitmodules` file have all submodules pointing to `master`. Some
repositories (those that implementing the feature), have a branch called
`feature/abc`, containing the changes, until they're finished, in which case
they'll be merged back to master and the base repository commit then references
the new changes to the library and application repository.

Assuming that the base repository is already on `master`, then

```sh
git rj cobr feature/abc
```

will check out all repositories to `feature/abc` if they exist, else to `master`
if they happened to be on a different branch. If the `--force` flag is used, the
check out is forced, erasing the local changes.

#### 2.6.2. Use Case: Feature Development on a Development Branch

There's a development branch, which contains multiple major new features, and
can run in parallel to `master`. The development branch might be called
`dev/xyz`. Developers would then check out their feature branches `feature/abc`
from the development branch `dev/xyz`, while bugfixes might still continue on
`master`.

Get the development branch. The `.gitmodules` file will have some repositories
point the default branch to `master`, others to `dev/xyz`.

```sh
git checkout -f dev/xyz && git pull
```

Then check out the feature branch, where the branch `feature/abc` branches from
`dev/xyz`.

```sh
git rj cobr feature/abc
```

It works by checking out to `feature/abc` first. If that doesn't exist, it
checks out to the default branch `dev/xyz` or `master` depending on the
`.gitmodules` file. If the default branch points to `dev/xyz`, but that doesn't
exist (locally or after a fetch), then `master` is checked out.

#### 2.6.3. Managing a Hot-Fix Branch

Usually the base repository only has a tag on a commit that has a release. The
submodules reference the precise configuration of what was built through the
submodule commit identifiers. As such, there is no branch.

The easiest way to create a set for development of a hot fix from a previous
release might be to first initialize

```sh
git checkout TAG
git rj init --init
```

Then create a local working hot fix branch

```sh
git submodule foreach `git checkout -b hotfix`
```

Thus, to check back out to the location of your hotfix, you just need to do:

```sh
git rj cobr hotfix
```

### 2.7. Showing all the Branches

To show a list of all the branches for all the repositories, execute the command

```sh
git rj shbr
```

This will show all the branches for the base repository and immediate
submodules. Then below this, the list of all branches on the remotes will be
shown. The result is a combined list of all branches for the base repository and
immediate submodules.

This allows one to see what is being worked on. Similar to how `git branch -a`
is used followed by a `git checkout branch`, one can list the local branches
that might be on one or more repositories with `git rj shbr`, to check out with
the command `git rj chbr`.

A sample output, with multiple remotes and branches, some locally (because the
remotes were pruned):

```text
Local:
  bugfix/helios-1661 (origin)
  feature/helios-1650
  feature/helios-1651
  feature/helios-828
  master (github origin)
Remote: origin
  HEAD
  bugfix/helios-1661
  bugfix/helios-809
  feature/helios-9_libssh
  master
Remote: github
```

Not all branches are shown, specifically branches beginning with the text
`release/` are suppressed. This can be shown in addition by using the option:

```sh
git rj shbr --show-release
```

### 2.8. Removing Branches

When working on multiple repositories simultaneously, there should be a way to
see all the branches available (as with the `git rj shbr` command), and to
remove groups of branches.

#### 2.8.1. 2.8.1.Pruning

When fetching from a remote repository, the default behavior set up by `git rj
init` is to enable pruning branches when fetching. This will remove the remote
references, but the local branches remain. It gets tedious to identify these
branches (even if `git rj shbr` helps), and then to remove the branches.

A simple mechanism to remove *all* branches that were once being tracked, but
their remote tracking branch no longer exists, is to use the command:

```sh
git rj rmbr -p
```

It will only remove branches that had a default remote assigned. This is the
case when a branch is checked out from a remote (where the default tracking
branch is set up by the `git checkout` command) or when the branch was pushed
(with the `git push --set-upstream remote branch` option).

It will *not* remove branches where you've made a local branch, but not yet
pushed.

It does this by looking for all local branches in all repositories. If there is
a default remote for that branch (found through the command `git config --local
branch.{branch}.remote`), and that remote branch no longer exists, then the
local branch is removed.

If you wish to only prune specific branches, specify the branches on the command
line. The branch will only be removed if the branch is given on the command line
and the branch can be pruned. If no branches are given, then all branches are
pruned.

#### 2.8.2. Removing Local and Remote Branches

Often after merging a change to master, one wants to locally remove the branch.
This can be done by specifying the name of the branch and the option to remove
with `--local` and remotely with the option `--remote`.

```sh
git rj rmbr -lr branch1 [branch2 ...]
```

One can add the option to prune `-p` in addition, in which case the list of
branches are used to remove locally and/or remotely, and all branches that can
be pruned will be removed.

## 3. Automating Builds

If the repository contains a valid `.gitrjbuild` file, the command

```sh
git rj build
```

can be used to build software on the current platform. The `.gitrjbuild` file
contains instructions on how to build the software dependent on the current
platform. It can instantiate new scripts, or do the work directly from Python.

The advantage of having the `git-rj` command as an entry point for building is
consistency and simplification for the developer making builds, without having
to worry about the commands required to build (which may be complex), and in
some cases may simplify creation of build scripts.

Not every platform has Powershell or supports Batch files, and not likewise, not
every platform supports shell scripts. But at least Python 3 must be installed
for this script to work, which can do some of the basic legwork to start builds.

### 3.1. Building in Developer Mode

To start a normal developer build, which will run the "build" and "test" commands:

```sh
git rj build
```

### 3.2. Build in Release Mode

The command to build in release mode is given by

```sh
git rj build --release
```

The `.gitrjbuild` command has the opportunity to provide different commands when
building in release mode. This can do extra checks, etc. depending on the build
scripts.

### 3.3. The .gitrjbuild Configuration File

This file is stored in the base of the repository, where the build commands
shall be run. It is called `.gitrjbuild` and is a text file following the JSON
file format.

The top level element in the configuration file is the configuration (as
determined by Python). The two most common configurations are:

* Windows
* Linux

Within the platform system name are up to three blocks

* "dev" for development builds;
* "release" for release builds; and
* "expansion" for variable expansion

The "dev" and "release" may also contain a section called "expansion" which is
the same, but for that specific configuration only when used.

#### 3.3.1. The "dev" and "release" section

The "dev" section is used in the absence of the `--release` option. Else when
the `--release` option is given, then the "release" section is used. Otherwise
the descriptions of the sections are identical. It is intended that different
compiler flags might be given between the two.

Inside this section are up to four commands:

* "build": the command to build the software
* "test": the command to run unit tests for the software
* "pack": the command to generate packages for the software
* "doc": the command to generate documentation for the software

Each command is the command that should be given to the Operating System to
start a new process. You must be careful, that these commands are secure and do
not provide unintended side effects.

Each command is treated literally, except when an expansion sequence is seen. An
expansion sequence is defined as `${xxxx}`, where `xxxx` is the expansion
variable. Note, this is not the same as normal shell expansion. The `git rj
build` command will process this before giving to the operating system for
execution.

#### 3.3.2. Expansion Variables

When a command contains an expansion, the expansion is first looked for in the
following order:

* An existing system environment variable (e.g. `${ProgramFiles}` or `${HOME}`);
  then
* In the "expansion" block, under "env", and then the key is sought for a
  variable

  ```json
  "expansion": {
    "env": {
      "xxxx": "value"
    }
  }
  ```

* In the "expansion" block, under "tools", which is a list of directories to
  look for a specific tool

  ```json
  "expansion": {
    "tools": {
      "toolname": {
        "exe": "msbuild.exe",
        "x86": {
          "path": [
            "${ProgramFiles}\\Visual Studio 2019\\BuildTools\\Bin",
            "${ProgramFiles}\\Visual Studio 2019\\Community\\Bin",
            "${ProgramFiles}\\Visual Studio 2019\\Professional\\Bin",
            "${ProgramFiles}\\Visual Studio 2019\\Enterprise\\Bin"
          ]
        },
        "AMD64": {
          "path": [
            "${ProgramFiles(x86)}\\Visual Studio 2019\\BuildTools\\Bin",
            "${ProgramFiles(x86)}\\Visual Studio 2019\\Community\\Bin",
            "${ProgramFiles(x86)}\\Visual Studio 2019\\Professional\\Bin",
            "${ProgramFiles(x86)}\\Visual Studio 2019\\Enterprise\\Bin"
          ]
        }
      }
    }
  }
  ```

  The tools section is more flexible, it defines the name of the tool, e.g.
  `${toolname}` in the command. The system will look for "exe" in the list of
  paths dependent on the system architecture of the platform in the order
  specified. This allows the user to not have to explicitly load a script that
  sets up the default paths, and the paths may be per tool.

  Values for the architecture are in the table below. This is derived from the
  values that Python3 returns directly (via the `platform.system()` and
  `platform.machine()` calls).

  | `platform.system()` | 32-bit Intel | 64-bit Intel |
  | ------------------- | ------------ | ------------ |
  | Windows             |              | `AMD64`      |
  | Linux               |              | `x86_64`     |

The section "expansion" may be under the configuration (e.g. "dev" or
"release"), or under the system platform (e.g. "Windows" or "Linux"). Where it
is placed defines the precedence (the order of which expansion is used first).
This allows common variables to be placed for the system, and build specific
variables to override the default for the current system.

Expansions are recursive. You can place expansions within expansions, and
they're expanded.

## 4. Performance Testing

### 4.1. Running Performance Tests

For checking and comparing microbenchmarks, and automation with
[BenchmarkDotNet](https://benchmarkdotnet.org/articles/overview.html) has been
implemented, through the `git rj perf` command.

To run benchmarks:

1. Build the software in release mode

   ```
   git rj build -c preview
   ```

   This builds the software in release mode, but doesn't sign the binaries. If
   the build succeeds, testing should start. All test cases should pass.

2. Run the benchmarks on the machine. It is recommended to do this on a desktop
   (laptops typically throttle the CPU very quickly to constrain temperatures,
   thus producing highly variable benchmark results)

   ```
   git rj perf
   ```

   This executes the built benchmarks.

### 4.2. Defining a Performance Test

The file `.gitrjbuild` contains a section called `perf` that defiens the
performance tests and the commands to execute. For example:

```json
{
    "": {
        "perf": {
            "datastructures": {
                "net48": "framework/datastructures/DatastructuresBenchmark/bin/Release/net48/RJCP.Core.DatastructuresBenchmark.exe",
                "netcore31": "framework/datastructures/DatastructuresBenchmark/bin/Release/netcoreapp3.1/RJCP.Core.DatastructuresBenchmark.dll"
            }
```

This defines a performance test called `datastructures` and executes it for .NET
4.8, and .NET Core 3.1. The paths are relative to the base of the repository.

### 4.3. Executing a Specific Performance Tests

To run a specific performance test (running them all might take a significant
amount of time, upwards of hours), it helps to be able to run only a specific
test (which can take up to 30 minutes) to monitor for changes in the
microbenchmarks (as well as to compare the performance between various .NET
versions).

```
git rj perf datastructures
```

The benchmarks will run, and the result of the benchmark is a Markdown table,
that can be easily copied into markdown documentation files.

### 4.4. Printing the Last Results

In case that the benchmark shouldn't be re-run, but only the last results should
be reprinted:

```cmd
git rj perf -r datastructures
```
