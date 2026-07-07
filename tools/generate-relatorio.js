#!/usr/bin/env node
/**
 * tools/generate-relatorio.js
 *
 * Gera o Relatório de Entrega (PDF) exigido pelo edital FIAP: nome do grupo, participantes
 * e usernames no Discord, link da documentação, link do repositório e link do vídeo.
 * Usa o Playwright (já instalado para tools/record-delivery.js) para renderizar um HTML
 * simples em PDF via page.pdf() — sem depender de nenhuma lib de PDF adicional.
 *
 * Uso:
 *   node generate-relatorio.js                # detecta a fase pela branch atual
 *   node generate-relatorio.js --phase=4       # força a fase
 *   VIDEO_URL="https://youtu.be/..." node generate-relatorio.js   # informa o link do vídeo
 *
 * Saída: docs/RELATORIO-ENTREGA-FASE-<N>.pdf
 */

'use strict';

const { chromium } = require('playwright');
const { execSync } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');

const REPO_ROOT = path.resolve(__dirname, '..');
const REPO_URL = 'https://github.com/guilhermesoatto/TheThroneOfGames';

// Entrega individual — sem grupo formal (ver conversa de entrega).
const GRUPO = 'Entrega individual';
const PARTICIPANTES = [{ nome: 'Guilherme Soatto', discord: 'gui_soatto' }];

const BRANCH_TO_PHASE = {
  'release/fase-2-monolito': 2,
  'release/fase-3-microservices': 3,
  'release/fase-4-kubernetes': 4,
};

const PHASE_INFO = {
  2: {
    label: 'Fase 2 — Monolito',
    branch: 'release/fase-2-monolito',
    docs: ['README.md', 'docs/Objectives/sprint-1/DELIVERABLE.md'],
  },
  3: {
    label: 'Fase 3 — Microsserviços',
    branch: 'release/fase-3-microservices',
    docs: ['README.md', 'docs/architecture-flow.md', 'docs/Objectives/sprint-2/DELIVERABLE.md'],
  },
  4: {
    label: 'Fase 4 — Kubernetes & Escala',
    branch: 'release/fase-4-kubernetes',
    docs: ['README.md', 'docs/k8s-architecture-flow.md', 'docs/eks-deploy.md', 'docs/Objectives/sprint-3/prd-fase4.json'],
  },
};

function detectBranch() {
  try {
    return execSync('git rev-parse --abbrev-ref HEAD', { cwd: REPO_ROOT }).toString().trim();
  } catch {
    return null;
  }
}

function resolvePhase() {
  const cliArg = process.argv.find((a) => a.startsWith('--phase='));
  if (cliArg) {
    const n = Number(cliArg.split('=')[1]);
    if ([2, 3, 4].includes(n)) return n;
    throw new Error(`--phase inválido: "${cliArg}". Use 2, 3 ou 4.`);
  }
  const branch = detectBranch();
  if (branch && BRANCH_TO_PHASE[branch]) return BRANCH_TO_PHASE[branch];
  throw new Error(`Não foi possível determinar a fase pela branch atual ("${branch}"). Use --phase=N.`);
}

function buildHtml(phase) {
  const info = PHASE_INFO[phase];
  const videoUrl = process.env.VIDEO_URL || null;
  const repoLink = `${REPO_URL}/tree/${info.branch}`;
  const docLinks = info.docs.map((d) => `${REPO_URL}/blob/${info.branch}/${d}`);
  const today = new Date().toLocaleDateString('pt-BR', { year: 'numeric', month: 'long', day: 'numeric' });

  const participantesHtml = PARTICIPANTES.map(
    (p) => `<li>${p.nome} — Discord: <code>${p.discord}</code></li>`
  ).join('');
  const docLinksHtml = docLinks.map((l) => `<li><a href="${l}">${l}</a></li>`).join('');
  const videoHtml = videoUrl
    ? `<a href="${videoUrl}">${videoUrl}</a>`
    : `<span class="pending">[PENDENTE — subir docs/videos/FCG_ENTREGA_FASE_${phase}.mp4 no YouTube (não listado) e colar o link aqui antes do envio]</span>`;

  return `<!doctype html><html><head><meta charset="utf-8"><style>
    body { font-family: -apple-system, Segoe UI, Roboto, sans-serif; color: #1a1a1a; padding: 48px; line-height: 1.6; }
    h1 { font-size: 26px; border-bottom: 3px solid #1a1a1a; padding-bottom: 12px; }
    h2 { font-size: 16px; text-transform: uppercase; letter-spacing: 0.5px; color: #444; margin-top: 32px; }
    a { color: #0645ad; word-break: break-all; }
    code { background: #f0f0f0; padding: 2px 6px; border-radius: 3px; }
    .pending { color: #b00; font-weight: bold; }
    .meta { color: #666; font-size: 13px; }
    ul { padding-left: 20px; }
  </style></head><body>
    <h1>Relatório de Entrega — Tech Challenge FIAP</h1>
    <p class="meta">${info.label} · Gerado em ${today}</p>

    <h2>Nome do grupo</h2>
    <p>${GRUPO}</p>

    <h2>Participantes e usernames no Discord</h2>
    <ul>${participantesHtml}</ul>

    <h2>Link da documentação</h2>
    <ul>${docLinksHtml}</ul>

    <h2>Link do repositório</h2>
    <p><a href="${repoLink}">${repoLink}</a></p>

    <h2>Link do vídeo</h2>
    <p>${videoHtml}</p>
  </body></html>`;
}

async function main() {
  const phase = resolvePhase();
  const html = buildHtml(phase);
  const outPath = path.join(REPO_ROOT, 'docs', `RELATORIO-ENTREGA-FASE-${phase}.pdf`);

  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.setContent(html, { waitUntil: 'load' });
  await page.pdf({ path: outPath, format: 'A4', margin: { top: '20mm', bottom: '20mm' } });
  await browser.close();

  console.log(`[DONE] Relatório salvo em ${path.relative(REPO_ROOT, outPath)}`);
  if (!process.env.VIDEO_URL) {
    console.log('[WARN] VIDEO_URL não informado — o link do vídeo ficou marcado como PENDENTE no PDF.');
  }
}

main().catch((err) => {
  console.error('[FATAL]', err);
  process.exitCode = 1;
});
