"""Exercise the exact bundled engine against disposable local account fixtures."""
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import re

engine = Path(sys.argv[1]).resolve()
passed = 0

def check(condition, description):
    global passed
    assert condition, description
    passed += 1
    print('PASS', description, flush=True)

with tempfile.TemporaryDirectory(prefix='EasyMount-pool-') as tmp:
    root = Path(tmp)
    targets = [root / 'first cloud', root / 'second cloud', root / 'third cloud']
    for target in targets:
        target.mkdir()
    config = root / 'accounts.conf'
    config.write_text('\n'.join(f'[account {i}]\ntype = alias\nremote = {target.as_posix()}\n' for i, target in enumerate(targets)), encoding='utf-8')
    env = os.environ.copy()
    env.update(RCLONE_UNION_UPSTREAMS='"account 0:" "account 1:" "account 2:"',
               RCLONE_UNION_CREATE_POLICY='ff', RCLONE_UNION_ACTION_POLICY='epall',
               RCLONE_UNION_SEARCH_POLICY='epall', RCLONE_UNION_EASYMOUNT_PRIORITY='true',
               RCLONE_UNION_EASYMOUNT_LIMITS='8,8,8')

    def run(*args, ok=True):
        plan = root / 'pool plan.json'
        plan.write_text(json.dumps([{'Remote': remote, 'LimitBytes': int(limit)} for remote, limit in zip(re.findall(r'"([^"]*)"', env['RCLONE_UNION_UPSTREAMS']), env['RCLONE_UNION_EASYMOUNT_LIMITS'].split(','))]), encoding='utf-8')
        runtime_env = {k: v for k, v in env.items() if k not in ('RCLONE_UNION_UPSTREAMS', 'RCLONE_UNION_EASYMOUNT_LIMITS', 'RCLONE_UNION_EASYMOUNT_PRIORITY', 'RCLONE_UNION_CREATE_POLICY', 'RCLONE_UNION_SEARCH_POLICY', 'RCLONE_UNION_ACTION_POLICY')}
        runtime_env['RCLONE_CONFIG_EASYMOUNT_TEST_POOL_TYPE'] = 'union'
        runtime_env['RCLONE_CONFIG_EASYMOUNT_TEST_POOL_EASYMOUNT_PLAN'] = str(plan)
        result = subprocess.run([str(engine), '--config', str(config), '--retries', '1', '--low-level-retries', '1', *args], env=runtime_env, capture_output=True, text=True, encoding='utf-8', timeout=60)
        if ok and result.returncode:
            raise AssertionError(result.stderr)
        return result

    def upload(name, content, ok=True):
        src = root / 'upload.fixture'
        src.write_bytes(content)
        return run('copyto', str(src), 'easymount_test_pool:' + name, '--ignore-times', ok=ok)

    check('easymount.1' in run('version').stdout, 'the modified priority engine is the tested executable')
    check('cmount' in run('version').stdout, 'Windows mount support is compiled in')
    upload('a.txt', b'aaaa')
    check((targets[0] / 'a.txt').read_bytes() == b'aaaa' and not (targets[1] / 'a.txt').exists(), 'first file uses the highest-priority cloud')
    upload('b.txt', b'bbbb')
    check((targets[0] / 'b.txt').exists(), 'highest-priority source is filled before moving on')
    upload('c.txt', b'cccc')
    check((targets[1] / 'c.txt').exists() and not (targets[0] / 'c.txt').exists(), 'full first source spills to second source')
    upload('nested space/d.txt', b'dddddd')
    check((targets[2] / 'nested space/d.txt').read_bytes() == b'dddddd', 'file too large for remaining space moves intact to the next source')
    names = run('lsf', 'easymount_test_pool:', '--recursive').stdout
    check(all(name in names for name in ['a.txt', 'b.txt', 'c.txt', 'nested space/d.txt']), 'one union view lists files across every source')
    upload('too-big.txt', b'x' * 9, ok=False)
    check(not any((target / 'too-big.txt').exists() for target in targets), 'no fitting source rejects the write without creating a partial file')
    upload('e.txt', b'eeee')
    upload('f.txt', b'ff')
    full = upload('full.txt', b'z', ok=False)
    check(full.returncode != 0 and 'no source has enough free space' in full.stderr, 'all-full result is explicit and never reports a successful upload')
    upload('a.txt', b'AAAA')
    check((targets[0] / 'a.txt').read_bytes() == b'AAAA' and not (targets[1] / 'a.txt').exists(), 'updating an existing file keeps it on its original account')
    env['RCLONE_UNION_EASYMOUNT_LIMITS'] = '16,16,16'
    env['RCLONE_UNION_UPSTREAMS'] = '"account 2:" "account 1:" "account 0:"'
    upload('priority.txt', b'new')
    check((targets[2] / 'priority.txt').exists(), 'changing configured priority changes the next new-file destination')
    (targets[0] / 'duplicate.txt').write_text('first')
    (targets[2] / 'duplicate.txt').write_text('third')
    check(run('cat', 'easymount_test_pool:duplicate.txt').stdout == 'third', 'duplicate file reads follow configured order')
    env['RCLONE_UNION_EASYMOUNT_LIMITS'] = '1,100,100'
    upload('over-limit.txt', b'one')
    check((targets[1] / 'over-limit.txt').exists(), 'source already over its configured limit is skipped')
    source_bytes = (targets[1] / 'over-limit.txt').read_bytes()
    run('copyto', 'easymount_test_pool:over-limit.txt', 'easymount_test_pool:copy.txt', '--ignore-times')
    check((targets[1] / 'copy.txt').read_bytes() == source_bytes and not (targets[2] / 'copy.txt').exists(), 'copies inside the pool still use placement checks')
    env['RCLONE_UNION_EASYMOUNT_LIMITS'] = '0,0,0'
    upload('auto-quota.txt', b'auto')
    check((targets[2] / 'auto-quota.txt').exists(), 'automatic provider free-space queries preserve priority')
    with config.open('a', encoding='utf-8') as handle:
        handle.write('\n[no-quota]\ntype = memory\n')
    env['RCLONE_UNION_UPSTREAMS'] = '"no-quota:" "account 1:"'
    env['RCLONE_UNION_EASYMOUNT_LIMITS'] = '0,1000'
    unknown = upload('unknown.txt', b'data', ok=False)
    check(unknown.returncode != 0 and 'does not report free space' in unknown.stderr and not (targets[1] / 'unknown.txt').exists(), 'unknown quota fails visibly instead of assuming unlimited free space')
    env['RCLONE_UNION_EASYMOUNT_LIMITS'] = '8,1000'
    upload('manual-limit.txt', b'data')
    check(not (targets[1] / 'manual-limit.txt').exists(), 'a user-defined capacity enables a backend that has no quota API')
    with config.open('a', encoding='utf-8') as handle:
        handle.write('\n[inner]\ntype = union\nupstreams = "account 0:" "account 1:"\ncreate_policy = ff\nsearch_policy = epall\n')
    env['RCLONE_UNION_UPSTREAMS'] = '"inner:" "account 2:"'
    env['RCLONE_UNION_EASYMOUNT_LIMITS'] = '1000,1000'
    upload('nested-pool.txt', b'wrapped')
    check((targets[0] / 'nested-pool.txt').exists(), 'pool plans do not leak into nested union source configurations')
    print(f'RESULT: {passed} engine integration checks passed. No real cloud account or Windows drive was used.')
