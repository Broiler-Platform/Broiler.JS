#!/usr/bin/env bash
# readE.sh <repo root> <out dir> [<bridge-codes.tsv> [<revision token>]]: bundle sjl-14-001's base/part-c/scripts/read_base.sh
# over the retained run folder and the tools step's --text run, into <out dir> (records part 5, 2026-11-23). Reads only.
R=$1; shift
bash $R/docs/evidence/sjl-14-001/base/part-c/scripts/read_base.sh $R \
  /tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2base/base \
  /home/user/js-w2base-build/69a8a13b/text "$@"
