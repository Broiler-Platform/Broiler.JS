(function(){ var reads = 0, writes = 0, stored = 5;
var o = {};
Object.defineProperty(o, 'x', {
  get: function () { reads++; return 1; },
  set: function (v) { writes++; stored = v; }
});
var y = o.x++;
return reads + ',' + writes + ',' + (stored === 2 ? 5 : stored) + ',' + (y + 1); })()