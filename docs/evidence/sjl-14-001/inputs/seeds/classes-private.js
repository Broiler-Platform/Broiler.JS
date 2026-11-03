// SJL-14.1, part J seed: classes with public and private fields, private methods and accessors, static blocks, derived
// constructors and super, computed keys.
class Base { #secret = 1; static count = 0; static { Base.count++; } get secret() { return this.#secret; } m() { return 'b'; } }
class Derived extends Base {
  #own = 2; static #s = 3; ['k' + 1] = 4;
  constructor() { super(); this.v = super.m() + this.#own; }
  #pm() { return this.#own + Derived.#s; }
  static has(o) { return #own in o; }
  get both() { return this.secret + this.#pm(); }
}
var o = new Derived();
[o.v, o.both, o.k1, Derived.has(o), Base.count].join(',');
