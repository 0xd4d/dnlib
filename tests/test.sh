#!/usr/bin/env bash
# Builds and runs the tests against one or more builds of dnlib.
#
# usage: tests/test.sh [leg...]
#
# Legs (default: all of them):
#   net10.0          dnlib's net10.0 build
#   net10.0-nts      dnlib's net10.0 build without THREAD_SAFE
#   netstandard2.0   dnlib's netstandard2.0 build, tests run on .NET 10
#   net45            dnlib's net45 build, tests run on .NET Framework 4.8 (Mono if this isn't Windows)
#   net35            dnlib's net35 build, tests run on .NET Framework 4.8 (Mono if this isn't Windows)
#
# Options after "--" are passed to the test program, eg. tests/test.sh net10.0 -- -class dnlib.Tests.IO.DataReaderTests
set -euo pipefail

cd "$(dirname "$0")"
configuration=Release
project=dnlib.Tests/dnlib.Tests.csproj

legs=()
test_args=()
while [ $# -gt 0 ]; do
	if [ "$1" = "--" ]; then
		shift
		test_args=("$@")
		break
	fi
	legs+=("$1")
	shift
done
if [ ${#legs[@]} -eq 0 ]; then
	legs=(net10.0 net10.0-nts netstandard2.0 net45 net35)
fi

is_windows=false
case "$(uname -s)" in
	MINGW*|MSYS*|CYGWIN*) is_windows=true ;;
esac

# run_leg <name> <test target framework> <msbuild properties...>
run_leg() {
	local name=$1 tfm=$2
	shift 2
	echo "=== $name"
	dotnet build "$project" -v:m -nologo -c "$configuration" -f "$tfm" -warnaserror "$@"
	local out=dnlib.Tests/bin/$configuration/$tfm
	if [ "$tfm" = net10.0 ]; then
		dotnet "$out/dnlib.Tests.dll" "${test_args[@]}"
	elif $is_windows; then
		"$out/dnlib.Tests.exe" "${test_args[@]}"
	elif command -v mono >/dev/null; then
		# Mono 6.12 can crash when the process exits, after all tests have run and the summary was printed.
		# The exit code is then wrong, so the summary line decides.
		local log
		log=$(mktemp)
		mono "$out/dnlib.Tests.exe" "${test_args[@]}" 2>&1 | tee "$log" || true
		if ! grep -q "Errors: 0, Failed: 0" "$log"; then
			rm -f "$log"
			return 1
		fi
		rm -f "$log"
	else
		echo "Skipped: .NET Framework tests need Windows or Mono"
	fi
}

for leg in "${legs[@]}"; do
	case "$leg" in
		net10.0)        run_leg "$leg" net10.0 ;;
		net10.0-nts)    run_leg "$leg" net10.0 -p:DnlibThreadSafe=false ;;
		netstandard2.0) run_leg "$leg" net10.0 -p:DnlibTfm=netstandard2.0 ;;
		net45)          run_leg "$leg" net48 ;;
		net35)          run_leg "$leg" net48 -p:DnlibTfm=net35 ;;
		*) echo "Unknown leg: $leg" >&2; exit 1 ;;
	esac
done
echo "=== All legs passed: ${legs[*]}"
