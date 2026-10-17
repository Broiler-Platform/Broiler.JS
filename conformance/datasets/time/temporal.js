// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Temporal in the ISO 8601 and Gregorian calendars (JSD-0054): each line one operation and its
// answer, or the name of the error it threw. The program uses no host function and completes with
// its lines, so the reference polyfill and the profile run the same text.
(function () {
  var lines = [];
  function line(label, f) {
    var answer;
    try { answer = String(f()); } catch (e) { answer = 'throws ' + e.name; }
    lines.push(label + ' => ' + answer);
  }

  var units = ['year', 'month', 'week', 'day', 'hour', 'minute', 'second', 'millisecond', 'microsecond', 'nanosecond'];
  var modes = ['ceil', 'floor', 'expand', 'trunc', 'halfCeil', 'halfFloor', 'halfExpand', 'halfTrunc', 'halfEven'];

  // ---- Duration --------------------------------------------------------------------------------
  var durations = ['P1Y2M3W4DT5H6M7.008009010S', '-P1Y6M', 'PT49H', 'P45D', 'PT0.000000001S', '-PT36H30M',
                   'P2Y11M30DT23H59M59.999999999S', 'PT90061.5S', 'P1W', '-P400D'];
  durations.forEach(function (text) {
    var d = Temporal.Duration.from(text);
    line('duration ' + text + ' sign/blank', function () { return d.sign + '/' + d.blank; });
    line('duration ' + text + ' negated abs', function () { return d.negated().toString() + ' ' + d.abs().toString(); });
    ['2020-01-31', '2019-02-28', '2024-02-29T12:00[America/New_York]', '2021-03-28T01:30[Europe/Berlin]'].forEach(function (rel) {
      ['year', 'month', 'week', 'day', 'hour'].forEach(function (largest) {
        line('duration ' + text + ' round largest ' + largest + ' rel ' + rel, function () {
          return d.round({ largestUnit: largest, relativeTo: rel });
        });
      });
      ['year', 'month', 'week', 'day', 'hour', 'second'].forEach(function (unit) {
        line('duration ' + text + ' total ' + unit + ' rel ' + rel, function () {
          return d.total({ unit: unit, relativeTo: rel });
        });
      });
      modes.forEach(function (mode) {
        line('duration ' + text + ' round month ' + mode + ' rel ' + rel, function () {
          return d.round({ smallestUnit: 'month', roundingMode: mode, relativeTo: rel });
        });
      });
    });
    line('duration ' + text + ' round day no rel', function () { return d.round({ largestUnit: 'day', smallestUnit: 'hour', roundingIncrement: 3 }); });
    line('duration ' + text + ' toString 2 digits', function () { return d.toString({ fractionalSecondDigits: 2, roundingMode: 'halfExpand' }); });
    line('duration ' + text + ' add PT23H', function () { return d.add('PT23H'); });
  });
  line('duration compare months rel', function () {
    return Temporal.Duration.compare({ months: 1 }, { days: 30 }, { relativeTo: '2021-02-01' }) + ' ' +
      Temporal.Duration.compare({ months: 1 }, { days: 30 }, { relativeTo: '2021-01-01' });
  });
  line('duration invalid mixed signs', function () { return new Temporal.Duration(1, -1); });
  line('duration max seconds', function () { return Temporal.Duration.from({ seconds: Number.MAX_SAFE_INTEGER }).toString(); });
  line('duration too large', function () { return Temporal.Duration.from({ seconds: Number.MAX_SAFE_INTEGER + 1 }); });

  // ---- PlainDate --------------------------------------------------------------------------------
  var dates = ['2020-02-29', '2021-01-31', '1999-12-31', '-000001-03-01', '+275760-09-13', '1970-01-01'];
  dates.forEach(function (text) {
    var date = Temporal.PlainDate.from(text);
    line('date ' + text + ' fields', function () {
      return [date.year, date.month, date.monthCode, date.day, date.dayOfWeek, date.dayOfYear, date.weekOfYear, date.yearOfWeek,
              date.daysInMonth, date.daysInYear, date.inLeapYear].join();
    });
    ['P1M', 'P1Y', '-P1M', 'P1Y1M1D', 'P10W', '-P400D'].forEach(function (d) {
      line('date ' + text + ' add ' + d, function () { return date.add(d); });
      line('date ' + text + ' add ' + d + ' reject', function () { return date.add(d, { overflow: 'reject' }); });
    });
    ['2020-01-31', '2023-03-01', '2000-02-29'].forEach(function (other) {
      ['year', 'month', 'week', 'day'].forEach(function (largest) {
        line('date ' + text + ' until ' + other + ' ' + largest, function () { return date.until(other, { largestUnit: largest }); });
        line('date ' + text + ' since ' + other + ' ' + largest + ' round month', function () {
          return date.since(other, { largestUnit: largest === 'week' || largest === 'day' ? 'year' : largest, smallestUnit: 'month', roundingMode: 'halfEven' });
        });
      });
    });
    line('date ' + text + ' gregory', function () {
      var g = date.withCalendar('gregory');
      return g.era + ' ' + g.eraYear + ' ' + g.toString() + ' ' + g.weekOfYear;
    });
    line('date ' + text + ' toZonedDateTime New York', function () { return date.toZonedDateTime('America/New_York'); });
  });
  line('date from fields constrain', function () { return Temporal.PlainDate.from({ year: 2021, month: 2, day: 31 }); });
  line('date from fields reject', function () { return Temporal.PlainDate.from({ year: 2021, month: 2, day: 31 }, { overflow: 'reject' }); });
  line('date from monthCode', function () { return Temporal.PlainDate.from({ year: 2021, monthCode: 'M12', day: 25 }); });
  line('date from era', function () { return Temporal.PlainDate.from({ era: 'bce', eraYear: 10, month: 1, day: 1, calendar: 'gregory' }); });
  line('date with', function () { return Temporal.PlainDate.from('2021-03-31').with({ month: 4 }); });
  line('date string with calendar', function () { return Temporal.PlainDate.from('2021-03-31[u-ca=gregory]').toString({ calendarName: 'always' }); });
  line('date string bad', function () { return Temporal.PlainDate.from('2021-13-01'); });

  // ---- PlainTime and PlainDateTime ---------------------------------------------------------------
  var time = Temporal.PlainTime.from('13:45:30.123456789');
  units.slice(4).forEach(function (unit) {
    modes.forEach(function (mode) {
      line('time round ' + unit + ' ' + mode, function () { return time.round({ smallestUnit: unit, roundingMode: mode, roundingIncrement: unit === 'hour' ? 6 : 1 }); });
    });
    line('time until 02:00 ' + unit, function () { return time.until('02:00', { largestUnit: unit }); });
  });
  var dateTime = Temporal.PlainDateTime.from('2021-12-31T23:59:59.999999999');
  ['day', 'hour', 'minute', 'second', 'millisecond'].forEach(function (unit) {
    line('datetime round ' + unit, function () { return dateTime.round(unit); });
  });
  ['year', 'month', 'week', 'day', 'hour'].forEach(function (largest) {
    line('datetime until 2020-02-29T12:00 ' + largest, function () { return dateTime.until('2020-02-29T12:00', { largestUnit: largest }); });
  });
  line('datetime toString digits', function () { return dateTime.toString({ fractionalSecondDigits: 4 }) + ' ' + dateTime.toString({ smallestUnit: 'minute' }); });

  // ---- ZonedDateTime -----------------------------------------------------------------------------
  var zones = ['America/New_York', 'Europe/Berlin', 'Australia/Sydney', 'Asia/Kolkata', 'Pacific/Apia', '+05:30', 'UTC'];
  var instants = ['2011-12-29T12:00Z', '2021-03-14T06:30Z', '2021-03-28T00:30Z', '2021-10-03T15:59:59Z', '2021-11-07T05:30Z'];
  zones.forEach(function (zone) {
    instants.forEach(function (text) {
      var zoned = Temporal.Instant.from(text).toZonedDateTimeISO(zone);
      line('zoned ' + zone + ' ' + text, function () { return zoned.toString() + ' ' + zoned.hoursInDay + ' ' + zoned.offsetNanoseconds; });
      line('zoned ' + zone + ' ' + text + ' startOfDay', function () { return zoned.startOfDay(); });
      line('zoned ' + zone + ' ' + text + ' add P1D PT24H', function () { return zoned.add('P1D').toString() + ' ' + zoned.add('PT24H').toString(); });
      line('zoned ' + zone + ' ' + text + ' round day', function () { return zoned.round({ smallestUnit: 'day', roundingMode: 'halfExpand' }); });
      line('zoned ' + zone + ' ' + text + ' next', function () { var t = zoned.getTimeZoneTransition('next'); return t && t.toString(); });
      line('zoned ' + zone + ' ' + text + ' previous', function () { var t = zoned.getTimeZoneTransition('previous'); return t && t.toString(); });
      line('zoned ' + zone + ' ' + text + ' until +P1M', function () {
        return zoned.until(zoned.add({ months: 1, hours: 5 }), { largestUnit: 'month', smallestUnit: 'hour', roundingMode: 'halfExpand' });
      });
    });
    ['2021-03-14T02:30', '2021-11-07T01:30', '2021-03-28T02:30', '2021-10-03T02:30', '2011-12-30T12:00'].forEach(function (wall) {
      ['compatible', 'earlier', 'later', 'reject'].forEach(function (d) {
        line('wall ' + wall + ' in ' + zone + ' ' + d, function () {
          return Temporal.PlainDateTime.from(wall).toZonedDateTime(zone, { disambiguation: d });
        });
      });
    });
  });
  line('zoned from offset prefer', function () { return Temporal.ZonedDateTime.from('2021-11-07T01:30-04:00[America/New_York]'); });
  line('zoned from offset mismatch', function () { return Temporal.ZonedDateTime.from('2021-07-01T12:00+01:00[America/New_York]'); });
  line('zoned from offset ignore', function () { return Temporal.ZonedDateTime.from('2021-07-01T12:00+01:00[America/New_York]', { offset: 'ignore' }); });
  line('zoned with keeps offset', function () { return Temporal.ZonedDateTime.from('2021-11-07T01:30-05:00[America/New_York]').with({ minute: 45 }); });
  line('zoned toString options', function () {
    var z = Temporal.ZonedDateTime.from('2021-11-07T01:30:00.5-05:00[America/New_York]');
    return z.toString({ timeZoneName: 'never', offset: 'never', smallestUnit: 'second', roundingMode: 'ceil' }) + ' ' +
      z.toString({ timeZoneName: 'critical', calendarName: 'always' });
  });

  // ---- Instant -----------------------------------------------------------------------------------
  var instant = Temporal.Instant.from('1969-12-31T23:59:59.999999999Z');
  ['hour', 'minute', 'second', 'millisecond'].forEach(function (unit) {
    modes.forEach(function (mode) {
      line('instant round ' + unit + ' ' + mode, function () { return instant.round({ smallestUnit: unit, roundingMode: mode }); });
    });
  });
  ['2001-09-09T01:46:40.123987500Z', '2001-09-09T01:46:40.123988500Z', '-000001-01-01T00:00:00.5Z'].forEach(function (text) {
    modes.forEach(function (mode) {
      line('instant tie ' + text + ' ' + mode, function () {
        var tie = Temporal.Instant.from(text);
        return tie.round({ smallestUnit: 'microsecond', roundingMode: mode }).toString() + ' ' + tie.toString({ smallestUnit: 'second', roundingMode: mode });
      });
    });
  });
  line('instant epoch', function () { return instant.epochMilliseconds + ' ' + instant.epochNanoseconds; });
  line('instant toString zone', function () { return instant.toString({ timeZone: 'Asia/Kolkata' }); });
  line('instant until', function () { return instant.until('2021-01-01T00:00Z', { largestUnit: 'hour', smallestUnit: 'minute' }); });
  line('instant from offset', function () { return Temporal.Instant.from('2021-01-01T00:00+05:30[Asia/Kolkata]'); });
  line('instant out of range', function () { return Temporal.Instant.fromEpochMilliseconds(8.64e15 + 1); });

  // ---- PlainYearMonth and PlainMonthDay ----------------------------------------------------------
  line('yearmonth add', function () { return Temporal.PlainYearMonth.from('2021-01').add({ months: 13 }); });
  line('yearmonth until', function () { return Temporal.PlainYearMonth.from('2019-07').until('2021-03', { largestUnit: 'year' }); });
  line('yearmonth fields', function () { var ym = Temporal.PlainYearMonth.from('2024-02'); return ym.daysInMonth + ' ' + ym.inLeapYear + ' ' + ym.monthCode; });
  line('monthday leap', function () { return Temporal.PlainMonthDay.from('02-29').toString() + ' ' + Temporal.PlainMonthDay.from({ month: 2, day: 30 }).toString(); });
  line('monthday toPlainDate', function () { return Temporal.PlainMonthDay.from('02-29').toPlainDate({ year: 2021 }); });

  return lines.join('\n');
})();
