// Copyright (C) 2026 Broiler.  All rights reserved.
/*---
description: >
    An async test that calls $DONE with an error. Under the completion-value protocol this
    settled the script's completion value as a rejection, which `--script-host` evaluated
    and discarded, so the test exited 0 and was counted as a pass.
flags: [async]
---*/

$DONE(new Test262Error('this fixture reports a deliberate failure'));
