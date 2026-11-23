#!/usr/bin/env bash
# memwatch.sh: every 10 s, append the time, MemAvailable and the three largest processes by RSS (diagnosis of container restarts).
out=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2base/memwatch.log
while true; do
  { echo "$(date -u +%FT%TZ) $(grep MemAvailable /proc/meminfo)"; ps -eo rss=,pid=,args= --sort=-rss | head -3 | cut -c1-180; } >> "$out"
  sync "$out" 2>/dev/null
  sleep 10
done
