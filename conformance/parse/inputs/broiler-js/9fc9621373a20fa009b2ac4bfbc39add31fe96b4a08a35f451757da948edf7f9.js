function dense(v) {
  switch (v) {
    case 10: return 'ten';
    case 11: return 'eleven';
    case 12: return 'twelve';
    case 13: return 'thirteen';
    default: return 'miss';
  }
}
function text(v) {
  switch (v) {
    case 'aa': return 1;
    case 'bb': return 2;
    case 'cc': return 3;
    case 'dd': return 4;
    default: return 0;
  }
}
[dense(10), dense(13), dense(99), dense('10'), text('aa'), text('dd'), text('zz')].join('|');