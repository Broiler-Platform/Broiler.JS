'use strict';
var a = [1];
Object.freeze(a);
var i = 0;
var caught = 'none';
try { a[i] = 99; } catch (e) { caught = e.name; }
caught + ':' + a[0];