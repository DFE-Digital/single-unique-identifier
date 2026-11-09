import { readFileSync } from 'node:fs';
import { parse } from 'yaml';
import { mkdtemp, rm } from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { afterEach } from 'node:test';

// Register cleanup once per test file and keep each fixture isolated.
export function temporaryDirectories() {
  const roots = [];
  afterEach(async () => {
    await Promise.all(roots.splice(0).map((root) => rm(root, { recursive: true, force: true })));
  });
  return async (prefix) => {
    const root = await mkdtemp(path.join(os.tmpdir(), prefix));
    roots.push(root);
    return root;
  };
}

export function readWorkflow(url) {
  return parse(readFileSync(url, 'utf8'));
}

export function workflowStep(workflow, jobId, stepName) {
  const steps = workflow.jobs?.[jobId]?.steps?.filter((step) => step.name === stepName) ?? [];
  if (steps.length !== 1 || typeof steps[0].run !== 'string') {
    throw new Error(`Expected one shell step ${stepName} in ${jobId}`);
  }
  return steps[0].run;
}
