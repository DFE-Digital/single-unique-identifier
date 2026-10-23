import assert from 'node:assert/strict';
import { execFile } from 'node:child_process';
import { mkdtemp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { afterEach, it } from 'node:test';
import { promisify } from 'node:util';

const execFileAsync = promisify(execFile);
const script = new URL('../actions/publish-scanned-container-to-acr/publish.sh', import.meta.url).pathname;
const roots = [];
afterEach(async () => {
  await Promise.all(roots.splice(0).map((root) => rm(root, { recursive: true, force: true })));
});

async function fixture() {
  const root = await mkdtemp(path.join(os.tmpdir(), 'sui-container-publish-'));
  roots.push(root);
  const bin = path.join(root, 'bin');
  await mkdir(bin);
  await writeFile(path.join(bin, 'az'), '#!/usr/bin/env bash\nprintf "az %s\\n" "$*" >> "$TEST_CALLS"\nif [[ "$*" == *"repository show"* ]]; then printf "%s\\n" "$TEST_DIGEST"; fi\n', { mode: 0o755 });
  await writeFile(path.join(bin, 'docker'), '#!/usr/bin/env bash\nprintf "docker %s\\n" "$*" >> "$TEST_CALLS"\n', { mode: 0o755 });
  const dockerfile = path.join(root, 'Dockerfile');
  await writeFile(dockerfile, `FROM builder@sha256:${'a'.repeat(64)} AS build\nFROM runtime@sha256:${'b'.repeat(64)}\nCOPY . .\n`);
  const env = {
    ...process.env,
    PATH: `${bin}:${process.env.PATH}`,
    TEST_CALLS: path.join(root, 'calls'),
    TEST_DIGEST: `sha256:${'c'.repeat(64)}`,
    LOCAL_IMAGE: 'local:tested',
    REGISTRY_NAME: 'example',
    REGISTRY_SERVER: 'example.azurecr.io',
    REPOSITORY: 'team/service',
    IMAGE_TAG: 'd'.repeat(40),
    DOCKERFILE: dockerfile,
    METADATA_PATH: path.join(root, 'container.json'),
    GITHUB_OUTPUT: path.join(root, 'github-output'),
  };
  return { root, env };
}

it('pushes the scanned local image and records the registry and final base digests', async () => {
  const { env } = await fixture();
  await execFileAsync('bash', [script], { env });
  assert.deepEqual((await readFile(env.TEST_CALLS, 'utf8')).trim().split('\n'), [
    'docker image inspect local:tested',
    'az acr login --name example',
    `docker tag local:tested example.azurecr.io/team/service:${env.IMAGE_TAG}`,
    `docker push example.azurecr.io/team/service:${env.IMAGE_TAG}`,
    `az acr repository show --name example --image team/service:${env.IMAGE_TAG} --query digest --output tsv`,
  ]);
  assert.deepEqual(JSON.parse(await readFile(env.METADATA_PATH, 'utf8')), {
    repository: 'example.azurecr.io/team/service',
    digest: env.TEST_DIGEST,
    base_image: 'runtime',
    base_digest: `sha256:${'b'.repeat(64)}`,
    scan: 'success',
    registry_publication: 'success',
  });
  assert.equal(await readFile(env.GITHUB_OUTPUT, 'utf8'),
    `repository=example.azurecr.io/team/service\ndigest=${env.TEST_DIGEST}\n`);
});

it('refuses an unpinned runtime base before registry access', async () => {
  const { env } = await fixture();
  await writeFile(env.DOCKERFILE, 'FROM runtime:latest\n');
  await assert.rejects(execFileAsync('bash', [script], { env }));
  await assert.rejects(readFile(env.TEST_CALLS));
});

it('fails closed when the registry does not return a digest', async () => {
  const { env } = await fixture();
  env.TEST_DIGEST = 'invalid';
  await assert.rejects(execFileAsync('bash', [script], { env }));
  await assert.rejects(readFile(env.METADATA_PATH));
});
