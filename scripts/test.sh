#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
dotnet test "$root/tests/Grove.Domain.Tests/Grove.Domain.Tests.csproj"
