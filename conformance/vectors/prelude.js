// The prelude of the shared conformance corpus (SCHEMA.md section 7).
//
// An engine-level runner evaluates this file as a Script in every fresh realm, before the vector's
// own script. It defines what a script needs to build its inputs without passing them through the
// conversion under test: a Number is built from its IEEE 754 bit pattern, never from a decimal
// literal.

// The Number whose bit pattern is hi * 2**32 + lo, with hi and lo unsigned 32-bit integers.
function $f64(hi, lo) {
  var view = new DataView(new ArrayBuffer(8));
  view.setUint32(0, hi);
  view.setUint32(4, lo);
  return view.getFloat64(0);
}
