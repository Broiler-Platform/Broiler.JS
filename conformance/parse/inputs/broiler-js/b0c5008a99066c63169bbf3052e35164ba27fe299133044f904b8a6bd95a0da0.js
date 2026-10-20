"use strict";
var before;
var after;
try { f; before = "value"; } catch (e) { before = e.name; }
switch (1) {
  case 1:
    function f() {}
}
try { f; after = "value"; } catch (e) { after = e.name; }
[before, after].join("|");