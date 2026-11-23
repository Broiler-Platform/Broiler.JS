#!/usr/bin/env bash
# chain2.sh: written 2026-11-23 by the orchestrator. chain.sh stopped at bstep's phase5, which exited 1 because
# collect_phase5.py's report reads "offline compiler output differs from the checked-in program" at the base commit
# (the emitter's bit-pattern form for constants, 588ca262, against the copy checked in at 68f9a87a). That run is the
# base's record for part E, which the rule judges at the after commit only; it is kept as taken and not re-run.
# This takes the remaining base steps, pack, api, jseal and html, by the same driver, then records "chain done".
set -u
bash /tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2base/scripts/bdrive.sh 69a8a13bc446a1714d3c8be9be3cfd742aebee6e base pack api jseal html || exit $?
echo "$(date -u +%FT%TZ) chain done" >> /tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2base/chain.log
