// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// The reference, asked what the Windows host's answers are (SJL-17.2, corpus revision 34 as corrected before the
// merge). Run by the reference Node.js v26.12.0 (ICU 78.3, CLDR 48.0):
//
//     node tailoring_dropped.mjs <conformance/vectors/collation> <ci-windows-run-41418502645-revision-34.tsv.gz>
//
// For every collation vector whose script asks for a tailoring (-u-co-phonebk, collation "phonebk" or usage
// "search"), it evaluates the script as written, which must give the vector's out, and again with the tailoring
// dropped (the keyword co-phonebk, the collation option and usage "search" removed), and compares the second with
// what Broiler.JS rendered on CI's windows-latest host. It reads every vector the Windows run reports as a difference
// or a match; a classified divergence differs there for its own row and is listed but not compared. It is a witness of
// what that host answers, never of Broiler.JS's code and never an expected value.
import fs from 'node:fs';
import path from 'node:path';
import zlib from 'node:zlib';

const [dir, report] = process.argv.slice(2);
const runs = new Map();
for (const line of zlib.gunzipSync(fs.readFileSync(report)).toString('utf8').split('\n')) {
  const f = line.split('\t');
  if (f.length === 5 && f[0].startsWith('intl/collation/')) runs.set(f[0], { rendered: f[3], verdict: f[4] });
}
const value = v => JSON.parse(v.replace(/^value: str:/, ''));
const drop = s => s.replace(/-u-co-phonebk"/g, '"').replace(/-u-co-phonebk-/g, '-u-').replace(/collation:"phonebk",?/g, '')
  .replace(/usage:"search",?/g, '').replace(/,\}/g, '}');
const counts = {};
for (const name of fs.readdirSync(dir).filter(n => n.endsWith('.vectors')).sort()) {
  for (const block of fs.readFileSync(path.join(dir, name), 'utf8').split('\n\n')) {
    const field = k => (block.split('\n').find(l => l.startsWith(k + ': ')) || '').slice(k.length + 2);
    const id = field('id'), script = field('script');
    if (!id || !/-u-co-phonebk|collation:"phonebk"|usage:"search"/.test(script)) continue;
    const run = runs.get(id);
    if (!run) throw new Error(`${id}: not in the report`);
    const asWritten = (0, eval)(script) === value(field('out')) ? 'reference=out' : 'reference!=out';
    let result;
    if (run.verdict === 'classified-divergence') result = 'not compared (classified divergence)';
    else result = (0, eval)(drop(script)) === value(run.rendered) ? 'dropped=windows' : 'dropped!=windows';
    const key = `${run.verdict}: ${asWritten}, ${result}`;
    counts[key] = (counts[key] || 0) + 1;
    console.log([id, run.verdict, asWritten, result].join('\t'));
  }
}
console.log(`# ${process.version}, ICU ${process.versions.icu}, CLDR ${process.versions.cldr}`);
for (const key of Object.keys(counts).sort()) console.log(`# ${counts[key]}\t${key}`);
