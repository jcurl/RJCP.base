# Using GIT to Obtain Initial Development

The repository used for storing the source code is with BitBucket. The initial
development was done in a sprint according to a design created in Enterprise
Architect, and after an initial amount of testing it is squashed into a single
master commit that forms the basis of the first commit.

## Goal

To hide the initial development, but keep the history if still required.

## Obtaining the References

Clone first the git repository. Once it is cloned, obtain the development tags

```sh
git fetch origin refs/devtags/*:refs/devtags/*
```

You'll be able to see the tags in the original repository and check the history.

## Creating a New Reference

Git allows you to easily create a new reference, but using more low level
commands:

```sh
git update-ref refs/devtags/dotnet-XXX_TagNotes 82c7720f6cb93358c240b2133a05988025ea8619
```

or for the current commit

```sh
git update-ref refs/devtags/dotnet-XXX_TagNotes `git rev-parse HEAD`
```

These are the commands used to create snapshots during the initial development
phase for testing. As they're not official versions, they haven't been made
tags, but are useful none-the-less during the development phase, or for just
saving a commit that can be referenced to later by other documentation (e.g. a
test that isn't suitable for commit, but contains a lesson learned anycase).

## Pushing the References

Once you've made the references, you can push them to the server:

```sh
git push origin refs/devtags/*
```

Then you can obtain the references as described above. The BitBucket UI will not
show the references in the commit history, but they are there, if you want to
look at them later.

## Deleting the Remote Refernece

In case the remote reference was pushed and it must be removed:

```sh
git push origin :refs/devtags/dotnet-XXX_TagNotes
```

This will push an empty refernece, removing the remote reference. This allows
you to update a new version of the reference.
