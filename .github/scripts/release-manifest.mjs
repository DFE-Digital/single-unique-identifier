#!/usr/bin/env node
import { copyFile, mkdir, readdir, stat, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// Only these applications currently produce promotable ZIP releases.
// Notification Service keeps its separate build workflow until container publication is ready.
const projects = {
  AuthEmulator: { project: 'SUI.AuthEmulator', slug: 'auth-emulator' },
  GetAnIdentifier: { project: 'SUI.GetAnIdentifier.API', slug: 'get-an-identifier' },
  UIHarness: { project: 'SUI.UIHarness.Web', slug: 'ui-harness' },
};

// Require one tested ZIP per selected component, then prepare independent release manifests.
export async function createManifests({ components, assetsDir, sourceSha,
  runId, runAttempt, releaseDate, buildNumber, generatedAt = new Date().toISOString().replace(/\.\d{3}Z$/, 'Z') }) {
  if (!Array.isArray(components) || components.length === 0 ||
      new Set(components).size !== components.length ||
      components.some((name) => !Object.hasOwn(projects, name))) {
    throw new Error('Select at least one unique, known component');
  }
  if (!/^[0-9a-f]{40}$/.test(sourceSha) || !/^\d+$/.test(runId) || !/^\d+$/.test(runAttempt) ||
      !/^\d{4}\.\d{2}\.\d{2}$/.test(releaseDate) || !/^[1-9]\d*$/.test(buildNumber)) {
    throw new Error('Full source SHA, workflow run identity, release date and build number are required');
  }

  const entries = await readdir(assetsDir, { withFileTypes: true });
  const zipFiles = entries.filter((entry) => entry.name.endsWith('.zip'));
  const sbomFiles = entries.filter((entry) => entry.name.endsWith('.cdx.json'));
  if (zipFiles.length !== components.length || sbomFiles.length !== components.length ||
      entries.length !== zipFiles.length + sbomFiles.length) throw new Error('ZIP and SBOM inventory does not match selection');

  const releases = [];
  for (const component of components) {
    const { project, slug } = projects[component];
    const matches = zipFiles.filter((entry) => entry.name.startsWith(`${component}-${project}-`));
    if (matches.length !== 1 || !matches[0].isFile()) {
      throw new Error(`Expected exactly one ZIP for ${component}`);
    }
    const filename = matches[0].name;
    const sbomFilename = `${filename}.cdx.json`;
    if (sbomFiles.filter((entry) => entry.name === sbomFilename && entry.isFile()).length !== 1) {
      throw new Error(`Expected exactly one SBOM for ${component}`);
    }
    const sbomPath = path.join(assetsDir, sbomFilename);
    if ((await stat(sbomPath)).size === 0) throw new Error(`Empty SBOM for ${component}`);
    const zipPath = path.join(assetsDir, filename);
    const fileStat = await stat(zipPath);
    if (!fileStat.isFile() || fileStat.size === 0) {
      throw new Error(`Empty or invalid ZIP for ${component}`);
    }
    releases.push({ component, slug, zipPath, sbomPath, sbomFilename, manifest: {
      schema_version: 1,
      release: { tag: `${slug}-${releaseDate}-build.${buildNumber}`, source_sha: sourceSha,
        release_date: releaseDate, build_number: buildNumber, workflow_run_id: runId, workflow_run_attempt: runAttempt, generated_at: generatedAt },
      assets: [{ component, filename }],
      // Retain the manifest shape while container releases are deferred.
      container: null,
    } });
  }
  return releases;
}

if (process.argv[1] && fileURLToPath(import.meta.url) === path.resolve(process.argv[1])) {
  // The workflow invokes this file directly; tests import createManifests without running this block.
  try {
    const selectedComponents = [
      ['AUTH', 'AuthEmulator'], ['GET_AN_ID', 'GetAnIdentifier'], ['UI', 'UIHarness'],
    ].filter(([key]) => process.env[key] === 'true').map(([, name]) => name);
    const releases = await createManifests({
      components: selectedComponents,
      assetsDir: 'drop',
      sourceSha: process.env.GITHUB_SHA,
      runId: process.env.GITHUB_RUN_ID,
      runAttempt: process.env.GITHUB_RUN_ATTEMPT,
      releaseDate: process.env.RELEASE_DATE,
      buildNumber: process.env.GITHUB_RUN_NUMBER,
    });
    for (const { component, zipPath, sbomPath, sbomFilename, manifest } of releases) {
      const directory = path.join('release-assets', component);
      await mkdir(directory, { recursive: true });
      await copyFile(zipPath, path.join(directory, path.basename(zipPath)));
      await copyFile(sbomPath, path.join(directory, sbomFilename));
      await writeFile(path.join(directory, 'release-manifest.json'), `${JSON.stringify(manifest, null, 2)}\n`);
    }
    const matrix = JSON.stringify({ component: releases.map(({ component, slug, manifest }) => ({
      name: component, slug, tag: manifest.release.tag,
    })) });
    if (process.env.GITHUB_OUTPUT) await writeFile(process.env.GITHUB_OUTPUT, `matrix=${matrix}\n`, { flag: 'a' });
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}
