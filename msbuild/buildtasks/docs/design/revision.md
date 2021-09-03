# Design Documentation - Revision Control <!-- omit in toc -->

- [1. The SourceProvider](#1-the-sourceprovider)
  - [1.1. Class Structure](#11-class-structure)
    - [1.1.1. Top Level Factory](#111-top-level-factory)
- [2. The GitProvider](#2-the-gitprovider)
- [3. Extending with a new Provider (e.g. Mercurial or Subversion)](#3-extending-with-a-new-provider-eg-mercurial-or-subversion)

## 1. The SourceProvider

The software is designed that multiple source providers can theoretically be
supported, but this implementation only has one source provider, the
`GitProvider`.

### 1.1. Class Structure

The folder `Infrastructure\SourceProvider` contains the code that knows how to
get information about the revision control system. It is structured so that
further revision control systems can be added with minor modifications.

#### 1.1.1. Top Level Factory

The top level factory to a `ISourceControl` object, that knows specifically how
to talk to binaries to learn about the current path, is `SourceFactory`.

```csharp
ISourceFactory factory = new SourceFactory();
ISourceControl provider = factory.Create("providername", "C:\Users\Me\sources\MyRepo");
```

The `providername` can be one of:

* `auto` for automatic detection. At the moment this can only know about git.
* `git` for a git controlled revision control system.

## 2. The GitProvider

The `GitProvider` knows how to query GIT (the command `git` must be installed
and in the path) to get information required for knowing if there are changes to
the sources or not.

Because code using the `async` model, the `GitProvider.CreateAsync` method is
used to get a new instance which is awaitable (constructors aren't awaitable,
and should therefore not do any long running operations). This method takes the
base path of the project, which may be the top level of a GIT repository, but a
subdirectory in the git repository.

The following operations are performed:

* `GetCurrentBranchAsync(path)`:
  * Executes `git symbolic-ref -q --short HEAD`.
  * The path is ignored as branches in GIT apply to the entire repository.
* `GetCommitAsync(path)` and `GetCommitShortAsync(path)`:
  * Executes `git log -1 --format=%H -- path`.
  * This returns the last commit for the given path, not the current commit.
* `GetCommitDateAsync`:
  * Executes `git log -1 --format=%cI -- path`.
  * This returns the last commit for the given path, not the current commit.
* `IsDirtyAsync(path)`:
  * Gets the commit of HEAD with `git show-ref -s HEAD`. This is to see if the
    repository is empty or not. If it's empty, then it's dirty.
  * Executes `git diff-index --quiet HEAD -- path`.
  * This works for staged and unstaged changes, but files not in the tree are
    ignored (so be careful, it will be clean if files are "forgotten" to be
    added)
* `IsTaggedAsync(tag, path)`:
  * Gets the commit of HEAD with `git show-ref -s HEAD`.
  * Gets the commit of the tag with `git show-ref -s tag`.
  * If they both exist, then it checks if the commits are the same for the given
    path with `git diff --quiet headcommit tagcommit -- path`.

Because executing the GIT process can take time, the results are cached between
calls for an instance of the `GitProvider` class. This is the purpose of the
`AsyncValue` and `AsyncCache` classes designed to with with C# Tasks and to
execute the internal function only once, while waiting for it to complete when
called twice or return cached values for subsequent calls.

## 3. Extending with a new Provider (e.g. Mercurial or Subversion)

The provider should be created as a new class. Fill in the details that knows
how to actually get the details required for a given path.

```csharp
internal class MyProvider : ISourceControl {
    public MyProvider(string path) {
        ...
    }

    ...
}
```

Create a new factory for this provider:

```csharp
internal class MyProviderFactory : ISourceFactory {
    using System;

    internal class MyProviderFactory : ISourceFactory
    {
        public ISourceControl Create(string provider, string path)
        {
            return new MyProvider(path);
        }
    }
}
```

An integration test can then use this factory to instantiate the class and the
path.

Extend the top level factory to create the revision control system. If you can
detect the revision control system some how by the path, then you should extend
the functionality for the `auto` provider to first check and then create the
factory and the class based on that.

```csharp
namespace RJCP.MSBuildTasks.Infrastructure.SourceProvider
{
    using System;

    internal class SourceFactory : ISourceFactory
    {
        public ISourceControl Create(string provider, string path)
        {
            ISourceFactory factory = null;
            if (provider.Equals("auto")) {
                // Check the path if we can determine what source control is
                // used. May need to check parent paths, looking for special
                // folders.
            } else if (provider.Equals("git", StringComparison.OrdinalIgnoreCase)) {
                factory = new GitSourceFactory();
            } else if (provider.Equals("myprovider", StringComparison.OrdinalIgnoreCase)) {
                factory = new MyProviderFactory();
            }

            if (factory == null)
                throw new UnknownSourceProviderException(Resources.Infra_Source_UnknownProvider, provider);

            return factory.Create(provider, path);
        }
    }
}
```
