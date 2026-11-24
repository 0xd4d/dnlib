#!/usr/bin/env bash
# Runs the tests and prints which lines of dnlib they execute, per source directory.
#
# usage: tests/coverage.sh [--slow] [--minimum <percent>] [leg...]
#
# --minimum fails (exit code 1) if less than that percentage of dnlib's lines is covered, so a change that
# removes tests or adds a lot of untested code is noticed.
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
summary_options=()
while [ $# -gt 0 ]; do
	if [ "$1" = "--slow" ]; then
		options+=("$1")
	elif [ "$1" = "--minimum" ]; then
		summary_options+=(--minimum "$2")
		shift
	else
		legs+=("$1")
	fi
	shift
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
python3 coverage-summary.py ${summary_options[@]+"${summary_options[@]}"} "$(cd ../src && pwd)" coverage/*.cobertura.xml | tee coverage/summary.txt
