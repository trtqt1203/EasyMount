"""Opt-in five-source VFS acceptance; only UUID roots created by the companion tool.

No recursive cleanup or mount management. Failed fixtures/cache are retained.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import time

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--engine', required=True)
p.add_argument('--config', required=True)
p.add_argument('--profiles', required=True)
p.add_argument('--report', required=True)
a = p.parse_args()
profiles = json.loads(Path(a.profiles).read_text(encoding='utf-8-sig'))
if len(profiles) != 1 or profiles[0].get('ReadOnly'):
    p.error('Expected one temporary writable pool profile.')
profile = profiles[0]
sources = profile.get('Sources', [])
roots = [s['Remote'] for s in sources]
if len(set(roots)) != 5 or any(s['LimitBytes'] != 1572864 for s in sources):
    p.error('Expected five distinct roots with the controlled 1.5 MiB limits.')
if any(':EasyMount-VFS-root-' not in root or '/' in root or '\\' in root for root in roots):
    p.error('Refusing roots outside generated acceptance folders.')
mounted = Path(profile['Letter'] + '\\')
record = {'status': 'running', 'sourceCount': 5, 'fixtureBytes': 1048576,
          'limitBytesPerSource': 1572864, 'checks': []}


def cloud(operation, root):
    return subprocess.run([a.engine, operation, root, '--config', a.config,
                           '--retries', '1', '--low-level-retries', '1',
                           '--contimeout', '10s', '--timeout', '20s'],
                          capture_output=True, timeout=35)


def eventually(predicate):
    deadline = time.monotonic() + 120
    while time.monotonic() < deadline:
        if predicate():
            return
        time.sleep(3)
    raise RuntimeError('Independent cloud verification did not complete.')


def save():
    Path(a.report).write_text(json.dumps([record], indent=2), encoding='utf-8')


def check(ok, name):
    if not ok:
        raise RuntimeError(name)
    record['checks'].append(name)
    save()


names = ['Kiểm thử ưu tiên English-' + str(i + 1) + '.bin' for i in range(5)]
try:
    for i, root in enumerate(roots):
        data = hashlib.sha256((root + names[i]).encode()).digest() * 32768
        expected = hashlib.sha256(data).digest()
        path = mounted / names[i]
        path.write_bytes(data)
        check(hashlib.sha256(path.read_bytes()).digest() == expected,
              'mounted-write-read-sha256-source-' + str(i + 1))

        def uploaded():
            result = cloud('cat', root + '/' + names[i])
            return result.returncode == 0 and hashlib.sha256(result.stdout).digest() == expected

        eventually(uploaded)
        check(True, 'independent-cloud-priority-whole-file-source-' + str(i + 1))

    # Each source must contain only its assigned file: no splitting/duplicates.
    for i, root in enumerate(roots):
        result = cloud('lsjson', root)
        entries = json.loads(result.stdout) if result.returncode == 0 else []
        check(result.returncode == 0 and len(entries) == 1 and
              entries[0]['Name'] == names[i] and entries[0]['Size'] == 1048576,
              'only-assigned-file-on-source-' + str(i + 1))
    for name in names:
        (mounted / name).unlink()
    for i, root in enumerate(roots):
        def empty():
            result = cloud('lsjson', root)
            return result.returncode == 0 and json.loads(result.stdout) == []
        eventually(empty)
        check(True, 'mounted-deletion-confirmed-source-' + str(i + 1))
    record['status'] = 'passed'
except (OSError, RuntimeError, subprocess.TimeoutExpired, ValueError) as error:
    record['status'] = 'failed'
    record['failure'] = str(error) if isinstance(error, RuntimeError) else type(error).__name__
save()
print('Five-source VFS:', record['status'], len(record['checks']), 'checks', flush=True)
raise SystemExit(0 if record['status'] == 'passed' else 1)
