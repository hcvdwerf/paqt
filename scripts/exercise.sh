#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
case "${1:-}" in
  1) project=opdrachten/01-fizzbuzz/src ;;
  2) project=opdrachten/02-verzamelingen/src ;;
  3) project=opdrachten/03-volledige-weken/src ;;
  *) echo 'Gebruik: scripts/exercise.sh 1|2|3 [argumenten]' >&2; exit 1 ;;
esac
shift
docker run --rm -v "$PWD:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0@sha256:0d99ebf1ccad15037b5c98c9fe8979d6b8dbac23812feae2277a0b277b5c5ffa dotnet run --project "$project" -- "$@"
