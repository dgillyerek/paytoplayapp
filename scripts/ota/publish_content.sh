#!/usr/bin/env bash
# Upload one Addressables content build to content/<BuildTarget>/<content_version>/ (Docs/OTA.md).
#   scripts/ota/publish_content.sh Android 2026.11.15-1
# Input: ServerData/<BuildTarget>/ from Survival/OTA/Build Content (or the ota-content workflow).
# Content folders are immutable: an existing version is never overwritten (pick a new version). Phones only start
# using it once a remote config with content_version=<version> is published (scripts/ota/publish_config.sh).
set -euo pipefail
source "$(dirname "$0")/_env.sh"

target="${1:?usage: publish_content.sh <BuildTarget e.g. Android|iOS> <content_version>}"
version="${2:?usage: publish_content.sh <BuildTarget> <content_version>}"
[[ "$version" =~ ^[A-Za-z0-9._-]{1,64}$ && "$version" != *..* ]] || { echo "content_version must be 1-64 of [A-Za-z0-9._-]" >&2; exit 2; }

src="$REPO_ROOT/ServerData/$target"
[ -f "$src/content_manifest.json" ] || { echo "No content build at $src (run Survival/OTA/Build Content first)." >&2; exit 3; }
grep -q "\"content_version\": \"$version\"" "$src/content_manifest.json" || { echo "$src was built for another content_version: $(grep content_version "$src/content_manifest.json")" >&2; exit 4; }

dest="$OTA_BUCKET_URL/content/$target/$version"
if ota_s3 ls "$dest/" >/dev/null 2>&1 && [ -n "$(ota_s3 ls "$dest/" || true)" ]; then
  echo "$dest already exists; content versions are immutable. Build with a new content_version." >&2
  exit 5
fi

# Bundles have hashed names: cache for a year. Catalog / hash / manifest: one minute (they are re-read at boot).
# Spaces listing is private, so every object is uploaded ACL public-read (ota_s3_put).
ota_s3_put cp "$src/" "$dest/" --recursive --exclude "catalog*" --exclude "content_manifest.json" \
  --cache-control "public, max-age=31536000, immutable" --no-progress
ota_s3_put cp "$src/" "$dest/" --recursive --exclude "*" --include "catalog*" --include "content_manifest.json" \
  --cache-control "public, max-age=60" --no-progress
echo "Published content $version for $target to $dest."
echo "Go live: set \"content_version\": \"$version\" in the remote config and run scripts/ota/publish_config.sh."
echo "Then flush the Spaces CDN so edges drop any previous catalog: doctl compute cdn flush <cdn-id> --files /content/$target/$version/*"
