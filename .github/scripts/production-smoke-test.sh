#!/usr/bin/env bash

set -euo pipefail

web_url="${1:?Web URL is required.}"
request_count="${2:-20}"
web_url="${web_url%/}"

if [[ ! "$request_count" =~ ^[0-9]+$ ]] || \
   (( request_count < 1 || request_count > 100 ))
then
  echo "::error::Request count must be an integer from 1 through 100."
  exit 1
fi

bash "$(dirname "$0")/smoke-test.sh" "$web_url"

work_directory="$(mktemp -d)"
trap 'rm -rf "$work_directory"' EXIT

curl_options=(
  --fail
  --silent
  --show-error
  --connect-timeout 10
  --max-time 20
  --proto '=https'
  --tlsv1.2
  --header 'Cache-Control: no-cache'
)

for request_number in $(seq 1 "$request_count"); do
  curl "${curl_options[@]}" \
    "${web_url}/health/live?rollout-probe=${request_number}" \
    --output /dev/null
  curl "${curl_options[@]}" \
    "${web_url}/api/analyses?rollout-probe=${request_number}" \
    --output "${work_directory}/analyses-${request_number}.json"
  jq --exit-status 'type == "array"' \
    "${work_directory}/analyses-${request_number}.json" > /dev/null
done

echo "Production smoke test passed across ${request_count} rollout probes."
