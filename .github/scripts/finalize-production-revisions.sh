#!/usr/bin/env bash

set -euo pipefail

resource_group="${1:?Resource group is required.}"
app_name="${2:?Container App name is required.}"
previous_revision="${3:?Previous revision is required.}"
stable_revision="${4:?New stable revision is required.}"

az containerapp revision label add \
  --resource-group "$resource_group" \
  --name "$app_name" \
  --revision "$previous_revision" \
  --label previous \
  --yes \
  --output none \
  --only-show-errors

az containerapp revision label add \
  --resource-group "$resource_group" \
  --name "$app_name" \
  --revision "$stable_revision" \
  --label stable \
  --yes \
  --output none \
  --only-show-errors

traffic_json="$(
  az containerapp show \
    --resource-group "$resource_group" \
    --name "$app_name" \
    --query properties.configuration.ingress.traffic \
    --output json \
    --only-show-errors
)"

jq --exit-status \
  --arg previous "$previous_revision" \
  --arg stable "$stable_revision" '
    ([.[] | select(
      .revisionName == $previous and
      .weight == 0 and
      .label == "previous"
    )] | length) == 1 and
    ([.[] | select(
      .revisionName == $stable and
      .weight == 100 and
      .label == "stable"
    )] | length) == 1
  ' <<< "$traffic_json" > /dev/null

echo "${app_name}: ${stable_revision} is stable and ${previous_revision} is retained for rollback."
