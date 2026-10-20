(function(){ var o = {
  _y: 1,
  get x() { this._y = 10; return 1; },
  get y() { return this._y; }
};
return o.x + o.y; })()