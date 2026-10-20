(function(){ var log = [];
var o = {
  get x() { throw new Error('boom'); },
  get y() { log.push('y'); return 1; }
};
try { var r = o.x + o.y; } catch (e) { return 'caught,' + log.join(','); }
return 'no throw'; })()