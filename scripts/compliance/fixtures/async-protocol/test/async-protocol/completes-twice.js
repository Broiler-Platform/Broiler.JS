// Copyright (C) 2026 Broiler.  All rights reserved.
/*---
description: >
    An async test that calls $DONE twice: once claiming success and once reporting a
    failure it reached afterwards. A first-one-wins reading would record the outcome the
    test did not end at, so two completions are a failure.
flags: [async]
---*/

Promise.resolve().then(function () {
  $DONE();
  $DONE(new Test262Error('this fixture keeps going after it says it finished'));
});
