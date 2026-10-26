// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// German and English lists, the dataset slice I4's ListFormat record keeps (JSD-0048). Each format,
// of every type and style, joins lists of no element to five, as a string and as parts, and writes
// its resolved options; a last group answers the errors ECMA-402 states. It uses no host function and
// completes with its lines, so Node and the profile run the same text. Non-ASCII is escaped.

var locales = ['en', 'en-US', 'de', 'de-DE'];
var types = ['conjunction', 'disjunction', 'unit'];
var styles = ['long', 'short', 'narrow'];
var lists = [[], ['A'], ['A', 'B'], ['A', 'B', 'C'], ['A', 'B', 'C', 'D'], ['Anna', '', 'Carl', 'Dora', 'Emil']];

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

function attempt(run) {
  try {
    return run();
  } catch (error) {
    return error.name;
  }
}

var lines = [];

for (var l = 0; l < locales.length; l++) {
  for (var t = 0; t < types.length; t++) {
    for (var s = 0; s < styles.length; s++) {
      var options = { type: types[t], style: styles[s] };
      var head = locales[l] + ' ' + JSON.stringify(options);
      var format = new Intl.ListFormat(locales[l], options);

      for (var n = 0; n < lists.length; n++) {
        var list = lists[n];
        lines.push(head + ' format ' + JSON.stringify(list) + ' => ' + format.format(list));
        lines.push(head + ' formatToParts ' + JSON.stringify(list) + ' => ' + format.formatToParts(list).map(function (part) {
          return part.type + ':' + part.value;
        }).join('|'));
      }

      lines.push(head + ' resolvedOptions => ' + JSON.stringify(format.resolvedOptions()));
    }
  }
}

var plain = new Intl.ListFormat('en');
var closed = 0;
var iterable = {};
iterable[Symbol.iterator] = function () {
  var at = 0;
  return {
    next: function () { at++; return at === 1 ? { value: 'x', done: false } : { value: 7, done: false }; },
    return: function () { closed++; return {}; }
  };
};

lines.push('default => ' + JSON.stringify(plain.resolvedOptions()));
lines.push('undefined => [' + plain.format() + ']');
lines.push('string => ' + plain.format('abc'));
lines.push('set => ' + plain.format(new Set(['x', 'y', 'x'])));
lines.push('number element => ' + attempt(function () { return plain.format(['a', 1]); }));
lines.push('iterator closed => ' + attempt(function () { return plain.format(iterable); }) + ' ' + closed);
lines.push('null => ' + attempt(function () { return plain.format(null); }));
lines.push('primitive options => ' + attempt(function () { return new Intl.ListFormat('en', 'long'); }));
lines.push('null options => ' + attempt(function () { return new Intl.ListFormat('en', null); }));
lines.push('bad type => ' + attempt(function () { return new Intl.ListFormat('en', { type: 'and' }); }));
lines.push('bad style => ' + attempt(function () { return new Intl.ListFormat('en', { style: 'medium' }); }));
lines.push('call => ' + attempt(function () { return Intl.ListFormat(); }));
lines.push('supportedLocalesOf => ' + Intl.ListFormat.supportedLocalesOf(['de-AT', 'en', 'de-DE', 'zz']).join(','));

lines.map(escape).join('\n');
