var proto = {}; proto.p = 1;
var o = Object.create(proto); o.own = 2;
String(o.hasOwnProperty('own')) + ',' + String(o.hasOwnProperty('p')) + ',' +
    String('p' in o) + ',' + String('nope' in o);