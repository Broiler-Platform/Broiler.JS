function f() {}
var d = Object.getOwnPropertyDescriptor(f, 'caller');
String(d.writable) + ',' + String(d.enumerable) + ',' + String(d.configurable);