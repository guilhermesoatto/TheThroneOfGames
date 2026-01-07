#!/usr/bin/env python3
import sys
import os
from datetime import datetime

repo_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
workflow_path = os.path.join(repo_root, '.github', 'workflows', 'docker-build-push.yml')
report_path = os.path.join(repo_root, 'docs', 'YAML_WORKFLOW_VALIDATION.md')

try:
    import yaml
except Exception:
    # try to install pyyaml locally
    import subprocess
    print('PyYAML not found, attempting to install via pip...')
    subprocess.check_call([sys.executable, '-m', 'pip', 'install', '--user', 'PyYAML'])
    import yaml

issues = []
content = None
try:
    with open(workflow_path, 'r', encoding='utf-8') as f:
        content = f.read()
    # safe_load_all to detect all documents
    docs = list(yaml.safe_load_all(content))
    # No parse errors
except Exception as e:
    issues.append(str(e))

# prepare report
now = datetime.utcnow().isoformat() + 'Z'
lines = []
lines.append('# YAML Workflow Validation Report')
lines.append('')
lines.append(f'- File: `{workflow_path}`')
lines.append(f'- Checked at (UTC): {now}')
lines.append('')
if not issues:
    lines.append('## Result: No parse errors found')
    lines.append('')
else:
    lines.append('## Result: Errors found')
    lines.append('')
    lines.append('### Errors')
    lines.append('')
    for i, it in enumerate(issues, 1):
        lines.append(f'{i}. ```')
        lines.append(it)
        lines.append('```')
        lines.append('')

# include the workflow content for reference (truncated if long)
lines.append('## Workflow content (first 200 lines)')
lines.append('')
if content is not None:
    workflow_lines = content.splitlines()
    max_lines = 200
    for ln in workflow_lines[:max_lines]:
        # escape backticks
        lines.append('    ' + ln.replace('`', "'"))
    if len(workflow_lines) > max_lines:
        lines.append('    ... (truncated)')
else:
    lines.append('Unable to read workflow file.')

# write report
os.makedirs(os.path.dirname(report_path), exist_ok=True)
with open(report_path, 'w', encoding='utf-8') as f:
    f.write('\n'.join(lines))

print('Validation complete. Report written to', report_path)
if issues:
    sys.exit(2)
else:
    sys.exit(0)
