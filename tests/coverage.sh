#!/usr/bin/env bash
# Runs the tests and prints which lines of dnlib they execute, per source directory.
#
# usage: tests/coverage.sh [--slow] [leg...]
#
# The arguments are those of tests/test.sh. The default is the net10.0 leg only; with several legs, a line is
# covered if any leg covers it. The reports are written to tests/coverage/ (Cobertura XML, one per leg).
#
# Needs dotnet-coverage: dotnet tool install --global dotnet-coverage
set -euo pipefail

cd "$(dirname "$0")"
if ! command -v dotnet-coverage >/dev/null; then
	echo "dotnet-coverage wasn't found. Install it with: dotnet tool install --global dotnet-coverage" >&2
	exit 1
fi

options=()
legs=()
for arg in "$@"; do
	if [ "$arg" = "--slow" ]; then
		options+=("$arg")
	else
		legs+=("$arg")
	fi
done
if [ ${#legs[@]} -eq 0 ]; then
	legs=(net10.0)
fi

rm -rf coverage
mkdir coverage
for leg in "${legs[@]}"; do
	# The test process is started by test.sh, dotnet-coverage also collects from child processes
	dotnet-coverage collect --output-format cobertura --output "coverage/$leg.cobertura.xml" \
		"./test.sh ${options[*]} $leg"
done
python3 coverage-summary.py "$(cd ../src && pwd)" coverage/*.cobertura.xml | tee coverage/summary.txt
