function* strictGen() {
  'use strict';
  try { genA = 1; out.push('no throw'); } catch (e) { out.push(e.name); }
  yield 1;
  try { genB = 1; out.push('no throw'); } catch (e) { out.push(e.name); }
}
var it = strictGen();
it.next(); it.next();