// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// German and English durations, the dataset slice I4's DurationFormat record keeps (JSD-0051). Each
// format, of every style and of per-unit styles, displays, fractional digits and a numbering system,
// writes sixteen durations, some as parts, and its resolved options; a last group answers the errors
// ECMA-402 states. It uses no host function and completes with its lines, so Node and the profile run
// the same text. Non-ASCII is escaped.

var locales = ['en', 'de'];
var formats = [
  {}, { style: 'long' }, { style: 'short' }, { style: 'narrow' }, { style: 'digital' },
  { style: 'digital', fractionalDigits: 2 }, { style: 'digital', hours: '2-digit' },
  { style: 'digital', hoursDisplay: 'auto' }, { style: 'long', minutes: 'numeric' },
  { style: 'long', seconds: 'numeric', fractionalDigits: 3 }, { style: 'short', milliseconds: 'numeric' },
  { style: 'narrow', microseconds: 'numeric', fractionalDigits: 0 }, { style: 'long', yearsDisplay: 'always', daysDisplay: 'always' },
  { years: 'long', months: 'short', weeks: 'narrow', days: 'long' }, { style: 'short', numberingSystem: 'arab' }
];
var durations = [
  { years: 1, months: 2, weeks: 3, days: 4, hours: 5, minutes: 6, seconds: 7, milliseconds: 8, microseconds: 9, nanoseconds: 10 },
  { hours: 1, minutes: 2, seconds: 3 }, { hours: 1, seconds: 3 }, { minutes: 5 }, { seconds: 0 },
  { days: 1 }, { years: 2, days: 1 }, { milliseconds: 1500 }, { seconds: 1, milliseconds: 234, microseconds: 567, nanoseconds: 891 },
  { hours: 25, minutes: 61 }, { years: -1, months: -2 }, { hours: -1, minutes: -2, seconds: -3 }, { minutes: -5, seconds: -0 },
  { days: 1000, hours: 12345 }, { nanoseconds: 1 }, { weeks: 1, milliseconds: 1 }
];

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
  for (var f = 0; f < formats.length; f++) {
    var head = locales[l] + ' ' + JSON.stringify(formats[f]);
    var format = attempt(function () { return new Intl.DurationFormat(locales[l], formats[f]); });

    if (typeof format === 'string') {
      lines.push(head + ' => ' + format);
      continue;
    }

    for (var d = 0; d < durations.length; d++) {
      lines.push(head + ' ' + JSON.stringify(durations[d]) + ' => ' + attempt(function () { return format.format(durations[d]); }));
    }

    lines.push(head + ' parts => ' + format.formatToParts(durations[0]).map(function (part) {
      return part.type + ':' + part.value + (part.unit ? ':' + part.unit : '');
    }).join('|'));
    lines.push(head + ' resolvedOptions => ' + JSON.stringify(format.resolvedOptions()));
  }
}

var plain = new Intl.DurationFormat('en');

lines.push('string => ' + attempt(function () { return plain.format('PT1H'); }));
lines.push('number => ' + attempt(function () { return plain.format(5); }));
lines.push('empty => ' + attempt(function () { return plain.format({}); }));
lines.push('fraction => ' + attempt(function () { return plain.format({ hours: 1.5 }); }));
lines.push('mixed signs => ' + attempt(function () { return plain.format({ hours: 1, minutes: -1 }); }));
lines.push('too large => ' + attempt(function () { return plain.format({ years: 4294967296 }); }));
lines.push('bad style => ' + attempt(function () { return new Intl.DurationFormat('en', { style: 'full' }); }));
lines.push('fractional always => ' + attempt(function () { return new Intl.DurationFormat('en', { milliseconds: 'numeric', millisecondsDisplay: 'always' }); }));
lines.push('long after numeric => ' + attempt(function () { return new Intl.DurationFormat('en', { hours: 'numeric', minutes: 'long' }); }));
lines.push('bad fractional digits => ' + attempt(function () { return new Intl.DurationFormat('en', { fractionalDigits: 10 }); }));
lines.push('primitive options => ' + attempt(function () { return new Intl.DurationFormat('en', 'long'); }));
lines.push('call => ' + attempt(function () { return Intl.DurationFormat(); }));
lines.push('supportedLocalesOf => ' + Intl.DurationFormat.supportedLocalesOf(['de-AT', 'en', 'de-DE', 'zz']).join(','));

lines.map(escape).join('\n');
