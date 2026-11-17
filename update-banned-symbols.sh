#!/usr/bin/env bash
# Regenerates src/BannedSymbols.txt (used by Microsoft.CodeAnalysis.BannedApiAnalyzers) from the net10.0 XML doc
# file. dnlib must not throw when it reads invalid metadata, so it must not use:
#   - any *OrThrow property (the Resolve*OrThrow/Find*OrThrow methods throw by design and aren't banned)
#   - the Importer.Import*/ModuleDef.Import overloads that throw if the import fails
# Run ./build.sh first. CI fails if the file changes, ie. if an OrThrow property was added but this script
# wasn't run.
set -euo pipefail

cd "$(dirname "$0")"
xml=src/bin/Release/net10.0/dnlib.xml
if [ ! -f "$xml" ]; then
	echo "$xml doesn't exist, run ./build.sh first" >&2
	exit 1
fi

ids=$(grep -o 'name="[^"]*"' "$xml" | sed 's/^name="//;s/"$//')
{
	echo "$ids" | grep -E '^P:.*OrThrow$' |
		sed 's/$/;dnlib must not throw when reading invalid metadata, use the member without the OrThrow suffix/'
	echo "$ids" | grep -E '^M:dnlib\.DotNet\.(Importer\.(Import|ImportAsTypeSig)|ModuleDef\.Import)\(' |
		sed 's|$|;It throws if the import fails, use Importer.ImportOrNull()/ImportAsTypeSigOrNull()|'
} | LC_ALL=C sort > src/BannedSymbols.txt
