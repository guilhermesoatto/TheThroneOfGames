#!/usr/bin/env node
/**
 * tools/record-delivery.js
 *
 * Automatiza a gravação de tela da demonstração de entrega (FIAP Tech Challenge) usando
 * Playwright: aquece todos os serviços (evita loading demorado aparecendo no vídeo), abre o
 * navegador em modo headed, e executa dois fluxos reais ponta a ponta via Swagger UI:
 *
 *   Fluxo A (admin):   registro -> ativação -> login (Admin) -> criar jogo
 *   Fluxo B (usuário):  registro -> ativação -> login (User)  -> procurar jogo -> comprar jogo
 *                        (compra não existe na Fase 2 — ver PHASE_CONFIG[2].flows.user)
 *
 * Cada passo é uma chamada real via Try-it-out do Swagger (não fetch direto) — o script lê a
 * resposta JSON renderizada na tela para encadear valores entre passos (token de ativação,
 * JWT, id do jogo criado, id do pedido), e usa o botão "Authorize" do Swagger para autenticar
 * as chamadas seguintes com o JWT de cada usuário.
 *
 * Depois dos dois fluxos, navega pela ferramenta de observabilidade da fase (Grafana/
 * Prometheus, Prometheus targets ou Jaeger).
 *
 * O script é agnóstico de fase: detecta a branch git atual (ou aceita --phase=N), lê o
 * README.md da raiz para extrair as URLs dos serviços e cai para os defaults conhecidos de
 * cada fase se o parsing não encontrar uma URL. Todas as URLs podem ser sobrescritas via
 * variável de ambiente — essencial para gravar contra o ambiente real na nuvem (Fase 4 / EKS)
 * em vez de localhost.
 *
 * NÃO sobe nem derruba infraestrutura (docker compose / kubectl) — pressupõe que o ambiente
 * da fase já está de pé e ESTÁVEL, seguindo as instruções do README.md da branch.
 *
 * Uso:
 *   cd tools && npm install && npm run playwright:install
 *   node record-delivery.js                  # detecta a fase pela branch atual
 *   node record-delivery.js --phase=4         # força a fase (2, 3 ou 4)
 *
 * Variáveis de ambiente (todas opcionais — sobrescrevem o que foi lido do README):
 *   BASE_URL_API, BASE_URL_GATEWAY, BASE_URL_USUARIOS, BASE_URL_CATALOGO, BASE_URL_VENDAS,
 *   BASE_URL_PROMETHEUS, BASE_URL_GRAFANA, BASE_URL_JAEGER
 *
 * Saída: docs/videos/FCG_ENTREGA_FASE_<N>.mp4
 */

'use strict';

const { chromium } = require('playwright');
const { execSync } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');

const REPO_ROOT = path.resolve(__dirname, '..');
const README_PATH = path.join(REPO_ROOT, 'README.md');
const VIDEOS_DIR = path.join(REPO_ROOT, 'docs', 'videos');
const VIEWPORT = { width: 1920, height: 1080 };

// Pausa padrão para o espectador ler o que está na tela (resposta de um endpoint, dashboard
// carregado, etc). 4s é o suficiente — mais que isso só deixa o vídeo arrastado.
const PAUSE = 4000;
// Pausa curta para transições que não precisam de leitura (clique em botão, troca de aba).
const BEAT = 1200;

// ── Logging ──────────────────────────────────────────────────────────────────

function log(step, message) {
  const ts = new Date().toISOString().slice(11, 19);
  console.log(`[${ts}] [${step}] ${message}`);
}

// ── Detecção de fase (branch git) ───────────────────────────────────────────

const BRANCH_TO_PHASE = {
  'release/fase-2-monolito': 2,
  'release/fase-3-microservices': 3,
  'release/fase-4-kubernetes': 4,
};

function detectBranch() {
  try {
    return execSync('git rev-parse --abbrev-ref HEAD', { cwd: REPO_ROOT }).toString().trim();
  } catch (err) {
    log('WARN', `Não foi possível detectar a branch git (${err.message}). Use --phase=N.`);
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

  throw new Error(
    `Não foi possível determinar a fase pela branch atual ("${branch}"). ` +
      'Rode a partir de release/fase-{2,3,4}-* ou passe --phase=N explicitamente.'
  );
}

// ── Leitura dinâmica do README.md (URLs dos serviços) ───────────────────────

/**
 * Extrai pares "label: URL" do README — cobre tanto listas com marcador
 * ("- API Gateway: http://localhost:8080") quanto linhas de tabela em pipe
 * ("| `api` | ... | http://localhost:5000 |"), que são os dois formatos usados
 * pelos READMEs das 3 fases. Se o layout do README mudar, ajuste esta regex.
 */
function parseReadmeUrls(readmePath) {
  const urls = {};
  if (!fs.existsSync(readmePath)) return urls;

  const content = fs.readFileSync(readmePath, 'utf-8');

  const bulletRegex = /^-\s+(?:\*\*)?([^:*|]+?)(?:\*\*)?:\s+(https?:\/\/\S+)/gm;
  const tableRegex = /\|\s*`?([^|`]+?)`?\s*\|[^|]*\|[^|]*?(https?:\/\/\S+?)\s*\|/gm;

  for (const regex of [bulletRegex, tableRegex]) {
    let match;
    while ((match = regex.exec(content)) !== null) {
      const label = match[1].trim().toLowerCase();
      const url = match[2].trim().replace(/[).,]+$/, '');
      urls[label] = url;
    }
  }
  return urls;
}

/** Encontra a primeira URL cujo rótulo (lowercase) contém algum dos termos dados. */
function findUrlByLabel(readmeUrls, terms) {
  for (const [label, url] of Object.entries(readmeUrls)) {
    if (terms.some((t) => label.includes(t))) return url;
  }
  return null;
}

function resolveUrl(target, readmeUrls) {
  if (target.envVar && process.env[target.envVar]) return process.env[target.envVar];
  const fromReadme = findUrlByLabel(readmeUrls, target.readmeTerms);
  return fromReadme || target.default;
}

function swaggerUrlOf(baseUrl) {
  return /\/swagger\/?$/.test(baseUrl) ? baseUrl : `${baseUrl.replace(/\/$/, '')}/swagger`;
}

// ── Serviços por fase (nome, termos para achar a URL no README, default) ───────

// As chaves lógicas 'usuarios'/'catalogo'/'vendas' são usadas pelos fluxos abaixo
// independente da fase — na Fase 2 (monolito) as 3 apontam pro mesmo serviço, então
// runStep() naturalmente não navega de novo entre passos (mesma URL resolvida).
const SERVICES = {
  2: (() => {
    const api = { name: 'API (monolito)', envVar: 'BASE_URL_API', readmeTerms: ['api'], default: 'http://localhost:5000' };
    return { usuarios: api, catalogo: api, vendas: api };
  })(),
  3: {
    usuarios: { name: 'Usuários API', envVar: 'BASE_URL_USUARIOS', readmeTerms: ['usuário', 'usuarios'], default: 'http://localhost:5001' },
    catalogo: { name: 'Catálogo API', envVar: 'BASE_URL_CATALOGO', readmeTerms: ['catálogo', 'catalogo'], default: 'http://localhost:5002' },
    vendas: { name: 'Vendas API', envVar: 'BASE_URL_VENDAS', readmeTerms: ['vendas'], default: 'http://localhost:5003' },
  },
  4: {
    usuarios: { name: 'Usuários API', envVar: 'BASE_URL_USUARIOS', readmeTerms: ['usuário', 'usuarios'], default: 'http://localhost:5001' },
    catalogo: { name: 'Catálogo API', envVar: 'BASE_URL_CATALOGO', readmeTerms: ['catálogo', 'catalogo'], default: 'http://localhost:5002' },
    vendas: { name: 'Vendas API', envVar: 'BASE_URL_VENDAS', readmeTerms: ['vendas'], default: 'http://localhost:5003' },
  },
};

const OBSERVABILITY = {
  2: {
    type: 'grafana-prometheus',
    prometheus: { envVar: 'BASE_URL_PROMETHEUS', readmeTerms: ['prometheus'], default: 'http://localhost:9090' },
    grafana: { envVar: 'BASE_URL_GRAFANA', readmeTerms: ['grafana'], default: 'http://localhost:3000' },
  },
  3: {
    type: 'prometheus-targets',
    prometheus: { envVar: 'BASE_URL_PROMETHEUS', readmeTerms: ['prometheus'], default: 'http://localhost:9090' },
  },
  4: {
    type: 'jaeger',
    jaeger: { envVar: 'BASE_URL_JAEGER', readmeTerms: ['jaeger'], default: 'http://localhost:16686' },
  },
};

// Uma credencial nova a cada execução (evita colidir com contas de execuções anteriores).
const RUN_ID = Date.now();
const rnd = (label) => `demo.${label}+${RUN_ID}@example.com`;

/**
 * Fluxos por fase — lista declarativa de steps, executados em sequência pelo runStep().
 * Ver a implementação dos tipos de step (register/activate/login/authorize/call) mais abaixo.
 * `body`/`pathParams`/`query` podem ser objetos fixos OU funções `(state) => valor`, para usar
 * valores capturados em steps anteriores (token, id do jogo criado, id do pedido...).
 *
 * fase4-T-*: Fase 4 usa exatamente os mesmos contratos de API que a Fase 3 (só Docker/K8s
 * mudou) — os fluxos abaixo são compartilhados entre as duas.
 */
function buildFlows(phase) {
  const casing = phase === 2 ? { usuario: 'Usuario', admin: 'admin/Game' } : { usuario: 'usuario', admin: 'admin/game' };

  const registerStep = (service, role, captureAs) => ({
    type: 'register',
    service,
    path: `/api/${casing.usuario}/pre-register`,
    body: { name: role === 'Admin' ? 'Admin Demo' : 'Usuário Demo', email: rnd(role.toLowerCase()), password: 'Demo@12345', role },
    captureAs,
  });
  const activateStep = (service, credsFrom) => ({
    type: 'activate',
    service,
    path: `/api/${casing.usuario}/activate`,
    tokenFrom: credsFrom,
  });
  const loginStep = (service, credsFrom, captureAs) => ({
    type: 'login',
    service,
    path: `/api/${casing.usuario}/login`,
    credsFrom,
    captureAs,
  });

  const adminFlow = [
    registerStep('usuarios', 'Admin', 'admin'),
    activateStep('usuarios', 'admin'),
    loginStep('usuarios', 'admin', 'admin'),
    { type: 'authorize', service: 'catalogo', tokenFrom: 'admin' },
    {
      type: 'call',
      service: 'catalogo',
      method: 'POST',
      path: `/api/${casing.admin}`,
      body: { name: 'Elden Ring', genre: 'RPG', price: 59.99, description: 'Demo FIAP Tech Challenge' },
      captureAs: 'createdGame',
    },
  ];

  const userFlowCommon = [
    registerStep('usuarios', 'User', 'user'),
    activateStep('usuarios', 'user'),
    loginStep('usuarios', 'user', 'user'),
    { type: 'authorize', service: 'catalogo', tokenFrom: 'user' },
    {
      type: 'call',
      service: 'catalogo',
      method: 'GET',
      path: phase === 2 ? '/api/game' : '/api/game/available',
    },
  ];

  const userFlowPurchase =
    phase === 2
      ? [] // Fase 2 não tem endpoint de compra implementado — ver docs/ (decisão registrada).
      : [
          { type: 'authorize', service: 'vendas', tokenFrom: 'user' },
          {
            type: 'call',
            service: 'vendas',
            method: 'POST',
            path: '/api/pedidos',
            captureAs: 'pedido',
            extract: (json) => ({ id: json?.entityId }),
          },
          {
            type: 'call',
            service: 'vendas',
            method: 'POST',
            path: (state) => `/api/pedidos/${state.pedido.id}/itens`,
            pathParams: (state) => ({ pedidoId: state.pedido.id }),
            body: (state) => ({
              jogoId: state.createdGame.id,
              nomeJogo: state.createdGame.name,
              preco: state.createdGame.price,
            }),
          },
          {
            type: 'call',
            service: 'vendas',
            method: 'POST',
            path: (state) => `/api/pedidos/${state.pedido.id}/finalizar`,
            pathParams: (state) => ({ pedidoId: state.pedido.id }),
            body: { metodoPagamento: 'CreditCard' },
          },
        ];

  return { admin: adminFlow, user: [...userFlowCommon, ...userFlowPurchase] };
}

// ── Aquecimento (evita loading/JIT/cold-start aparecendo no vídeo gravado) ──────

async function warmUpUrl(url, { retries = 15, delayMs = 2000 } = {}) {
  for (let attempt = 1; attempt <= retries; attempt++) {
    try {
      const res = await fetch(url, { signal: AbortSignal.timeout(10000) });
      if (res.status) return true;
    } catch {
      // ainda subindo — tenta de novo
    }
    await new Promise((r) => setTimeout(r, delayMs));
  }
  return false;
}

async function warmUp(urls) {
  log('WARMUP', `Aquecendo ${urls.length} serviço(s) antes de iniciar a gravação...`);
  const results = await Promise.all(urls.map(async (url) => ({ url, ok: await warmUpUrl(url) })));
  for (const { url, ok } of results) log('WARMUP', `${ok ? 'OK' : 'TIMEOUT'} — ${url}`);
  if (results.some((r) => !r.ok)) {
    log('WARN', 'Um ou mais serviços não responderam a tempo — confira `docker compose ps` antes de gravar.');
  }
}

// ── Slides de abertura/encerramento ──────────────────────────────────────────

function buildSlideDataUrl({ title, subtitle, bullets = [] }) {
  const bulletsHtml = bullets.map((b) => `<li>${b}</li>`).join('');
  const html = `<!doctype html><html><head><meta charset="utf-8"><style>
    body { margin:0; height:100vh; display:flex; flex-direction:column; justify-content:center; align-items:center;
           background:#0d1117; color:#f0f6fc; font-family:-apple-system,Segoe UI,Roboto,sans-serif; text-align:center; }
    h1 { font-size:56px; margin-bottom:8px; } h2 { font-size:28px; font-weight:400; color:#8b949e; margin-top:0; }
    ul { text-align:left; font-size:22px; line-height:1.8; }
  </style></head><body>
    <h1>${title}</h1>
    <h2>${subtitle}</h2>
    <ul>${bulletsHtml}</ul>
  </body></html>`;
  return `data:text/html,${encodeURIComponent(html)}`;
}

function extractReadmeSummary(readmePath) {
  if (!fs.existsSync(readmePath)) return { title: 'TheThroneOfGames', paragraph: '' };
  const content = fs.readFileSync(readmePath, 'utf-8');
  const titleMatch = content.match(/^#\s+(.+)$/m);
  const paraMatch = content.match(/^(?!#|\s*$)(.+)$/m);
  return {
    title: titleMatch ? titleMatch[1].trim() : 'TheThroneOfGames',
    paragraph: paraMatch ? paraMatch[1].replace(/[*_`]/g, '').trim() : '',
  };
}

// ── Primitivas de Swagger UI ──────────────────────────────────────────────────
// Seletores confirmados contra o Swashbuckle/Swagger UI padrão do ASP.NET Core (inspecionado
// ao vivo). Se o tema/layout do Swagger mudar, ajuste as funções abaixo.

async function findOpblock(page, method, urlPath) {
  return page
    .locator('.opblock')
    .filter({ has: page.locator('.opblock-summary-method', { hasText: method.toUpperCase() }) })
    .filter({ has: page.locator('.opblock-summary-path', { hasText: urlPath }) })
    .first();
}

/** Abre o modal "Authorize", cola "Bearer <token>" e confirma. Precisa ser refeito a cada
 * troca de página do Swagger (estado de auth não sobrevive à navegação) e a cada troca de
 * usuário/token dentro da mesma página. */
async function authorize(page, token) {
  const authBtn = page.locator('.btn.authorize');
  await authBtn.click();
  await page.waitForTimeout(BEAT);

  // Se já havia um token autorizado nesta página (ex.: trocando do admin pro usuário), o
  // modal não mostra mais o campo de input — só "Logout"/"Close" com o valor mascarado
  // ("******"). Precisa fazer Logout antes que o campo de input volte a aparecer.
  const logoutBtn = page.locator('.auth-btn-wrapper button[aria-label="Remove authorization"]');
  if (await logoutBtn.count().then((c) => c > 0).catch(() => false)) {
    await logoutBtn.click();
    await page.waitForTimeout(BEAT);
  }

  const input = page.locator('#auth-bearer-value');
  // O texto de ajuda do modal ("Insira 'Bearer {token}'") é enganoso para um scheme
  // Type=Http/Scheme=bearer: o Swagger UI já prefixa "Bearer " sozinho ao montar o header.
  // Colar "Bearer <token>" aqui produz "Authorization: Bearer Bearer <token>" e todo
  // endpoint [Authorize] responde 401 (confirmado testando os dois casos direto via fetch).
  await input.fill(token);
  await page.locator('.auth-btn-wrapper button.authorize').click();
  await page.waitForTimeout(BEAT);
  const closeBtn = page.locator('.btn.modal-btn.auth.btn-done');
  if (await closeBtn.count()) await closeBtn.click();
  await page.waitForTimeout(BEAT);
}

/**
 * Expande um endpoint, preenche parâmetros de rota/query (tr[data-param-name]) e/ou body
 * (textarea), executa, espera a resposta, lê o JSON da resposta e recolhe o bloco de volta.
 * Retorna o JSON parseado (ou null se a resposta não for JSON/o parse falhar).
 */
async function callEndpoint(page, { method, path: urlPath, params, body }) {
  const opblock = await findOpblock(page, method, urlPath);
  if ((await opblock.count()) === 0) {
    log('WARN', `Endpoint ${method} ${urlPath} não encontrado no Swagger.`);
    return null;
  }

  await opblock.scrollIntoViewIfNeeded();
  await opblock.locator('.opblock-summary').click();
  await page.waitForTimeout(BEAT);

  // Se o mesmo endpoint já foi chamado antes nesta gravação (ex.: /pre-register chamado nos
  // dois fluxos), o opblock reaberto já está em modo "try it out" — o botão único vira
  // Cancel+Reset, então só clica se o botão de alternância (não tocado ainda) existir.
  const tryItOutBtn = opblock.locator('button.try-out__btn:not(.cancel):not(.reset)');
  if (await tryItOutBtn.count()) {
    await tryItOutBtn.click();
    await page.waitForTimeout(BEAT);
  }

  if (params) {
    for (const [name, value] of Object.entries(params)) {
      const input = opblock.locator(`tr[data-param-name="${name}"] input, tr[data-param-name="${name}"] textarea`);
      if (await input.count()) await input.first().fill(String(value));
    }
    await page.waitForTimeout(BEAT / 2);
  }

  if (body) {
    const textarea = opblock.locator('textarea');
    if (await textarea.count()) {
      await textarea.first().fill(JSON.stringify(body, null, 2));
      await page.waitForTimeout(BEAT);
    }
  }

  log('SWAGGER', `Executando ${method} ${urlPath}`);
  await opblock.locator('button.execute').click();
  await opblock.locator('.responses-wrapper').waitFor({ timeout: 15000 }).catch(() => {});
  await opblock.locator('.responses-wrapper').scrollIntoViewIfNeeded().catch(() => {});
  await page.waitForTimeout(PAUSE);

  const responseCode = opblock.locator('tr.response .response-col_status').first();
  const status = (await responseCode.count()) ? (await responseCode.innerText()).trim() : '?';
  log('SWAGGER', `${method} ${urlPath} -> ${status}`);

  let json = null;
  const responseBody = opblock.locator('tr.response .response-col_description pre.microlight code').first();
  if (await responseBody.count()) {
    try {
      json = JSON.parse(await responseBody.innerText());
    } catch {
      // resposta não é JSON (texto puro, ex.: "Usuário ativado com sucesso.") — segue sem capturar.
    }
  }

  await opblock.locator('.opblock-summary').click(); // recolhe antes do próximo passo
  await page.waitForTimeout(BEAT);
  return json;
}

// ── Motor de steps (register/activate/login/authorize/call) ────────────────────

function resolveMaybeFn(value, state) {
  return typeof value === 'function' ? value(state) : value;
}

async function runStep(page, currentUrlRef, services, readmeUrls, state, step) {
  const target = services[step.service];
  const swaggerUrl = swaggerUrlOf(resolveUrl(target, readmeUrls));
  if (currentUrlRef.value !== swaggerUrl) {
    log('SWAGGER', `Abrindo ${target.name} — ${swaggerUrl}`);
    await page.goto(swaggerUrl, { waitUntil: 'domcontentloaded', timeout: 30000 });
    await page.waitForSelector('.swagger-ui', { timeout: 30000 });
    await page.waitForTimeout(BEAT);
    currentUrlRef.value = swaggerUrl;
  }

  switch (step.type) {
    case 'register': {
      const body = resolveMaybeFn(step.body, state);
      const json = await callEndpoint(page, { method: 'POST', path: step.path, body });
      state[step.captureAs] = { ...body, activationToken: json?.activationToken };
      if (!json?.activationToken) log('WARN', `register: activationToken não veio na resposta (${JSON.stringify(json)}).`);
      break;
    }
    case 'activate': {
      const creds = state[step.tokenFrom];
      await callEndpoint(page, { method: 'POST', path: step.path, params: { activationToken: creds.activationToken } });
      break;
    }
    case 'login': {
      const creds = state[step.credsFrom];
      const json = await callEndpoint(page, {
        method: 'POST',
        path: step.path,
        body: { email: creds.email, password: creds.password },
      });
      state[step.captureAs] = { ...state[step.captureAs], token: json?.token, role: json?.role };
      if (!json?.token) log('WARN', `login: token não veio na resposta (${JSON.stringify(json)}).`);
      break;
    }
    case 'authorize': {
      const token = state[step.tokenFrom]?.token;
      if (!token) {
        log('WARN', `authorize: sem token capturado em state.${step.tokenFrom} — pulando.`);
        break;
      }
      log('SWAGGER', `Autorizando como "${step.tokenFrom}" (role ${state[step.tokenFrom].role || '?'})`);
      await authorize(page, token);
      break;
    }
    case 'call': {
      const urlPath = resolveMaybeFn(step.path, state);
      const params = step.pathParams ? resolveMaybeFn(step.pathParams, state) : step.query ? resolveMaybeFn(step.query, state) : undefined;
      const body = step.body ? resolveMaybeFn(step.body, state) : undefined;
      const json = await callEndpoint(page, { method: step.method, path: urlPath, params, body });
      if (step.captureAs) {
        state[step.captureAs] = step.extract ? step.extract(json) : json;
      }
      break;
    }
    default:
      throw new Error(`Tipo de step desconhecido: ${step.type}`);
  }
}

async function runFlow(page, currentUrlRef, services, readmeUrls, state, steps, flowName) {
  log('FLOW', `Iniciando fluxo "${flowName}"`);
  for (const step of steps) {
    await runStep(page, currentUrlRef, services, readmeUrls, state, step);
  }
  log('FLOW', `Fluxo "${flowName}" concluído`);
}

// ── Navegação: Observabilidade ───────────────────────────────────────────────

async function demoGrafanaPrometheus(page, { prometheusUrl, grafanaUrl }) {
  log('OBS', `Prometheus targets — ${prometheusUrl}/targets`);
  await page.goto(`${prometheusUrl}/targets`, { waitUntil: 'domcontentloaded', timeout: 20000 });
  await page.waitForTimeout(PAUSE);

  log('OBS', `Grafana — ${grafanaUrl}`);
  await page.goto(grafanaUrl, { waitUntil: 'domcontentloaded', timeout: 20000 });
  await page.waitForTimeout(BEAT);

  const userField = page.locator('input[name="user"], input[aria-label="Username input field"]');
  if (await userField.count().then((c) => c > 0).catch(() => false)) {
    log('OBS', 'Grafana: autenticando (admin/admin)');
    await userField.first().fill('admin');
    await page.locator('input[name="password"], input[aria-label="Password input field"]').first().fill('admin');
    await page.keyboard.press('Enter');
    await page.waitForTimeout(BEAT);
    const skipBtn = page.locator('button:has-text("Skip")');
    if (await skipBtn.count().then((c) => c > 0).catch(() => false)) await skipBtn.first().click();
  }

  await page.waitForTimeout(PAUSE);
}

async function demoPrometheusTargets(page, { prometheusUrl }) {
  log('OBS', `Prometheus targets — ${prometheusUrl}/targets`);
  await page.goto(`${prometheusUrl}/targets`, { waitUntil: 'domcontentloaded', timeout: 20000 });
  await page.waitForTimeout(PAUSE);
}

async function demoJaeger(page, { jaegerUrl }) {
  log('OBS', `Jaeger UI — ${jaegerUrl}/search`);
  await page.goto(`${jaegerUrl}/search`, { waitUntil: 'domcontentloaded', timeout: 20000 });
  await page.waitForTimeout(BEAT);

  try {
    const serviceSelect = page.locator('.ant-select').first();
    await serviceSelect.click();
    await page.waitForTimeout(BEAT);

    const preferredServices = ['catalogo-api', 'usuarios-api', 'vendas-api'];
    let picked = false;
    for (const name of preferredServices) {
      const option = page.locator('.ant-select-item-option', { hasText: name });
      if (await option.count()) {
        log('OBS', `Jaeger: selecionando serviço "${name}"`);
        await option.first().click();
        picked = true;
        break;
      }
    }
    if (!picked) {
      log('WARN', 'Jaeger: nenhum dos serviços esperados apareceu no dropdown — usando a primeira opção.');
      await page.locator('.ant-select-item-option').first().click();
    }
    await page.waitForTimeout(BEAT);

    const findBtn = page.locator('button[data-test="submit-btn"]');
    await page
      .waitForFunction(
        () => {
          const btn = document.querySelector('[data-test="submit-btn"]');
          return btn && !btn.disabled;
        },
        { timeout: 5000 }
      )
      .catch(() => log('WARN', 'Jaeger: botão "Find Traces" não habilitou a tempo — tentando clicar mesmo assim.'));
    await findBtn.click();
    await page.waitForTimeout(PAUSE);

    const firstTraceLink = page.locator('a[href^="/trace/"]').first();
    if (await firstTraceLink.count()) {
      log('OBS', 'Jaeger: abrindo o trace mais recente (grafo de spans)');
      await firstTraceLink.click();
      await page.waitForTimeout(PAUSE);
    } else {
      log('WARN', 'Jaeger: nenhum trace encontrado na busca — mostrando só a tela de resultados.');
    }
  } catch (err) {
    log('WARN', `Jaeger: navegação best-effort falhou (${err.message}) — seguindo com a tela atual.`);
  }

  await page.waitForTimeout(BEAT);
}

// ── Execução principal ────────────────────────────────────────────────────────

async function main() {
  const phase = resolvePhase();
  const services = SERVICES[phase];
  const observability = OBSERVABILITY[phase];
  const flows = buildFlows(phase);
  log('INIT', `Fase ${phase} — gravação iniciando`);

  fs.mkdirSync(VIDEOS_DIR, { recursive: true });
  const tmpVideoDir = fs.mkdtempSync(path.join(VIDEOS_DIR, '.tmp-'));

  const readmeUrls = parseReadmeUrls(README_PATH);
  const summary = extractReadmeSummary(README_PATH);

  const warmUpTargets = [
    ...Object.values(services).map((t) => swaggerUrlOf(resolveUrl(t, readmeUrls))),
    ...Object.entries(observability).filter(([k]) => k !== 'type').map(([, t]) => resolveUrl(t, readmeUrls)),
  ];
  await warmUp(warmUpTargets);

  const browser = await chromium.launch({ headless: false, args: ['--start-maximized'] });
  const context = await browser.newContext({
    viewport: VIEWPORT,
    recordVideo: { dir: tmpVideoDir, size: VIEWPORT },
  });
  const page = await context.newPage();
  const currentUrlRef = { value: null };
  const state = {};

  try {
    log('STEP-1', 'Slide de abertura');
    await page.goto(
      buildSlideDataUrl({
        title: summary.title,
        subtitle: 'Tech Challenge FIAP — Demonstração de Entrega',
        bullets: [summary.paragraph].filter(Boolean),
      })
    );
    await page.waitForTimeout(PAUSE);

    log('STEP-2', 'Fluxo A — registro -> login (admin) -> criar jogo');
    await runFlow(page, currentUrlRef, services, readmeUrls, state, flows.admin, 'admin');

    log('STEP-3', 'Fluxo B — registro -> login (usuário) -> procurar jogo -> comprar jogo');
    if (phase === 2) {
      log('INFO', 'Fase 2 não tem endpoint de compra implementado — fluxo encerra em "procurar jogo".');
    }
    await runFlow(page, currentUrlRef, services, readmeUrls, state, flows.user, 'usuário');

    log('STEP-4', `Observabilidade (${observability.type})`);
    if (observability.type === 'grafana-prometheus') {
      await demoGrafanaPrometheus(page, {
        prometheusUrl: resolveUrl(observability.prometheus, readmeUrls),
        grafanaUrl: resolveUrl(observability.grafana, readmeUrls),
      });
    } else if (observability.type === 'prometheus-targets') {
      await demoPrometheusTargets(page, { prometheusUrl: resolveUrl(observability.prometheus, readmeUrls) });
    } else if (observability.type === 'jaeger') {
      await demoJaeger(page, { jaegerUrl: resolveUrl(observability.jaeger, readmeUrls) });
    }

    log('STEP-5', 'Slide de encerramento');
    await page.goto(buildSlideDataUrl({ title: 'Fim da demonstração', subtitle: `Fase ${phase}`, bullets: [] }));
    await page.waitForTimeout(PAUSE);
  } finally {
    log('CLOSE', 'Finalizando gravação...');
    await context.close();
    await browser.close();
  }

  const [recordedFile] = fs.readdirSync(tmpVideoDir).filter((f) => f.endsWith('.webm'));
  if (!recordedFile) {
    log('WARN', `Nenhum arquivo de vídeo encontrado em ${tmpVideoDir} — verifique a saída do Playwright.`);
    return;
  }

  const webmPath = path.join(VIDEOS_DIR, `FCG_ENTREGA_FASE_${phase}.webm`);
  const mp4Path = path.join(VIDEOS_DIR, `FCG_ENTREGA_FASE_${phase}.mp4`);
  fs.renameSync(path.join(tmpVideoDir, recordedFile), webmPath);
  fs.rmdirSync(tmpVideoDir);

  try {
    execSync('ffmpeg -version', { stdio: 'ignore' });
    log('DONE', 'Convertendo para .mp4 via ffmpeg...');
    execSync(`ffmpeg -y -i "${webmPath}" -c:v libx264 -preset fast -crf 22 "${mp4Path}"`, { stdio: 'ignore' });
    fs.unlinkSync(webmPath);
    log('DONE', `Vídeo salvo em ${path.relative(REPO_ROOT, mp4Path)}`);
  } catch {
    log('WARN', 'ffmpeg não encontrado no PATH — mantendo o .webm nativo do Playwright.');
    log('DONE', `Vídeo salvo em ${path.relative(REPO_ROOT, webmPath)} (converta manualmente: ffmpeg -i FCG_ENTREGA_FASE_${phase}.webm FCG_ENTREGA_FASE_${phase}.mp4)`);
  }
}

main().catch((err) => {
  console.error('[FATAL]', err);
  process.exitCode = 1;
});
