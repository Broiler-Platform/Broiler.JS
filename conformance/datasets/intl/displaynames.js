// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// German and English display names, the dataset slice I4's DisplayNames record keeps (JSD-0052).
// Each type, style, language display and fallback names codes of every shape in en and de: languages
// with scripts, regions and variants, regions and areas, scripts, currencies, calendars and date-time
// fields, and codes the data does not name; a last group answers the errors ECMA-402 states. It uses
// no host function and completes with its lines, so Node and the profile run the same text.
// Non-ASCII is escaped.

var codes = {
  language: ['en', 'de', 'en-US', 'en-GB', 'en-AU', 'de-AT', 'de-CH', 'fr-CA', 'es-419', 'pt-BR', 'nl-BE',
    'zh-Hans', 'zh-Hant', 'zh-Hant-TW', 'zh-Hans-CN', 'sr-Latn', 'sr-Cyrl-RS', 'en-Latn-US', 'de-1996',
    'sl-rozaj-biske', 'ja-Kana', 'ar-001', 'xyz', 'xyz-DE', 'en-XY', 'und', 'zxx', 'mul', 'iw', 'in', 'sh', 'EN-us'],
  region: ['US', 'GB', 'DE', 'AT', 'CH', '419', '001', '150', 'HK', 'MO', 'PS', 'BA', 'MM', 'CI', 'CZ', 'XK',
    'ZZ', 'AA', '999', 'us', 'de'],
  script: ['Latn', 'Cyrl', 'Hans', 'Hant', 'Arab', 'Zzzz', 'Zyyy', 'Zsye', 'Qaaa', 'latn', 'HANS'],
  currency: ['USD', 'EUR', 'CHF', 'JPY', 'GBP', 'BTN', 'XYZ', 'usd', 'eur'],
  calendar: ['gregory', 'buddhist', 'islamic-civil', 'islamic-umalqura', 'iso8601', 'ethioaa', 'roc', 'japanese',
    'chinese', 'xyz', 'GREGORY'],
  dateTimeField: ['era', 'year', 'quarter', 'month', 'weekOfYear', 'weekday', 'day', 'dayPeriod', 'hour', 'minute',
    'second', 'timeZoneName']
};
var invalid = {
  language: ['', 'e', 'en-', 'en_US', 'en-u-ca-gregory', 'root', 'en-x-private', '123'],
  region: ['', 'U', 'USA', '1234', 'u1'],
  script: ['', 'Lat', 'Latin', 'La1n'],
  currency: ['', 'US', 'USDD', 'U1D'],
  calendar: ['', 'gregory-', 'gregory_', 'ab', 'abcdefghi'],
  dateTimeField: ['', 'Year', 'years', 'week', 'zone']
};
var locales = ['en', 'de'];
var styles = ['long', 'short', 'narrow'];

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
  for (var type in codes) {
    var displays = type === 'language' ? ['dialect', 'standard'] : [undefined];

    for (var s = 0; s < styles.length; s++) {
      for (var d = 0; d < displays.length; d++) {
        var options = { type: type, style: styles[s] };

        if (displays[d]) {
          options.languageDisplay = displays[d];
        }

        var names = new Intl.DisplayNames(locales[l], options);
        var none = new Intl.DisplayNames(locales[l], Object.assign({ fallback: 'none' }, options));
        var head = locales[l] + ' ' + JSON.stringify(options);

        for (var c = 0; c < codes[type].length; c++) {
          lines.push(head + ' ' + codes[type][c] + ' => ' + attempt(function () { return names.of(codes[type][c]); }) +
            ' | ' + attempt(function () { return none.of(codes[type][c]); }));
        }

        lines.push(head + ' resolvedOptions => ' + JSON.stringify(names.resolvedOptions()));
      }
    }
  }
}

for (var kind in invalid) {
  var plain = new Intl.DisplayNames('en', { type: kind });
  lines.push(kind + ' invalid => ' + invalid[kind].map(function (code) {
    return JSON.stringify(code) + ':' + attempt(function () { return plain.of(code); });
  }).join(' '));
}

lines.push('no options => ' + attempt(function () { return new Intl.DisplayNames('en'); }));
lines.push('no type => ' + attempt(function () { return new Intl.DisplayNames('en', {}); }));
lines.push('bad type => ' + attempt(function () { return new Intl.DisplayNames('en', { type: 'unit' }); }));
lines.push('primitive options => ' + attempt(function () { return new Intl.DisplayNames('en', 'region'); }));
lines.push('call => ' + attempt(function () { return Intl.DisplayNames(); }));
lines.push('symbol code => ' + attempt(function () { return new Intl.DisplayNames('en', { type: 'region' }).of(Symbol()); }));
lines.push('supportedLocalesOf => ' + Intl.DisplayNames.supportedLocalesOf(['de-AT', 'en', 'de-DE', 'zz']).join(','));

lines.map(escape).join('\n');
