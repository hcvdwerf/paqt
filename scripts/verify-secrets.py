#!/usr/bin/env python3
"""Gerichte lokale lekcontrole; geen vervanging voor een volledige SAST/secret-scanner."""
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parent.parent
ignored = {'.git', '.local', '.structurizr', 'node_modules', 'bin', 'obj'}
secret_dir = root / '.local' / 'secrets'
secrets = [p.read_bytes().strip() for p in secret_dir.glob('*') if p.is_file()]
private_key = re.compile(b'-----BEGIN (?:RSA |EC )?PRIVATE' + b' KEY-----')
bearer_token = re.compile(rb'eyJ[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}')
findings = []
count = 0
for path in root.rglob('*'):
    if not path.is_file() or any(part in ignored for part in path.relative_to(root).parts):
        continue
    data = path.read_bytes()
    count += 1
    if private_key.search(data) or bearer_token.search(data) or any(value and value in data for value in secrets):
        findings.append(str(path.relative_to(root)))
if findings:
    print('Mogelijke secretlekken (inhoud geredigeerd):', *findings, sep='\n', file=sys.stderr)
    sys.exit(1)
print(f'{count} bron-/rapportbestanden gecontroleerd: geen lokale secretwaarden, private sleutels of JWT-patronen aangetroffen.')
