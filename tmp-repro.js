var s1 = /\ud834\udf06/u.source;
var s2 = /\u{1d306}/u.source;
var re1 = eval('/' + s1 + '/u');
var re2 = eval('/' + s2 + '/u');
var result = [s1, s2, String(re1.test('\ud834\udf06')), String(re1.test('𝌆')), String(re2.test('\ud834\udf06')), String(re2.test('𝌆'))].join('|');
if (!(re1.test('\ud834\udf06') && re1.test('𝌆') && re2.test('\ud834\udf06') && re2.test('𝌆'))) {
  throw new Error(result);
}
