var functionCallCount = 0;
var methodCallCount = 0;
var functionError = 'none';
var methodError = 'none';
var fn = function (a = eval("var a = 42")) {
  functionCallCount++;
};
var obj = {
  method(a = eval("var a = 42")) {
    methodCallCount++;
  }
};

try {
  fn();
} catch (e) {
  functionError = e.name;
}

try {
  obj.method();
} catch (e) {
  methodError = e.name;
}

[functionError, functionCallCount, methodError, methodCallCount].join("|");