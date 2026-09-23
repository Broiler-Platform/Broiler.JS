import { execFileSync } from 'node:child_process';
import { appendFileSync, readFileSync, readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('../', import.meta.url));

const PREVIEW = /^(\d+\.\d+\.\d+)-preview\.([1-9]\d*)$/i;

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
export function chooseVersion(configured, used, { suffix = '', tag = '' } = {}) {
  const { prefix, number: floor } = parsePreview(configured);
  let next = floor;
  for (const version of used) {
    const match = PREVIEW.exec(version);
    if (match && match[1] === prefix && BigInt(match[2]) >= next) {
      next = BigInt(match[2]) + 1n;
    }
  }

  const automatic = `${prefix}-preview.${next}`;
  const requested = tag ? tag.replace(/^v/, '') : suffix ? `${prefix}-${suffix}` : automatic;
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
export function readTags(run = execFileSync) {
  const output = run('git', ['tag', '--list', 'v*'], { cwd: root, encoding: 'utf8' });
  return output.split('\n').map(line => line.trim()).filter(Boolean).map(tag => tag.replace(/^v/, ''));
}

function readPackages() {
  const solutions = readdirSync(root).filter(name => name.endsWith('.slnx'));
  if (solutions.length !== 1) throw new Error('Expected exactly one solution.');
  const solution = readFileSync(resolve(root, solutions[0]), 'utf8');
  const packages = [];
  for (const [, project] of solution.matchAll(/<Project\s+Path="([^"]+)"/g)) {
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
  const packages = readPackages();
  const configured = packages[0].PackageVersion;
  parsePreview(configured);
  const packageIds = packages.map(p => p.PackageId);
  const tag = process.env.GITHUB_EVENT_NAME === 'push'
    ? (process.env.GITHUB_REF || '').replace(/^refs\/tags\//, '') : '';
  if (process.env.GITHUB_EVENT_NAME === 'push' && !tag.startsWith('v')) {
    throw new Error('Publishing on push requires a v-prefixed preview tag.');
  }
  const published = await readVersions('https://api.nuget.org/v3/index.json', packageIds);
  // A tag-triggered run is a REQUEST for its own tag's version, so that one tag is not
  // a number already spent by an earlier run. Every other tag is.
  const requested = tag.replace(/^v/, '');
  const used = [...published, ...readTags().filter(version => version !== requested)];
  const version = chooseVersion(configured, used, { suffix: process.env.VERSION_SUFFIX || '', tag });
  console.log(`Version: ${version} -> nuget.org (${packageIds.length} packages; dry-run: ${process.env.DRY_RUN ?? 'true'})`);
  if (process.env.GITHUB_OUTPUT) {
    appendFileSync(process.env.GITHUB_OUTPUT,
      `version=${version}\nversion_args=-p:Version=${version} -p:PackageVersion=${version}\n`);
  }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main().catch(error => {
    console.error(error.message);
    process.exitCode = 1;
  });
}
