#!/bin/sh
set -eu
kind=${1:-unit}
if [ "$kind" = integration ]; then
    dotnet test opdrachten/04-wmo/tests/Integration --no-restore -c Release --logger 'trx;LogFileName=integration.trx' --results-directory /artifacts/integration
else
    for project in opdrachten/01-fizzbuzz/tests opdrachten/02-verzamelingen/tests opdrachten/03-volledige-weken/tests opdrachten/04-wmo/tests/Unit; do
        dotnet test "$project" --no-restore -c Release --logger trx --results-directory /artifacts/unit
    done
fi
