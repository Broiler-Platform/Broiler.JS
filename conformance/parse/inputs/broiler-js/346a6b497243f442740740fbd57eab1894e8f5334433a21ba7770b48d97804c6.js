async function strictAsync() {
  'use strict';
  try { beforeAwait = 1; out.push('no throw'); } catch (e) { out.push(e.name); }
  await Promise.resolve(0);
  try { afterAwait = 1; out.push('no throw'); } catch (e) { out.push(e.name); }
}
strictAsync();