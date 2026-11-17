#!/usr/bin/env bash
# Shared settings for the OTA publish scripts (Docs/OTA.md). Sourced, not run.
# Storage is any S3-compatible bucket. Default: DigitalOcean Spaces (nyc3), served through
# ota.brightmoatstudios.com (fallback while DNS propagates:
# kingdomdefense-ota.nyc3.cdn.digitaloceanspaces.com).
#   OTA_S3_ACCESS_KEY_ID      Spaces access key (GitHub secret)
#   OTA_S3_SECRET_ACCESS_KEY  Spaces secret (GitHub secret)
#   OTA_S3_BUCKET             bucket name (kingdomdefense-ota)
#   OTA_S3_ENDPOINT           S3 API endpoint (https://nyc3.digitaloceanspaces.com)
#   OTA_S3_REGION             region (default nyc3)
# AWS S3 + CloudFront instead: set OTA_S3_ENDPOINT=aws and use an IAM key with the same
# three key/bucket variables; optionally OTA_CLOUDFRONT_DISTRIBUTION_ID to invalidate
# the config paths after a publish.
set -euo pipefail

: "${OTA_S3_ACCESS_KEY_ID:?set OTA_S3_ACCESS_KEY_ID (GitHub secret)}"
: "${OTA_S3_SECRET_ACCESS_KEY:?set OTA_S3_SECRET_ACCESS_KEY (GitHub secret)}"
: "${OTA_S3_BUCKET:?set OTA_S3_BUCKET (GitHub secret)}"

export AWS_ACCESS_KEY_ID="$OTA_S3_ACCESS_KEY_ID"
export AWS_SECRET_ACCESS_KEY="$OTA_S3_SECRET_ACCESS_KEY"
export AWS_EC2_METADATA_DISABLED=true
# Spaces and older S3-compatible stores: only send checksums when the API requires them (aws cli >= 2.23 default changed).
export AWS_REQUEST_CHECKSUM_CALCULATION=WHEN_REQUIRED
export AWS_RESPONSE_CHECKSUM_VALIDATION=WHEN_REQUIRED

# Spaces buckets restrict listing to signed requests; every object must be uploaded with ACL
# public-read so phones can GET it over the CDN. AWS S3 + CloudFront typically serves through
# the distribution instead, so the ACL is omitted there.
if [ "${OTA_S3_ENDPOINT:-}" = "aws" ]; then
  export AWS_DEFAULT_REGION="${OTA_S3_REGION:-us-east-1}"
  OTA_ENDPOINT_ARGS=()
  OTA_ACL_ARGS=()
else
  export AWS_DEFAULT_REGION="${OTA_S3_REGION:-nyc3}"
  : "${OTA_S3_ENDPOINT:=https://nyc3.digitaloceanspaces.com}"
  OTA_ENDPOINT_ARGS=(--endpoint-url "$OTA_S3_ENDPOINT")
  OTA_ACL_ARGS=(--acl public-read)
fi

OTA_BUCKET_URL="s3://${OTA_S3_BUCKET}"

ota_s3() { aws "${OTA_ENDPOINT_ARGS[@]}" s3 "$@"; }

# Uploads (cp / sync) get the public-read ACL on Spaces. Listing and downloads do not.
ota_s3_put() {
  if [ ${#OTA_ACL_ARGS[@]} -gt 0 ]; then
    ota_s3 "$@" "${OTA_ACL_ARGS[@]}"
  else
    ota_s3 "$@"
  fi
}

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
