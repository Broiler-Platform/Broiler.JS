#!/usr/bin/env bash
# chain3.sh: written 2026-11-23 by the orchestrator. chain2.sh stopped at bstep's jseal (rc=1): Broiler.JSeal's
# eng/test-package-consumer.ps1 requires the checkout's shipping projects restored before a candidate run, which
# bstep.sh did not do (a harness omission; the attempt is kept as base/part-g/jseal-attempt-1 and logs-jseal-attempt-1).
# bstep.sh's jseal step now restores Broiler.JSeal.slnx in Release-VM first. This takes jseal and html, then "chain done".
set -u
bash /tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2base/scripts/bdrive.sh 69a8a13bc446a1714d3c8be9be3cfd742aebee6e base jseal html || exit $?
echo "$(date -u +%FT%TZ) chain done" >> /tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2base/chain.log
