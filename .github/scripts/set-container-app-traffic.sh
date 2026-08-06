#!/usr/bin/env bash

set -euo pipefail

resource_group="${1:?Resource group is required.}"
app_name="${2:?Container App name is required.}"
stable_revision="${3:?Stable revision is required.}"
candidate_revision="${4:?Candidate revision is required.}"
candidate_weight="${5:?Candidate weight is required.}"

if [[ "$stable_revision" == "$candidate_revision" ]]; then
  echo "::error::Stable and candidate revisions must be different."
  exit 1
fi

if [[ ! "$candidate_weight" =~ ^[0-9]+$ ]] || \
   (( candidate_weight < 0 || candidate_weight > 100 ))
then
  echo "::error::Candidate weight must be an integer from 0 through 100."
  exit 1
fi

stable_weight=$((100 - candidate_weight))

az containerapp ingress traffic set \
  --resource-group "$resource_group" \
  --name "$app_name" \
  --revision-weight \
    "${stable_revision}=${stable_weight}" \
    "${candidate_revision}=${candidate_weight}" \
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
  --arg stable "$stable_revision" \
  --arg candidate "$candidate_revision" \
  --argjson stableWeight "$stable_weight" \
  --argjson candidateWeight "$candidate_weight" '
    ([.[].weight] | add) == 100 and
    ([.[] | select(.revisionName == $stable and .weight == $stableWeight)] | length) == 1 and
    ([.[] | select(.revisionName == $candidate and .weight == $candidateWeight)] | length) == 1 and
    ([.[] | select(
      .revisionName != $stable and
      .revisionName != $candidate and
      .weight > 0
    )] | length) == 0
  ' <<< "$traffic_json" > /dev/null

echo "${app_name}: stable=${stable_weight}%, candidate=${candidate_weight}%."
