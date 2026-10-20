var gets = 0, sets = 0, last = 0;
var proto = {
    get x() { gets++; return 1; },
    set x(v) { sets++; last = v; }
};
var o = Object.create(proto);
o.own = 1;
for (var i = 0; i < 400; i++) o.x += 2;
[gets, sets, last === 3 ? 400 : last].join('|');