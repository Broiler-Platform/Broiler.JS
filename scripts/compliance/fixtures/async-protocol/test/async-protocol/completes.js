// Copyright (C) 2026 Broiler.  All rights reserved.
/*---
description: >
    An async test that completes: the control for the protocol fixtures, and the only
    one of them the runner may report as a pass.
flags: [async]
---*/

Promise.resolve(1).then(function (value) {
  assert.sameValue(value, 1, 'the fixture resolves with its own value');
}).then($DONE, $DONE);
