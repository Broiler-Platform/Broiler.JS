'use strict';
function f() {}
var d = Object.getOwnPropertyDescriptor(Function.prototype, 'caller');
var r = [];
try { d.get.call(f); } catch (e) { r.push(e.constructor.name); }
try { d.set.call(f, 1); } catch (e) { r.push(e.constructor.name); }
r.join('|');