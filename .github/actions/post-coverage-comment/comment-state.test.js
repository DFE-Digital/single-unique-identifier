const assert = require('node:assert/strict');
const test = require('node:test');
const {
  buildCommentBody,
  getRunVersion,
  shouldUpdateComment
} = require('./comment-state.js');

test('coverage comment identifies the workflow, source commit and run attempt', () => {
  const body = buildCommentBody({
    summary: 'Coverage summary',
    component: 'Apps/AuthEmulator',
    workflow: 'CI/CD Orchestrator (release validation)',
    sha: 'abcdef0123456789',
    run: { id: 1234, attempt: 2 },
    runNumber: '42',
    serverUrl: 'https://github.com',
    repository: 'DFE-Digital/single-unique-identifier'
  });

  assert.match(body, /\*\*Workflow:\*\* CI\/CD Orchestrator \(release validation\)/);
  assert.match(body, /\[abcdef0\]\(https:\/\/github\.com\/DFE-Digital\/single-unique-identifier\/commit\/abcdef0123456789\)/);
  assert.match(body, /\[#42, attempt 2\]\(https:\/\/github\.com\/DFE-Digital\/single-unique-identifier\/actions\/runs\/1234\/attempts\/2\)/);
  assert.match(body, /<!-- sui-coverage:Apps\/AuthEmulator -->/);
});

test('newer runs and rerun attempts update the existing component comment', () => {
  const body = '<!-- sui-coverage-run:1234:1 -->';

  assert.equal(shouldUpdateComment(body, { id: 1235, attempt: 1 }), true);
  assert.equal(shouldUpdateComment(body, { id: 1234, attempt: 2 }), true);
  assert.equal(shouldUpdateComment(body, { id: 1234, attempt: 1 }), false);
  assert.equal(shouldUpdateComment(body, { id: 1233, attempt: 9 }), false);
});

test('comments from before run attribution are updated once to add metadata', () => {
  assert.equal(shouldUpdateComment('Old coverage summary', { id: 1234, attempt: 1 }), true);
  assert.deepEqual(getRunVersion('<!-- sui-coverage-run:1234:2 -->'), { id: 1234, attempt: 2 });
  assert.equal(getRunVersion('No run marker'), null);
});
