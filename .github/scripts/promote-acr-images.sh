#!/usr/bin/env bash

set -euo pipefail

source_registry="${1:?Source registry name is required.}"
source_login_server="${2:?Source login server is required.}"
target_registry="${3:?Target registry name is required.}"
image_tag="${4:?Image tag is required.}"

if [[ "$source_registry" == "$target_registry" ]]; then
  echo "::error::Source and target registries must be different."
  exit 1
fi

if [[ ! "$image_tag" =~ ^sha-[0-9a-f]{40}$ ]]; then
  echo "::error::Image tag must use the immutable sha-<40 hex characters> format."
  exit 1
fi

repositories=(carlens-api carlens-web carlens-aiworker)

for repository_name in "${repositories[@]}"; do
  source_state="$(
    az acr repository show \
      --name "$source_registry" \
      --image "${repository_name}:${image_tag}" \
      --query '[digest, changeableAttributes.writeEnabled, changeableAttributes.deleteEnabled]' \
      --output tsv \
      --only-show-errors
  )"
  IFS=$'\t' read -r source_digest source_write_enabled source_delete_enabled \
    <<< "$source_state"

  if [[ ! "$source_digest" =~ ^sha256:[0-9a-f]{64}$ ]]; then
    echo "::error::Source image ${repository_name}:${image_tag} has no valid digest."
    exit 1
  fi

  if [[ "$source_write_enabled" != "false" || "$source_delete_enabled" != "false" ]]; then
    echo "::error::Source image ${repository_name}:${image_tag} is not immutable."
    exit 1
  fi

  if target_digest="$(
    az acr repository show \
      --name "$target_registry" \
      --image "${repository_name}:${image_tag}" \
      --query digest \
      --output tsv \
      --only-show-errors 2> /dev/null
  )"
  then
    if [[ "$target_digest" != "$source_digest" ]]; then
      echo "::error::Production tag ${repository_name}:${image_tag} points to a different digest."
      exit 1
    fi
  else
    az acr import \
      --name "$target_registry" \
      --source "${source_login_server}/${repository_name}@${source_digest}" \
      --image "${repository_name}:${image_tag}" \
      --output none \
      --only-show-errors
  fi

  az acr repository update \
    --name "$target_registry" \
    --image "${repository_name}:${image_tag}" \
    --write-enabled false \
    --delete-enabled false \
    --output none \
    --only-show-errors

  target_state="$(
    az acr repository show \
      --name "$target_registry" \
      --image "${repository_name}:${image_tag}" \
      --query '[digest, changeableAttributes.writeEnabled, changeableAttributes.deleteEnabled]' \
      --output tsv \
      --only-show-errors
  )"
  IFS=$'\t' read -r target_digest target_write_enabled target_delete_enabled \
    <<< "$target_state"

  if [[ "$target_digest" != "$source_digest" || \
        "$target_write_enabled" != "false" || \
        "$target_delete_enabled" != "false" ]]
  then
    echo "::error::Production image ${repository_name}:${image_tag} failed digest or immutability verification."
    exit 1
  fi

  echo "Promoted ${repository_name}:${image_tag} at ${source_digest}."
done

echo "All production artifacts were promoted without rebuilding."
