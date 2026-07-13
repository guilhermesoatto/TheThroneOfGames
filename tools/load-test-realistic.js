#!/usr/bin/env node
/**
 * tools/load-test-realistic.js
 *
 * Teste de carga realista (think-time + mix de navegação/compra) usado para medir a
 * capacidade REAL de UMA réplica de cada API antes de cruzar o alvo de 70% de CPU do HPA
 * (k8s/hpa/*.yaml) — diferente de k8s/load-test/load-test-job.yaml, que gera RPS bruto em
 * loop apertado (sem think-time) só para validar que o HPA reage, não para medir "quantos
 * usuários reais" uma réplica aguenta.
 *
 * Cada "usuário virtual" simula uma sessão real: pausa (think-time) entre ações, navega o
 * catálogo na maior parte do tempo, e ocasionalmente completa uma compra ponta a ponta
 * (criar pedido -> adicionar item -> finalizar). Roda em estágios de concorrência crescente
 * contra o ambiente local (docker compose, 1 réplica por serviço = capacidade "por pod"), e
 * para cada estágio consulta o Prometheus (rate(process_cpu_seconds_total)) para saber a
 * utilização real de CPU de cada serviço, convertida para % do `resources.requests.cpu`
 * configurado em k8s/deployments/ (150m) — a mesma base que o HorizontalPodAutoscaler usa.
 *
 * Uso:
 *   node tools/load-test-realistic.js
 *
 * Saída: docs/reports/load-test-realistic-<timestamp>.json (bruto, para o relatório de custo)
 */

'use strict';

const fs = require('node:fs');
const path = require('node:path');

const USUARIOS_URL = process.env.BASE_URL_USUARIOS || 'http://localhost:5001';
const CATALOGO_URL = process.env.BASE_URL_CATALOGO || 'http://localhost:5002';
const VENDAS_URL = process.env.BASE_URL_VENDAS || 'http://localhost:5003';
const PROMETHEUS_URL = process.env.BASE_URL_PROMETHEUS || 'http://localhost:9090';

const USER_POOL_SIZE = 60;
// Override para uma carga sustentada de estágio único (ex.: validar HPA escalando de verdade
// num cluster real, que precisa de vários ciclos de sync-period sob carga constante, não de um
// ramp que passa rápido demais por cada nível): STAGES=150 STAGE_ACTIVE_SECONDS=240 node ...
const STAGES = process.env.STAGES ? process.env.STAGES.split(',').map(Number) : [10, 20, 35, 50, 75, 100, 130, 160, 200];
const STAGE_ACTIVE_SECONDS = process.env.STAGE_ACTIVE_SECONDS ? Number(process.env.STAGE_ACTIVE_SECONDS) : 25;
const STAGE_SETTLE_SECONDS = process.env.STAGE_SETTLE_SECONDS ? Number(process.env.STAGE_SETTLE_SECONDS) : 8;
const CPU_REQUEST_MILLICORES = { 'usuarios-api': 150, 'catalogo-api': 150, 'vendas-api': 150 };

// Mix realista: a maior parte do tráfego é navegação; compra é uma minoria das sessões.
const ACTION_WEIGHTS = { browse: 0.85, buy: 0.15 };

const RUN_ID = Date.now();
const rnd = (label) => `loadtest.${label}.${Math.random().toString(36).slice(2)}+${RUN_ID}@example.com`;

function log(msg) {
  console.log(`[${new Date().toISOString().slice(11, 19)}] ${msg}`);
}

async function jsonFetch(url, opts) {
  const res = await fetch(url, opts);
  const text = await res.text();
  let json = null;
  try { json = JSON.parse(text); } catch { /* não-JSON */ }
  return { status: res.status, json };
}

// ── Setup: pool de usuários autenticados + um jogo disponível ──────────────────────────

async function registerAndLogin(i) {
  const email = rnd(`u${i}`);
  const body = { name: `LoadTest ${i}`, email, password: 'Demo@12345', role: 'User' };
  const reg = await jsonFetch(`${USUARIOS_URL}/api/usuario/pre-register`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body),
  });
  if (!reg.json?.activationToken) throw new Error(`pre-register falhou para ${email}: ${JSON.stringify(reg.json)}`);

  await jsonFetch(`${USUARIOS_URL}/api/usuario/activate?activationToken=${reg.json.activationToken}`, { method: 'POST' });

  const login = await jsonFetch(`${USUARIOS_URL}/api/usuario/login`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password: 'Demo@12345' }),
  });
  if (!login.json?.token) throw new Error(`login falhou para ${email}: ${JSON.stringify(login.json)}`);
  return login.json.token;
}

async function buildUserPool(size) {
  log(`Registrando pool de ${size} usuários autenticados...`);
  const tokens = [];
  const CONCURRENCY = 10;
  for (let i = 0; i < size; i += CONCURRENCY) {
    const batch = await Promise.all(
      Array.from({ length: Math.min(CONCURRENCY, size - i) }, (_, j) => registerAndLogin(i + j))
    );
    tokens.push(...batch);
  }
  log(`Pool pronto: ${tokens.length} tokens.`);
  return tokens;
}

async function getAvailableGame(token) {
  const { json } = await jsonFetch(`${CATALOGO_URL}/api/game/available`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  const games = Array.isArray(json) ? json : json?.items || [];
  if (!games.length) throw new Error('Nenhum jogo disponível no catálogo — crie um antes de rodar o teste.');
  return games[0];
}

// ── Ações do usuário virtual ────────────────────────────────────────────────────────────

async function actionBrowse(token) {
  await jsonFetch(`${CATALOGO_URL}/api/game/available`, { headers: { Authorization: `Bearer ${token}` } });
}

async function actionBuy(token, game) {
  const pedido = await jsonFetch(`${VENDAS_URL}/api/pedidos`, {
    method: 'POST', headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' }, body: '{}',
  });
  const pedidoId = pedido.json?.entityId;
  if (!pedidoId) return;
  await jsonFetch(`${VENDAS_URL}/api/pedidos/${pedidoId}/itens`, {
    method: 'POST', headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    // GET /api/game/available responde em português (nome/preco) — diferente do DTO de
    // criação (POST /api/admin/game), que usa name/price. Ver tools/record-delivery.js.
    body: JSON.stringify({ jogoId: game.id, nomeJogo: game.nome, preco: game.preco }),
  });
  await jsonFetch(`${VENDAS_URL}/api/pedidos/${pedidoId}/finalizar`, {
    method: 'POST', headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({ metodoPagamento: 'CreditCard' }),
  });
}

function thinkTime() {
  return 1000 + Math.random() * 2000; // 1-3s, simula tempo de leitura/decisão real
}

async function virtualUserLoop(tokens, game, stopAt, stats) {
  while (Date.now() < stopAt) {
    const token = tokens[Math.floor(Math.random() * tokens.length)];
    const isBuy = Math.random() < ACTION_WEIGHTS.buy;
    try {
      if (isBuy) { await actionBuy(token, game); stats.buys++; } else { await actionBrowse(token); stats.browses++; }
      stats.ok++;
    } catch { stats.errors++; }
    await new Promise((r) => setTimeout(r, thinkTime()));
  }
}

// ── Prometheus: CPU real por serviço ────────────────────────────────────────────────────

async function queryCpuMillicores(job, windowSeconds) {
  const q = `rate(process_cpu_seconds_total{job="${job}"}[${windowSeconds}s]) * 1000`;
  const { json } = await jsonFetch(`${PROMETHEUS_URL}/api/v1/query?query=${encodeURIComponent(q)}`);
  const value = json?.data?.result?.[0]?.value?.[1];
  return value ? Number(value) : null;
}

async function measureCpu() {
  const jobs = Object.keys(CPU_REQUEST_MILLICORES);
  const out = {};
  for (const job of jobs) {
    const millicores = await queryCpuMillicores(job, STAGE_ACTIVE_SECONDS);
    out[job] = {
      millicores: millicores == null ? null : Math.round(millicores * 10) / 10,
      pctOfRequest: millicores == null ? null : Math.round((millicores / CPU_REQUEST_MILLICORES[job]) * 1000) / 10,
    };
  }
  return out;
}

// ── Execução principal ──────────────────────────────────────────────────────────────────

async function main() {
  const tokens = await buildUserPool(USER_POOL_SIZE);
  const game = await getAvailableGame(tokens[0]);
  log(`Jogo usado no fluxo de compra: "${game.nome}" (id=${game.id})`);

  const results = [];

  for (const concurrency of STAGES) {
    log(`--- Estágio: ${concurrency} usuários virtuais concorrentes (${STAGE_ACTIVE_SECONDS}s) ---`);
    const stats = { ok: 0, errors: 0, browses: 0, buys: 0 };
    const stopAt = Date.now() + STAGE_ACTIVE_SECONDS * 1000;
    const workers = Array.from({ length: concurrency }, () => virtualUserLoop(tokens, game, stopAt, stats));
    await Promise.all(workers);

    const cpu = await measureCpu();
    const rps = Math.round((stats.ok / STAGE_ACTIVE_SECONDS) * 10) / 10;
    log(
      `concurrency=${concurrency} rps~=${rps} ok=${stats.ok} errors=${stats.errors} ` +
        Object.entries(cpu).map(([k, v]) => `${k}=${v.pctOfRequest}%`).join(' ')
    );
    results.push({ concurrency, rps, ...stats, cpu });

    log(`Aguardando ${STAGE_SETTLE_SECONDS}s para CPU assentar antes do próximo estágio...`);
    await new Promise((r) => setTimeout(r, STAGE_SETTLE_SECONDS * 1000));
  }

  const outDir = path.join(__dirname, '..', 'docs', 'reports');
  fs.mkdirSync(outDir, { recursive: true });
  const outFile = path.join(outDir, `load-test-realistic-${RUN_ID}.json`);
  fs.writeFileSync(outFile, JSON.stringify({ runId: RUN_ID, stages: STAGES, cpuRequestMillicores: CPU_REQUEST_MILLICORES, results }, null, 2));
  log(`Resultado salvo em ${path.relative(path.join(__dirname, '..'), outFile)}`);
}

main().catch((err) => {
  console.error('[FATAL]', err);
  process.exitCode = 1;
});
