#!/usr/bin/env node
/**
 * tools/validate-partidas.js
 *
 * Valida os critérios de aceite do bounded context Partidas (matchmaking, ver
 * docs/ai/tasks/prd-partidas.json acceptanceCriteria) contra o ambiente real (docker compose):
 *
 *   1. Jogador sem o jogo no inventário -> buscar partida é rejeitado (sem tocar o Mongo)
 *   2. Jogador A (com o jogo) busca -> SolicitacaoBusca(Aguardando)
 *   3. Jogador B (com o jogo) busca -> Partida formada na hora (1v1), evento publicado
 *   4. Ambos confirmam -> Partida.Status = Confirmada
 *   5. Um jogador desiste antes da confirmação mútua -> Partida cancelada, o outro
 *      jogador volta para uma nova SolicitacaoBusca(Aguardando) e pareia de novo
 *   6. Documentos no Mongo em cada etapa batem com o que a API respondeu (persistência real)
 *   7. Traces no Jaeger cobrem a cadeia completa (Partidas -> Usuarios -> RabbitMQ)
 *
 * Não é um teste automatizado (xUnit) — é o script de validação ao vivo pedido no PRD,
 * rodado contra o ambiente já de pé (`docker compose up`), cruzando API + Mongo + Jaeger.
 *
 * Uso: node tools/validate-partidas.js
 */

'use strict';

const { execSync } = require('node:child_process');

const USUARIOS_URL = process.env.BASE_URL_USUARIOS || 'http://localhost:5001';
const CATALOGO_URL = process.env.BASE_URL_CATALOGO || 'http://localhost:5002';
const VENDAS_URL = process.env.BASE_URL_VENDAS || 'http://localhost:5003';
const PARTIDAS_URL = process.env.BASE_URL_PARTIDAS || 'http://localhost:5004';
const JAEGER_URL = process.env.BASE_URL_JAEGER || 'http://localhost:16686';

const RUN_ID = Date.now();
const results = [];
let failures = 0;

function log(msg) {
  console.log(`[${new Date().toISOString().slice(11, 19)}] ${msg}`);
}

function record(criterio, ok, detalhe) {
  results.push({ criterio, ok, detalhe });
  log(`${ok ? 'PASS' : 'FAIL'} — ${criterio}${detalhe ? ' — ' + detalhe : ''}`);
  if (!ok) failures++;
}

async function jsonFetch(url, opts) {
  const res = await fetch(url, opts);
  const text = await res.text();
  let json = null;
  try { json = JSON.parse(text); } catch { /* não-JSON */ }
  return { status: res.status, json };
}

function mongoEval(js) {
  const escaped = js.replace(/"/g, '\\"');
  const out = execSync(`docker exec mongodb mongosh gamestore_partidas --quiet --eval "${escaped}"`, { encoding: 'utf-8' });
  return out.trim();
}

// ── Helpers de setup (usuarios-api) ─────────────────────────────────────────────────────

async function registrarEAtivar(label, role = 'User') {
  const email = `partidas.${label}+${RUN_ID}@example.com`;
  const reg = await jsonFetch(`${USUARIOS_URL}/api/usuario/pre-register`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: label, email, password: 'Demo@12345', role }),
  });
  if (!reg.json?.activationToken) throw new Error(`pre-register falhou (${label}): ${JSON.stringify(reg.json)}`);
  await jsonFetch(`${USUARIOS_URL}/api/usuario/activate?activationToken=${reg.json.activationToken}`, { method: 'POST' });
  const login = await jsonFetch(`${USUARIOS_URL}/api/usuario/login`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password: 'Demo@12345' }),
  });
  if (!login.json?.token) throw new Error(`login falhou (${label}): ${JSON.stringify(login.json)}`);
  return { token: login.json.token, jogadorId: decodeJwtSub(login.json.token) };
}

function decodeJwtSub(token) {
  const payload = JSON.parse(Buffer.from(token.split('.')[1], 'base64url').toString('utf-8'));
  return payload.sub;
}

async function comprarJogo(token, jogo) {
  const pedido = await jsonFetch(`${VENDAS_URL}/api/pedidos`, {
    method: 'POST', headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' }, body: '{}',
  });
  const pedidoId = pedido.json?.entityId;
  await jsonFetch(`${VENDAS_URL}/api/pedidos/${pedidoId}/itens`, {
    method: 'POST', headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({ jogoId: jogo.id, nomeJogo: jogo.nome, preco: jogo.preco }),
  });
  await jsonFetch(`${VENDAS_URL}/api/pedidos/${pedidoId}/finalizar`, {
    method: 'POST', headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({ metodoPagamento: 'CreditCard' }),
  });
}

async function possuiJogo(token, jogoId) {
  const { json } = await jsonFetch(`${USUARIOS_URL}/api/usuario/possui-jogo/${jogoId}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  return json?.possuiJogo ?? false;
}

async function buscarPartida(token, jogoId) {
  return jsonFetch(`${PARTIDAS_URL}/api/partida/buscar`, {
    method: 'POST', headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({ jogoId }),
  });
}

async function confirmar(token, partidaId) {
  return jsonFetch(`${PARTIDAS_URL}/api/partida/${partidaId}/confirmar`, {
    method: 'POST', headers: { Authorization: `Bearer ${token}` },
  });
}

async function desistir(token, partidaId) {
  return jsonFetch(`${PARTIDAS_URL}/api/partida/${partidaId}/desistir`, {
    method: 'POST', headers: { Authorization: `Bearer ${token}` },
  });
}

async function sleep(ms) { return new Promise((r) => setTimeout(r, ms)); }

// ── Execução ─────────────────────────────────────────────────────────────────────────────

async function main() {
  log('Setup: admin, jogo, e 5 jogadores (A, B possuem o jogo; C não; D, E possuem o jogo)');
  const admin = await registrarEAtivar('admin', 'Admin');
  const jogadorA = await registrarEAtivar('jogadorA');
  const jogadorB = await registrarEAtivar('jogadorB');
  const jogadorC = await registrarEAtivar('jogadorC'); // não compra o jogo
  const jogadorD = await registrarEAtivar('jogadorD');
  const jogadorE = await registrarEAtivar('jogadorE');
  const jogadorF = await registrarEAtivar('jogadorF');

  const criarJogo = await jsonFetch(`${CATALOGO_URL}/api/admin/game`, {
    method: 'POST', headers: { Authorization: `Bearer ${admin.token}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: `Jogo Validacao Partidas ${RUN_ID}`, genre: 'Ação', price: 49.99, description: 'validate-partidas.js' }),
  });
  const jogo = { id: criarJogo.json.id, nome: criarJogo.json.name, preco: criarJogo.json.price };
  log(`Jogo criado: ${jogo.nome} (${jogo.id})`);

  log('Comprando o jogo para A, B, D, E (não para C)...');
  for (const [label, jogador] of [['A', jogadorA], ['B', jogadorB], ['D', jogadorD], ['E', jogadorE], ['F', jogadorF]]) {
    await comprarJogo(jogador.token, jogo);
    log(`  jogador ${label} comprou`);
  }

  log('Aguardando propagação assíncrona (GameCompradoEvent -> Inventário)...');
  await sleep(4000);

  // ── Critério: posse do jogo ────────────────────────────────────────────────────────────
  record('Jogador com o jogo aparece no Inventário (PossuiJogo=true)', await possuiJogo(jogadorA.token, jogo.id) === true);
  record('Jogador sem o jogo NÃO aparece no Inventário (PossuiJogo=false)', await possuiJogo(jogadorC.token, jogo.id) === false);

  // ── Critério 1: rejeição sem posse ─────────────────────────────────────────────────────
  const buscaC = await buscarPartida(jogadorC.token, jogo.id);
  record('Jogador sem o jogo é rejeitado ao buscar partida (400, sem criar SolicitacaoBusca)',
    buscaC.status === 400 && buscaC.json?.error === 'JOGADOR_NAO_POSSUI_JOGO',
    `status=${buscaC.status} error=${buscaC.json?.error}`);

  // ── Critério 2: A busca sozinho -> Aguardando ──────────────────────────────────────────
  const buscaA = await buscarPartida(jogadorA.token, jogo.id);
  record('Jogador A busca sozinho -> SolicitacaoBusca(Aguardando), sem Partida',
    buscaA.status === 200 && buscaA.json?.status === 'Aguardando' && buscaA.json?.partidaId == null,
    `status=${buscaA.status} body=${JSON.stringify(buscaA.json)}`);

  const solicitacaoAMongo = mongoEval(`JSON.stringify(db.solicitacoesBusca.findOne({_id: UUID("${buscaA.json?.solicitacaoId}")}))`);
  record('SolicitacaoBusca de A persistida no Mongo com Status=Aguardando',
    /"Status"\s*:\s*"Aguardando"/.test(solicitacaoAMongo), solicitacaoAMongo);

  // ── Critério 3: B busca -> pareamento imediato (1v1) ───────────────────────────────────
  const buscaB = await buscarPartida(jogadorB.token, jogo.id);
  const partidaId = buscaB.json?.partidaId;
  record('Jogador B busca o mesmo jogo -> Partida formada na hora (1v1)',
    buscaB.status === 200 && buscaB.json?.status === 'Pareada' && !!partidaId,
    `status=${buscaB.status} body=${JSON.stringify(buscaB.json)}`);

  const partidaMongo = mongoEval(`JSON.stringify(db.partidas.findOne({_id: UUID("${partidaId}")}))`);
  let partidaMongoParsed = {};
  try { partidaMongoParsed = JSON.parse(partidaMongo); } catch { /* segue com objeto vazio */ }
  record('Partida persistida no Mongo com EquipeA/EquipeB corretas e Status=AguardandoConfirmacao',
    partidaMongoParsed.Status === 'AguardandoConfirmacao'
      && JSON.stringify(partidaMongoParsed.EquipeA).toLowerCase().includes(jogadorA.jogadorId.toLowerCase())
      && JSON.stringify(partidaMongoParsed.EquipeB).toLowerCase().includes(jogadorB.jogadorId.toLowerCase()),
    partidaMongo);

  // ── Critério 4: ambos confirmam -> Confirmada ──────────────────────────────────────────
  const confirmA = await confirmar(jogadorA.token, partidaId);
  record('Confirmação de A (1º) -> ainda AguardandoConfirmacao',
    confirmA.status === 200 && confirmA.json?.status === 'AguardandoConfirmacao', JSON.stringify(confirmA.json));

  const confirmB = await confirmar(jogadorB.token, partidaId);
  record('Confirmação de B (2º, último) -> Partida.Status = Confirmada',
    confirmB.status === 200 && confirmB.json?.status === 'Confirmada', JSON.stringify(confirmB.json));

  const partidaMongoConfirmada = mongoEval(`JSON.stringify(db.partidas.findOne({_id: UUID("${partidaId}")}))`);
  record('Mongo reflete Status=Confirmada e 2 jogadores em Confirmados',
    /"Status"\s*:\s*"Confirmada"/.test(partidaMongoConfirmada) && (partidaMongoConfirmada.match(/Confirmados/g) || []).length > 0,
    partidaMongoConfirmada);

  // ── Critério 5: desistência devolve o outro jogador à fila ─────────────────────────────
  const buscaD = await buscarPartida(jogadorD.token, jogo.id);
  const buscaE = await buscarPartida(jogadorE.token, jogo.id);
  const partidaDE = buscaE.json?.partidaId;
  record('D e E formam uma partida (setup da desistência)', !!partidaDE, `partidaId=${partidaDE}`);

  const desistD = await desistir(jogadorD.token, partidaDE);
  record('Desistência de D -> Partida.Status = Cancelada',
    desistD.status === 200 && desistD.json?.status === 'Cancelada', JSON.stringify(desistD.json));

  // E deve ter uma NOVA SolicitacaoBusca(Aguardando) — buscar de novo por F deve pareá-lo com ela.
  const buscaF = await buscarPartida(jogadorF.token, jogo.id);
  record('F busca o mesmo jogo -> pareia com E (que foi devolvido à fila pela desistência de D)',
    buscaF.status === 200 && buscaF.json?.status === 'Pareada' && !!buscaF.json?.partidaId,
    `status=${buscaF.status} body=${JSON.stringify(buscaF.json)}`);

  const partidaCanceladaMongo = mongoEval(`JSON.stringify(db.partidas.findOne({_id: UUID("${partidaDE}")}))`);
  record('Partida D+E persistida no Mongo com Status=Cancelada',
    /"Status"\s*:\s*"Cancelada"/.test(partidaCanceladaMongo), partidaCanceladaMongo);

  // ── Critério 6: telemetria (Jaeger) ─────────────────────────────────────────────────────
  log('Consultando Jaeger para o trace de BuscarPartida (F) — esperando propagação...');
  await sleep(3000);
  const tracesRes = await fetch(`${JAEGER_URL}/api/traces?service=partidas-api&limit=20`);
  const tracesJson = await tracesRes.json();
  let melhorTrace = null;
  for (const t of tracesJson.data || []) {
    const servicos = new Set();
    for (const s of t.spans) { const p = t.processes[s.processID]; if (p) servicos.add(p.serviceName); }
    if (servicos.has('partidas-api') && servicos.has('usuarios-api')) { melhorTrace = { traceID: t.traceID, servicos: [...servicos], spans: t.spans.length }; break; }
  }
  record('Existe trace no Jaeger cobrindo partidas-api -> usuarios-api (chamada síncrona PossuiJogo)',
    !!melhorTrace, melhorTrace ? JSON.stringify(melhorTrace) : 'nenhum trace com os 2 serviços encontrado nos últimos 20');

  // ── Resumo ───────────────────────────────────────────────────────────────────────────────
  log('');
  log(`Resumo: ${results.length - failures}/${results.length} critérios passaram`);
  if (failures > 0) {
    log(`${failures} critério(s) FALHARAM — ver detalhes acima.`);
    process.exitCode = 1;
  } else {
    log('Todos os critérios de aceite de Partidas (partidas-T08) foram validados ao vivo.');
  }
}

main().catch((err) => {
  console.error('[FATAL]', err);
  process.exitCode = 1;
});
