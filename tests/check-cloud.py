"""Real-provider smoke test. Writes only a unique disposable test folder.

Use explicit remotes, never a mounted user directory. Failed runs retain their
test folder for diagnosis. No tokens, file listings or provider error bodies
are included in the report.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import uuid

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--engine", required=True)
parser.add_argument("--config", required=True)
parser.add_argument("--remote", action="append", required=True)
parser.add_argument("--report", required=True)
args = parser.parse_args()
folder = "EasyMount-release-test-" + uuid.uuid4().hex
results = []
operation = ""


def run(*command):
    global operation
    operation = command[0]
    result = subprocess.run(
        [args.engine, *command, "--config", args.config, "--retries", "1",
         "--low-level-retries", "1", "--contimeout", "15s", "--timeout", "30s"],
        capture_output=True, timeout=90)
    if result.returncode:
        message = result.stderr.decode("utf-8", errors="replace").lower()
        category = ("authentication" if any(s in message for s in
                    ("invalid_grant", "unauthorized", "token has", "401", "access_denied"))
                    else "permission" if "403" in message or "permission" in message
                    else "provider-operation")
        raise RuntimeError(category)
    return result.stdout


with tempfile.TemporaryDirectory(prefix="EasyMount-cloud-check-") as temp:
    local = Path(temp)
    for index, remote in enumerate(args.remote, 1):
        if not remote.endswith(":") or "/" in remote or "\\" in remote:
            raise ValueError("Supply a remote account root ending in colon.")
        entry = {"account": index, "testFolder": folder, "checks": []}
        target = remote + folder
        try:
            original = local / "source.bin"
            seed = hashlib.sha256((folder + str(index)).encode()).digest()
            original.write_bytes(seed * 131072)  # 4 MiB unique generated fixture
            filename = "/Kiểm thử tiếng Việt – English.bin"
            run("copyto", str(original), target + filename)
            downloaded = local / "download.bin"
            downloaded.unlink(missing_ok=True)
            run("copyto", target + filename, str(downloaded))
            expected = hashlib.sha256(original.read_bytes()).hexdigest()
            if hashlib.sha256(downloaded.read_bytes()).hexdigest() != expected:
                raise RuntimeError("integrity")
            entry["checks"].append("upload-download-sha256-unicode")
            renamed = target + "/renamed.bin"
            run("moveto", target + filename, renamed)
            entry["checks"].append("rename")
            original.write_bytes(b"EasyMount edited disposable fixture\n" * 1024)
            run("copyto", str(original), renamed)
            downloaded.unlink()
            run("copyto", renamed, str(downloaded), "--ignore-times")
            if downloaded.read_bytes() != original.read_bytes():
                raise RuntimeError("edited-integrity")
            entry["checks"].append("edit-download-integrity")
            run("deletefile", renamed)
            run("rmdir", target)  # only removes the now-empty unique test folder
            entry["checks"].append("delete-disposable-fixture")
            entry["status"] = "passed"
        except (RuntimeError, subprocess.TimeoutExpired) as error:
            entry["status"] = "failed"
            entry["operation"] = operation
            entry["error"] = "timeout" if isinstance(error, subprocess.TimeoutExpired) else str(error)
        results.append(entry)
        print(f"Account {index}: {entry['status']} ({len(entry['checks'])} checks)", flush=True)
        Path(args.report).write_text(json.dumps(results, indent=2), encoding="utf-8")
raise SystemExit(0 if all(r["status"] == "passed" for r in results) else 1)
