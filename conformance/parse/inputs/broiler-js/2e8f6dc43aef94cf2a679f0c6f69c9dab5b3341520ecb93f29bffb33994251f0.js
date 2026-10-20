function who(n) { var a = arguments; return a.callee === who; }
[who(1), who(1), who(1), who(1)].join('|');