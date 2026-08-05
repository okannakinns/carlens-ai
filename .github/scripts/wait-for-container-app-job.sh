#!/usr/bin/env bash

set -euo pipefail

resource_group="${1:?Resource group is required.}"
job_name="${2:?Job name is required.}"
execution_name="${3:?Execution name is required.}"
timeout_seconds="${4:-1200}"
deadline=$((SECONDS + timeout_seconds))

while (( SECONDS < deadline )); do
  status="$(
    az containerapp job execution list \
      --resource-group "$resource_group" \
      --name "$job_name" \
      --query "[?name=='${execution_name}'].properties.status | [0]" \
      --output tsv \
      --only-show-errors
  )"

  case "$status" in
    Succeeded)
      echo "Migration execution ${execution_name} succeeded."
      exit 0
      ;;
    Failed|Stopped|Degraded)
      echo "::error::Migration execution ${execution_name} finished with status ${status}."
      az containerapp job execution list \
        --resource-group "$resource_group" \
        --name "$job_name" \
        --output table \
        --only-show-errors
      exit 1
      ;;
    "")
      echo "Waiting for migration execution ${execution_name} to appear."
      ;;
    *)
      echo "Migration execution ${execution_name}: ${status}."
      ;;
  esac

  sleep 10
done

az containerapp job stop \
  --resource-group "$resource_group" \
  --name "$job_name" \
  --job-execution-name "$execution_name" \
  --only-show-errors || true
echo "::error::Migration execution ${execution_name} exceeded ${timeout_seconds} seconds."
exit 1
