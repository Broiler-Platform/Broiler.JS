import { execFileSync } from 'node:child_process';
import { appendFileSync, readFileSync, readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('../', import.meta.url));

const PREVIEW = /^(\d+\.\d+\.\d+)-preview\.([1-9]\d*)$/i;

/**
 * The repository's two version streams (decision SJL-D-3). Each has its own projects, its own tag
 * prefix and so its own record of spent numbers: the engine's packages are tagged v*, the shared
 * libraries' ecmascript-v*, and neither stream's tags are read as the other's. `git tag --list v*`
 * cannot match an ecmascript-v tag, because a glob is anchored at the start of the name.
 */
export const STREAMS = {
  engine: { tagPrefix: 'v', solutionFilter: null },
  shared: { tagPrefix: 'ecmascript-v', solutionFilter: 'Broiler.ECMAScript.slnf' },
};

const SHARED_FOLDER = 'Broiler.ECMAScript/';

function parsePreview(version) {
  const match = PREVIEW.exec(version);
  if (!match) throw new Error(`Only X.Y.Z-preview.N versions (N >= 1) may be published: '${version}'.`);
  return { prefix: match[1], number: BigInt(match[2]) };
}

/**
 * Pick the next preview number from every number this repository has ever SPENT,
 * not merely from what nuget.org still serves.
 *
 * A preview number is spent the moment a publish run claims it, and it stays spent
 * afterwards: nuget.org keeps unlisted versions in its flat container but drops
 * deleted ones, and a run that pushed some packages and then failed leaves the rest
 * of the set missing from the feed entirely. Reading the feed alone would hand the
 * same number to a second, different build. So `used` is the union of the feed and
 * the repository's own v* publish tags, and the answer is one past the highest
 * number in it - cumulative, and monotonic even across a feed that forgets.
 */
export function chooseVersion(configured, used, { suffix = '', tag = '', tagPrefix = 'v' } = {}) {
  const { prefix, number: floor } = parsePreview(configured);
  let next = floor;
  for (const version of used) {
    const match = PREVIEW.exec(version);
    if (match && match[1] === prefix && BigInt(match[2]) >= next) {
      next = BigInt(match[2]) + 1n;
    }
  }

  const automatic = `${prefix}-preview.${next}`;
  if (tag && !tag.startsWith(tagPrefix)) {
    throw new Error(`The tag '${tag}' does not start with this stream's prefix '${tagPrefix}'.`);
  }
  const requested = tag ? tag.slice(tagPrefix.length) : suffix ? `${prefix}-${suffix}` : automatic;
  const preview = parsePreview(requested);
  if (preview.prefix !== prefix || preview.number < next) {
    throw new Error(`Requested '${requested}' must use ${prefix} and be at least '${automatic}'.`);
  }
  if (tag && suffix && requested !== `${prefix}-${suffix}`) {
    throw new Error('The tag and version suffix must agree.');
  }
  return requested;
}

async function readJson(url, headers, allowMissing, fetchImpl) {
  const response = await fetchImpl(url, { headers, signal: AbortSignal.timeout(30_000) });
  if (allowMissing && response.status === 404) return null;
  if (!response.ok) throw new Error(`Version lookup failed: HTTP ${response.status} from ${url}`);
  return response.json();
}

// PackageBaseAddress includes unlisted versions. Search results do not.
// https://learn.microsoft.com/nuget/api/package-base-address-resource
export async function readVersions(source, packageIds, headers = {}, fetchImpl = fetch) {
  const index = await readJson(source, headers, false, fetchImpl);
  const base = index.resources?.find(resource =>
    resource['@type'] === 'PackageBaseAddress/3.0.0')?.['@id'];
  if (!base) throw new Error(`No PackageBaseAddress resource in ${source}.`);
  const results = await Promise.all(packageIds.map(async packageId => {
    const url = `${base.replace(/\/$/, '')}/${packageId.toLowerCase()}/index.json`;
    const result = await readJson(url, headers, true, fetchImpl);
    if (result === null) return [];
    if (!Array.isArray(result.versions) || result.versions.some(value => typeof value !== 'string')) {
      throw new Error(`Invalid package version response for ${packageId}.`);
    }
    return result.versions;
  }));
  return results.flat();
}

/**
 * The versions this repository's v* tags record. A publish run tags the version it
 * pushed, so the tags outlive anything the feed stops reporting; the caller must have
 * fetched them, because an unfetched tag is silently an unspent number.
 */
export function readTags(run = execFileSync, tagPrefix = 'v') {
  const output = run('git', ['tag', '--list', `${tagPrefix}*`], { cwd: root, encoding: 'utf8' });
  return output.split('\n').map(line => line.trim()).filter(Boolean)
    .filter(tag => tag.startsWith(tagPrefix)).map(tag => tag.slice(tagPrefix.length));
}

/** The projects of a stream: the shared solution filter's, or every other project of the solution. */
export function streamProjects(stream, read = path => readFileSync(resolve(root, path), 'utf8'), list = () => readdirSync(root)) {
  if (stream.solutionFilter) {
    const projects = JSON.parse(read(stream.solutionFilter)).solution.projects;
    for (const project of projects) {
      if (!project.startsWith(SHARED_FOLDER)) throw new Error(`${stream.solutionFilter} names ${project}, outside ${SHARED_FOLDER}.`);
    }
    return projects;
  }
  const solutions = list().filter(name => name.endsWith('.slnx'));
  if (solutions.length !== 1) throw new Error('Expected exactly one solution.');
  const solution = read(solutions[0]);
  return [...solution.matchAll(/<Project\s+Path="([^"]+)"/g)].map(match => match[1])
    .filter(project => !project.replace(/\\/g, '/').startsWith(SHARED_FOLDER));
}

function readPackages(stream) {
  const packages = [];
  for (const project of streamProjects(stream)) {
    const output = execFileSync('dotnet', [
      'msbuild', project, '-nologo', '-p:Configuration=Release',
      '-getProperty:IsPackable,PackageId,PackageVersion',
    ], { cwd: root, encoding: 'utf8' });
    const properties = JSON.parse(output).Properties;
    if (properties.IsPackable.toLowerCase() === 'true') packages.push(properties);
  }
  if (!packages.length) throw new Error('No packable projects found in the solution.');
  if (new Set(packages.map(p => p.PackageVersion)).size !== 1) {
    throw new Error('All packages must share the configured preview version.');
  }
  return packages;
}

async function main() {
  const name = process.env.PROJECT_SET || 'engine';
  const stream = STREAMS[name];
  if (!stream) throw new Error(`PROJECT_SET must be one of ${Object.keys(STREAMS).join(', ')}: '${name}'.`);
  const packages = readPackages(stream);
  const configured = packages[0].PackageVersion;
  parsePreview(configured);
  const packageIds = packages.map(p => p.PackageId);
  const tag = process.env.GITHUB_EVENT_NAME === 'push'
    ? (process.env.GITHUB_REF || '').replace(/^refs\/tags\//, '') : '';
  if (process.env.GITHUB_EVENT_NAME === 'push' && !tag.startsWith(stream.tagPrefix)) {
    throw new Error(`Publishing the ${name} stream on push requires a ${stream.tagPrefix}-prefixed preview tag.`);
  }
  const published = await readVersions('https://api.nuget.org/v3/index.json', packageIds);
  // A tag-triggered run is a REQUEST for its own tag's version, so that one tag is not
  // a number already spent by an earlier run. Every other tag is.
  const requested = tag.slice(stream.tagPrefix.length);
  const used = [...published, ...readTags(execFileSync, stream.tagPrefix).filter(version => version !== requested)];
  const version = chooseVersion(configured, used, { suffix: process.env.VERSION_SUFFIX || '', tag, tagPrefix: stream.tagPrefix });
  console.log(`Version: ${version} -> nuget.org (${name} stream, ${packageIds.length} packages; dry-run: ${process.env.DRY_RUN ?? 'true'})`);
  if (process.env.GITHUB_OUTPUT) {
    appendFileSync(process.env.GITHUB_OUTPUT,
      `version=${version}\ntag=${stream.tagPrefix}${version}\nversion_args=-p:Version=${version} -p:PackageVersion=${version}\n`);
  }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main().catch(error => {
    console.error(error.message);
    process.exitCode = 1;
  });
}
