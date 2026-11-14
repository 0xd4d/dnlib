#!/usr/bin/env bash
set -euo pipefail

configuration=Release

dotnet build -v:m -c "$configuration"
dotnet pack -v:m -c "$configuration" src/dnlib.csproj
