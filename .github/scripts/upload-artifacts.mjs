import fs from 'fs';
import path from 'path';
import { DefaultArtifactClient } from '@actions/artifact';
import { create as createGlob } from '@actions/glob';

function parseJson(label, value) {
  try {
    return JSON.parse(value);
  } catch (error) {
    throw new Error(`Invalid JSON in ${label}: ${error.message}`);
  }
}

function getRetentionDays() {
  const raw = process.env.ARTIFACT_RETENTION_DAYS;
  if (!raw) {
    return undefined;
  }
  const parsed = Number.parseInt(raw, 10);
  if (Number.isNaN(parsed) || parsed <= 0) {
    throw new Error('ARTIFACT_RETENTION_DAYS must be a positive integer.');
  }
  return parsed;
}

async function collectFiles(rootDir) {
  const globber = await createGlob(`${rootDir}/**`, {
    followSymbolicLinks: false,
    implicitDescendants: true,
    excludeHiddenFiles: false,
  });
  const matches = await globber.glob();
  return matches.filter((file) => fs.statSync(file).isFile());
}

async function run() {
  const projectsJson = process.env.PROJECTS_JSON || '[]';
  const artifactsJson = process.env.ARTIFACT_NAMES || '{}';

  const projects = parseJson('PROJECTS_JSON', projectsJson);
  const artifactNames = parseJson('ARTIFACT_NAMES', artifactsJson);

  if (!Array.isArray(projects)) {
    throw new Error('PROJECTS_JSON must be a JSON array.');
  }

  const retentionDays = getRetentionDays();
  const options = retentionDays ? { retentionDays } : {};
  const client = new DefaultArtifactClient();

  for (const project of projects) {
    const artifactName = artifactNames[project];
    if (!artifactName) {
      throw new Error(`Missing artifact name for project: ${project}`);
    }

    const repoRoot = process.env.GITHUB_WORKSPACE || process.cwd();
    const projectPath = path.resolve(repoRoot, project);
    if (!fs.existsSync(projectPath)) {
      throw new Error(`Project path does not exist: ${projectPath}`);
    }

    const files = await collectFiles(projectPath);
    if (files.length === 0) {
      throw new Error(`No files found to upload for ${project}`);
    }

    await client.uploadArtifact(artifactName, files, projectPath, options);
    console.log(`Uploaded ${artifactName} (${files.length} files).`);
  }
}

run().catch((error) => {
  console.error(error);
  process.exit(1);
});
