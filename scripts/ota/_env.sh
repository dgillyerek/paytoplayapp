#!/usr/bin/env bash
# Shared settings for the OTA publish scripts (Docs/OTA.md). Sourced, not run.
# Storage is any S3-compatible bucket. Default: Cloudflare R2 (no egress fees), served through a public custom domain.
#   OTA_R2_ACCOUNT_ID         Cloudflare account id (endpoint https://<id>.r2.cloudflarestorage.com)
#   OTA_R2_ACCESS_KEY_ID      R2 API token access key id (Object Read & Write on the bucket only)
#   OTA_R2_SECRET_ACCESS_KEY  R2 API token secret
#   OTA_R2_BUCKET             bucket name
# AWS S3 + CloudFront instead: set OTA_S3_ENDPOINT=aws and use the same three key/bucket variables with an IAM key;
# optionally OTA_CLOUDFRONT_DISTRIBUTION_ID to invalidate the config paths after a publish.
set -euo pipefail

: "${OTA_R2_ACCESS_KEY_ID:?set OTA_R2_ACCESS_KEY_ID (GitHub secret)}"
: "${OTA_R2_SECRET_ACCESS_KEY:?set OTA_R2_SECRET_ACCESS_KEY (GitHub secret)}"
: "${OTA_R2_BUCKET:?set OTA_R2_BUCKET (GitHub secret)}"

export AWS_ACCESS_KEY_ID="$OTA_R2_ACCESS_KEY_ID"
export AWS_SECRET_ACCESS_KEY="$OTA_R2_SECRET_ACCESS_KEY"
export AWS_DEFAULT_REGION="${OTA_S3_REGION:-auto}"
export AWS_EC2_METADATA_DISABLED=true
# R2 and older S3-compatible stores: only send checksums when the API requires them (aws cli >= 2.23 default changed).
export AWS_REQUEST_CHECKSUM_CALCULATION=WHEN_REQUIRED
export AWS_RESPONSE_CHECKSUM_VALIDATION=WHEN_REQUIRED

if [ "${OTA_S3_ENDPOINT:-}" = "aws" ]; then
  OTA_ENDPOINT_ARGS=()
  [ "$AWS_DEFAULT_REGION" = "auto" ] && export AWS_DEFAULT_REGION=us-east-1
else
  : "${OTA_R2_ACCOUNT_ID:?set OTA_R2_ACCOUNT_ID (GitHub secret), or OTA_S3_ENDPOINT=aws for S3}"
  OTA_ENDPOINT_ARGS=(--endpoint-url "${OTA_S3_ENDPOINT:-https://${OTA_R2_ACCOUNT_ID}.r2.cloudflarestorage.com}")
fi

OTA_BUCKET_URL="s3://${OTA_R2_BUCKET}"

ota_s3() { aws "${OTA_ENDPOINT_ARGS[@]}" s3 "$@"; }

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
