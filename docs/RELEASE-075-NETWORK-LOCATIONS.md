# 0.7.5 Network locations cleanup

2026-11-06. Source 55da6c406b82ebb2d89a75b6182eadff0df41575.

The 0.7.3 check observed only Explorer's navigation tree. The user subsequently reproduced nine ghost entries in the main This PC / Network locations view despite no logical drives or running app/worker processes. F5 alone initially retained them. The previous result was insufficient to establish full shell cleanup.

The shared post-unmount helper now sends a bounded network-volume removal notification (WM_DEVICECHANGE / DBT_DEVICEREMOVECOMPLETE / DBTF_NET), followed by an update of the Computer known-folder namespace. Existing safe-disconnect/pending-upload checks remain unchanged. No registry deletion or Explorer process restart is part of the fix.

References: WinFsp v2.1 src/dll/mount.c (bundled corresponding-source archive), and https://learn.microsoft.com/en-us/windows/win32/devio/dbt-deviceremovecomplete .

Verification:
- 116 core checks passed with actual disposable WinFsp mounts; 36 localization/theme checks passed.
- Final 0.7.5 compiled installer passed 37 checks; installed upgrade exit 0, personal drive profiles unchanged.
- Installed native GUI fixture: Y: appeared in the already-open main This PC Network locations list; closing the app removed it without pressing F5.
- A backed-up, disposable Z: MountPoints2 cache entry was temporarily removed during diagnosis, then restored. No personal MountPoints2 entries were deleted. The production fix does not rely on deleting registry cache.
- Setup SHA256: D5AE5AB96FAE14D3AEE2EBCD0E3A905DF99C3C255F75B97089808E10EEEF7D06.

This is a local beta fix, not a signed/public Stable release. Forced termination/power loss and reboot are outside this test.

- Personal installed app: five Google Drive entries R: through V: were observed in the main Network locations view during startup. A queued close completed safely. The main list automatically returned to C: only, with zero app/worker processes and no R:-Z: logical drives. Navigating away from This PC and returning still showed C: only. Final private screenshot: outputs/EasyMount-075-this-pc-clean.png.
