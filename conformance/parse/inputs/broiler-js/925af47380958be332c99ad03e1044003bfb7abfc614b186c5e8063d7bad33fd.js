var o = {};
o.x = 1;
o[true] = 2;
o[9007199254740990] = 3;
[o.a, ...o.rest] = [4, 5, 6];
for (o.key in { first: 1, second: 2 }) {}
[o.x, o.true, o[9007199254740990], o.a, o.rest.join(','), o.key].join('|');