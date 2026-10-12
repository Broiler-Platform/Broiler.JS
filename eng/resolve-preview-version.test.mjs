import assert from 'node:assert/strict';
import test from 'node:test';
import { STREAMS, chooseVersion, readTags, readVersions, streamProjects } from './resolve-preview-version.mjs';

test('first publish uses the configured preview; later publishes increment numerically', () => {
  assert.equal(chooseVersion('0.1.0-preview.1', []), '0.1.0-preview.1');
  assert.equal(chooseVersion('0.1.0-preview.1', ['0.1.0-preview.1']), '0.1.0-preview.2');
  assert.equal(chooseVersion('0.1.0-preview.1', ['0.1.0-preview.9', '0.1.0-preview.10']), '0.1.0-preview.11');
});

test('configured preview is a floor and other release lines do not affect it', () => {
  assert.equal(chooseVersion('0.1.0-preview.4', [
    '0.1.0-preview.1', '0.2.0-preview.99', '0.1.0', '0.1.0-rc.9',
  ]), '0.1.0-preview.4');
});

test('a number spent anywhere is spent everywhere', () => {
  // nuget.org reports preview.2 while a tag records that preview.3 was already
  // claimed: the next publish is preview.4, never a second preview.3.
  assert.equal(chooseVersion('0.1.0-preview.1',
    ['0.1.0-preview.2', '0.1.0-preview.3']), '0.1.0-preview.4');
  // Order of the union must not matter.
  assert.equal(chooseVersion('0.1.0-preview.1',
    ['0.1.0-preview.3', '0.1.0-preview.2']), '0.1.0-preview.4');
  // A gap left by a deleted or never-listed version is not reused either.
  assert.equal(chooseVersion('0.1.0-preview.1',
    ['0.1.0-preview.1', '0.1.0-preview.7']), '0.1.0-preview.8');
});

test('only unused previews on the configured release line are accepted', () => {
  const used = ['0.1.0-preview.1'];
  assert.equal(chooseVersion('0.1.0-preview.1', used, { suffix: 'preview.3' }), '0.1.0-preview.3');
  assert.equal(chooseVersion('0.1.0-preview.1', used, { tag: 'v0.1.0-preview.2' }), '0.1.0-preview.2');
  for (const suffix of ['preview.1', 'rc.2', 'preview.0', 'preview.02', 'preview.2;evil']) {
    assert.throws(() => chooseVersion('0.1.0-preview.1', used, { suffix }));
  }
  for (const tag of ['v0.1.0', 'v0.1.0-rc.2', 'v0.2.0-preview.2', 'v0.1.0-preview.1']) {
    assert.throws(() => chooseVersion('0.1.0-preview.1', used, { tag }));
  }
  assert.throws(() => chooseVersion('0.1.0', used));
});

test('publish tags are read as versions and a failure is not treated as "no tags"', () => {
  assert.deepEqual(readTags(() => 'v0.1.0-preview.1\nv0.1.0-preview.2\n'),
    ['0.1.0-preview.1', '0.1.0-preview.2']);
  assert.deepEqual(readTags(() => '  v0.1.0-preview.3  \n\n'), ['0.1.0-preview.3']);
  assert.deepEqual(readTags(() => ''), []);
  assert.throws(() => readTags(() => { throw new Error('not a git repository'); }));
});

function fakeFeed(responses) {
  return async (url, options) => {
    assert.ok(options.signal);
    if (url === 'https://feed/index.json') return Response.json({
      resources: [{ '@type': 'PackageBaseAddress/3.0.0', '@id': 'https://feed/flat/' }],
    });
    assert.ok(Object.hasOwn(responses, url), `Unexpected request ${url}`);
    const response = responses[url];
    return typeof response === 'number' ? new Response(null, { status: response }) : Response.json(response);
  };
}

test('all packages contribute, including a partially published newer preview', async () => {
  const versions = await readVersions('https://feed/index.json', ['Core', 'Provider', 'New'], {}, fakeFeed({
    'https://feed/flat/core/index.json': { versions: ['0.1.0-preview.1'] },
    'https://feed/flat/provider/index.json': { versions: ['0.1.0-preview.1', '0.1.0-preview.2'] },
    'https://feed/flat/new/index.json': 404,
  }));
  assert.equal(chooseVersion('0.1.0-preview.1', versions), '0.1.0-preview.3');
});

test('feed failures and malformed responses stop publication', async () => {
  for (const response of [401, 403, 429, 500, {}, { versions: [2] }]) {
    await assert.rejects(readVersions('https://feed/index.json', ['Core'], {}, fakeFeed({
      'https://feed/flat/core/index.json': response,
    })));
  }
  await assert.rejects(readVersions('https://feed/index.json', ['Core'], {}, async () => {
    throw new Error('Network unavailable');
  }));
  await assert.rejects(readVersions('https://feed/index.json', ['Core'], {}, async () => Response.json({})));
});

test('each stream reads only its own tags, by its own prefix', () => {
  // The shared stream's tags are ecmascript-v*; git is asked for that prefix, and a name that does
  // not carry it is not read as a version even if the runner returns one.
  let asked;
  const shared = readTags((program, args) => { asked = args; return 'ecmascript-v0.1.0-preview.2\nv0.1.0-preview.9\n'; }, 'ecmascript-v');
  assert.deepEqual(asked, ['tag', '--list', 'ecmascript-v*']);
  assert.deepEqual(shared, ['0.1.0-preview.2']);
  assert.deepEqual(readTags(() => 'v0.1.0-preview.3\n'), ['0.1.0-preview.3']);
});

test('a tag-requested version must carry the stream prefix', () => {
  const prefix = STREAMS.shared.tagPrefix;
  assert.equal(chooseVersion('0.1.0-preview.1', [], { tag: 'ecmascript-v0.1.0-preview.1', tagPrefix: prefix }), '0.1.0-preview.1');
  assert.throws(() => chooseVersion('0.1.0-preview.1', [], { tag: 'v0.1.0-preview.1', tagPrefix: prefix }));
  assert.throws(() => chooseVersion('0.1.0-preview.1', [], { tag: 'ecmascript-v0.1.0-probe.1', tagPrefix: prefix }));
});

test('the streams partition the projects: the shared filter, and everything else', () => {
  const files = {
    'Broiler.JS.slnx': '<Solution><Project Path="Broiler.ECMAScript/A/A.csproj" /><Project Path="Broiler.JS/B/B.csproj" /></Solution>',
    'Broiler.ECMAScript.slnf': JSON.stringify({ solution: { path: 'Broiler.JS.slnx', projects: ['Broiler.ECMAScript/A/A.csproj'] } }),
  };
  const read = path => files[path];
  const list = () => Object.keys(files);
  assert.deepEqual(streamProjects(STREAMS.engine, read, list), ['Broiler.JS/B/B.csproj']);
  assert.deepEqual(streamProjects(STREAMS.shared, read, list), ['Broiler.ECMAScript/A/A.csproj']);
  files['Broiler.ECMAScript.slnf'] = JSON.stringify({ solution: { projects: ['Broiler.JS/B/B.csproj'] } });
  assert.throws(() => streamProjects(STREAMS.shared, read, list));
});
