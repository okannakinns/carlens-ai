#!/usr/bin/env bash

set -euo pipefail

destination="${1:-${RUNNER_TEMP:-/tmp}/bicep}"
version="${BICEP_VERSION:-v0.46.1}"
expected_sha256="${BICEP_SHA256:-3e011d629ea4311b7a7dd8f0040ab2b1a072ea4ff5d02cb75e0e55a9a6703fb9}"

mkdir -p "$(dirname "$destination")"
curl --fail --location --silent --show-error \
  "https://github.com/Azure/bicep/releases/download/${version}/bicep-linux-x64" \
  --output "$destination"
echo "${expected_sha256}  ${destination}" | sha256sum --check
chmod 0555 "$destination"
