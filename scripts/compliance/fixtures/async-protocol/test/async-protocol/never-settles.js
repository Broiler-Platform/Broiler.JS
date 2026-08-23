// Copyright (C) 2026 Broiler.  All rights reserved.
/*---
description: >
    An async test that never calls $DONE. Nothing keeps the host alive, so it exits
    normally having reported nothing — which must be a failure and not a pass.
flags: [async]
---*/

var neverSettles = new Promise(function () {});
neverSettles.then($DONE, $DONE);
