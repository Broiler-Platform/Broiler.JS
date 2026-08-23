// Copyright (C) 2026 Broiler.  All rights reserved.
/*---
description: >
    An async test that neither completes nor exits. This is the leg of the protocol the
    markers cannot decide: the runner's per-test timeout has to, deterministically.
flags: [async]
---*/

while (true) {
  // Spin without allocating: the run ends when the runner's timeout ends it.
}
