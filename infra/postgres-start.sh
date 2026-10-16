#!/bin/sh
set -eu
cp /certs/postgres/server.key /tmp/server.key
chown postgres:postgres /tmp/server.key
chmod 600 /tmp/server.key
cp /bootstrap/postgres-init.sh /docker-entrypoint-initdb.d/01-init.sh
chmod 644 /docker-entrypoint-initdb.d/01-init.sh
# Alle TCP-verbindingen vereisen TLS; lokale socket is uitsluitend voor bootstrap/health.
exec docker-entrypoint.sh postgres -c ssl=on -c ssl_cert_file=/certs/postgres/server.crt -c ssl_key_file=/tmp/server.key -c hba_file=/etc/postgresql/pg_hba.conf
