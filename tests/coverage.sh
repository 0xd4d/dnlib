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

# The full path of this bash: on Windows, dotnet-coverage would start the "bash" of the Windows Subsystem for
# Linux (C:\Windows\System32\bash.exe) and not Git Bash
bash_path=$BASH
if command -v cygpath >/dev/null; then
	bash_path=$(cygpath -m "$BASH")
fi

rm -rf coverage
mkdir coverage
for leg in "${legs[@]}"; do
	# The test process is started by test.sh, dotnet-coverage also collects from child processes
	dotnet-coverage collect --output-format cobertura --output "coverage/$leg.cobertura.xml" \
		"$bash_path" ./test.sh ${options[@]+"${options[@]}"} "$leg"
done

# On Windows (Git Bash), Python needs the Windows form of the path (pwd -W), and python3 can be missing
source_directory=$(cd ../src && (pwd -W 2>/dev/null || pwd))
python=python3
if ! command -v python3 >/dev/null; then
	python=python
fi
$python coverage-summary.py ${summary_options[@]+"${summary_options[@]}"} "$source_directory" coverage/*.cobertura.xml | tee coverage/summary.txt
