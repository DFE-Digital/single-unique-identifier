#!/usr/bin/env node
/**
 * Validates selected build outputs and writes the release manifest consumed by
 * the main-only release publisher.
 * @module release-manifest
 */

import { createHash } from 'node:crypto';
import { readFile, readdir, stat, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseArgs } from 'node:util';

/** Expected artifact project names, keyed by release component. */
export const PROJECT_BY_COMPONENT = {
  AuthEmulator: 'SUI.AuthEmulator',
  GetAnIdentifier: 'SUI.GetAnIdentifier.API',
  NotificationService: 'SUI.NotificationService',
  UIHarness: 'SUI.UIHarness.Web',
};

const IMAGE_DIGEST_PATTERN = /^sha256:[0-9a-f]{64}$/;

/**
 * @typedef {object} ManifestInputs
 * @property {string} assetsDir Directory containing selected component ZIPs.
 * @property {string} selectedJson JSON file listing the selected components.
 * @property {string} gatesJson JSON file containing the actual quality-gate outcomes.
 * @property {string} tag Release tag, which must identify sourceSha.
 * @property {string} sourceSha Full source commit SHA.
 * @property {string} runId GitHub Actions run ID.
 * @property {string} runAttempt GitHub Actions run attempt.
 * @property {string} output File to receive the generated manifest.
 * @property {string} [containerJson] Optional Notification Service image metadata file.
 */

function assertCondition(condition, message) {
  if (!condition) throw new Error(message);
}

async function readJson(file) {
  try {
    return JSON.parse(await readFile(file, 'utf8'));
  } catch (error) {
    throw new Error(`Missing or invalid JSON: ${file}`, { cause: error });
  }
}

async function sha256(file) {
  return createHash('sha256').update(await readFile(file)).digest('hex');
}

function validateSelection(selectedComponents) {
  assertCondition(
    Array.isArray(selectedComponents) && selectedComponents.length > 0 &&
      selectedComponents.every((name) => typeof name === 'string'),
    'Selected components must be a non-empty list',
  );
  assertCondition(
    new Set(selectedComponents).size === selectedComponents.length &&
      selectedComponents.every((name) => Object.hasOwn(PROJECT_BY_COMPONENT, name)),
    'Selected components must be unique and known',
  );
}

function validateReleaseIdentity({ sourceSha, tag, runId, runAttempt }) {
  assertCondition(/^[0-9a-f]{40}$/.test(sourceSha), 'Full source SHA is required');
  assertCondition(tag === `sui-${sourceSha}`, 'Release tag must identify the source commit');
  assertCondition(/^\d+$/.test(runId) && /^\d+$/.test(runAttempt), 'Workflow run identity is required');
}

function validateQualityGates(qualityGates, selectedComponents) {
  assertCondition(
    qualityGates && typeof qualityGates === 'object' &&
      qualityGates.components && typeof qualityGates.components === 'object',
    'Component gate outcomes are required',
  );

  for (const name of ['secret_scan', 'iac_scan', 'repository_scan']) {
    assertCondition(qualityGates[name] === 'success', `${name} must succeed`);
  }

  const componentNames = Object.keys(qualityGates.components);
  assertCondition(
    componentNames.length === selectedComponents.length &&
      selectedComponents.every((name) => Object.hasOwn(qualityGates.components, name)),
    'Gate outcomes must match selected components',
  );

  for (const [component, results] of Object.entries(qualityGates.components)) {
    // UIHarness intentionally skips tests and Sonar in its workflow; record that exemption explicitly.
    const expectedGateStatus = component === 'UIHarness' ? 'skipped' : 'success';
    assertCondition(
      results && results.tests === expectedGateStatus && results.sonar_quality_gate === expectedGateStatus,
      `${component} tests and Sonar quality gate must be ${expectedGateStatus}`,
    );
  }
}

async function collectAssets(assetsDir, selectedComponents) {
  const zipFilenames = (await readdir(assetsDir, { withFileTypes: true }))
    .filter((entry) => entry.isFile() && entry.name.endsWith('.zip'))
    .map((entry) => entry.name);

  // The release may be partial, but every selected component must have exactly one ZIP.
  assertCondition(
    zipFilenames.length === selectedComponents.length,
    'ZIP inventory does not match selected components',
  );

  const assets = [];
  for (const component of selectedComponents) {
    const expectedPrefix = `${component}-${PROJECT_BY_COMPONENT[component]}-`;
    const matches = zipFilenames.filter((filename) => filename.startsWith(expectedPrefix));
    assertCondition(matches.length === 1, `Exactly one non-empty ZIP is required for ${component}`);

    const filename = matches[0];
    const file = path.join(assetsDir, filename);
    const fileStat = await stat(file);
    assertCondition(fileStat.size > 0, `Exactly one non-empty ZIP is required for ${component}`);

    assets.push({
      component,
      filename,
      sha256: await sha256(file),
      size_bytes: fileStat.size,
    });
  }

  return assets.sort((left, right) => left.component.localeCompare(right.component));
}

function validateContainer(selectedComponents, containerMetadata) {
  if (!selectedComponents.includes('NotificationService')) {
    assertCondition(containerMetadata === null, 'Container metadata is only valid for Notification Service');
    return;
  }

  const validImage = containerMetadata && containerMetadata.repository &&
    IMAGE_DIGEST_PATTERN.test(containerMetadata.digest ?? '');
  const validBaseImage = containerMetadata?.base_image &&
    IMAGE_DIGEST_PATTERN.test(containerMetadata.base_digest ?? '');
  const requiredChecksPassed = containerMetadata?.scan === 'success' &&
    containerMetadata?.registry_publication === 'success';

  assertCondition(
    validImage && validBaseImage && requiredChecksPassed,
    'Scanned and published immutable Notification Service image is required',
  );
}

/**
 * Validates the selected component outputs and writes their release manifest.
 * @param {ManifestInputs} inputs Paths and run identity used to build the manifest.
 * @returns {Promise<void>}
 * @throws {Error} If a gate, asset, release identity, or container field is missing or invalid.
 */
export async function createManifest(inputs) {
  const selectedComponents = await readJson(inputs.selectedJson);
  const qualityGates = await readJson(inputs.gatesJson);

  validateSelection(selectedComponents);
  validateReleaseIdentity(inputs);
  validateQualityGates(qualityGates, selectedComponents);

  const assets = await collectAssets(inputs.assetsDir, selectedComponents);
  const containerMetadata = inputs.containerJson ? await readJson(inputs.containerJson) : null;
  validateContainer(selectedComponents, containerMetadata);

  const manifest = {
    schema_version: 1,
    release: {
      tag: inputs.tag,
      source_sha: inputs.sourceSha,
      workflow_run_id: inputs.runId,
      workflow_run_attempt: inputs.runAttempt,
      generated_at: new Date().toISOString().replace(/\.\d{3}Z$/, 'Z'),
    },
    quality_gates: qualityGates,
    assets,
    container: containerMetadata,
  };

  await writeFile(inputs.output, `${JSON.stringify(manifest, null, 2)}\n`, 'utf8');
}

async function main() {
  const { values } = parseArgs({
    options: {
      'assets-dir': { type: 'string' },
      'selected-json': { type: 'string' },
      'gates-json': { type: 'string' },
      tag: { type: 'string' },
      'source-sha': { type: 'string' },
      'run-id': { type: 'string' },
      'run-attempt': { type: 'string' },
      output: { type: 'string' },
      'container-json': { type: 'string' },
      help: { type: 'boolean', short: 'h' },
    },
    strict: true,
  });

  if (values.help) {
    console.log(
      'Usage: node release-manifest.mjs --assets-dir DIR --selected-json FILE --gates-json FILE --tag TAG --source-sha SHA --run-id ID --run-attempt ATTEMPT --output FILE [--container-json FILE]',
    );
    return;
  }

  const requiredOptions = [
    'assets-dir', 'selected-json', 'gates-json', 'tag', 'source-sha', 'run-id', 'run-attempt', 'output',
  ];
  for (const name of requiredOptions) {
    assertCondition(values[name], `Missing required option: --${name}`);
  }

  await createManifest({
    assetsDir: values['assets-dir'],
    selectedJson: values['selected-json'],
    gatesJson: values['gates-json'],
    tag: values.tag,
    sourceSha: values['source-sha'],
    runId: values['run-id'],
    runAttempt: values['run-attempt'],
    output: values.output,
    containerJson: values['container-json'],
  });
}

if (process.argv[1] && fileURLToPath(import.meta.url) === path.resolve(process.argv[1])) {
  main().catch((error) => {
    console.error(error.message);
    process.exitCode = 1;
  });
}
