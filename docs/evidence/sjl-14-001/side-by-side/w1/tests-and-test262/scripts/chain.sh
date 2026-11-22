#!/usr/bin/env bash
# chain.sh: resumes W1's owed part C runs, then the base commit's step.sh runs, then bstep.sh's (parts C tool, D, E, G, H).
# Written 2026-11-19 by the orchestrator after the stage's agent was lost to a container restart; each driver skips steps
# already done (their markers), so the chain can be started again after a restart.
set -u
bash /tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2base/scripts/drive.sh 8e5519d8cf81808856e32f2275fcb4d3b98a73f3 w1 archive build-off terser tests-off t262-off:0 t262-off:1 t262-off:2 t262-off:3 t262-off:4 t262-off:5 t262-off:6 t262-off:7 t262merge-off t262rerun-off t262-on:0 t262-on:1 t262-on:2 t262-on:3 t262-on:4 t262-on:5 t262-on:6 t262-on:7 t262merge-on t262rerun-on build-on tests-on || exit $?
bash /tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2base/scripts/drive.sh 69a8a13bc446a1714d3c8be9be3cfd742aebee6e base archive build-off terser tests-off t262-off:0 t262-off:1 t262-off:2 t262-off:3 t262-off:4 t262-off:5 t262-off:6 t262-off:7 t262merge-off t262rerun-off t262-on:0 t262-on:1 t262-on:2 t262-on:3 t262-on:4 t262-on:5 t262-on:6 t262-on:7 t262merge-on t262rerun-on build-on tests-on || exit $?
bash /tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2base/scripts/bdrive.sh 69a8a13bc446a1714d3c8be9be3cfd742aebee6e base tools phase5 pack api jseal html || exit $?
echo "$(date -u +%FT%TZ) chain done" >> /tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2base/chain.log
