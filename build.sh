#!/usr/bin/env bash
set -euo pipefail

configuration=Release

dotnet build -v:m -c "$configuration" -warnaserror
dotnet pack -v:m -c "$configuration" -warnaserror src/dnlib.csproj
