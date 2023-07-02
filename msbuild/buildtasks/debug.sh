#!/bin/sh

NUGET_PACKAGE=$HOME/.nuget/packages/rjcp.msbuildtasks/0.2.3
SRCDIR=`pwd`

echo "Preparing Debug RJCP.MSBuildTasks"
cd ../..
git clean -xfd
git rj clean
pkill -f dotnet
rm -rf ${NUGET_PACKAGE}
sleep 1

echo "Building Debug RJCP.MSBuildTasks"
cd ${SRCDIR}
dotnet build
pkill -f dotnet
rm -rf ${NUGET_PACKAGE}
sleep 1

echo "Building Release RJCP.MSBuildTasks"
# Because this uses the official version of the package, it is restored.
dotnet build -c Release
pkill -f dotnet
sleep 1

echo "Copying Release RJCP.MSBuildTasks"
cp ./buildtasks/bin/Release/netstandard2.1/RJCP.MSBuildTasks.dll ${NUGET_PACKAGE}/tools/netstandard2.1
