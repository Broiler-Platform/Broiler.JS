(function () {
  var lexicalRead = (() => { let x = 1; return eval('x'); })();
  var lexicalArrow = (() => { let x = 1; return eval('(() => x)()'); })();
  var tdzError = 'none';

  try {
    ((a = eval('(() => a)()')) => a)();
  } catch (e) {
    tdzError = e.name;
  }

  return [lexicalRead, lexicalArrow, tdzError].join('|');
})()