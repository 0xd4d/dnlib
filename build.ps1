$ErrorActionPreference = 'Stop'

#
# dotnet build isn't used because it can't build net35 tfms
#

msbuild -v:m -restore -t:Build -p:Configuration=Release dnlib.sln
if ($LASTEXITCODE) { exit $LASTEXITCODE }
msbuild -v:m -t:Pack -p:Configuration=Release dnlib.sln
if ($LASTEXITCODE) { exit $LASTEXITCODE }
