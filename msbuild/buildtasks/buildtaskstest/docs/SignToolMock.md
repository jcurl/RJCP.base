# Mocking the Sign Tool

This document gives on overview of how the SignTool is mocked, so that when unit
testing, we don't actually need to have the `signtool.exe` binary in the path.

## The ToolsFactory

The class `Infrastructure.Tools.ToolFactory` has a static `Instance` method that
is responsible for returning a "real" instance of the `SignTool` class. This
class creates the process `signtool.exe` to sign the object in question.

The `Instance` parameter can be overwritten with a different factory, which can
return an object which *simulates* signing, which is the core of the unit tests.

## Simulating Signing

An instance of `Infrastructure.Tools.TestToolsFactory` is assigned to the
`Instance` parameter of the `ToolsFactory` object. The MSBuild task will then
get the simulated `SignToolMock` from the `TestToolsFactory`, instead of the
real `SignTool` object from the `ToolsFactory`. To MSBuild, they should behave
the same (given the raw command line options and working directory, as if it
were given to a `Process` object).

## The Executable SignTool

In the real execution, the object `SignTool` looks for the `signtool.exe`
binary, and the default implementation provided by `Executable` will then create
a new `Process` object to execute that binary. The `Executable` is just a
wrapper over the `RunProcess` class that can allow a number of instances to run
in parallel, and simplifies searching the file system for the process that
should be run.

The `RunProcess` is what creates and executes the `Process`, wrapping it around
asynchronous methods, so it is easier to run processes in parallel using the
task programming library (TPL). But it also provides one important abstraction
required for simulation - instead of providing an executable path found in the
file system, a `SimAction` can be given that can simulate the binary running.

### SignToolSimProcess and SignToolMock

The `SignToolSimProcess` is the object that derives from the `RunProcess` object
that instead of executing a real binary, it simulates it via a lambda function
instead. It replaces the `SignTool` class (this class can't be used in unit
tests).

The `SignToolMock` derives from the `Executable` object, just like `SignTool`
does, but now overrides the methods responsible for actually instantiating the
process to execute calling the `SignToolSimProcess` (derived by `RunProcess`)
instead of calling `RunProcess` direct.

The sequence from the build task is therefore:

* Get the instance from `ToolsFactory.Instance` which is the object
  `TestToolsFactory`.
* The `TestToolsFactory` reutrn an instance of `SignToolMock` instead of the
  real binary `SignTool`.
* `SignToolMock` overrides what happens when the `Executable.Run` methods are
  called. Where the original `SignTool` uses functionality within `Executable`
  to start the process via `RunProcess`, it instead instantiates a new
  `SignToolSimProcess` providing a lambda function simulating the `signtool.exe`
  instead.
* `SignToolSimProcess` has the ability to check the command line options and to
  return success or failure, depending on the kind of test we want to do.
