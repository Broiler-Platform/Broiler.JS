// Copyright (C) 2026 Broiler.  All rights reserved.
/*---
description: >
    An async test whose assertion fails inside a promise reaction, reaching $DONE the way
    the suite's own tests do — `.then($DONE, $DONE)`. The failure must be reported as one.
flags: [async]
---*/

Promise.resolve(1).then(function (value) {
  assert.sameValue(value, 2, 'this fixture asserts something deliberately untrue');
}).then($DONE, $DONE);
