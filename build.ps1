param([switch]$NoMsbuild)

$ErrorActionPreference = 'Stop'

$configuration = 'Release'

#
# dotnet build isn't used because it can't build net35 tfms
#

$env:NoTargetFrameworkNet35 = ''

$useMsbuild = $IsWindows -or $IsWindows -eq $null
if ($NoMsbuild) {
	$useMsbuild = $false
}

if (!$useMsbuild) {
	# There are currently no net35 reference assemblies on nuget
	$env:NoTargetFrameworkNet35 = 'true'
}

if ($useMsbuild) {
	msbuild -v:m -restore -t:Build -p:Configuration=$configuration
	if ($LASTEXITCODE) { exit $LASTEXITCODE }
	msbuild -v:m -t:Pack -p:Configuration=$configuration src/dnlib.csproj
	if ($LASTEXITCODE) { exit $LASTEXITCODE }
}
else {
	dotnet build -v:m -c $configuration
	if ($LASTEXITCODE) { exit $LASTEXITCODE }
	dotnet pack -v:m -c $configuration src/dnlib.csproj
	if ($LASTEXITCODE) { exit $LASTEXITCODE }
}

$env:NoTargetFrameworkNet35 = ''
