#!/usr/bin/env bash

set -euo pipefail

repository="${1:?Repository is required.}"
commit_sha="${2:?Commit SHA is required.}"
api_url="${GITHUB_API_URL:-https://api.github.com}"

if [[ ! "$repository" =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]]; then
  echo "::error::Repository must use the owner/name format."
  exit 1
fi

if [[ ! "$commit_sha" =~ ^[0-9a-f]{40}$ ]]; then
  echo "::error::A full lowercase 40-character commit SHA is required."
  exit 1
fi

if [[ -z "${GH_TOKEN:-}" ]]; then
  echo "::error::GH_TOKEN is required to verify staging deployments."
  exit 1
fi

work_directory="$(mktemp -d)"
trap 'rm -rf "$work_directory"' EXIT

curl_options=(
  --fail
  --silent
  --show-error
  --retry 3
  --retry-all-errors
  --header "Accept: application/vnd.github+json"
  --header "Authorization: Bearer ${GH_TOKEN}"
  --header "X-GitHub-Api-Version: 2022-11-28"
)

deployments_file="${work_directory}/deployments.json"
curl "${curl_options[@]}" \
  "${api_url}/repos/${repository}/deployments?environment=staging&sha=${commit_sha}&per_page=100" \
  --output "$deployments_file"

mapfile -t deployment_ids < <(
  jq --raw-output 'sort_by(.id) | reverse | .[].id' "$deployments_file"
)

for deployment_id in "${deployment_ids[@]}"; do
  statuses_file="${work_directory}/statuses-${deployment_id}.json"
  curl "${curl_options[@]}" \
    "${api_url}/repos/${repository}/deployments/${deployment_id}/statuses?per_page=100" \
    --output "$statuses_file"

  latest_state="$(
    jq --raw-output 'if length == 0 then "" else max_by(.id).state end' \
      "$statuses_file"
  )"
  has_success="$(jq --raw-output 'any(.state == "success")' "$statuses_file")"

  if [[ "$latest_state" == "success" || \
        ( "$latest_state" == "inactive" && "$has_success" == "true" ) ]]
  then
    echo "Verified staging deployment ${deployment_id} for ${commit_sha}."
    exit 0
  fi
done

echo "::error::Commit ${commit_sha} has no successful staging deployment."
exit 1
