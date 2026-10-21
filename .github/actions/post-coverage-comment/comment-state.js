const marker = '<!-- sui-coverage-run:';

function buildCommentBody({
  summary,
  component,
  workflow,
  sha,
  run,
  runNumber,
  serverUrl,
  repository
}) {
  const runUrl = `${serverUrl}/${repository}/actions/runs/${run.id}/attempts/${run.attempt}`;
  const commitUrl = `${serverUrl}/${repository}/commit/${sha}`;

  return [
    `### ${component} coverage`,
    '',
    `**Workflow:** ${workflow}`,
    `**Commit:** [${sha.slice(0, 7)}](${commitUrl})`,
    `**Run:** [#${runNumber}, attempt ${run.attempt}](${runUrl})`,
    '',
    summary,
    `<!-- sui-coverage:${component} -->`,
    `${marker}${run.id}:${run.attempt} -->`
  ].join('\n');
}

function getRunVersion(commentBody) {
  const match = commentBody.match(/<!-- sui-coverage-run:(\d+):(\d+) -->/);
  if (!match) {
    return null;
  }

  return { id: Number(match[1]), attempt: Number(match[2]) };
}

function shouldUpdateComment(existingBody, incomingRun) {
  const existingRun = getRunVersion(existingBody);
  if (!existingRun) {
    return true;
  }

  return incomingRun.id > existingRun.id ||
    (incomingRun.id === existingRun.id && incomingRun.attempt > existingRun.attempt);
}

module.exports = { buildCommentBody, getRunVersion, shouldUpdateComment };
