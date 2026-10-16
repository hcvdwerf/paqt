"""Maak uitsluitend lokale demo-secrets en certificaten; log nooit hun inhoud."""
from pathlib import Path
import json
import os
import secrets
import subprocess

root = Path('/local')
root.mkdir(exist_ok=True)
for name in ('certs', 'secrets', 'keycloak', 'kong', 'postgres', 'ca', 'client-secrets'):
    (root / name).mkdir(exist_ok=True)

def command(*args):
    subprocess.run(args, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

def secret(name):
    path = root / 'secrets' / name
    if not path.exists():
        path.write_text(secrets.token_hex(24))
    # Alleen lokaal, alleen betreffende containers krijgen deze map als read-only mount.
    path.chmod(0o444)
    return path.read_text().strip()

for name in ('postgres-password', 'wmo-owner-password', 'wmo-password', 'keycloak-password', 'keycloak-admin-password'):
    secret(name)
ca = root / 'ca'
if not (ca / 'ca.key').exists():
    command('openssl', 'req', '-x509', '-newkey', 'rsa:3072', '-nodes', '-days', '365', '-subj', '/CN=PAQT local development CA',
            '-keyout', str(ca/'ca.key'), '-out', str(root/'certs/ca.crt'))
    (ca/'ca.key').chmod(0o600)
for name in ('api', 'keycloak', 'kong', 'postgres'):
    folder = root / 'certs' / name
    folder.mkdir(exist_ok=True)
    if not (folder/'server.key').exists():
        command('openssl', 'req', '-newkey', 'rsa:2048', '-nodes', '-subj', f'/CN={name}',
                '-keyout', str(folder/'server.key'), '-out', str(folder/'server.csr'))
        (folder/'extensions').write_text(f'subjectAltName=DNS:{name},DNS:localhost,IP:127.0.0.1\nextendedKeyUsage=serverAuth\n')
        command('openssl', 'x509', '-req', '-in', str(folder/'server.csr'), '-CA', str(root/'certs/ca.crt'),
                '-CAkey', str(ca/'ca.key'), '-CAcreateserial', '-days', '90', '-extfile', str(folder/'extensions'), '-out', str(folder/'server.crt'))
        (folder/'server.csr').unlink()
        (folder/'extensions').unlink()
    (folder/'server.key').chmod(0o444)

clients = []
for name, role, company in [('callcenter', 'callcenter', None), ('carrier-a', 'carrier', '10000000-0000-0000-0000-000000000001'),
                            ('carrier-b', 'carrier', '10000000-0000-0000-0000-000000000002'), ('renewal', 'renewal', None),
                            ('unauthorized', 'none', None), ('bad-audience', 'callcenter', None), ('carrier-missing-company', 'carrier', None)]:
    mappers = [{'name': 'role', 'protocol': 'openid-connect', 'protocolMapper': 'oidc-hardcoded-claim-mapper',
                'config': {'claim.name': 'roles', 'claim.value': role, 'jsonType.label': 'String', 'access.token.claim': 'true'}}]
    if name != 'bad-audience':
        mappers.append({'name': 'audience', 'protocol': 'openid-connect', 'protocolMapper': 'oidc-audience-mapper',
                        'config': {'included.custom.audience': 'wmo-api', 'access.token.claim': 'true'}})
    if company:
        mappers.append({'name': 'company', 'protocol': 'openid-connect', 'protocolMapper': 'oidc-hardcoded-claim-mapper',
                        'config': {'claim.name': 'taxi_company_id', 'claim.value': company, 'jsonType.label': 'String', 'access.token.claim': 'true'}})
    clients.append({'clientId': name, 'secret': secret(name+'-secret'), 'protocol': 'openid-connect',
                    'publicClient': False, 'serviceAccountsEnabled': True, 'standardFlowEnabled': False,
                    'directAccessGrantsEnabled': False, 'fullScopeAllowed': False, 'protocolMappers': mappers})
for client in clients:
    path = root/'client-secrets'/(client['clientId']+'-secret')
    path.write_text(client['secret'])
    path.chmod(0o444)
realm = {'realm': 'paqt', 'enabled': True, 'sslRequired': 'all', 'accessTokenLifespan': 300,
         'registrationAllowed': False, 'clients': clients}
(root/'keycloak/paqt-realm.json').write_text(json.dumps(realm))
(root/'keycloak/paqt-realm.json').chmod(0o444)
ca_id = '40000000-0000-0000-0000-000000000001'
kong = {
    '_format_version': '3.0',
    'ca_certificates': [{'id': ca_id, 'cert': (root/'certs/ca.crt').read_text()}],
    'services': [{'name': 'wmo', 'url': 'https://api:8443', 'tls_verify': True, 'ca_certificates': [ca_id],
                  'connect_timeout': 5000, 'read_timeout': 20000, 'write_timeout': 20000, 'retries': 0,
                  'routes': [{'name': 'wmo-api', 'paths': ['/api/v1', '/openapi', '/swagger', '/health'], 'strip_path': False, 'protocols': ['https']}],
                  'plugins': [{'name': 'rate-limiting', 'config': {'minute': 600, 'policy': 'local', 'limit_by': 'ip'}},
                              {'name': 'request-size-limiting', 'config': {'allowed_payload_size': 64, 'size_unit': 'kilobytes'}}]}]
}
(root/'kong/kong.json').write_text(json.dumps(kong))
print('Lokale certificaten, secrets, realm en Kong-configuratie zijn gereed.')
