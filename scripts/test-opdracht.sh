#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
case "${1:-}" in
    1|2|3|wmo-unit) ;;
    *) echo 'Gebruik: scripts/test-opdracht.sh 1|2|3|wmo-unit' >&2; exit 1 ;;
esac
mkdir -p artifacts
docker compose build unit-tests
docker compose run --rm --no-deps unit-tests "$1"
