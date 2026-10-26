// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Intl.DateTimeFormat in the CLDR calendars (JSD-0057): each line one formatting and its answer, or
// the name of the error it threw. It uses no host function and completes with its lines, so the
// reference polyfill on Node and the profile run the same text.
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

  var calendars = ['buddhist', 'chinese', 'coptic', 'dangi', 'ethioaa', 'ethiopic', 'hebrew', 'indian', 'islamic-civil',
                   'islamic-tbla', 'islamic-umalqura', 'japanese', 'persian', 'roc'];
  var locales = ['en', 'de'];

  // Dates chosen for each calendar's months, leap months, eras and new years: a Chinese leap month
  // (2020 and 2023), the Hebrew Adar I and II of 5784 and the Adar of 5785, the last day of Showa,
  // the first of Heisei and of Reiwa, Nowruz, Muharram, the Coptic and Ethiopian thirteenth month,
  // and a date before 1900.
  var dates = ['2024-05-23', '2020-05-23', '2023-03-22', '2024-02-15', '2024-03-15', '2025-03-15', '1989-01-07',
               '1989-01-08', '2019-05-01', '2024-03-20', '2023-07-19', '2023-09-08', '1912-07-30'];
  var optionSets = [
    {},
    { dateStyle: 'full' }, { dateStyle: 'long' }, { dateStyle: 'medium' }, { dateStyle: 'short' },
    { year: 'numeric', month: 'long', day: 'numeric', era: 'short' }, { era: 'long', year: 'numeric' },
    { era: 'narrow', year: 'numeric', month: 'numeric', day: 'numeric' },
    { year: 'numeric', month: 'short' }, { month: 'long', day: 'numeric' }, { month: 'numeric', day: 'numeric' },
    { year: '2-digit', month: '2-digit', day: '2-digit' }, { year: 'numeric' }, { month: 'narrow' },
    { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' }
  ];

  locales.forEach(function (locale) {
    calendars.forEach(function (calendar) {
      optionSets.forEach(function (options) {
        var full = Object.assign({ timeZone: 'UTC', calendar: calendar }, options);
        var head = locale + ' ' + JSON.stringify(full);
        var dtf;
        try { dtf = new Intl.DateTimeFormat(locale, full); } catch (e) { lines.push(head + ' => throws ' + e.name); return; }
        dates.forEach(function (text) {
          line(head + ' ' + text, function () { return dtf.format(Date.parse(text + 'T12:00Z')); });
        });
      });
      var head = locale + ' ' + calendar;
      var tagged = locale + '-u-ca-' + calendar;
      var dtf = new Intl.DateTimeFormat(locale, { timeZone: 'UTC', calendar: calendar, dateStyle: 'long' });
      var era = new Intl.DateTimeFormat(locale, { timeZone: 'UTC', calendar: calendar, year: 'numeric', month: 'long', day: 'numeric', era: 'long' });
      var noon = function (text) { return Date.parse(text + 'T12:00Z'); };
      line(head + ' parts', function () { return parts(dtf.formatToParts(noon('2020-05-23'))); });
      line(head + ' era parts', function () { return parts(era.formatToParts(noon('2023-03-22'))); });
      line(head + ' range in a month', function () { return dtf.formatRange(noon('2024-05-23'), noon('2024-05-25')); });
      line(head + ' range across months', function () { return dtf.formatRange(noon('2024-05-23'), noon('2024-07-02')); });
      line(head + ' range across years', function () { return dtf.formatRange(noon('2023-05-23'), noon('2024-07-02')); });
      line(head + ' range parts', function () { return parts(dtf.formatRangeToParts(noon('2024-05-23'), noon('2024-07-02'))); });
      line(head + ' date and time', function () {
        return new Intl.DateTimeFormat(locale, { timeZone: 'UTC', calendar: calendar, dateStyle: 'medium', timeStyle: 'short', hourCycle: 'h23' }).format(noon('2024-05-23'));
      });
      line(head + ' resolved', function () { return dtf.resolvedOptions().calendar; });
      line(head + ' plain date', function () { return Temporal.PlainDate.from('2024-05-23').withCalendar(calendar).toLocaleString(tagged); });
      line(head + ' plain date long', function () { return Temporal.PlainDate.from('2023-03-22').withCalendar(calendar).toLocaleString(tagged, { dateStyle: 'long' }); });
      line(head + ' plain year-month', function () { return Temporal.PlainDate.from('2024-05-23').withCalendar(calendar).toPlainYearMonth().toLocaleString(tagged); });
      line(head + ' plain month-day', function () { return Temporal.PlainDate.from('2024-05-23').withCalendar(calendar).toPlainMonthDay().toLocaleString(tagged); });
      line(head + ' plain date-time', function () { return Temporal.PlainDateTime.from('2024-05-23T08:30').withCalendar(calendar).toLocaleString(tagged, { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }); });
      line(head + ' zoned', function () { return Temporal.ZonedDateTime.from('2024-05-23T08:30[UTC]').withCalendar(calendar).toLocaleString(tagged, { dateStyle: 'medium' }); });
      line(head + ' plain date in another calendar', function () { return Temporal.PlainDate.from('2024-05-23').withCalendar(calendar).toLocaleString(locale); });
      line(head + ' plain date in gregory', function () {
        return new Intl.DateTimeFormat(locale, { calendar: calendar }).format(Temporal.PlainDate.from('2024-05-23').withCalendar('gregory'));
      });
    });
  });

  // The requested calendar each locale resolves, aliases and fallbacks included, and the list.
  ['buddhist', 'chinese', 'coptic', 'dangi', 'ethioaa', 'ethiopic', 'ethiopic-amete-alem', 'gregory', 'hebrew', 'indian',
   'islamic', 'islamic-civil', 'islamic-rgsa', 'islamic-tbla', 'islamic-umalqura', 'islamicc', 'iso8601', 'japanese',
   'persian', 'roc', 'julian'].forEach(function (calendar) {
    line('resolved option ' + calendar, function () { return new Intl.DateTimeFormat('en', { calendar: calendar }).resolvedOptions().calendar; });
    line('resolved extension ' + calendar, function () { return new Intl.DateTimeFormat('de-u-ca-' + calendar).resolvedOptions().calendar; });
  });
  line('supportedValuesOf calendar', function () { return Intl.supportedValuesOf('calendar').join(); });

  return lines.join('\n');
})();
