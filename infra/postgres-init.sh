#!/bin/sh
set -eu
# De waarden zijn hexadecimaal gegenereerd; er wordt geen vrij ingevoerde SQL geïnterpoleerd.
owner=$(cat /secrets/wmo-owner-password)
runtime=$(cat /secrets/wmo-password)
keycloak=$(cat /secrets/keycloak-password)
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres <<SQL
CREATE ROLE wmo_owner LOGIN PASSWORD '$owner';
CREATE ROLE wmo LOGIN PASSWORD '$runtime';
CREATE ROLE keycloak LOGIN PASSWORD '$keycloak';
CREATE DATABASE wmo OWNER wmo_owner;
CREATE DATABASE wmo_test OWNER wmo_owner;
CREATE DATABASE keycloak OWNER keycloak;
REVOKE ALL ON DATABASE wmo FROM PUBLIC;
REVOKE ALL ON DATABASE wmo_test FROM PUBLIC;
GRANT CONNECT ON DATABASE wmo TO wmo;
GRANT CONNECT ON DATABASE wmo_test TO wmo;
SQL
for database in wmo wmo_test; do
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$database" -c 'REVOKE CREATE ON SCHEMA public FROM PUBLIC; GRANT USAGE ON SCHEMA public TO wmo;'
done
