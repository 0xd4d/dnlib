#!/usr/bin/env bash
# Tests dnlib's net35 build on the runtime that it's for: builds tests/dnlib.Net35Tests and runs it on
# .NET Framework 3.5 (CLR 2.0). The other tests (test.sh net35) run that build on .NET Framework 4.8.
#
# usage: tests/test-net35.sh [--required]
#
# .NET Framework 3.5 only exists on Windows and is a Windows feature that must be turned on:
#   dism /online /enable-feature /featurename:NetFx3 /all
# Without it, nothing is tested and the script succeeds, unless --required is given.
set -euo pipefail

cd "$(dirname "$0")"

required=false
if [ "${1:-}" = "--required" ]; then
	required=true
fi

clr2=
case "$(uname -s)" in
	MINGW*|MSYS*|CYGWIN*) clr2="$(cygpath -u "$WINDIR")/Microsoft.NET/Framework64/v2.0.50727/mscorwks.dll" ;;
esac
if [ -z "$clr2" ] || [ ! -f "$clr2" ]; then
	if $required; then
		echo ".NET Framework 3.5 isn't installed" >&2
		exit 1
	fi
	echo "Skipped: .NET Framework 3.5 isn't installed"
	exit 0
fi

dotnet build dnlib.Net35Tests/dnlib.Net35Tests.csproj -v:m -nologo -c Release -warnaserror
dnlib.Net35Tests/bin/Release/net35/dnlib.Net35Tests.exe
echo "=== All passed: .NET Framework 3.5"
