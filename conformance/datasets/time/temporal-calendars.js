// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Temporal in the CLDR calendars (JSD-0056): each line one operation and its answer, or the name of
// the error it threw. The program uses no host function and completes with its lines, so the
// reference polyfill and the profile run the same text.
(function () {
  var lines = [];
  function line(label, f) {
    var answer;
    try { answer = String(f()); } catch (e) { answer = 'throws ' + e.name; }
    lines.push(label + ' => ' + answer);
  }

  var calendars = ['buddhist', 'chinese', 'coptic', 'dangi', 'ethioaa', 'ethiopic', 'gregory', 'hebrew', 'indian',
                   'islamic-civil', 'islamic-tbla', 'islamic-umalqura', 'japanese', 'persian', 'roc'];

  function fields(d) {
    return [d.era, d.eraYear, d.year, d.month, d.monthCode, d.day, d.dayOfYear, d.daysInMonth, d.daysInYear,
            d.monthsInYear, d.inLeapYear].join();
  }

  // ---- ISO dates to each calendar: every 613th day from 1850 to 2050, and dates far from it --------
  var iso = [];
  for (var day = Temporal.PlainDate.from('1850-01-01'); Temporal.PlainDate.compare(day, '2050-12-31') <= 0; day = day.add({ days: 613 })) {
    iso.push(day.toString());
  }
  iso.push('-000500-07-01', '0000-01-01', '0001-01-01', '0622-07-19', '1000-06-15', '1582-10-15', '1700-03-01',
           '1872-12-31', '1873-01-01', '1912-07-30', '1926-12-25', '1989-01-08', '2019-05-01', '2101-01-29',
           '2200-02-01', '3000-09-01', '+010000-01-01');

  calendars.forEach(function (calendar) {
    iso.forEach(function (text) {
      line(calendar + ' ' + text, function () { return fields(Temporal.PlainDate.from(text).withCalendar(calendar)); });
    });
  });

  // ---- From fields, arithmetic and differences in each calendar --------------------------------------
  calendars.forEach(function (calendar) {
    var base = Temporal.PlainDate.from('2023-03-15').withCalendar(calendar);
    var year = base.year;

    line(calendar + ' from year/month/day', function () { return Temporal.PlainDate.from({ calendar: calendar, year: year, month: 1, day: 1 }); });
    line(calendar + ' from monthCode M12 day 30', function () { return Temporal.PlainDate.from({ calendar: calendar, year: year, monthCode: 'M12', day: 30 }); });
    line(calendar + ' from month 13 constrain', function () { return Temporal.PlainDate.from({ calendar: calendar, year: year, month: 13, day: 31 }); });
    line(calendar + ' from month 13 reject', function () { return Temporal.PlainDate.from({ calendar: calendar, year: year, month: 13, day: 1 }, { overflow: 'reject' }); });
    line(calendar + ' from day 31 reject', function () { return Temporal.PlainDate.from({ calendar: calendar, year: year, month: 2, day: 31 }, { overflow: 'reject' }); });
    line(calendar + ' from M13', function () { return Temporal.PlainDate.from({ calendar: calendar, year: year, monthCode: 'M13', day: 1 }); });
    line(calendar + ' from M05L constrain', function () { return Temporal.PlainDate.from({ calendar: calendar, year: year, monthCode: 'M05L', day: 1 }); });
    line(calendar + ' from M05L reject', function () { return Temporal.PlainDate.from({ calendar: calendar, year: year, monthCode: 'M05L', day: 1 }, { overflow: 'reject' }); });
    line(calendar + ' from month and monthCode disagree', function () { return Temporal.PlainDate.from({ calendar: calendar, year: year, month: 2, monthCode: 'M03', day: 1 }); });
    if (base.era !== undefined) {
      line(calendar + ' from era', function () { return Temporal.PlainDate.from({ calendar: calendar, era: base.era, eraYear: base.eraYear, month: 6, day: 10 }); });
      line(calendar + ' from era and wrong year', function () { return Temporal.PlainDate.from({ calendar: calendar, era: base.era, eraYear: base.eraYear, year: year + 1, month: 6, day: 10 }); });
      line(calendar + ' from era without eraYear', function () { return Temporal.PlainDate.from({ calendar: calendar, era: base.era, month: 6, day: 10 }); });
    }
    line(calendar + ' from no year', function () { return Temporal.PlainDate.from({ calendar: calendar, month: 6, day: 10 }); });

    [{ years: 1 }, { years: -3 }, { months: 1 }, { months: 13 }, { months: -25 }, { years: 2, months: 5, days: 40 },
     { weeks: 10 }, { days: -400 }, { years: 19, months: 2 }].forEach(function (duration) {
      var text = JSON.stringify(duration);
      line(calendar + ' add ' + text, function () { return base.add(duration); });
      line(calendar + ' add ' + text + ' to the 30th', function () { return base.with({ day: 30 }).add(duration); });
      line(calendar + ' add ' + text + ' reject from the 30th', function () { return base.with({ day: 30 }).add(duration, { overflow: 'reject' }); });
    });

    ['1999-01-01', '2023-03-14', '2024-02-29', '2030-12-31', '1850-06-06'].forEach(function (other) {
      var target = Temporal.PlainDate.from(other).withCalendar(calendar);
      ['years', 'months', 'weeks', 'days'].forEach(function (unit) {
        line(calendar + ' until ' + other + ' ' + unit, function () { return base.until(target, { largestUnit: unit }); });
      });
      line(calendar + ' since ' + other + ' years', function () { return base.since(target, { largestUnit: 'years' }); });
      line(calendar + ' until ' + other + ' round months', function () {
        return base.until(target, { largestUnit: 'years', smallestUnit: 'months', roundingMode: 'halfExpand' });
      });
    });

    line(calendar + ' with month 1', function () { return base.with({ month: 1 }); });
    line(calendar + ' with monthCode M01 day 29', function () { return base.with({ monthCode: 'M01', day: 29 }); });
    line(calendar + ' with year', function () { return base.with({ year: year - 5 }); });
    line(calendar + ' toPlainYearMonth', function () { return base.toPlainYearMonth(); });
    line(calendar + ' toPlainMonthDay', function () { return base.toPlainMonthDay(); });
    line(calendar + ' year-month add', function () { return base.toPlainYearMonth().add({ months: 7 }); });
    line(calendar + ' year-month subtract', function () { return base.toPlainYearMonth().subtract({ years: 1, months: 1 }); });
    line(calendar + ' year-month until', function () {
      return base.toPlainYearMonth().until(Temporal.PlainDate.from('2031-01-10').withCalendar(calendar).toPlainYearMonth(), { largestUnit: 'years' });
    });
    line(calendar + ' year-month from', function () { return Temporal.PlainYearMonth.from({ calendar: calendar, year: year, month: 2 }); });
    line(calendar + ' duration round relativeTo', function () {
      return Temporal.Duration.from({ days: 1000 }).round({ largestUnit: 'years', relativeTo: base });
    });
    line(calendar + ' duration total relativeTo', function () {
      return Temporal.Duration.from({ days: 1000 }).total({ unit: 'months', relativeTo: base });
    });
    line(calendar + ' zoned add month', function () {
      return base.toZonedDateTime({ timeZone: 'UTC' }).add({ months: 1 }).toString();
    });
  });

  // ---- Month-days: a reference year for each month code and day -------------------------------------
  calendars.forEach(function (calendar) {
    ['M01', 'M02', 'M03', 'M04', 'M05', 'M05L', 'M06', 'M06L', 'M07', 'M08', 'M09', 'M10', 'M11', 'M12', 'M13'].forEach(function (code) {
      [1, 29, 30].forEach(function (d) {
        line(calendar + ' month-day ' + code + '-' + d, function () {
          return Temporal.PlainMonthDay.from({ calendar: calendar, monthCode: code, day: d }).toString();
        });
      });
    });
    line(calendar + ' month-day M02-31 reject', function () {
      return Temporal.PlainMonthDay.from({ calendar: calendar, monthCode: 'M02', day: 31 }, { overflow: 'reject' });
    });
    line(calendar + ' month-day month without year', function () {
      return Temporal.PlainMonthDay.from({ calendar: calendar, month: 2, day: 1 });
    });
    line(calendar + ' month-day to date', function () {
      return Temporal.PlainMonthDay.from({ calendar: calendar, monthCode: 'M02', day: 10 }).toPlainDate({ year: Temporal.PlainDate.from('2001-06-01').withCalendar(calendar).year });
    });
  });

  return lines.join('\n');
})();
