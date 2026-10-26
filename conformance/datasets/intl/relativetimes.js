// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// German and English relative times, the dataset slice I4's RelativeTimeFormat record keeps
// (JSD-0049). Each format, of every style and numeric option, writes twelve values in each of the
// eight units, some as parts, and its resolved options; a last group answers plural units, a
// numbering system and the errors ECMA-402 states. It uses no host function and completes with its
// lines, so Node and the profile run the same text. Non-ASCII is escaped.

var locales = ['en', 'en-US', 'de', 'de-DE'];
var styles = ['long', 'short', 'narrow'];
var numerics = ['always', 'auto'];
var units = ['second', 'minute', 'hour', 'day', 'week', 'month', 'quarter', 'year'];
var values = [-2, -1, -0, 0, 1, 2, 1.5, -1.5, 10, -1000, 1234567.891, 0.0001];

function escape(text) {
  var out = '';
  for (var i = 0; i < text.length; i++) {
    var unit = text.charCodeAt(i);
    out += unit < 0x20 || unit > 0x7e || text[i] === '\\'
      ? '\\u' + ('000' + unit.toString(16)).slice(-4)
      : text[i];
  }
  return out;
}

function show(value) {
  return value === 0 && 1 / value < 0 ? '-0' : String(value);
}

function parts(list) {
  return list.map(function (part) {
    return part.type + ':' + part.value + (part.unit ? ':' + part.unit : '');
  }).join('|');
}

function attempt(run) {
  try {
    return run();
  } catch (error) {
    return error.name;
  }
}

var lines = [];

for (var l = 0; l < locales.length; l++) {
  for (var s = 0; s < styles.length; s++) {
    for (var n = 0; n < numerics.length; n++) {
      var options = { style: styles[s], numeric: numerics[n] };
      var head = locales[l] + ' ' + JSON.stringify(options);
      var format = new Intl.RelativeTimeFormat(locales[l], options);

      for (var u = 0; u < units.length; u++) {
        for (var v = 0; v < values.length; v++) {
          lines.push(head + ' ' + units[u] + ' ' + show(values[v]) + ' => ' + format.format(values[v], units[u]));
        }
      }

      for (var p = 0; p < 4; p++) {
        lines.push(head + ' parts day ' + show(values[p * 3]) + ' => ' + parts(format.formatToParts(values[p * 3], 'day')));
      }

      lines.push(head + ' parts hour -1000 => ' + parts(format.formatToParts(-1000, 'hour')));
      lines.push(head + ' resolvedOptions => ' + JSON.stringify(format.resolvedOptions()));
    }
  }
}

var plain = new Intl.RelativeTimeFormat('en');
var arab = new Intl.RelativeTimeFormat('en', { numberingSystem: 'arab' });

lines.push('plural units => ' + ['seconds', 'minutes', 'hours', 'days', 'weeks', 'months', 'quarters', 'years'].map(function (unit) {
  return plain.format(3, unit);
}).join('; '));
lines.push('arab => ' + arab.format(-12, 'day') + ' ' + JSON.stringify(arab.resolvedOptions()));
lines.push('keyword => ' + new Intl.RelativeTimeFormat('de-u-nu-arab').format(5, 'week'));
lines.push('string value => ' + plain.format('3', 'day'));
lines.push('NaN => ' + attempt(function () { return plain.format(NaN, 'day'); }));
lines.push('Infinity => ' + attempt(function () { return plain.format(Infinity, 'day'); }));
lines.push('bad unit => ' + attempt(function () { return plain.format(1, 'decade'); }));
lines.push('bad style => ' + attempt(function () { return new Intl.RelativeTimeFormat('en', { style: 'full' }); }));
lines.push('bad numeric => ' + attempt(function () { return new Intl.RelativeTimeFormat('en', { numeric: 'never' }); }));
lines.push('bad numbering system => ' + attempt(function () { return new Intl.RelativeTimeFormat('en', { numberingSystem: 'a' }); }));
lines.push('call => ' + attempt(function () { return Intl.RelativeTimeFormat(); }));
lines.push('supportedLocalesOf => ' + Intl.RelativeTimeFormat.supportedLocalesOf(['de-AT', 'en', 'de-DE', 'zz']).join(','));

lines.map(escape).join('\n');
