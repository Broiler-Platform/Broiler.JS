function fact(n) { var t = n <= 1 ? 1 : n * fact(n - 1); return t; }
[fact(5), fact(5), fact(5), fact(5)].join('|');