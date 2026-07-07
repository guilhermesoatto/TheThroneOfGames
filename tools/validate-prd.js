#!/usr/bin/env node
/**
 * validate-prd.js — Valida um arquivo prd.json contra o schema prd-v1.
 * Uso: node tools/validate-prd.js <caminho/para/prd.json>
 *
 * Exit codes:
 *   0 = PRD válido
 *   1 = PRD inválido (erros listados no stdout)
 *   2 = Arquivo não encontrado ou não é JSON
 */

import { readFileSync, existsSync } from 'node:fs';
import { resolve, relative } from 'node:path';

const VALID_STATUSES   = ['not-started', 'in-progress', 'blocked', 'done', 'pending'];
const VALID_PHASES     = ['0-prerequisites', '1-domain', '2-application', '3-infrastructure',
                          '4-containerization', '5-kubernetes', '6-cicd', '7-observability',
                          '1-scaffold', '2-first-domain', '3-eda'];
const REQUIRED_TOP     = ['$schema', 'aggregate', 'boundedContext', 'createdAt',
                          'currentPhase', 'description', 'tasks'];
const REQUIRED_TASK    = ['id', 'phase', 'description', 'status', 'passes', 'testCommand', 'artifacts'];

// ── Carrega o arquivo ────────────────────────────────────────────────────────

const filePath = process.argv[2];
if (!filePath) {
    console.error('Uso: node tools/validate-prd.js <prd.json>');
    process.exit(2);
}

const absPath = resolve(filePath);
if (!existsSync(absPath)) {
    console.error(`[ERRO] Arquivo não encontrado: ${absPath}`);
    process.exit(2);
}

let prd;
try {
    prd = JSON.parse(readFileSync(absPath, 'utf8'));
} catch (err) {
    console.error(`[ERRO] JSON inválido: ${err.message}`);
    process.exit(2);
}

// ── Validações ────────────────────────────────────────────────────────────────

const errors   = [];
const warnings = [];

function err(msg)  { errors.push(`  [X] ${msg}`); }
function warn(msg) { warnings.push(`  [!] ${msg}`); }

// Campos obrigatórios raiz
for (const field of REQUIRED_TOP) {
    if (prd[field] === undefined || prd[field] === null) {
        err(`Campo obrigatório ausente: "${field}"`);
    }
}

// currentPhase válido
if (prd.currentPhase && !VALID_PHASES.includes(prd.currentPhase)) {
    err(`currentPhase inválido: "${prd.currentPhase}". Válidos: ${VALID_PHASES.join(', ')}`);
}

// createdAt formato ISO 8601
if (prd.createdAt && isNaN(Date.parse(prd.createdAt))) {
    err(`createdAt não é uma data ISO 8601 válida: "${prd.createdAt}"`);
}

// Tasks
if (Array.isArray(prd.tasks)) {
    if (prd.tasks.length === 0) {
        warn('tasks[] está vazio — PRD sem tarefas definidas');
    }

    const ids = new Set();
    prd.tasks.forEach((task, i) => {
        const prefix = `tasks[${i}] (id="${task.id ?? '?'}")`;

        // Campos obrigatórios por task
        for (const field of REQUIRED_TASK) {
            if (task[field] === undefined || task[field] === null) {
                err(`${prefix}: campo obrigatório ausente: "${field}"`);
            }
        }

        // IDs duplicados
        if (task.id !== undefined) {
            if (ids.has(task.id)) {
                err(`${prefix}: id duplicado: "${task.id}"`);
            }
            ids.add(task.id);
        }

        // Status válido
        if (task.status && !VALID_STATUSES.includes(task.status)) {
            err(`${prefix}: status inválido: "${task.status}". Válidos: ${VALID_STATUSES.join(', ')}`);
        }

        // Phase válida
        if (task.phase && !VALID_PHASES.includes(task.phase)) {
            err(`${prefix}: phase inválida: "${task.phase}"`);
        }

        // artifacts deve ser array
        if (task.artifacts !== undefined && !Array.isArray(task.artifacts)) {
            err(`${prefix}: "artifacts" deve ser um array`);
        }

        // passes deve ser boolean
        if (task.passes !== undefined && typeof task.passes !== 'boolean') {
            err(`${prefix}: "passes" deve ser boolean`);
        }

        // Warn se task está "in-progress" mas passes = false (pode ser um bloqueio silencioso)
        if (task.status === 'in-progress' && task.passes === false && !task.blockReason) {
            warn(`${prefix}: status "in-progress" + passes=false sem "blockReason" — verificar se está bloqueada`);
        }
    });

    // Coerência de fase: tasks com phase anterior à currentPhase devem ter status "done"
    const phaseOrder = VALID_PHASES;
    const currentIdx = phaseOrder.indexOf(prd.currentPhase);
    if (currentIdx > -1) {
        prd.tasks.forEach((task, i) => {
            const taskIdx = phaseOrder.indexOf(task.phase);
            if (taskIdx > -1 && taskIdx < currentIdx && task.status !== 'done') {
                warn(`tasks[${i}] (id="${task.id}"): phase "${task.phase}" é anterior à currentPhase mas status="${task.status}" (esperado: "done")`);
            }
        });
    }
} else if (prd.tasks !== undefined) {
    err('"tasks" deve ser um array');
}

// acceptanceCriteria (opcional mas recomendado)
if (!prd.acceptanceCriteria) {
    warn('"acceptanceCriteria" ausente — recomendado para rastreabilidade FIAP');
}

// ── Resultado ─────────────────────────────────────────────────────────────────

const file = relative(process.cwd(), absPath);

if (errors.length === 0 && warnings.length === 0) {
    console.log(`\n[OK] ${file} — PRD válido (${prd.tasks?.length ?? 0} tarefas, fase: ${prd.currentPhase})\n`);
    process.exit(0);
}

console.log(`\n[RESULTADO] ${file}`);

if (warnings.length > 0) {
    console.log(`\nAvisos (${warnings.length}):`);
    warnings.forEach(w => console.log(w));
}

if (errors.length > 0) {
    console.log(`\nErros (${errors.length}):`);
    errors.forEach(e => console.log(e));
    console.log('');
    process.exit(1);
} else {
    console.log(`\n[OK] PRD válido com ${warnings.length} aviso(s).\n`);
    process.exit(0);
}
