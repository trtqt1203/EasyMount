"""Verify generated fixture writes through existing Windows cloud mounts.

Never connects/disconnects drives or modifies an existing cloud file. Failed
fixtures remain in their unique directories; do not delete pending VFS cache.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import time
import uuid

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--engine', required=True)
p.add_argument('--config', required=True)
p.add_argument('--profiles', required=True)
p.add_argument('--report', required=True)
p.add_argument('--fixture-mib', type=int, choices=[1, 128], default=1)
a = p.parse_args()
profiles = json.loads(Path(a.profiles).read_text(encoding='utf-8-sig'))
results = []


def cloud(*command):
    return subprocess.run([a.engine, *command, '--config', a.config, '--retries', '1',
                           '--low-level-retries', '1', '--contimeout', '10s', '--timeout', '20s'],
                          capture_output=True, timeout=180 if a.fixture_mib > 1 else 35)


def eventually(predicate, description):
    end = time.monotonic() + (300 if a.fixture_mib > 1 else 120)
    while time.monotonic() < end:
        if predicate():
            return
        time.sleep(3)
    raise RuntimeError(description)


for profile in profiles:
    if profile.get('Sources') or profile.get('ReadOnly'):
        continue
    root = Path(profile['Letter'] + '\\')
    if not root.is_dir():
        continue
    folder = 'EasyMount-VFS-test-' + uuid.uuid4().hex
    name = 'Kiểm thử English.bin'
    local_dir = root / folder
    remote_root = profile['Remote'].rstrip('/')
    remote_dir = remote_root + ('' if remote_root.endswith(':') else '/') + folder
    remote_file = remote_dir + '/' + name
    record = {'letter': profile['Letter'], 'fixtureFolder': folder, 'fixtureBytes': a.fixture_mib * 1048576, 'checks': []}
    try:
        data = hashlib.sha256(folder.encode()).digest() * (32768 * a.fixture_mib)
        expected = hashlib.sha256(data).hexdigest()
        local_dir.mkdir()
        local_file = local_dir / name
        local_file.write_bytes(data)
        if hashlib.sha256(local_file.read_bytes()).hexdigest() != expected:
            raise RuntimeError('mounted local integrity')
        record['checks'].append('mounted-write-read-sha256')

        def uploaded():
            result = cloud('cat', remote_file)
            return result.returncode == 0 and hashlib.sha256(result.stdout).hexdigest() == expected

        eventually(uploaded, 'cloud upload/hash did not complete within deadline')
        record['checks'].append('independent-cloud-download-sha256')
        local_file.unlink()

        def deleted():
            result = cloud('lsjson', remote_dir)
            return result.returncode == 0 and json.loads(result.stdout) == []

        eventually(deleted, 'mounted deletion was not visible on cloud')
        record['checks'].append('mounted-delete-confirmed-on-cloud')
        local_dir.rmdir()  # never recursive; generated directory must be empty
        record['status'] = 'passed'
    except (OSError, RuntimeError, subprocess.TimeoutExpired) as error:
        record['status'] = 'failed'
        record['error'] = str(error) if isinstance(error, RuntimeError) else type(error).__name__
    results.append(record)
    Path(a.report).write_text(json.dumps(results, indent=2), encoding='utf-8')
    print(f"{profile['Letter']} {record['status']}: {len(record['checks'])} checks", flush=True)
if not results:
    raise SystemExit('No existing writable non-pool cloud mount was available.')
raise SystemExit(0 if all(r['status'] == 'passed' for r in results) else 1)
