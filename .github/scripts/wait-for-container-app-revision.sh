#!/usr/bin/env bash

set -euo pipefail

resource_group="${1:?Resource group is required.}"
app_name="${2:?Container App name is required.}"
revision_name="${3:?Revision name is required.}"
expected_image="${4:?Expected image is required.}"
timeout_seconds="${5:-600}"
deadline=$((SECONDS + timeout_seconds))

while (( SECONDS < deadline )); do
  if revision_json="$(
    az containerapp revision show \
      --resource-group "$resource_group" \
      --name "$app_name" \
      --revision "$revision_name" \
      --output json \
      --only-show-errors 2> /dev/null
  )"
  then
    provisioning_state="$(jq --raw-output '.properties.provisioningState // ""' <<< "$revision_json")"
    health_state="$(jq --raw-output '.properties.healthState // ""' <<< "$revision_json")"
    active="$(jq --raw-output '.properties.active // false' <<< "$revision_json")"
    actual_image="$(jq --raw-output '.properties.template.containers[0].image // ""' <<< "$revision_json")"

    if [[ "$actual_image" != "$expected_image" ]]; then
      echo "::error::Revision ${revision_name} references an unexpected image."
      exit 1
    fi

    case "$provisioning_state" in
      Failed|ProvisioningFailed|Deprovisioned)
        echo "::error::Revision ${revision_name} entered ${provisioning_state}."
        exit 1
        ;;
    esac

    if [[ ( "$provisioning_state" == "Provisioned" || "$provisioning_state" == "Succeeded" ) && \
          "$health_state" == "Healthy" && "$active" == "true" ]]
    then
      echo "Revision ${revision_name} is healthy and ready."
      exit 0
    fi

    echo "Revision ${revision_name}: provisioning=${provisioning_state}, health=${health_state}, active=${active}."
  else
    echo "Waiting for revision ${revision_name} to appear."
  fi

  sleep 10
done

echo "::error::Revision ${revision_name} did not become ready within ${timeout_seconds} seconds."
exit 1
