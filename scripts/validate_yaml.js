#!/usr/bin/env node
const fs = require('fs');
const path = require('path');
const yaml = require('js-yaml');

const repoRoot = path.resolve(__dirname, '..');
const workflowPath = path.join(repoRoot, '.github', 'workflows', 'docker-build-push.yml');
const reportPath = path.join(repoRoot, 'docs', 'YAML_WORKFLOW_VALIDATION.md');

let content = null;
let errors = [];
try {
  content = fs.readFileSync(workflowPath, 'utf8');
} catch (err) {
  errors.push(`Failed to read workflow file: ${err.message}`);
}

if (content !== null) {
  try {
    // try parsing all documents
    const docs = yaml.loadAll(content);
    // optionally, we could do additional structure checks here
  } catch (err) {
    errors.push(err.toString());
  }
}

const now = new Date().toISOString();
const lines = [];
lines.push('# YAML Workflow Validation Report');
lines.push('');
lines.push(`- File: \\`${workflowPath}\\``);
lines.push(`- Checked at (UTC): ${now}`);
lines.push('');
if (errors.length === 0) {
  lines.push('## Result: No parse errors found');
  lines.push('');
} else {
  lines.push('## Result: Errors found');
  lines.push('');
  lines.push('### Errors');
  lines.push('');
  errors.forEach((e, i) => {
    lines.push(`${i+1}. \`\`\``);
    lines.push(e);
    lines.push('```');
    lines.push('');
  });
}

lines.push('## Workflow content (first 200 lines)');
lines.push('');
if (content !== null) {
  const workflowLines = content.split(/\r?\n/);
  const maxLines = 200;
  for (let i = 0; i < Math.min(workflowLines.length, maxLines); i++) {
    // escape backticks
    lines.push('    ' + workflowLines[i].replace(/`/g, "'") );
  }
  if (workflowLines.length > maxLines) lines.push('    ... (truncated)');
} else {
  lines.push('Unable to read workflow file.');
}

// ensure docs dir exists
try {
  fs.mkdirSync(path.dirname(reportPath), { recursive: true });
  fs.writeFileSync(reportPath, lines.join('\n'), 'utf8');
  console.log('Validation complete. Report written to', reportPath);
  if (errors.length > 0) process.exitCode = 2;
} catch (err) {
  console.error('Failed to write report:', err);
  process.exitCode = 3;
}
