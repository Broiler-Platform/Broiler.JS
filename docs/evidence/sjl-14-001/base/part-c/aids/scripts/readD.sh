#!/usr/bin/env bash
# readD.sh <out dir> [<bridge-codes.tsv>]: bundle sjl-14-001's base/part-c/scripts/read_base.sh over the retained run
# folder and the tools step's --text run, into <out dir>, with or without the reading aid's codes (records part 4,
# 2026-11-23). Reads only.
R=/home/user/js-w2base
bash $R/docs/evidence/sjl-14-001/base/part-c/scripts/read_base.sh $R \
  /tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2base/base \
  /home/user/js-w2base-build/69a8a13b/text "$@"
