#!/usr/bin/env bash
# achain.sh: bundle sjl-14-001's local runs at the after commit 83a9bd82 (W2's merge), every step of astep.sh by
# adrive.sh. Written 2026-11-23. Started detached (setsid nohup) and resumable: after a container restart it is started
# again by the same command, and adrive.sh skips every step whose marker is in after/done/ (README.md, "Restart").
# Changed 2026-11-23, after review (the text as started is fix/achain.sh.before-fix): one chain at a time, by a lock on
# disk (achain.lock, held by this shell alone: the driver is started with the descriptor closed), so a second start
# writes a line to chain.log and stops instead of moving a live step's folder; and the steps dcodes and dtext after
# dcontrol.
set -u
SCR=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2after
exec 8>>$SCR/achain.lock
flock -n 8 || { echo "$(date -u +%FT%TZ) achain.sh not started: another process holds achain.lock (a chain or a driver is running)" >> $SCR/chain.log; exit 0; }
bash $SCR/scripts/adrive.sh 83a9bd82a5d258d3001468095ebf43f9efc3e9a7 after archive partd dcontrol dcodes dtext ptests phase5 closure pack pristine pcontrol obsolete api jseal html fuzz fcontrol delsearch 8>&- || exit $?
f=; [ -s $SCR/after/FAILED ] && f=" (non-zero exits, in after/FAILED: $(tr '\n' ';' < $SCR/after/FAILED))"
echo "$(date -u +%FT%TZ) chain done$f" >> $SCR/chain.log
