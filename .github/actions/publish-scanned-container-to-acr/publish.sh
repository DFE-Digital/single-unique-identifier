#!/usr/bin/env bash
set -euo pipefail

[[ "$REGISTRY_SERVER" =~ ^[a-z0-9.-]+(:[0-9]+)?$ ]]
[[ "$REPOSITORY" =~ ^[a-z0-9]+([._/-][a-z0-9]+)*$ ]]
[[ "$IMAGE_TAG" =~ ^[A-Za-z0-9_][A-Za-z0-9_.-]{0,127}$ ]]
test -f "$DOCKERFILE"
base_ref=$(awk 'toupper($1) == "FROM" { ref = $2 } END { print ref }' "$DOCKERFILE")
[[ "$base_ref" =~ ^([^[:space:]@]+)@(sha256:[0-9a-f]{64})$ ]]
base_image=${BASH_REMATCH[1]}
base_digest=${BASH_REMATCH[2]}
mkdir -p "$(dirname "$METADATA_PATH")"
docker image inspect "$LOCAL_IMAGE" >/dev/null

az acr login --name "$REGISTRY_NAME"
image_repository="$REGISTRY_SERVER/$REPOSITORY"
image="$image_repository:$IMAGE_TAG"
docker tag "$LOCAL_IMAGE" "$image"
docker push "$image"
digest=$(az acr repository show --name "$REGISTRY_NAME" \
  --image "$REPOSITORY:$IMAGE_TAG" --query digest --output tsv)
[[ "$digest" =~ ^sha256:[0-9a-f]{64}$ ]]
echo "repository=$image_repository" >> "$GITHUB_OUTPUT"
echo "digest=$digest" >> "$GITHUB_OUTPUT"
jq -n --arg repository "$image_repository" --arg digest "$digest" \
  --arg base_image "$base_image" --arg base_digest "$base_digest" \
  '{repository:$repository,digest:$digest,base_image:$base_image,base_digest:$base_digest,scan:"success",registry_publication:"success"}' \
  > "$METADATA_PATH"
