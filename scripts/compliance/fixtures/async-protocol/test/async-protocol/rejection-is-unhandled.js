// Copyright (C) 2026 Broiler.  All rights reserved.
/*---
description: >
    An async test whose promise rejects with nothing routing the rejection to $DONE. The
    host reports no unhandled rejection and exits 0, so the only evidence that the test
    did not finish is the completion marker it never printed.
flags: [async]
---*/

Promise.reject(new Test262Error('this fixture rejects and reports nothing'));
