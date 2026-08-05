#!/usr/bin/env bash

set -euo pipefail

web_url="${1:?Web URL is required.}"
web_url="${web_url%/}"

if [[ ! "$web_url" =~ ^https:// ]]; then
  echo "::error::Smoke tests require an HTTPS URL."
  exit 1
fi

work_directory="$(mktemp -d)"
trap 'rm -rf "$work_directory"' EXIT

curl_options=(
  --fail
  --silent
  --show-error
  --location
  --retry 18
  --retry-all-errors
  --retry-delay 10
  --connect-timeout 10
  --max-time 20
  --proto '=https'
  --tlsv1.2
)

curl "${curl_options[@]}" "${web_url}/health/live" \
  --output "${work_directory}/live.txt"
curl "${curl_options[@]}" "${web_url}/health/ready" \
  --output "${work_directory}/ready.txt"
curl "${curl_options[@]}" "${web_url}/" \
  --dump-header "${work_directory}/headers.txt" \
  --output "${work_directory}/index.html"
curl "${curl_options[@]}" "${web_url}/api/analyses" \
  --cookie-jar "${work_directory}/cookies.txt" \
  --output "${work_directory}/analyses.json"

grep --quiet --fixed-strings 'Carlens AI' "${work_directory}/index.html"
grep --quiet --ignore-case '^strict-transport-security:' \
  "${work_directory}/headers.txt"
jq --exit-status 'type == "array"' \
  "${work_directory}/analyses.json" > /dev/null

echo "Staging live, readiness, UI, security header and API gateway checks passed."
