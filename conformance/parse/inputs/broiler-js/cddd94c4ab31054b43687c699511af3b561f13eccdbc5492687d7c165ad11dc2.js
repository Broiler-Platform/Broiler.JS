async function s() {
  'use strict';
  try { leakA = 1; out.push('sloppy'); } catch (e) { out.push('strict'); }
  await Promise.resolve(0);
  try { leakB = 1; out.push('sloppy'); } catch (e) { out.push('strict'); }
}
async function n() {
  try { leakC = 1; out.push('sloppy'); } catch (e) { out.push('strict'); }
  await Promise.resolve(0);
  try { leakD = 1; out.push('sloppy'); } catch (e) { out.push('strict'); }
}
s(); n();