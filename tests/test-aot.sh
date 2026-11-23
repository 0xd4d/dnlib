#!/usr/bin/env bash
# Tests dnlib in a program that's compiled to native code (NativeAOT) and in a trimmed program:
# publishes tests/dnlib.AotTests and runs it. Trimmer and native compiler warnings are errors.
#
# usage: tests/test-aot.sh [aot|trimmed]... [-r <runtime identifier>]
#
# Default: both modes, for this machine's OS and CPU.
#
# A Windows program can be built on Linux and is then run with Wine: tests/test-aot.sh -r win-x64
# For that, DNLIB_XWIN_SDK must be the directory of the Windows SDK and MSVC libraries as downloaded by xwin
# (https://github.com/Jake-Shadle/xwin: xwin splat --output <dir>), and lld-link and wine must be installed.
set -euo pipefail

cd "$(dirname "$0")"
project=dnlib.AotTests/dnlib.AotTests.csproj

modes=()
rid=
while [ $# -gt 0 ]; do
	case "$1" in
		aot|trimmed) modes+=("$1") ;;
		-r) rid=$2; shift ;;
		*) echo "Unknown argument: $1" >&2; exit 1 ;;
	esac
	shift
done
if [ ${#modes[@]} -eq 0 ]; then
	modes=(aot trimmed)
fi
if [ -z "$rid" ]; then
	# The portable identifier (eg. linux-x64), not the one of this Linux distribution
	rid=$(dotnet msbuild "$project" -getProperty:NETCoreSdkPortableRuntimeIdentifier)
fi

is_windows=false
case "$(uname -s)" in
	MINGW*|MSYS*|CYGWIN*) is_windows=true ;;
esac

exe=dnlib.AotTests
run=()
if [[ "$rid" == win-* ]]; then
	exe=dnlib.AotTests.exe
	if ! $is_windows; then
		: "${DNLIB_XWIN_SDK:?Set DNLIB_XWIN_SDK to the xwin directory to build a Windows program on this OS}"
		# The native compiler runs a linker called 'link': give it lld-link with the Windows libraries
		mkdir -p dnlib.AotTests/obj/cross-tools
		cat > dnlib.AotTests/obj/cross-tools/link <<LINK
#!/bin/sh
exec lld-link /libpath:"$DNLIB_XWIN_SDK/crt/lib/x86_64" /libpath:"$DNLIB_XWIN_SDK/sdk/lib/um/x86_64" /libpath:"$DNLIB_XWIN_SDK/sdk/lib/ucrt/x86_64" "\$@"
LINK
		chmod +x dnlib.AotTests/obj/cross-tools/link
		PATH="$PWD/dnlib.AotTests/obj/cross-tools:$PATH"
		export DNLIB_AOT_TEST_CROSS_WINDOWS=true
		export WINEDEBUG=-all
		run=(wine)
	fi
fi

for mode in "${modes[@]}"; do
	echo "=== $mode $rid"
	out=dnlib.AotTests/bin/publish/$mode-$rid
	rm -rf "$out"
	# The mode is an environment variable and not a -p: option: an option would also be used for the dnlib
	# project, and would break its other target frameworks
	DNLIB_AOT_TEST_MODE=$mode dotnet publish "$project" -v:m -nologo -c Release -r "$rid" -o "$out"
	"${run[@]}" "$out/$exe"
done
echo "=== All passed: ${modes[*]} ($rid)"
