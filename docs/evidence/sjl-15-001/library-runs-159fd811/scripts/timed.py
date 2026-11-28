import resource, subprocess, sys, time
out = sys.argv[1]; cmd = sys.argv[2:]
t0 = time.time(); rc = subprocess.call(cmd)
ru = resource.getrusage(resource.RUSAGE_CHILDREN)
open(out, 'w').write('wall=%.1fs maxrss=%dMB\n' % (time.time() - t0, ru.ru_maxrss // 1024))
sys.exit(rc if rc >= 0 else 128 - rc)
