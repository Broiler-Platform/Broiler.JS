(function(){ var log = [];
var o = {
  get x() { log.push('x'); return 10; },
  get y() { log.push('y'); return 20; }
};
var r = o.x + o.y;
return r + ',' + log.join(','); })()