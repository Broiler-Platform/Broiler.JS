function f() {}
var c = Object.getOwnPropertyDescriptor(f, 'caller');
var a = Object.getOwnPropertyDescriptor(f, 'arguments');
[
    ('value' in c) && ('value' in a) && !('get' in c) && !('get' in a),
    f.caller,
    f.arguments,
    [c.writable, c.enumerable, c.configurable].join(','),
    [a.writable, a.enumerable, a.configurable].join(',')
].join('|');