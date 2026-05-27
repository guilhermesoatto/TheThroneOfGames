#!/usr/bin/env node
/**
 * generate-readme.js
 * ==================
 * Regenera o README.md raiz baseado no template static (tools/readme-template.md)
 * + conteúdo dinâmico extraído da estrutura do repositório.
 *
 * Disparado pelo GitHub Action .github/workflows/update-readme.yml após cada PR merge.
 * Pode também ser executado manualmente: node tools/generate-readme.js
 *
 * O arquivo README.md é sobrescrito na raiz do repositório.
 */

'use strict';

const fs = require('fs');
const path = require('path');

const REPO_ROOT = path.resolve(__dirname, '..');
const TEMPLATE_PATH = path.join(__dirname, 'readme-template.md');
const OUTPUT_PATH = path.join(REPO_ROOT, 'README.md');

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function fileExists(filePath) {
    try { fs.accessSync(filePath); return true; } catch { return false; }
}

function listMarkdownFiles(dir) {
    if (!fs.existsSync(dir)) return [];
    return fs.readdirSync(dir)
        .filter(f => f.endsWith('.md'))
        .map(f => f.replace('.md', ''));
}

function toTitle(filename) {
    return filename
        .split('-')
        .map(w => w.charAt(0).toUpperCase() + w.slice(1))
        .join(' ');
}

// ---------------------------------------------------------------------------
// Dynamic section builders
// ---------------------------------------------------------------------------

function buildStacksTable() {
    const stacks = [
        {
            stack: 'TypeScript / Node.js',
            folder: '`/` (root)',
            runtime: 'Node.js 20+',
            lang: 'TypeScript strict',
            available: fileExists(path.join(REPO_ROOT, 'copilot-instructions.md')),
        },
        {
            stack: 'C# / .NET 10',
            folder: '`/dotnet/`',
            runtime: '.NET 10',
            lang: 'C# 13',
            available: fileExists(path.join(REPO_ROOT, 'dotnet', 'copilot-instructions.md')),
        },
        {
            stack: 'Angular',
            folder: '`/angular/`',
            runtime: 'Node.js 20+ / Angular 17+',
            lang: 'TypeScript strict',
            available: fileExists(path.join(REPO_ROOT, 'angular', 'copilot-instructions.md')),
        },
    ];

    const rows = stacks.map(s => {
        const status = s.available ? '✅' : '🚧';
        return `| ${status} ${s.stack} | ${s.folder} | ${s.runtime} | ${s.lang} |`;
    });

    return [
        '| Stack | Folder | Runtime | Language |',
        '|---|---|---|---|',
        ...rows,
    ].join('\n');
}

function buildSkillsList(stackDir, label) {
    const skillsDir = path.join(REPO_ROOT, stackDir, 'docs', 'ai', 'skills');
    const skills = listMarkdownFiles(skillsDir);
    if (!skills.length) return `_No skills yet in ${label}_`;
    return skills.map(s => `- **${toTitle(s)}** — \`${stackDir}/docs/ai/skills/${s}.md\``).join('\n');
}

function buildWorkflowsList(stackDir, label) {
    const workflowsDir = path.join(REPO_ROOT, stackDir, 'docs', 'ai', 'workflows');
    const workflows = listMarkdownFiles(workflowsDir);
    if (!workflows.length) return `_No workflows yet in ${label}_`;
    return workflows.map(w => `- **${toTitle(w)}** — \`${stackDir}/docs/ai/workflows/${w}.md\``).join('\n');
}

function buildKnowledgeList() {
    const knowledgeDir = path.join(REPO_ROOT, 'docs', 'ai', 'knowledge');
    const files = listMarkdownFiles(knowledgeDir).filter(f => f !== 'README');
    if (!files.length) return '_No knowledge files yet_';
    return files.map(f => `- **${toTitle(f)}** — \`docs/ai/knowledge/${f}.md\``).join('\n');
}

function buildInstallSection() {
    return `
### TypeScript / Node.js
\`\`\`bash
cp copilot-instructions.md /your-project/
cp -r docs/ /your-project/docs/
cp -r tools/ /your-project/tools/
\`\`\`

### C# / .NET 10
\`\`\`bash
cp dotnet/copilot-instructions.md /your-project/
cp -r dotnet/docs/ /your-project/docs/
cp -r tools/ /your-project/tools/
\`\`\`

### Angular
\`\`\`bash
cp angular/copilot-instructions.md /your-project/
cp -r angular/docs/ /your-project/docs/
cp -r docs/ai/knowledge/ /your-project/docs/ai/knowledge/
cp -r tools/ /your-project/tools/
\`\`\`

### VS Code — Enable custom instructions
Create \`.vscode/settings.json\`:
\`\`\`json
{
  "github.copilot.chat.codeGeneration.instructions": [
    { "file": "copilot-instructions.md" }
  ]
}
\`\`\`
`.trim();
}

function buildChromaSection() {
    return `
\`\`\`bash
# 1. Install Python dependencies (one-time)
pip install -r tools/requirements.txt

# 2. Embed all knowledge into ChromaDB
python tools/embed-knowledge.py

# 3. Test a semantic query
python tools/query-knowledge.py "when to use Strategy pattern"
python tools/query-knowledge.py "what is the order entity called" --source ubiquitous-language
\`\`\`
`.trim();
}

// ---------------------------------------------------------------------------
// Main generator
// ---------------------------------------------------------------------------

function generate() {
    if (!fileExists(TEMPLATE_PATH)) {
        console.error(`[generate-readme] Template not found: ${TEMPLATE_PATH}`);
        process.exit(1);
    }

    let readme = fs.readFileSync(TEMPLATE_PATH, 'utf-8');

    // Replace dynamic placeholders
    readme = readme.replace('{{STACKS_TABLE}}', buildStacksTable());
    readme = readme.replace('{{INSTALL_SECTION}}', buildInstallSection());
    readme = readme.replace('{{CHROMA_SECTION}}', buildChromaSection());

    // TypeScript skills/workflows
    readme = readme.replace('{{TS_SKILLS}}', buildSkillsList('.', 'TypeScript'));
    readme = readme.replace('{{TS_WORKFLOWS}}', buildWorkflowsList('docs/ai', 'TypeScript'));
    readme = readme.replace('{{DOTNET_SKILLS}}', buildSkillsList('dotnet', '.NET'));
    readme = readme.replace('{{DOTNET_WORKFLOWS}}', buildWorkflowsList('dotnet/docs/ai', '.NET'));
    readme = readme.replace('{{ANGULAR_SKILLS}}', buildSkillsList('angular', 'Angular'));
    readme = readme.replace('{{ANGULAR_WORKFLOWS}}', buildWorkflowsList('angular/docs/ai', 'Angular'));
    readme = readme.replace('{{KNOWLEDGE_FILES}}', buildKnowledgeList());

    // Auto-generated timestamp
    const now = new Date().toISOString().split('T')[0];
    readme = readme.replace('{{GENERATED_DATE}}', now);

    fs.writeFileSync(OUTPUT_PATH, readme, 'utf-8');
    console.log(`[generate-readme] README.md updated (${now})`);
}

generate();
