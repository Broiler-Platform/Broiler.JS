function Outer() {
  this.v = 1;
  var self = this;
  this.get = function () { return self.v + arguments.length; };
}
var o = new Outer();
o.get();