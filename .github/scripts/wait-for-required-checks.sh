#!/usr/bin/env bash

set -euo pipefail

repository="${1:?Repository is required.}"
commit_sha="${2:?Commit SHA is required.}"
timeout_seconds="${3:-1800}"
api_url="${GITHUB_API_URL:-https://api.github.com}"

if [[ ! "$commit_sha" =~ ^[0-9a-f]{40}$ ]]; then
  echo "::error::Expected a full 40-character commit SHA."
  exit 1
fi

if [[ -z "${GH_TOKEN:-}" ]]; then
  echo "::error::GH_TOKEN is required to inspect check runs."
  exit 1
fi

required_checks=(
  ".NET build and test"
  "React build"
  "Analyze source code"
  "Testcontainers integration tests"
  "Container security"
  "Bicep validation"
)

deadline=$((SECONDS + timeout_seconds))
response_file="$(mktemp)"
trap 'rm -f "$response_file"' EXIT

while (( SECONDS < deadline )); do
  curl --fail --silent --show-error \
    --retry 3 \
    --retry-all-errors \
    --header "Accept: application/vnd.github+json" \
    --header "Authorization: Bearer ${GH_TOKEN}" \
    --header "X-GitHub-Api-Version: 2022-11-28" \
    "${api_url}/repos/${repository}/commits/${commit_sha}/check-runs?per_page=100" \
    --output "$response_file"

  pending_checks=()

  for check_name in "${required_checks[@]}"; do
    check_state="$(
      jq --raw-output --arg name "$check_name" '
        [.check_runs[] | select(.name == $name)]
        | if length == 0 then "missing|"
          else max_by(.id) | "\(.status)|\(.conclusion // "")"
          end
      ' "$response_file"
    )"
    status="${check_state%%|*}"
    conclusion="${check_state#*|}"

    if [[ "$status" == "completed" && "$conclusion" == "success" ]]; then
      continue
    fi

    if [[ "$status" == "completed" ]]; then
      echo "::error::Required check '${check_name}' completed with '${conclusion}'."
      exit 1
    fi

    pending_checks+=("${check_name} (${status})")
  done

  if (( ${#pending_checks[@]} == 0 )); then
    echo "All required checks passed for ${commit_sha}."
    exit 0
  fi

  printf 'Waiting for required checks: %s\n' "$(IFS=', '; echo "${pending_checks[*]}")"
  sleep 15
done

echo "::error::Timed out waiting for required checks on ${commit_sha}."
exit 1
