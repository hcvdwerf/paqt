#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
# Bootstrap eerst uitvoeren voorkomt dat Compose nog niet bestaande bestandsmounts als mappen aanmaakt.
mkdir -p .local artifacts
chmod 700 .local
docker compose run --rm --no-deps --build init
docker compose up --build -d postgres keycloak migrate api kong
# DB-less Kong leest declaratieve configuratie bij opstarten; ook een bestaande gateway vernieuwen.
docker compose restart kong
docker compose ps
printf '\nAPI via Kong: https://localhost:8443\nKeycloak: https://localhost:8444\nCA: .local/certs/ca.crt\n'
