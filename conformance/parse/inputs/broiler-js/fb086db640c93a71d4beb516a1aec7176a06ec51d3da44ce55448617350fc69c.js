function who(n) { var t = n; return arguments.callee === who; }
[who(1), who(1), who(1), who(1)].join('|');