#!/usr/bin/env node
// Test-only gh executable. All invocations share the fixture's temporary state file.
const fs = require('node:fs');
const crypto = require('node:crypto');
const path = require('node:path');
const args = process.argv.slice(2);
const state = JSON.parse(fs.readFileSync(process.env.MOCK_STATE, 'utf8'));
const log = (value) => fs.appendFileSync(process.env.MOCK_LOG, value + '\n');
const save = () => fs.writeFileSync(process.env.MOCK_STATE, JSON.stringify(state));

if (args[0] === 'api' && args.includes('--paginate')) {
  log('list');
  console.log(JSON.stringify(state.release ? [state.release] : []));
} else if (args[0] === 'api' && args[1].includes('/git/ref/tags/')) {
  log('tag');
  if (!state.tagSha) { process.stderr.write('(HTTP 404)\n'); process.exit(1); }
  console.log('commit\t' + state.tagSha);
} else if (args[0] === 'release' && args[1] === 'delete') {
  log('delete');
  if (!state.release?.draft) process.exit(1);
  state.release = null;
  state.assetContents = {};
  save();
} else if (args[0] === 'release' && args[1] === 'create') {
  log('create');
  if (state.release) process.exit(1);
  const files = args.slice(3, args.indexOf('--repo'));
  const sha = args[args.indexOf('--target') + 1];
  // Retain uploaded bytes so a later 'gh release download' returns the original manifest.
  state.assetContents = Object.fromEntries(files.map((file) => [
    path.basename(file), fs.readFileSync(file).toString('base64'),
  ]));
  state.release = {
    id: 42, html_url: 'https://github.com/acme/repo/releases/tag/' + args[2],
    tag_name: args[2], target_commitish: sha, draft: true, immutable: false,
    assets: files.map((file) => ({ name: path.basename(file),
      digest: 'sha256:' + crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex') })),
  };
  save();
} else if (args[0] === 'release' && args[1] === 'download') {
  log('download');
  const name = args[args.indexOf('--pattern') + 1];
  const dir = args[args.indexOf('--dir') + 1];
  if (!state.assetContents?.[name]) process.exit(1);
  fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(path.join(dir, name), Buffer.from(state.assetContents[name], 'base64'));
} else if (args[0] === 'release' && args[1] === 'edit') {
  log('publish');
  if (!state.release?.draft) process.exit(1);
  state.release.draft = false;
  state.release.immutable = state.immutableEnabled;
  state.tagSha = state.release.target_commitish;
  save();
} else {
  throw new Error('Unexpected gh call: ' + args.join(' '));
}
