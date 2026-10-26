// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Segments of German, English and other text, the dataset slice I4's Segmenter record keeps
// (JSD-0050). Each granularity segments each text for en and de, writing every segment with its index
// and, for words, whether it is word-like; a last group answers containing, the iterator and the
// errors ECMA-402 states. It uses no host function and completes with its lines, so Node and the
// profile run the same text. Non-ASCII is escaped.

var texts = [
  'Hello, world! How are you?',
  'Mr. Smith went to Washington. He arrived at 3 p.m. on Jan. 5th.',
  'Die Straße ist 3,5 km lang. Sie führt über die Brücke.',
  '„Guten Tag“, sagte er. „Wie geht’s?“',
  'e-mail: a.b@example.com, URL http://example.com/x_y?z=1',
  'Prices: $3.50, 1,000.25 and 10%.',
  'can’t won\'t rock\'n\'roll',
  'first line\r\nsecond line\nthird fourth',
  'tab\tseparated  double  spaces',
  '👨‍👩‍👧 👍🏽 🇩🇪🇺🇸',
  'é ä क्ष नमस्ते',
  '각 가각',
  '日本語の文章。カタカナとひらがな。',
  'שלום עולם',
  'He said "Stop." Then he left!? Yes... (Really.) OK',
  '\uD800 lone \uDC00 surrogates',
  ''
];
var granularities = ['grapheme', 'word', 'sentence'];
var locales = ['en', 'de'];

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
  for (var g = 0; g < granularities.length; g++) {
    var segmenter = new Intl.Segmenter(locales[l], { granularity: granularities[g] });

    for (var t = 0; t < texts.length; t++) {
      var parts = [];

      for (var segment of segmenter.segment(texts[t])) {
        parts.push(segment.index + ':' + JSON.stringify(segment.segment) +
          (segment.isWordLike === undefined ? '' : segment.isWordLike ? '+' : '-'));
      }

      lines.push(locales[l] + ' ' + granularities[g] + ' ' + t + ' => ' + parts.join(' '));
    }

    lines.push(locales[l] + ' ' + granularities[g] + ' resolvedOptions => ' + JSON.stringify(segmenter.resolvedOptions()));
  }
}

var words = new Intl.Segmenter('en', { granularity: 'word' });
var segments = words.segment('Hello big world');

lines.push('containing => ' + [-1, 0, 4, 5, 6, 8, 14, 15, Infinity, NaN, '7', 4.9].map(function (n) {
  var found = segments.containing(n);
  return found === undefined ? 'undefined' : found.index + ':' + found.segment + ':' + Object.keys(found).join('/');
}).join(' '));

var iterator = segments[Symbol.iterator]();
lines.push('iterator => ' + Object.prototype.toString.call(iterator) + ' ' + JSON.stringify(iterator.next()) + ' ' +
  (Object.getPrototypeOf(Object.getPrototypeOf(iterator)) === Object.getPrototypeOf(Object.getPrototypeOf([][Symbol.iterator]()))));
lines.push('two iterators => ' + Array.from(segments).length + ' ' + Array.from(segments).length);
lines.push('default => ' + JSON.stringify(new Intl.Segmenter().resolvedOptions()));
lines.push('tag => ' + Object.prototype.toString.call(words) + ' ' + Object.prototype.toString.call(segments));
lines.push('number input => ' + Array.from(new Intl.Segmenter('en').segment(12.5)).map(function (s) { return s.segment; }).join('|'));
lines.push('bad granularity => ' + attempt(function () { return new Intl.Segmenter('en', { granularity: 'line' }); }));
lines.push('primitive options => ' + attempt(function () { return new Intl.Segmenter('en', 'word'); }));
lines.push('call => ' + attempt(function () { return Intl.Segmenter(); }));
lines.push('supportedLocalesOf => ' + Intl.Segmenter.supportedLocalesOf(['de-AT', 'en', 'de-DE', 'zz']).join(','));

lines.map(escape).join('\n');
