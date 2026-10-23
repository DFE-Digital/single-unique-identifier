#!/usr/bin/env node
import { copyFile, mkdir, readdir, stat, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const projects = {
  AuthEmulator: { project: 'SUI.AuthEmulator', slug: 'auth-emulator' },
  GetAnIdentifier: { project: 'SUI.GetAnIdentifier.API', slug: 'get-an-identifier' },
  UIHarness: { project: 'SUI.UIHarness.Web', slug: 'ui-harness' },
};

export async function createManifests({ components, assetsDir, sourceSha,
  runId, runAttempt, generatedAt = new Date().toISOString().replace(/\.\d{3}Z$/, 'Z') }) {
  if (!Array.isArray(components) || components.length === 0 ||
      new Set(components).size !== components.length ||
      components.some((name) => !Object.hasOwn(projects, name))) {
    throw new Error('Select at least one unique, known component');
  }
  if (!/^[0-9a-f]{40}$/.test(sourceSha) || !/^\d+$/.test(runId) || !/^\d+$/.test(runAttempt)) {
    throw new Error('Full source SHA and workflow run identity are required');
  }

  const zipFiles = (await readdir(assetsDir, { withFileTypes: true }))
    .filter((entry) => entry.name.endsWith('.zip'));
  if (zipFiles.length !== components.length) throw new Error('ZIP inventory does not match selection');

  const releases = [];
  for (const component of components) {
    const { project, slug } = projects[component];
    const matches = zipFiles.filter((entry) => entry.name.startsWith(`${component}-${project}-`));
    if (matches.length !== 1 || !matches[0].isFile()) {
      throw new Error(`Expected exactly one ZIP for ${component}`);
    }
    const filename = matches[0].name;
    const zipPath = path.join(assetsDir, filename);
    const fileStat = await stat(zipPath);
    if (!fileStat.isFile() || fileStat.size === 0) {
      throw new Error(`Empty or invalid ZIP for ${component}`);
    }
    releases.push({ component, slug, zipPath, manifest: {
      schema_version: 1,
      release: { tag: `sui-${slug}-${sourceSha}`, source_sha: sourceSha,
        workflow_run_id: runId, workflow_run_attempt: runAttempt, generated_at: generatedAt },
      assets: [{ component, filename }],
      container: null,
    } });
  }
  return releases;
}

if (process.argv[1] && fileURLToPath(import.meta.url) === path.resolve(process.argv[1])) {
  try {
    const selected = [
      ['AUTH', 'AuthEmulator'], ['GET_AN_ID', 'GetAnIdentifier'], ['UI', 'UIHarness'],
    ].filter(([key]) => process.env[key] === 'true').map(([, name]) => name);
    const releases = await createManifests({
      components: selected,
      assetsDir: 'drop',
      sourceSha: process.env.GITHUB_SHA,
      runId: process.env.GITHUB_RUN_ID,
      runAttempt: process.env.GITHUB_RUN_ATTEMPT,
    });
    for (const { component, zipPath, manifest } of releases) {
      const directory = path.join('release-assets', component);
      await mkdir(directory, { recursive: true });
      await copyFile(zipPath, path.join(directory, path.basename(zipPath)));
      await writeFile(path.join(directory, 'release-manifest.json'), `${JSON.stringify(manifest, null, 2)}\n`);
    }
    const matrix = JSON.stringify({ component: releases.map(({ component, slug }) => ({ name: component, slug })) });
    if (process.env.GITHUB_OUTPUT) await writeFile(process.env.GITHUB_OUTPUT, `matrix=${matrix}\n`, { flag: 'a' });
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}
