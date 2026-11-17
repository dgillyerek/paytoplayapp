#!/usr/bin/env bash
# Publish-time free-pace check for a remote config's balance patches (Docs/OTA.md).
#   scripts/ota/check_remote_balance.sh path/to/remote_config.json
# Validates the whole document (same rules as the phone), applies balance.patches to the shipped catalog files, runs the
# free-pace sim (tests/Survival.Domain.Tests RemoteBalancePaceTests), and fails when the free wall (L31) or any locked
# checkpoint day moves beyond 3%. On a pass it writes balance.free_pace_check (SHA-256 of the patches + the sim's days)
# into the file; phones apply balance patches only with a matching, in-tolerance check.
set -euo pipefail
config="$(cd "$(dirname "${1:?usage: check_remote_balance.sh <remote_config.json>}")" && pwd)/$(basename "$1")"
root="$(cd "$(dirname "$0")/../.." && pwd)"
out="$(mktemp)"
trap 'rm -f "$out"' EXIT

OTA_CANDIDATE_CONFIG="$config" OTA_ATTESTATION_OUT="$out" \
  dotnet test "$root/tests/Survival.Domain.Tests" --filter "FullyQualifiedName~RemoteBalancePaceTests.Candidate_config_keeps_the_locked_free_pace" --nologo -v q

[ -s "$out" ] || { echo "The check wrote no result." >&2; exit 3; }
python3 - "$config" "$out" <<'PY'
import json, sys
config_path, check_path = sys.argv[1], sys.argv[2]
with open(config_path, encoding="utf-8") as f:
    doc = json.load(f)
with open(check_path, encoding="utf-8") as f:
    check = json.load(f)
balance = doc.setdefault("balance", {})
balance.setdefault("patches", {})
balance["free_pace_check"] = check if balance["patches"] else None
with open(config_path, "w", encoding="utf-8") as f:
    json.dump(doc, f, indent=2, ensure_ascii=False)
    f.write("\n")
print("free-pace check passed; stamped", config_path, "(balance sha", check["balance_sha256"][:12] + "...)")
PY
