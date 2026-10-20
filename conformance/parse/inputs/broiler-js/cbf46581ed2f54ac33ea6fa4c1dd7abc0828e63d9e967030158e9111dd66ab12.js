var out = [];
function probe() { try { undeclared1 = 1; out.push('sloppy'); } catch (e) { out.push('strict'); } }
function s3() { 'use strict'; probeStrict(); }
function probeStrict() { 'use strict'; try { undeclared2 = 1; out.push('sloppy'); } catch (e) { out.push('strict'); } }
function level1() { probe(); probeStrict(); probe(); s3(); probe(); }
level1();
out.join(',');