// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Intl.DateTimeFormat over Temporal objects, toLocaleString, and the ISO 8601 calendar (JSD-0055):
// each line one formatting and its answer, or the name of the error it threw. It uses no host
// function and completes with its lines, so the reference polyfill on Node and the profile run the
// same text.
(function () {
  var lines = [];
  function line(label, f) {
    var answer;
    try { answer = String(f()); } catch (e) { answer = 'throws ' + e.name; }
    lines.push(label + ' => ' + answer);
  }
  function parts(list) {
    return list.map(function (part) { return part.type + ':' + part.value + (part.source ? '@' + part.source : ''); }).join('|');
  }

  var values = {
    date: new Temporal.PlainDate(2024, 3, 5),
    dateGregory: new Temporal.PlainDate(2024, 3, 5, 'gregory'),
    dateBce: new Temporal.PlainDate(-44, 3, 15, 'gregory'),
    dateTime: new Temporal.PlainDateTime(2024, 3, 5, 13, 4, 5, 678),
    time: new Temporal.PlainTime(0, 30, 45, 123),
    yearMonth: new Temporal.PlainYearMonth(2024, 3, 'gregory', 5),
    monthDay: new Temporal.PlainMonthDay(3, 5, 'gregory'),
    instant: Temporal.Instant.from('2024-03-05T13:04:05.678Z'),
    zoned: Temporal.ZonedDateTime.from('2024-03-05T13:04:05.678+01:00[Europe/Berlin]'),
    yearMonthIso: new Temporal.PlainYearMonth(2024, 3),
    monthDayIso: new Temporal.PlainMonthDay(3, 5)
  };
  var optionSets = [
    {},
    { dateStyle: 'full' }, { dateStyle: 'medium' }, { dateStyle: 'short' },
    { timeStyle: 'medium' }, { timeStyle: 'short' }, { dateStyle: 'long', timeStyle: 'short' },
    { year: 'numeric' }, { month: 'long' }, { day: 'numeric' }, { weekday: 'long' },
    { year: '2-digit', month: 'short', day: '2-digit' }, { era: 'short' },
    { hour: 'numeric', minute: '2-digit' }, { hour: '2-digit', hourCycle: 'h23' }, { second: 'numeric', fractionalSecondDigits: 3 },
    { timeZoneName: 'shortOffset' }, { month: 'long', hour: 'numeric' }
  ];
  var locales = ['en', 'de'];
  var calendars = [undefined, 'iso8601'];

  locales.forEach(function (locale) {
    calendars.forEach(function (calendar) {
      optionSets.forEach(function (options) {
        var full = Object.assign({ timeZone: 'Pacific/Apia' }, options);
        if (calendar) full.calendar = calendar;
        var head = locale + ' ' + JSON.stringify(full);
        var dtf;
        try { dtf = new Intl.DateTimeFormat(locale, full); } catch (e) { lines.push(head + ' => throws ' + e.name); return; }
        Object.keys(values).forEach(function (name) {
          line(head + ' ' + name, function () { return dtf.format(values[name]); });
        });
        line(head + ' date parts', function () { return parts(dtf.formatToParts(values.date)); });
        line(head + ' dateTime range', function () { return dtf.formatRange(values.dateTime, values.dateTime.add({ hours: 5 })); });
        line(head + ' date range parts', function () { return parts(dtf.formatRangeToParts(values.date, values.date.add({ days: 40 }))); });
        line(head + ' mixed range', function () { return dtf.formatRange(values.date, values.dateTime); });
        line(head + ' Date', function () { return dtf.format(Date.UTC(2024, 2, 5, 13, 4, 5, 678)); });
      });
    });
  });

  // toLocaleString, each type, with and without options.
  [['en', undefined], ['de', undefined], ['en', { dateStyle: 'medium' }], ['de', { month: 'long', day: 'numeric' }],
   ['en', { calendar: 'iso8601' }], ['en', { timeZone: 'Asia/Kolkata' }], ['en', { timeZoneName: 'shortOffset' }]].forEach(function (pair) {
    var head = pair[0] + ' ' + JSON.stringify(pair[1]);
    Object.keys(values).forEach(function (name) {
      line('toLocaleString ' + head + ' ' + name, function () { return values[name].toLocaleString(pair[0], pair[1]); });
    });
  });
  line('zoned toLocaleString UTC', function () { return Temporal.ZonedDateTime.from('2024-03-05T13:04Z[UTC]').toLocaleString('en'); });
  line('zoned toLocaleString Kolkata', function () { return Temporal.ZonedDateTime.from('2024-03-05T13:04+05:30[Asia/Kolkata]').toLocaleString('de'); });
  line('supportedValuesOf calendar', function () { return Intl.supportedValuesOf('calendar').filter(function (c) { return c === 'gregory' || c === 'iso8601'; }).join(); });
  line('resolved iso8601', function () { return new Intl.DateTimeFormat('en-u-ca-iso8601').resolvedOptions().calendar; });

  return lines.join('\n');
})();
