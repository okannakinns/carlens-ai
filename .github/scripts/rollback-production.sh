#!/usr/bin/env bash

set -uo pipefail

resource_group="${1:?Resource group is required.}"
api_app="${2:?API app name is required.}"
web_app="${3:?Web app name is required.}"
worker_app="${4:?Worker app name is required.}"
stable_api_revision="${5:?Stable API revision is required.}"
candidate_api_revision="${6:?Candidate API revision is required.}"
stable_web_revision="${7:?Stable Web revision is required.}"
candidate_web_revision="${8:?Candidate Web revision is required.}"
stable_worker_image="${9:?Stable Worker image is required.}"
rollback_suffix="${10:?Rollback suffix is required.}"
web_url="${11:?Production Web URL is required.}"
script_directory="$(cd "$(dirname "$0")" && pwd)"
rollback_failed=false

revision_exists() {
  local app_name="$1"
  local revision_name="$2"

  az containerapp revision show \
    --resource-group "$resource_group" \
    --name "$app_name" \
    --revision "$revision_name" \
    --output none \
    --only-show-errors > /dev/null 2>&1
}

restore_traffic() {
  local app_name="$1"
  local stable_revision="$2"
  local candidate_revision="$3"
  local traffic_json

  if revision_exists "$app_name" "$candidate_revision"; then
    if ! bash "${script_directory}/set-container-app-traffic.sh" \
      "$resource_group" \
      "$app_name" \
      "$stable_revision" \
      "$candidate_revision" \
      0
    then
      return 1
    fi
  else
    if ! az containerapp ingress traffic set \
      --resource-group "$resource_group" \
      --name "$app_name" \
      --revision-weight "${stable_revision}=100" \
      --output none \
      --only-show-errors
    then
      return 1
    fi
  fi

  if ! az containerapp revision label add \
    --resource-group "$resource_group" \
    --name "$app_name" \
    --revision "$stable_revision" \
    --label stable \
    --yes \
    --output none \
    --only-show-errors
  then
    return 1
  fi

  if revision_exists "$app_name" "$candidate_revision"; then
    if ! az containerapp revision label add \
      --resource-group "$resource_group" \
      --name "$app_name" \
      --revision "$candidate_revision" \
      --label candidate \
      --yes \
      --output none \
      --only-show-errors
    then
      return 1
    fi
  fi

  if ! traffic_json="$(
    az containerapp show \
      --resource-group "$resource_group" \
      --name "$app_name" \
      --query properties.configuration.ingress.traffic \
      --output json \
      --only-show-errors
  )"
  then
    return 1
  fi

  jq --exit-status \
    --arg stable "$stable_revision" \
    --arg candidate "$candidate_revision" '
      ([.[] | select(
        .revisionName == $stable and
        .weight == 100 and
        .label == "stable"
      )] | length) == 1 and
      ([.[] | select(.weight > 0 and .revisionName != $stable)] | length) == 0 and
      ([.[] | select(.revisionName == $candidate)] | length) <= 1
    ' <<< "$traffic_json" > /dev/null
}

if ! restore_traffic "$api_app" "$stable_api_revision" "$candidate_api_revision"; then
  echo "::error::API traffic rollback failed."
  rollback_failed=true
fi

if ! restore_traffic "$web_app" "$stable_web_revision" "$candidate_web_revision"; then
  echo "::error::Web traffic rollback failed."
  rollback_failed=true
fi

worker_revisions_json=""
if ! worker_revisions_json="$(
  az containerapp revision list \
    --resource-group "$resource_group" \
    --name "$worker_app" \
    --output json \
    --only-show-errors
)"
then
  echo "::error::Worker revisions could not be inspected during rollback."
  rollback_failed=true
fi

active_worker_count="$(
  jq '[.[] | select(.properties.active == true)] | length' \
    <<< "$worker_revisions_json" 2> /dev/null || printf '0'
)"
current_worker_revision="$(
  jq --raw-output '
    [.[] | select(.properties.active == true)] |
    if length == 1 then .[0].name else "" end
  ' <<< "$worker_revisions_json" 2> /dev/null || true
)"
current_worker_image="$(
  jq --raw-output '
    [.[] | select(.properties.active == true)] |
    if length == 1 then .[0].properties.template.containers[0].image else "" end
  ' <<< "$worker_revisions_json" 2> /dev/null || true
)"
restored_worker_revision="$current_worker_revision"

if [[ "$active_worker_count" != "1" || "$current_worker_image" != "$stable_worker_image" ]]; then
  restored_worker_revision="${worker_app}--${rollback_suffix}"

  if revision_exists "$worker_app" "$restored_worker_revision"; then
    if ! az containerapp revision activate \
      --resource-group "$resource_group" \
      --name "$worker_app" \
      --revision "$restored_worker_revision" \
      --output none \
      --only-show-errors
    then
      echo "::error::Existing Worker rollback revision could not be activated."
      rollback_failed=true
    fi
  elif ! az containerapp update \
    --resource-group "$resource_group" \
    --name "$worker_app" \
    --image "$stable_worker_image" \
    --revision-suffix "$rollback_suffix" \
    --output none \
    --only-show-errors
  then
    echo "::error::Worker rollback revision could not be created."
    rollback_failed=true
  fi

  if ! bash "${script_directory}/wait-for-container-app-revision.sh" \
    "$resource_group" \
    "$worker_app" \
    "$restored_worker_revision" \
    "$stable_worker_image" \
    600
  then
    rollback_failed=true
  fi
fi

if [[ -n "$restored_worker_revision" ]]; then
  if worker_revisions_json="$(
    az containerapp revision list \
      --resource-group "$resource_group" \
      --name "$worker_app" \
      --output json \
      --only-show-errors
  )"
  then
    mapfile -t active_worker_revisions < <(
      jq --raw-output \
        '.[] | select(.properties.active == true) | .name' \
        <<< "$worker_revisions_json"
    )

    for revision_name in "${active_worker_revisions[@]}"; do
      if [[ "$revision_name" == "$restored_worker_revision" ]]; then
        continue
      fi

      if ! az containerapp revision deactivate \
        --resource-group "$resource_group" \
        --name "$worker_app" \
        --revision "$revision_name" \
        --output none \
        --only-show-errors
      then
        rollback_failed=true
      fi
    done
  else
    rollback_failed=true
  fi

  if ! az containerapp revision list \
    --resource-group "$resource_group" \
    --name "$worker_app" \
    --output json \
    --only-show-errors |
    jq --exit-status \
      --arg expectedRevision "$restored_worker_revision" \
      --arg expectedImage "$stable_worker_image" '
        [.[] | select(.properties.active == true)] |
        length == 1 and
        .[0].name == $expectedRevision and
        .[0].properties.template.containers[0].image == $expectedImage
      ' > /dev/null
  then
    echo "::error::Worker rollback did not leave exactly one stable revision active."
    rollback_failed=true
  fi
else
  echo "::error::Worker rollback could not identify a stable revision."
  rollback_failed=true
fi

if ! bash "${script_directory}/production-smoke-test.sh" "$web_url" 10; then
  echo "::error::Production smoke test failed after rollback."
  rollback_failed=true
fi

if [[ "$rollback_failed" == "true" ]]; then
  echo "::error::Automatic production rollback did not fully complete."
  exit 1
fi

echo "Automatic production rollback completed successfully."
