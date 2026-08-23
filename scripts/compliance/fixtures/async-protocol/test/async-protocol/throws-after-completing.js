// Copyright (C) 2026 Broiler.  All rights reserved.
/*---
description: >
    An async test that completes and then dies. The marker says the test finished and the
    exit status says the host did not, and the run is a failure on the second fact.
flags: [async]
---*/

$DONE();
throw new Test262Error('this fixture throws after reporting completion');
