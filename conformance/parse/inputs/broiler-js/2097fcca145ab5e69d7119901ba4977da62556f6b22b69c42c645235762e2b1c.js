(function(){ var log = [];
var o = {
  get w() { log.push('w'); return 1; },
  get x() { log.push('x'); return 2; },
  get y() { log.push('y'); return 3; },
  get z() { log.push('z'); return 4; }
};
var r = o.w + o.x * o.y + o.z * 7;
return r + ',' + log.join(','); })()