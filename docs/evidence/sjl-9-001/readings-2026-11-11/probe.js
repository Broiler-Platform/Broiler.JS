// Clause 7 and 8 probe for branch js-c78: Intl.supportedValuesOf's currency and unit lists, and
// Decimal.prototype.toLocaleString's answers, each on one line, with the default locale beside them.
function line(label, f) {
  var r;
  try { r = String(f()); } catch (e) { r = "throws " + (e && e.name) + ": " + (e && e.message); }
  console.log(label + " => " + r);
}
line("defaultLocale", function () { return new Intl.NumberFormat().resolvedOptions().locale; });
line("currency.length", function () { return Intl.supportedValuesOf("currency").length; });
line("currency", function () { return Intl.supportedValuesOf("currency").join(","); });
line("unit.length", function () { return Intl.supportedValuesOf("unit").length; });
line("unit", function () { return Intl.supportedValuesOf("unit").join(","); });
line("currency sorted-unique", function () { var c = Intl.supportedValuesOf("currency"); return c.every(function (v, i) { return i === 0 || c[i - 1] < v; }); });
line("unit sorted-unique", function () { var c = Intl.supportedValuesOf("unit"); return c.every(function (v, i) { return i === 0 || c[i - 1] < v; }); });
var d = 1234567.891m, n = -0.5m, big = 12345678901234567890.123456789m, z = 0m;
line("typeof", function () { return typeof d; });
line("d.toString", function () { return d.toString(); });
line("d.toLocaleString()", function () { return d.toLocaleString(); });
line("d.toLocaleString(undefined)", function () { return d.toLocaleString(undefined); });
line("d.toLocaleString('en')", function () { return d.toLocaleString("en"); });
line("d.toLocaleString('en-US')", function () { return d.toLocaleString("en-US"); });
line("d.toLocaleString('de')", function () { return d.toLocaleString("de"); });
line("d.toLocaleString('de-DE')", function () { return d.toLocaleString("de-DE"); });
line("d.toLocaleString('fr')", function () { return d.toLocaleString("fr"); });
line("d.toLocaleString('hi-u-nu-deva')", function () { return d.toLocaleString("hi-u-nu-deva"); });
line("d.toLocaleString('en',{maximumFractionDigits:1})", function () { return d.toLocaleString("en", { maximumFractionDigits: 1 }); });
line("d.toLocaleString('de',{style:'currency',currency:'EUR'})", function () { return d.toLocaleString("de", { style: "currency", currency: "EUR" }); });
line("d.toLocaleString('en',{notation:'compact'})", function () { return d.toLocaleString("en", { notation: "compact" }); });
line("d.toLocaleString(undefined,{minimumFractionDigits:4})", function () { return d.toLocaleString(undefined, { minimumFractionDigits: 4 }); });
line("n.toLocaleString('en',{maximumFractionDigits:0})", function () { return n.toLocaleString("en", { maximumFractionDigits: 0 }); });
line("big.toLocaleString('en',{maximumFractionDigits:20})", function () { return big.toLocaleString("en", { maximumFractionDigits: 20 }); });
line("big.toLocaleString('en',{maximumSignificantDigits:21})", function () { return big.toLocaleString("en", { maximumSignificantDigits: 21 }); });
line("z.toLocaleString('en')", function () { return z.toLocaleString("en"); });
line("d.toLocaleString('xx-invalid-')", function () { return d.toLocaleString("xx-invalid-"); });
line("d.toLocaleString('en',{style:'currency'})", function () { return d.toLocaleString("en", { style: "currency" }); });
line("Decimal.prototype.toLocaleString.length", function () { return Decimal.prototype.toLocaleString.length; });
