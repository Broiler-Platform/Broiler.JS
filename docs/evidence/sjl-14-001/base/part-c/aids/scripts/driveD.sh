#!/usr/bin/env bash
# driveD.sh <group> <full-sha> <step[:arg]>...: each step of stepD.sh through run.sh (lock, idle gate, machine records),
# in order, stopping at the first failure; the default branch's head read by git ls-remote and the free disk checked
# before each (a build waits for 8 GiB free). Appends to driveD.log.
SD=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2baseD
RUN=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/s2/ev2/scripts/run.sh
G=$1; SHA=$2; shift 2
for s in "$@"; do
  step=${s%%:*}; arg=; [ "$s" != "$step" ] && arg=${s#*:}
  label=d-$G-$step${arg:+-$arg}
  head=$(timeout 60 git -C /home/user/js-w2base ls-remote origin refs/heads/main | cut -f1)
  free=$(df -BG --output=avail / | tail -1 | tr -dc 0-9)
  printf '%s %s start head=%s free=%sG\n' "$(date -u +%FT%TZ)" "$label" "$head" "$free" >> $SD/driveD.log
  [ "$head" = 69a8a13bc446a1714d3c8be9be3cfd742aebee6e ] || { echo "$(date -u +%FT%TZ) main moved or unread: $head; stop" >> $SD/driveD.log; exit 9; }
  [ "$free" -ge 8 ] || { echo "$(date -u +%FT%TZ) under 8G free; stop" >> $SD/driveD.log; exit 8; }
  bash $RUN $label $SD/logs /home/user -- bash $SD/scripts/stepD.sh $SHA $G $step $arg; rc=$?
  printf '%s %s end rc=%s\n' "$(date -u +%FT%TZ)" "$label" "$rc" >> $SD/driveD.log
  [ $rc = 0 ] || { echo "$(date -u +%FT%TZ) $label failed; stop" >> $SD/driveD.log; exit $rc; }
done
echo "$(date -u +%FT%TZ) $G done" >> $SD/driveD.log
