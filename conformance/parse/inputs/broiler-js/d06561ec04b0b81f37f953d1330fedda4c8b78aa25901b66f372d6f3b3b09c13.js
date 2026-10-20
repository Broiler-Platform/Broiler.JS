var o = {
  v: 3,
  run: function () {
    return (function (self) { return (() => self.v * 2)(); })(this);
  }
};
o.run();