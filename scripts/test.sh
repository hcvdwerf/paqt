#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
mkdir -p .local artifacts
chmod 700 .local
docker compose build init api unit-tests playwright
docker compose run --rm --no-deps init
docker compose run --rm --no-deps unit-tests
stack() { docker compose -p paqt-tests -f compose.yml -f compose.e2e.yml "$@"; }
# Dit project en zijn volumes zijn uitsluitend wegwerpbare testdata.
stack down --volumes --remove-orphans
trap 'stack down --volumes --remove-orphans' EXIT
stack up -d postgres keycloak migrate api kong
stack run --rm integration-tests
stack run --rm playwright
