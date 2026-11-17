#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
dotnet test "$root/tests/Survival.Domain.Tests/Survival.Domain.Tests.csproj"
(cd "$root/Backend/alliance" && npm test)
