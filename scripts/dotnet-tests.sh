#!/bin/sh
set -eu
kind=${1:-unit}
if [ "$kind" = integration ]; then
    dotnet test opdrachten/04-wmo/tests/Integration --no-restore -c Release --logger 'trx;LogFileName=integration.trx' --results-directory /artifacts/integration
elif [ "$kind" = 1 ]; then
    dotnet test opdrachten/01-fizzbuzz/tests --no-restore -c Release --logger trx --results-directory /artifacts/opdracht-1
elif [ "$kind" = 2 ]; then
    dotnet test opdrachten/02-verzamelingen/tests --no-restore -c Release --logger trx --results-directory /artifacts/opdracht-2
elif [ "$kind" = 3 ]; then
    dotnet test opdrachten/03-volledige-weken/tests --no-restore -c Release --logger trx --results-directory /artifacts/opdracht-3
elif [ "$kind" = wmo-unit ]; then
    dotnet test opdrachten/04-wmo/tests/Unit --no-restore -c Release --logger trx --results-directory /artifacts/wmo-unit
else
    for project in opdrachten/01-fizzbuzz/tests opdrachten/02-verzamelingen/tests opdrachten/03-volledige-weken/tests opdrachten/04-wmo/tests/Unit; do
        dotnet test "$project" --no-restore -c Release --logger trx --results-directory /artifacts/unit
    done
fi
