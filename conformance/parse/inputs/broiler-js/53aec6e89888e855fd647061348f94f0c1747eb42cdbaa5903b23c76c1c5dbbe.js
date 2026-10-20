var proto = {}; proto.inh = 1;
var child = Object.create(proto); child.own = 2;
var seen = [];
for (var k in child) seen.push(k);
seen.join(',');