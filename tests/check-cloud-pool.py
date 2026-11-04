"""Five real cloud sources, isolated quota/priority and duplicate-path checks.

Only generated files in new UUID directories are modified. A failed test leaves
its fixtures for diagnosis. Existing user data and mount configuration are unused.
"""
import argparse
import json
import os
from pathlib import Path
import subprocess
import tempfile
import uuid

p = argparse.ArgumentParser(description=__doc__)
p.add_argument("--engine", required=True)
p.add_argument("--config", required=True)
p.add_argument("--remote", action="append", required=True)
p.add_argument("--report", required=True)
a = p.parse_args()
if len(a.remote) != 5 or len(set(a.remote)) != 5:
    p.error("Supply exactly five distinct cloud remotes.")
if any(not r.endswith(":") or "/" in r or "\\" in r for r in a.remote):
    p.error("Supply account roots ending in colon.")
folder = "EasyMount-pool-test-" + uuid.uuid4().hex
sources = [r + folder + "/source" for r in a.remote]
env = os.environ.copy()
env.update(RCLONE_UNION_UPSTREAMS=" ".join('"' + s + '"' for s in sources),
           RCLONE_UNION_CREATE_POLICY="ff", RCLONE_UNION_ACTION_POLICY="epall",
           RCLONE_UNION_SEARCH_POLICY="epall", RCLONE_UNION_EASYMOUNT_PRIORITY="true",
           RCLONE_UNION_EASYMOUNT_LIMITS="8,8,8,8,8")
report = {"testFolder": folder, "sourceCount": 5, "checks": [], "status": "running"}


def run(*command, failure_expected=False):
    result = subprocess.run([a.engine, *command, "--config", a.config,
                             "--retries", "1", "--low-level-retries", "1",
                             "--contimeout", "15s", "--timeout", "30s"],
                            env=env, capture_output=True, timeout=120)
    if failure_expected:
        if not result.returncode or b"enough" not in result.stderr.lower():
            raise RuntimeError("Expected explicit capacity exhaustion")
    elif result.returncode:
        raise RuntimeError("Provider/union operation failed; fixtures retained")
    return result.stdout


def check(ok, description):
    if not ok:
        raise RuntimeError(description)
    report["checks"].append(description)
    print("PASS " + description, flush=True)
    Path(a.report).write_text(json.dumps(report, indent=2), encoding="utf-8")


try:
    with tempfile.TemporaryDirectory(prefix="EasyMount-cloud-pool-") as tmp:
        fixture = Path(tmp) / "fixture.bin"
        for source in sources:
            run("mkdir", source)
        for i, source in enumerate(sources):
            data = bytes([i + 1]) * 8
            fixture.write_bytes(data)
            name = "/priority-" + str(i + 1) + ".bin"
            run("copyto", str(fixture), ":union:" + name)
            check(run("cat", source + name) == data,
                  "priority file placed intact on source " + str(i + 1))
            check(run("cat", ":union:" + name) == data,
                  "union reads source " + str(i + 1))
        run("copyto", str(fixture), ":union:/must-not-fit.bin", failure_expected=True)
        check(True, "all five exhausted limits reject another whole file")
        for i, source in enumerate(sources):
            run("deletefile", source + "/priority-" + str(i + 1) + ".bin")
        # Existing duplicate paths use upstream epall semantics, documented in README.
        for i, source in enumerate(sources):
            fixture.write_bytes(bytes([i + 1]) * 3)
            run("copyto", str(fixture), source + "/duplicate.bin")
        check(run("cat", ":union:/duplicate.bin") == bytes([1]) * 3,
              "duplicate read resolves to first priority source")
        fixture.write_bytes(b"edit")
        run("copyto", str(fixture), ":union:/duplicate.bin", "--ignore-times")
        for i, source in enumerate(sources):
            check(run("cat", source + "/duplicate.bin") == b"edit",
                  "epall duplicate update affects source " + str(i + 1))
        run("deletefile", ":union:/duplicate.bin")
        for i, source in enumerate(sources):
            check(json.loads(run("lsjson", source)) == [],
                  "epall duplicate deletion empties source " + str(i + 1))
            run("rmdir", source)
            run("rmdir", a.remote[i] + folder)
        report["status"] = "passed"
except (RuntimeError, subprocess.TimeoutExpired) as error:
    report["status"] = "failed"
    report["error"] = "timeout" if isinstance(error, subprocess.TimeoutExpired) else str(error)
finally:
    Path(a.report).write_text(json.dumps(report, indent=2), encoding="utf-8")
raise SystemExit(0 if report["status"] == "passed" else 1)
