#!/bin/bash

# This script provides a sample on how we can scan through all project files and
# update the version information.
#
# Note: Some projects (nunitextensions) shouldn't have all versions updated, so
# either manually inspect all changes, or the script needs to be updated so that
# NUnit version 3.x aren't updated.
#
# This script would update over time with the new version information.

# To run the script, use Cygwin or MSYS2, and run the script from where the
# solution file is. It will iterate over all files ending in ".csproj".
#
# Note: This script does handle properly if there are spaces in the file name.

find . -type f -name '*.csproj' -print0 | while IFS= read -r -d '' FILE; do
  echo "Updating $FILE"
  sed -i 's/\(<PackageReference Include="NUnit" Version="\)[^"]*/\14.4.0/' "$FILE"
  sed -i 's/\(<PackageReference Include="NUnit.Analyzers" Version="\)[^"]*/\14.10.0/' "$FILE"
  sed -i 's/\(<PackageReference Include="NUnit.ConsoleRunner" Version="\)[^"]*/\13.20.1/' "$FILE"
  sed -i 's/\(<PackageReference Include="NUnit3TestAdapter" Version="\)[^"]*/\15.1.0/' "$FILE"
  unix2dos "$FILE"
done

