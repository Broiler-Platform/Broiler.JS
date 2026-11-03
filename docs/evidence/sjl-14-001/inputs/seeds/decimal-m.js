// SJL-14.1, part J seed: Broiler.JS's decimal literal extension, the m suffix, which reaches the library only through
// the lexer extension parameter (SJL-D-17; decision D17); TokenTypes.Decimal is a bridged fact (decision D44).
var d = 1.25m + 2m;
var e = 0.1m * 3m;
typeof d + ':' + String(d) + ':' + String(e);
