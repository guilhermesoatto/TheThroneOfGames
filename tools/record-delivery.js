#!/usr/bin/env node
/**
 * tools/record-delivery.js
 *
 * Automatiza a gravação de tela da demonstração de entrega (FIAP Tech Challenge) usando
 * Playwright: aquece todos os serviços (evita loading demorado aparecendo no vídeo), abre o
 * navegador em modo headed, navega pelos Swagger de cada serviço ativo na fase atual,
 * executa algumas chamadas reais de sucesso, e depois navega pela ferramenta de
 * observabilidade correspondente à fase (Grafana/Prometheus, Prometheus targets ou Jaeger).
 *
 * O script é agnóstico de fase: detecta a branch git atual (ou aceita --phase=N), lê o
 * README.md da raiz para extrair as URLs dos serviços ("Serviços disponíveis" / tabela de
 * URLs) e cai para os defaults conhecidos de cada fase se o parsing não encontrar uma URL.
 * Todas as URLs também podem ser sobrescritas via variável de ambiente (ver RESOLVERS
 * abaixo) — essencial para gravar contra o ambiente real na nuvem (Fase 4 / EKS) em vez de
 * localhost.
 *
 * NÃO sobe nem derruba infraestrutura (docker compose / kubectl) — pressupõe que o
 * ambiente da fase já está de pé e ESTÁVEL (rodando há um tempo, não acabou de subir),
 * seguindo as instruções do README.md da branch. O aquecimento (ver warmUp()) reduz mas
 * não elimina o efeito de containers recém-iniciados — para o melhor resultado, espere os
 * healthchecks do `docker compose ps` ficarem "healthy" antes de rodar este script.
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

// ── Configuração por fase ────────────────────────────────────────────────────

/**
 * Cada fase define:
 *  - swaggerTargets: serviços a demonstrar (nome, termos para achar a URL no README,
 *    default de fallback, e demoCalls opcionais — uma ou mais chamadas reais de sucesso).
 *  - observability: qual fluxo de navegação de observabilidade rodar depois dos Swaggers.
 */
const PHASE_CONFIG = {
  2: {
    label: 'Fase 2 — Monolito',
    swaggerTargets: [
      {
        name: 'API (monolito)',
        envVar: 'BASE_URL_API',
        readmeTerms: ['api'],
        default: 'http://localhost:5000',
        demoCalls: [
          {
            method: 'POST',
            path: '/api/usuario/pre-register',
            body: {
              name: 'Demo FIAP',
              email: `demo.fiap+${Date.now()}@example.com`,
              password: 'Demo@12345',
              role: 'Player',
            },
          },
        ],
      },
    ],
    observability: {
      type: 'grafana-prometheus',
      prometheus: { envVar: 'BASE_URL_PROMETHEUS', readmeTerms: ['prometheus'], default: 'http://localhost:9090' },
      grafana: { envVar: 'BASE_URL_GRAFANA', readmeTerms: ['grafana'], default: 'http://localhost:3000' },
    },
  },
  3: {
    label: 'Fase 3 — Microsserviços',
    swaggerTargets: [
      {
        name: 'Usuários API',
        envVar: 'BASE_URL_USUARIOS',
        readmeTerms: ['usuário', 'usuarios'],
        default: 'http://localhost:5001',
        demoCalls: [
          {
            method: 'POST',
            path: '/api/usuario/pre-register',
            body: {
              name: 'Demo FIAP',
              email: `demo.fiap+${Date.now()}@example.com`,
              password: 'Demo@12345',
              role: 'Player',
            },
          },
        ],
      },
      {
        name: 'Catálogo API',
        envVar: 'BASE_URL_CATALOGO',
        readmeTerms: ['catálogo', 'catalogo'],
        default: 'http://localhost:5002',
        demoCalls: [
          { method: 'GET', path: '/api/game' },
          { method: 'GET', path: '/api/game/available' },
        ],
      },
      { name: 'Vendas API', envVar: 'BASE_URL_VENDAS', readmeTerms: ['vendas'], default: 'http://localhost:5003' },
    ],
    observability: {
      type: 'prometheus-targets',
      prometheus: { envVar: 'BASE_URL_PROMETHEUS', readmeTerms: ['prometheus'], default: 'http://localhost:9090' },
    },
  },
  4: {
    label: 'Fase 4 — Kubernetes & Escala',
    swaggerTargets: [
      {
        name: 'Usuários API',
        envVar: 'BASE_URL_USUARIOS',
        readmeTerms: ['usuário', 'usuarios'],
        default: 'http://localhost:5001',
        demoCalls: [
          {
            method: 'POST',
            path: '/api/usuario/pre-register',
            body: {
              name: 'Demo FIAP',
              email: `demo.fiap+${Date.now()}@example.com`,
              password: 'Demo@12345',
              role: 'Player',
            },
          },
        ],
      },
      {
        name: 'Catálogo API',
        envVar: 'BASE_URL_CATALOGO',
        readmeTerms: ['catálogo', 'catalogo'],
        default: 'http://localhost:5002',
        demoCalls: [
          { method: 'GET', path: '/api/game' },
          { method: 'GET', path: '/api/game/available' },
        ],
      },
      { name: 'Vendas API', envVar: 'BASE_URL_VENDAS', readmeTerms: ['vendas'], default: 'http://localhost:5003' },
    ],
    observability: {
      type: 'jaeger',
      jaeger: { envVar: 'BASE_URL_JAEGER', readmeTerms: ['jaeger'], default: 'http://localhost:16686' },
    },
  },
};

function resolveUrl(target, readmeUrls) {
  if (target.envVar && process.env[target.envVar]) return process.env[target.envVar];
  const fromReadme = findUrlByLabel(readmeUrls, target.readmeTerms);
  return fromReadme || target.default;
}

function swaggerUrlOf(baseUrl) {
  return /\/swagger\/?$/.test(baseUrl) ? baseUrl : `${baseUrl.replace(/\/$/, '')}/swagger`;
}

// ── Aquecimento (evita loading/JIT/cold-start aparecendo no vídeo gravado) ──────

/**
 * Faz uma requisição HTTP simples pra cada URL (fora do navegador, sem gravar) até
 * responder ou esgotar as tentativas. O primeiro request de uma API .NET recém-iniciada
 * (JIT, geração do JSON do Swagger, migrations) é sempre o mais lento — aquecer aqui evita
 * que essa lentidão apareça gravada quando o Playwright navegar de verdade.
 */
async function warmUpUrl(url, { retries = 15, delayMs = 2000 } = {}) {
  for (let attempt = 1; attempt <= retries; attempt++) {
    try {
      const res = await fetch(url, { signal: AbortSignal.timeout(10000) });
      // Qualquer resposta HTTP (mesmo 401/404) já prova que a aplicação respondeu e "esquentou".
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
  const results = await Promise.all(
    urls.map(async (url) => ({ url, ok: await warmUpUrl(url) }))
  );
  for (const { url, ok } of results) {
    log('WARMUP', `${ok ? 'OK' : 'TIMEOUT'} — ${url}`);
  }
  if (results.some((r) => !r.ok)) {
    log('WARN', 'Um ou mais serviços não responderam a tempo — confira `docker compose ps` antes de gravar.');
  }
}

// ── Slide de abertura/encerramento (HTML inline, sem depender de renderizar o .md) ──

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

/** Extrai o título (H1) e o primeiro parágrafo descritivo do README para o slide de abertura. */
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

// ── Navegação: Swagger UI ────────────────────────────────────────────────────

/**
 * Abre o Swagger de um serviço, expande a lista de endpoints e executa cada chamada em
 * `demoCalls` ("Try it out" -> preenche o body, se houver -> "Execute" -> rola até a
 * resposta ficar visível -> pausa PAUSE ms para o espectador ler). Os seletores usados são
 * os do Swashbuckle/Swagger UI padrão do ASP.NET Core — se o tema/layout do Swagger mudar,
 * ajuste os seletores `.opblock*` abaixo.
 */
async function demoSwagger(page, serviceName, baseUrl, demoCalls) {
  const url = swaggerUrlOf(baseUrl);
  log('SWAGGER', `Abrindo ${serviceName} — ${url}`);
  await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForSelector('.swagger-ui', { timeout: 30000 });
  await page.waitForTimeout(BEAT);

  // Rola a lista de endpoints para o espectador ver a superfície da API antes de focar em um.
  await page.mouse.wheel(0, 300);
  await page.waitForTimeout(BEAT);

  if (!demoCalls || demoCalls.length === 0) {
    log('SWAGGER', `${serviceName}: sem chamada de demonstração configurada, seguindo em frente.`);
    return;
  }

  for (const demoCall of demoCalls) {
    const opblock = page
      .locator('.opblock')
      .filter({ has: page.locator('.opblock-summary-method', { hasText: demoCall.method.toUpperCase() }) })
      .filter({ has: page.locator('.opblock-summary-path', { hasText: demoCall.path }) })
      .first();

    if ((await opblock.count()) === 0) {
      log('WARN', `${serviceName}: endpoint ${demoCall.method} ${demoCall.path} não encontrado no Swagger — pulando.`);
      continue;
    }

    log('SWAGGER', `${serviceName}: expandindo ${demoCall.method} ${demoCall.path}`);
    await opblock.scrollIntoViewIfNeeded();
    await opblock.locator('.opblock-summary').click();
    await page.waitForTimeout(BEAT);

    const tryItOutBtn = opblock.locator('button.try-out__btn');
    if (await tryItOutBtn.count()) {
      await tryItOutBtn.click();
      await page.waitForTimeout(BEAT);
    }

    if (demoCall.body) {
      const textarea = opblock.locator('textarea');
      if (await textarea.count()) {
        await textarea.first().fill(JSON.stringify(demoCall.body, null, 2));
        await page.waitForTimeout(BEAT);
      }
    }

    log('SWAGGER', `${serviceName}: executando ${demoCall.method} ${demoCall.path}`);
    await opblock.locator('button.execute').click();
    await opblock.locator('.responses-wrapper').waitFor({ timeout: 15000 }).catch(() => {});
    // Garante que a resposta (não só o botão Execute) fique visível no quadro gravado.
    await opblock.locator('.responses-wrapper').scrollIntoViewIfNeeded().catch(() => {});
    await page.waitForTimeout(PAUSE); // dá tempo do espectador ver o status code/response body

    // Recolhe o bloco antes do próximo, pra não empilhar respostas gigantes na tela.
    await opblock.locator('.opblock-summary').click();
    await page.waitForTimeout(BEAT);
  }
}

// ── Navegação: Observabilidade ───────────────────────────────────────────────

async function demoGrafanaPrometheus(page, { prometheusUrl, grafanaUrl }) {
  log('OBS', `Prometheus targets — ${prometheusUrl}/targets`);
  await page.goto(`${prometheusUrl}/targets`, { waitUntil: 'domcontentloaded', timeout: 20000 });
  await page.waitForTimeout(PAUSE);

  log('OBS', `Grafana — ${grafanaUrl}`);
  await page.goto(grafanaUrl, { waitUntil: 'domcontentloaded', timeout: 20000 });
  await page.waitForTimeout(BEAT);

  // Login padrão (admin/admin) — só tenta se a tela de login aparecer.
  const userField = page.locator('input[name="user"], input[aria-label="Username input field"]');
  if (await userField.count().then((c) => c > 0).catch(() => false)) {
    log('OBS', 'Grafana: autenticando (admin/admin)');
    await userField.first().fill('admin');
    await page.locator('input[name="password"], input[aria-label="Password input field"]').first().fill('admin');
    await page.keyboard.press('Enter');
    await page.waitForTimeout(BEAT);
    // Pula o prompt de troca de senha, se aparecer.
    const skipBtn = page.locator('button:has-text("Skip")');
    if (await skipBtn.count().then((c) => c > 0).catch(() => false)) await skipBtn.first().click();
  }

  await page.waitForTimeout(PAUSE); // mostra o dashboard/lista carregada
}

async function demoPrometheusTargets(page, { prometheusUrl }) {
  log('OBS', `Prometheus targets — ${prometheusUrl}/targets`);
  await page.goto(`${prometheusUrl}/targets`, { waitUntil: 'domcontentloaded', timeout: 20000 });
  await page.waitForTimeout(PAUSE);
}

/**
 * Abre a UI do Jaeger, busca traces e abre o primeiro resultado para mostrar o grafo.
 * A estrutura do DOM do Jaeger varia por versão — os seletores abaixo são best-effort
 * (com fallback silencioso) e podem precisar de ajuste conforme a versão da imagem
 * jaegertracing/all-in-one usada (ver k8s/deployments/jaeger-deployment.yaml).
 */
async function demoJaeger(page, { jaegerUrl }) {
  log('OBS', `Jaeger UI — ${jaegerUrl}/search`);
  await page.goto(`${jaegerUrl}/search`, { waitUntil: 'domcontentloaded', timeout: 20000 });
  await page.waitForTimeout(BEAT);

  // Jaeger 1.60 usa Ant Design — o combo de serviço é um .ant-select (não um <select> nativo),
  // e o botão "Find Traces" ([data-test="submit-btn"]) fica desabilitado até um serviço ser
  // escolhido. Prefere um dos 3 microsserviços; cai para a primeira opção da lista se nenhum
  // desses aparecer (nome de serviço/versão do Jaeger pode mudar).
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
    await page.waitForFunction(
      () => {
        const btn = document.querySelector('[data-test="submit-btn"]');
        return btn && !btn.disabled;
      },
      { timeout: 5000 }
    ).catch(() => log('WARN', 'Jaeger: botão "Find Traces" não habilitou a tempo — tentando clicar mesmo assim.'));
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
  const config = PHASE_CONFIG[phase];
  log('INIT', `${config.label} — gravação iniciando`);

  fs.mkdirSync(VIDEOS_DIR, { recursive: true });
  const tmpVideoDir = fs.mkdtempSync(path.join(VIDEOS_DIR, '.tmp-'));

  const readmeUrls = parseReadmeUrls(README_PATH);
  const summary = extractReadmeSummary(README_PATH);

  // Resolve todas as URLs (Swagger + observabilidade) uma vez, antes de aquecer e gravar.
  const swaggerUrls = config.swaggerTargets.map((t) => ({ ...t, url: resolveUrl(t, readmeUrls) }));
  const obsUrls = Object.entries(config.observability)
    .filter(([key]) => key !== 'type')
    .map(([, target]) => resolveUrl(target, readmeUrls));

  // Aquece TUDO antes de abrir o navegador de gravação — elimina o loading demorado do
  // primeiro request de cada serviço aparecendo no vídeo (ver warmUp()).
  const warmUpTargets = [
    ...swaggerUrls.map((t) => swaggerUrlOf(t.url)),
    ...obsUrls,
  ];
  await warmUp(warmUpTargets);

  const browser = await chromium.launch({ headless: false, args: ['--start-maximized'] });
  const context = await browser.newContext({
    viewport: VIEWPORT,
    recordVideo: { dir: tmpVideoDir, size: VIEWPORT },
  });
  const page = await context.newPage();

  try {
    // Passo 1 — slide de abertura com o título/objetivo lidos do README.
    log('STEP-1', 'Slide de abertura');
    await page.goto(
      buildSlideDataUrl({
        title: summary.title,
        subtitle: 'Tech Challenge FIAP — Demonstração de Entrega',
        bullets: [summary.paragraph].filter(Boolean),
      })
    );
    await page.waitForTimeout(PAUSE);

    // Passo 2 — Swagger de cada serviço ativo na fase, com chamadas reais de sucesso.
    log('STEP-2', 'Navegação pelos Swagger dos serviços');
    for (const target of swaggerUrls) {
      await demoSwagger(page, target.name, target.url, target.demoCalls);
    }

    // Passo 3 — observabilidade específica da fase.
    log('STEP-3', `Observabilidade (${config.observability.type})`);
    if (config.observability.type === 'grafana-prometheus') {
      await demoGrafanaPrometheus(page, {
        prometheusUrl: resolveUrl(config.observability.prometheus, readmeUrls),
        grafanaUrl: resolveUrl(config.observability.grafana, readmeUrls),
      });
    } else if (config.observability.type === 'prometheus-targets') {
      await demoPrometheusTargets(page, {
        prometheusUrl: resolveUrl(config.observability.prometheus, readmeUrls),
      });
    } else if (config.observability.type === 'jaeger') {
      await demoJaeger(page, {
        jaegerUrl: resolveUrl(config.observability.jaeger, readmeUrls),
      });
    }

    // Passo 4 — slide de encerramento.
    log('STEP-4', 'Slide de encerramento');
    await page.goto(buildSlideDataUrl({ title: 'Fim da demonstração', subtitle: config.label, bullets: [] }));
    await page.waitForTimeout(PAUSE);
  } finally {
    log('CLOSE', 'Finalizando gravação...');
    await context.close();
    await browser.close();
  }

  // Playwright grava nativamente em .webm com um nome de arquivo gerado automaticamente.
  // Move para um .webm com o nome final e, se o ffmpeg estiver disponível no PATH,
  // converte para o .mp4 pedido pela entrega (senão mantém o .webm, que qualquer player
  // moderno/YouTube também aceita, e avisa no console).
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
