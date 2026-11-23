# timedv.py <out> <command...>: timed.py's record with the processor time and the exit status or signal added
# (records part 4, 2026-11-23). One line: wall, user and system processor seconds of the command and every descendant
# it waited for, peak resident set of the largest of them, and how the command ended.
import os, resource, subprocess, sys, time
out = sys.argv[1]; cmd = sys.argv[2:]
t0 = time.time(); rc = subprocess.call(cmd)
ru = resource.getrusage(resource.RUSAGE_CHILDREN)
end = ("exit=%d" % rc) if rc >= 0 else ("signal=%d" % -rc)
open(out, 'w').write('wall=%.1fs user=%.1fs sys=%.1fs maxrss=%dMB %s\n' % (time.time() - t0, ru.ru_utime, ru.ru_stime, ru.ru_maxrss // 1024, end))
sys.exit(rc if rc >= 0 else 128 - rc)
