#!/usr/bin/env bash
# Publish a remote config to config/<platform>/remote_config.json (Docs/OTA.md).
#   scripts/ota/publish_config.sh path/to/remote_config.json            # android and ios
#   scripts/ota/publish_config.sh path/to/remote_config.json android
# 1. check_remote_balance.sh: schema validation + free-pace sim on any balance patches (stamps free_pace_check);
# 2. refuses a config_version that is not above the live one (phones ignore lower versions anyway);
# 3. checks that content_version, when set, has a catalog on the bucket for that platform;
# 4. uploads a history copy (config/<platform>/history/config_v<N>.json) and then the live file (max-age=60).
# Rollback = publish the old content_version / values again with a higher config_version.
set -euo pipefail
source "$(dirname "$0")/_env.sh"

config="${1:?usage: publish_config.sh <remote_config.json> [android|ios]}"
platforms=("${2:-android}")
[ -z "${2:-}" ] && platforms=(android ios)

"$(dirname "$0")/check_remote_balance.sh" "$config"

read -r new_version content_version < <(python3 -c 'import json,sys; d=json.load(open(sys.argv[1])); print(d["config_version"], d.get("content_version","") or "-")' "$config")

for platform in "${platforms[@]}"; do
  live_key="config/$platform/remote_config.json"
  live="$(mktemp)"
  if ota_s3 cp "$OTA_BUCKET_URL/$live_key" "$live" --no-progress >/dev/null 2>&1; then
    live_version="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1])).get("config_version",0))' "$live")"
    if [ "$new_version" -le "$live_version" ]; then
      echo "config_version $new_version is not above the live $platform config ($live_version). Raise it." >&2
      exit 6
    fi
  fi
  rm -f "$live"

  if [ "$content_version" != "-" ]; then
    target=Android; [ "$platform" = "ios" ] && target=iOS
    if [ -z "$(ota_s3 ls "$OTA_BUCKET_URL/content/$target/$content_version/" 2>/dev/null || true)" ]; then
      echo "content_version $content_version has no content for $target on the bucket (scripts/ota/publish_content.sh $target $content_version)." >&2
      [ "$platform" = "ios" ] && [ "${OTA_ALLOW_MISSING_IOS:-1}" = "1" ] && { echo "  (ios: skipped; phones there stay on bundled content)"; } || exit 7
    fi
  fi

  ota_s3_put cp "$config" "$OTA_BUCKET_URL/config/$platform/history/config_v$new_version.json" --content-type application/json --cache-control "public, max-age=31536000" --no-progress
  ota_s3_put cp "$config" "$OTA_BUCKET_URL/$live_key" --content-type application/json --cache-control "public, max-age=60" --no-progress
  echo "Published config v$new_version for $platform (content_version ${content_version/-/(bundled)})."
done

if [ "${OTA_S3_ENDPOINT:-}" = "aws" ] && [ -n "${OTA_CLOUDFRONT_DISTRIBUTION_ID:-}" ]; then
  aws cloudfront create-invalidation --distribution-id "$OTA_CLOUDFRONT_DISTRIBUTION_ID" --paths "/config/*" >/dev/null
elif [ "${OTA_S3_ENDPOINT:-}" != "aws" ]; then
  echo "Flush the Spaces CDN so edges drop the previous live config: doctl compute cdn flush <cdn-id> --files /config/*"
fi
