class C { #p = 0; static write(o, v) { o.#p = v; } }
(function () { try { C.write({}, 1); return 'no-throw'; } catch (e) { return e.constructor.name; } })();