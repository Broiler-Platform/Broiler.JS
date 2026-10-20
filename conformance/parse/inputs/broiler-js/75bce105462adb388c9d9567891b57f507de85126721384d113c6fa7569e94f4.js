var await = 0;
var fromParam, fromBody;

class C {
  static {
    (function (x = fromParam = await) {
      fromBody = await;
    })();
  }
}

fromParam === 0 && fromBody === 0;