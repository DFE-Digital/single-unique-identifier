#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 2 ]]; then
  echo "Usage: $0 <tested-publish-output-directory> <image-tag>" >&2
  exit 2
fi

publish_output_dir="$(realpath "$1")"
image_tag="$2"

if [[ ! -d "$publish_output_dir" || ! -f "$publish_output_dir/SUI.NotificationService.dll" ]]; then
  echo "Publish output must contain SUI.NotificationService.dll: $publish_output_dir" >&2
  exit 1
fi

repo_root="$(git -C "$(dirname "$0")" rev-parse --show-toplevel)"
docker build \
  --platform linux/amd64 \
  --file "$repo_root/Apps/NotificationService/Dockerfile.release" \
  --tag "$image_tag" \
  "$publish_output_dir"
