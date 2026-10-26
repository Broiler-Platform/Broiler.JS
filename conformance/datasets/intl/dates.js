// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// German and English date formatting, the dataset JSD-0027 section 7 asks slice I3 to retain
// (JSD-0045). Each case formats the same instants with one Intl.DateTimeFormat, and writes each
// answer with its parts; ranges are written the same way, and each format's resolved options. Only
// UTC and fixed offsets are used, the time zones JSD-0027 section 5 item 5 admits. The program
// completes with its lines; it uses no host function, so Node and the profile run the same text.
// Non-ASCII is escaped, so the retained answers are ASCII.

var instants = [
  0,                      // 1970-01-01T00:00:00.000Z, a Thursday, midnight
  1709647629123,          // 2024-03-05T14:07:09.123Z, a Tuesday afternoon
  1735732800000,          // 2025-01-01T12:00:00.000Z, noon
  951782400000,           // 2000-02-29T00:00:00.000Z, a leap day
  -62198755200000,        // -000001-01-01T00:00:00.000Z, 1 January 2 BC
  253402300799999,        // 9999-12-31T23:59:59.999Z
  -1,                     // 1969-12-31T23:59:59.999Z
  -62167219200000,        // 0000-01-01T00:00:00.000Z, 1 January 1 BC, the year 0
  8.64e15,                // +275760-09-13T00:00:00.000Z, the last time value
  -8.64e15                // -271821-04-20T00:00:00.000Z, the first time value
];

var cases = [
  ['en', {}],
  ['en-US', { timeZone: 'UTC' }],
  ['en', { dateStyle: 'full' }],
  ['en', { dateStyle: 'long' }],
  ['en', { dateStyle: 'medium' }],
  ['en', { dateStyle: 'short' }],
  ['en', { timeStyle: 'full' }],
  ['en', { timeStyle: 'long' }],
  ['en', { timeStyle: 'medium' }],
  ['en', { timeStyle: 'short' }],
  ['en', { dateStyle: 'full', timeStyle: 'full' }],
  ['en', { dateStyle: 'long', timeStyle: 'short' }],
  ['en', { dateStyle: 'medium', timeStyle: 'medium' }],
  ['en', { dateStyle: 'short', timeStyle: 'long' }],
  ['en', { year: 'numeric', month: 'long', day: 'numeric' }],
  ['en', { year: '2-digit', month: '2-digit', day: '2-digit' }],
  ['en', { weekday: 'long', year: 'numeric', month: 'short', day: 'numeric' }],
  ['en', { weekday: 'short' }],
  ['en', { weekday: 'narrow', day: 'numeric' }],
  ['en', { month: 'narrow' }],
  ['en', { month: 'long', year: 'numeric' }],
  ['en', { month: 'short', day: 'numeric' }],
  ['en', { era: 'short', year: 'numeric' }],
  ['en', { era: 'long', year: 'numeric', month: 'long', day: 'numeric' }],
  ['en', { hour: 'numeric' }],
  ['en', { hour: 'numeric', minute: '2-digit' }],
  ['en', { hour: '2-digit', minute: '2-digit', second: '2-digit' }],
  ['en', { hour: 'numeric', minute: 'numeric', hour12: false }],
  ['en', { hour: 'numeric', minute: 'numeric', hourCycle: 'h23' }],
  ['en', { hour: 'numeric', minute: 'numeric', hourCycle: 'h24' }],
  ['en', { hour: 'numeric', minute: 'numeric', hourCycle: 'h11' }],
  ['en', { minute: '2-digit', second: '2-digit' }],
  ['en', { hour: 'numeric', minute: 'numeric', second: 'numeric', fractionalSecondDigits: 3 }],
  ['en', { second: 'numeric', fractionalSecondDigits: 1 }],
  ['en', { hour: 'numeric', dayPeriod: 'short' }],
  ['en', { dayPeriod: 'long' }],
  ['en', { hour: 'numeric', minute: 'numeric', timeZoneName: 'short' }],
  ['en', { hour: 'numeric', timeZoneName: 'long' }],
  ['en', { hour: 'numeric', timeZoneName: 'shortOffset', timeZone: '+05:30' }],
  ['en', { hour: 'numeric', minute: 'numeric', timeZoneName: 'longOffset', timeZone: '-08:00' }],
  ['en', { dateStyle: 'medium', timeStyle: 'full', timeZone: '+01:00' }],
  ['en', { year: 'numeric', month: 'numeric', day: 'numeric', hour: 'numeric', minute: 'numeric', second: 'numeric' }],
  ['en', { month: 'long', day: 'numeric', hour: 'numeric', minute: 'numeric' }],
  ['en-u-hc-h23', { hour: 'numeric', minute: 'numeric' }],
  ['en-u-nu-thai', { year: 'numeric', month: 'numeric', day: 'numeric' }],
  ['en-u-ca-gregory', { dateStyle: 'medium' }],
  ['de', {}],
  ['de-DE', { dateStyle: 'full', timeStyle: 'full' }],
  ['de', { dateStyle: 'long', timeStyle: 'medium' }],
  ['de', { dateStyle: 'medium', timeStyle: 'short' }],
  ['de', { dateStyle: 'short' }],
  ['de', { year: 'numeric', month: 'long', day: 'numeric' }],
  ['de', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' }],
  ['de', { weekday: 'short', month: 'short', day: 'numeric' }],
  ['de', { month: 'long', year: 'numeric' }],
  ['de', { era: 'short', year: 'numeric' }],
  ['de', { hour: 'numeric', minute: 'numeric' }],
  ['de', { hour: 'numeric', minute: 'numeric', hour12: true }],
  ['de', { hour: '2-digit', minute: '2-digit', second: '2-digit', timeZoneName: 'short' }],
  ['de', { hour: 'numeric', dayPeriod: 'long' }],
  ['de', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' }],
  ['en', { timeZoneName: 'shortGeneric' }],
  ['en', { hour: 'numeric', timeZoneName: 'longGeneric' }],
  ['en', { hour: 'numeric', minute: 'numeric', timeZoneName: 'long', timeZone: '+00:00' }],
  ['de', { hour: 'numeric', timeZoneName: 'short', timeZone: '+00:00' }],
  ['en', { dateStyle: 'medium', timeStyle: 'long', timeZone: 'Etc/GMT-3' }],
  ['en', { year: 'numeric', month: 'long', day: 'numeric', weekday: 'long', hour: '2-digit', minute: '2-digit', second: '2-digit', timeZoneName: 'shortOffset', timeZone: '-03:30' }],
  ['de', { hour: 'numeric', minute: 'numeric', hourCycle: 'h11' }],
  ['de', { hour: 'numeric', dayPeriod: 'short', hour12: true }],
  ['de', { dayPeriod: 'narrow' }],
  ['de', { weekday: 'short' }],
  ['de', { weekday: 'narrow', month: 'narrow' }],
  ['en', { era: 'narrow', year: 'numeric' }],
  ['en', { fractionalSecondDigits: 2 }],
  ['de', { minute: 'numeric', second: 'numeric', fractionalSecondDigits: 3 }],
  ['en', { dateStyle: 'short', timeStyle: 'short', hour12: false }],
  ['en', { timeStyle: 'medium', hourCycle: 'h23' }],
  ['de', { timeStyle: 'short', hour12: true }],
  ['en-u-hc-h11', { timeStyle: 'short' }],
  ['de-u-hc-h12', { hour: 'numeric', minute: 'numeric' }],
  ['en', { hour: 'numeric', hour12: true, hourCycle: 'h23' }],
  ['en', { weekday: 'long', month: 'long', day: 'numeric' }],
  ['de', { year: '2-digit', month: 'short' }],
  ['en', { era: 'short', month: 'short' }]
];

var ranges = [
  [1709647629123, 1709654829123],   // two hours apart
  [1709647629123, 1709733629123],   // a day apart
  [1709647629123, 1712326029123],   // a month apart
  [1709647629123, 1741183629123],   // a year apart
  [1709647629123, 1709647629123],   // the same instant
  [1709647629123, 1709647689123]    // a minute apart
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

function parts(list) {
  var out = '';
  for (var i = 0; i < list.length; i++) {
    out += (i ? ' ' : '') + list[i].type + (list[i].source ? '/' + list[i].source : '') + ':"' + escape(list[i].value) + '"';
  }
  return out;
}

var lines = [];

for (var c = 0; c < cases.length; c++) {
  var options = cases[c][1];

  if (options.timeZone === undefined) {
    options = Object.assign({ timeZone: 'UTC' }, options);
  }

  var format = new Intl.DateTimeFormat(cases[c][0], options);
  var head = (c + 1) + ' ' + cases[c][0] + ' ' + JSON.stringify(cases[c][1]);

  for (var i = 0; i < instants.length; i++) {
    lines.push(head + ' ' + instants[i] + ' => "' + escape(format.format(instants[i])) + '" ' + parts(format.formatToParts(instants[i])));
  }

  for (var r = 0; r < ranges.length; r++) {
    lines.push(head + ' range ' + ranges[r][0] + '..' + ranges[r][1] + ' => "' +
      escape(format.formatRange(ranges[r][0], ranges[r][1])) + '" ' + parts(format.formatRangeToParts(ranges[r][0], ranges[r][1])));
  }

  var resolved = format.resolvedOptions();
  var keys = [];
  for (var key in resolved) {
    keys.push(key + '=' + resolved[key]);
  }
  lines.push(head + ' resolved ' + escape(keys.join(' ')));
}

var date = new Date(1709647629123);
lines.push('toLocaleString => ' + escape(date.toLocaleString('en', { timeZone: 'UTC' })) + ' | ' +
  escape(date.toLocaleDateString('de', { timeZone: 'UTC' })) + ' | ' +
  escape(date.toLocaleTimeString('en-US', { timeZone: 'UTC' })) + ' | ' +
  escape(new Date(NaN).toLocaleString('en')));
lines.push('toLocale options => ' + escape(date.toLocaleString('de', { timeZone: 'UTC' })) + ' | ' +
  escape(date.toLocaleDateString('en', { timeZone: 'UTC', month: 'long' })) + ' | ' +
  escape(date.toLocaleTimeString('de', { timeZone: 'UTC', hour: 'numeric' })) + ' | ' +
  escape(date.toLocaleDateString('en', { timeZone: 'UTC', dateStyle: 'full' })));

lines.join('\n');
