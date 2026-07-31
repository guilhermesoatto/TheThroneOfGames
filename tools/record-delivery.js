#!/usr/bin/env node
/**
 * tools/record-delivery.js
 *
 * Automatiza a gravação da demonstração de entrega (FIAP Tech Challenge) usando Playwright:
 * aquece todos os serviços (evita loading demorado aparecendo no vídeo), abre o navegador em
 * modo headed, e executa dois fluxos reais ponta a ponta via Swagger UI:
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
 * Depois dos dois fluxos, navega pelo RabbitMQ Management UI (mensageria, fases 3/4) e pela
 * ferramenta de observabilidade da fase (Grafana/Prometheus, Prometheus targets ou Jaeger).
 *
 * NARRAÇÃO E MONITORAMENTO (Windows apenas): quando rodando no Windows com ffmpeg disponível,
 * o script grava a TELA CHEIA (ffmpeg gdigrab) em vez de só o viewport do navegador — isso
 * permite sobrepor um overlay sempre-no-topo com `docker stats`/`docker compose ps` num canto,
 * junto com uma narração em PT-BR gerada localmente via SAPI (voz "Microsoft Maria Desktop",
 * sem chave de API nenhuma — ver tools/win-tts.ps1 e tools/win-overlay.ps1). A narração de cada
 * passo é sintetizada antes do passo rodar, e o passo espera o tempo necessário pra não cortar
 * o áudio; os clipes são posicionados na linha do tempo certa e mixados no vídeo em pós-produção
 * (ffmpeg filter_complex: adelay por clipe + amix). Fora do Windows (ou sem ffmpeg no PATH), o
 * script cai automaticamente no modo antigo: só o vídeo do navegador via Playwright, sem áudio.
 * Para desativar narração/overlay explicitamente: NO_NARRATION=1 / NO_OVERLAY=1.
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
 *   NO_NARRATION=1 node record-delivery.js    # sem narração (só overlay + vídeo)
 *   NO_OVERLAY=1 node record-delivery.js      # sem overlay de monitoramento
 *
 * Variáveis de ambiente (todas opcionais — sobrescrevem o que foi lido do README):
 *   BASE_URL_API, BASE_URL_GATEWAY, BASE_URL_USUARIOS, BASE_URL_CATALOGO, BASE_URL_VENDAS,
 *   BASE_URL_PROMETHEUS, BASE_URL_GRAFANA, BASE_URL_JAEGER, BASE_URL_RABBITMQ
 *
 * Saída: docs/videos/FCG_ENTREGA_FASE_<N>.mp4
 */

'use strict';

const { chromium } = require('playwright');
const { execSync, execFileSync, spawn } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');

const REPO_ROOT = path.resolve(__dirname, '..');
const README_PATH = path.join(REPO_ROOT, 'README.md');
const VIDEOS_DIR = path.join(REPO_ROOT, 'docs', 'videos');
const VIEWPORT = { width: 1920, height: 1080 };

const IS_WINDOWS = process.platform === 'win32';
const TTS_SCRIPT = path.join(__dirname, 'win-tts.ps1');
const OVERLAY_SCRIPT = path.join(__dirname, 'win-overlay.ps1');
const NARRATION_VOICE = 'Microsoft Maria Desktop';
const NARRATE_ENABLED = process.env.NO_NARRATION !== '1';
const OVERLAY_ENABLED = process.env.NO_OVERLAY !== '1';

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

// RabbitMQ Management UI — só existe a partir da Fase 3 (mensageria assíncrona).
const MESSAGING = {
  3: { rabbitmq: { envVar: 'BASE_URL_RABBITMQ', readmeTerms: ['rabbitmq'], default: 'http://localhost:15672' } },
  4: { rabbitmq: { envVar: 'BASE_URL_RABBITMQ', readmeTerms: ['rabbitmq'], default: 'http://localhost:15672' } },
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
 * `narrate` (opcional) é o texto PT-BR falado antes do step rodar — ver createNarrator().
 *
 * fase4-T-*: Fase 4 usa exatamente os mesmos contratos de API que a Fase 3 (só Docker/K8s
 * mudou) — os fluxos abaixo são compartilhados entre as duas.
 */
function buildFlows(phase) {
  const casing = phase === 2 ? { usuario: 'Usuario', admin: 'admin/Game' } : { usuario: 'usuario', admin: 'admin/game' };

  const registerStep = (service, role, captureAs, narrate) => ({
    type: 'register',
    service,
    path: `/api/${casing.usuario}/pre-register`,
    body: { name: role === 'Admin' ? 'Admin Demo' : 'Usuário Demo', email: rnd(role.toLowerCase()), password: 'Demo@12345', role },
    captureAs,
    narrate,
  });
  const activateStep = (service, credsFrom, narrate) => ({
    type: 'activate',
    service,
    path: `/api/${casing.usuario}/activate`,
    tokenFrom: credsFrom,
    narrate,
  });
  const loginStep = (service, credsFrom, captureAs, narrate) => ({
    type: 'login',
    service,
    path: `/api/${casing.usuario}/login`,
    credsFrom,
    captureAs,
    narrate,
  });

  // Gestão de usuários pelo admin (/api/admin/user-management/*) — só existe em
  // GameStore.Usuarios.API nesta branch (release/fase-4-kubernetes). Fica de fora do fluxo em
  // qualquer outra fase pra não quebrar a gravação com 404 num endpoint que não existe lá.
  const adminUserManagementSteps =
    phase === 4
      ? [
          {
            type: 'authorize',
            service: 'usuarios',
            tokenFrom: 'admin',
            narrate: 'Autorizando o próprio Swagger de Usuários com o token do administrador, para explorar a gestão de usuários.',
          },
          {
            type: 'call',
            service: 'usuarios',
            method: 'GET',
            path: '/api/admin/user-management',
            narrate: 'Como administrador, listamos todos os usuários cadastrados na plataforma.',
          },
          {
            type: 'call',
            service: 'usuarios',
            method: 'POST',
            path: '/api/admin/user-management',
            body: { name: 'Usuário Gerenciado Demo', email: rnd('managed'), password: 'Managed@12345', role: 'User' },
            captureAs: 'managedUser',
            narrate: 'Cadastrando um novo usuário direto pelo painel de administração, sem depender do autoregistro público.',
          },
          {
            type: 'call',
            service: 'usuarios',
            method: 'GET',
            path: '/api/admin/user-management/{id}',
            pathParams: (state) => ({ id: state.managedUser.id }),
            narrate: 'Consultando os detalhes desse usuário recém-criado.',
          },
          {
            type: 'call',
            service: 'usuarios',
            method: 'PATCH',
            path: '/api/admin/user-management/{id}/role',
            pathParams: (state) => ({ id: state.managedUser.id }),
            body: { newRole: 'Admin' },
            narrate: 'Promovendo esse usuário a administrador.',
          },
          {
            type: 'call',
            service: 'usuarios',
            method: 'POST',
            path: '/api/admin/user-management/{id}/disable',
            pathParams: (state) => ({ id: state.managedUser.id }),
            narrate: 'Desativando a conta — por exemplo, diante de uma suspeita de fraude.',
          },
          {
            type: 'call',
            service: 'usuarios',
            method: 'POST',
            path: '/api/admin/user-management/{id}/enable',
            pathParams: (state) => ({ id: state.managedUser.id }),
            narrate: 'E reativando a conta normalmente.',
          },
        ]
      : [];

  const adminFlow = [
    registerStep('usuarios', 'Admin', 'admin', 'Vamos começar pelo fluxo de administrador: registrando uma nova conta na A P I de usuários.'),
    activateStep('usuarios', 'admin', 'O cadastro gera um token de ativação por e-mail. Aqui a conta é ativada antes do primeiro login.'),
    loginStep('usuarios', 'admin', 'admin', 'Login do administrador. A resposta traz um token JWT que autentica as próximas chamadas.'),
    ...adminUserManagementSteps,
    { type: 'authorize', service: 'catalogo', tokenFrom: 'admin', narrate: 'Autorizando o Swagger do Catálogo com o token do administrador.' },
    {
      type: 'call',
      service: 'catalogo',
      method: 'POST',
      path: `/api/${casing.admin}`,
      // Nome único por execução — o Catálogo rejeita nome duplicado ("Jogo já existe", 400),
      // e rodar o script mais de uma vez contra o mesmo banco é o caso comum em desenvolvimento.
      body: { name: `Elden Ring Demo ${RUN_ID}`, genre: 'RPG', price: 59.99, description: 'Demo FIAP Tech Challenge' },
      captureAs: 'createdGame',
      narrate: 'Como administrador, cadastramos um novo jogo no catálogo.',
    },
  ];

  const userFlowCommon = [
    registerStep('usuarios', 'User', 'user', 'Agora o fluxo de um usuário comum: registro de uma nova conta.'),
    activateStep('usuarios', 'user', 'Ativando a conta do usuário.'),
    loginStep('usuarios', 'user', 'user', 'Login do usuário. O token aqui tem permissões diferentes do administrador.'),
    { type: 'authorize', service: 'catalogo', tokenFrom: 'user', narrate: 'Autorizando o Catálogo com o token do usuário.' },
    {
      type: 'call',
      service: 'catalogo',
      method: 'GET',
      path: phase === 2 ? '/api/game' : '/api/game/available',
      narrate: 'Buscando os jogos disponíveis para compra.',
    },
  ];

  const userFlowPurchase =
    phase === 2
      ? [] // Fase 2 não tem endpoint de compra implementado — ver docs/ (decisão registrada).
      : [
          { type: 'authorize', service: 'vendas', tokenFrom: 'user', narrate: 'Autorizando o serviço de Vendas com o mesmo token do usuário.' },
          {
            type: 'call',
            service: 'vendas',
            method: 'POST',
            path: '/api/pedidos',
            captureAs: 'pedido',
            extract: (json) => ({ id: json?.entityId }),
            narrate: 'Criando um novo pedido para o usuário.',
          },
          {
            // `path` fica com o template ({pedidoId}) — é assim que o Swagger UI renderiza o
            // texto do opblock, então é isso que o findOpblock() precisa casar. O valor real
            // vai só em `pathParams`, preenchendo o campo de parâmetro depois de encontrado.
            type: 'call',
            service: 'vendas',
            method: 'POST',
            path: '/api/pedidos/{pedidoId}/itens',
            pathParams: (state) => ({ pedidoId: state.pedido.id }),
            body: (state) => ({
              jogoId: state.createdGame.id,
              nomeJogo: state.createdGame.name,
              preco: state.createdGame.price,
            }),
            narrate: 'Adicionando o jogo escolhido ao pedido.',
          },
          {
            type: 'call',
            service: 'vendas',
            method: 'POST',
            path: '/api/pedidos/{pedidoId}/finalizar',
            pathParams: (state) => ({ pedidoId: state.pedido.id }),
            body: { metodoPagamento: 'CreditCard' },
            narrate:
              'Finalizando o pedido e disparando o pagamento. Esse evento publica uma mensagem no RabbitMQ, processada de forma assíncrona pelo serviço de notificações.',
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

// ── Narração (TTS local via SAPI — ver tools/win-tts.ps1, sem chave de API) ─────────────

function ttsGenerateClip(text, outPath) {
  execFileSync(
    'powershell',
    ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', TTS_SCRIPT, '-Text', text, '-OutFile', outPath, '-VoiceName', NARRATION_VOICE],
    { stdio: ['ignore', 'ignore', 'pipe'] }
  );
}

function getAudioDurationMs(filePath) {
  const out = execFileSync('ffprobe', ['-v', 'error', '-show_entries', 'format=duration', '-of', 'default=noprint_wrappers=1:nokey=1', filePath])
    .toString()
    .trim();
  return Math.round(parseFloat(out) * 1000);
}

/**
 * Gera os clipes de narração sob demanda e registra o offset (ms desde o início da gravação)
 * de cada um — essa timeline é o que muxNarrationOntoVideo() usa depois pra posicionar cada
 * clipe no ponto certo do vídeo já gravado (ver adelay/amix lá embaixo).
 */
function createNarrator({ enabled, clipsDir }) {
  const timeline = [];
  let recordingStartedAt = null;
  let seq = 0;

  return {
    start() {
      recordingStartedAt = Date.now();
    },
    async say(text) {
      if (!enabled || !recordingStartedAt || !text) return 0;
      seq += 1;
      const clipPath = path.join(clipsDir, `clip-${String(seq).padStart(3, '0')}.wav`);
      try {
        ttsGenerateClip(text, clipPath);
        const durationMs = getAudioDurationMs(clipPath);
        const offsetMs = Date.now() - recordingStartedAt;
        timeline.push({ offsetMs, clipPath });
        log('NARRATE', text);
        return durationMs;
      } catch (err) {
        const detail = err.stderr ? err.stderr.toString().trim().split('\n')[0] : err.message;
        log('WARN', `Narração falhou ("${text.slice(0, 40)}..."): ${detail}`);
        return 0;
      }
    },
    getTimeline() {
      return timeline;
    },
  };
}

/** Fala `text` (se houver) antes de `action`, e garante que `action` não termine antes do
 * áudio — sem isso, a narração de um passo vazaria por cima do início do passo seguinte. */
async function sayAndPace(page, narrator, text, action) {
  if (!text) return action();
  const startedAt = Date.now();
  const durationMs = await narrator.say(text);
  if (durationMs) await page.waitForTimeout(Math.min(durationMs, BEAT));
  const result = await action();
  const remaining = durationMs - (Date.now() - startedAt);
  if (remaining > 0) await page.waitForTimeout(remaining);
  return result;
}

/**
 * Mixa os clipes de narração no vídeo já gravado. Cada clipe entra como um input de áudio
 * separado, atrasado (`adelay`) até o offset em que foi falado durante a gravação, e todos são
 * somados (`amix`) numa única trilha. `normalize=0` evita que o amix divida o volume de cada
 * clipe por N só porque há N inputs — os clipes não se sobrepõem de verdade, então essa
 * normalização automática só deixaria a narração mais baixa à toa.
 * Sem `-shortest`: a trilha de narração quase sempre termina antes do vídeo (slide de
 * encerramento sem fala por cima) — cortar o vídeo nesse ponto destruiria o final gravado.
 */
function muxNarrationOntoVideo(videoPath, timeline, outPath) {
  if (timeline.length === 0) {
    execFileSync('ffmpeg', ['-y', '-i', videoPath, '-c', 'copy', outPath]);
    return;
  }

  const inputArgs = ['-i', videoPath, ...timeline.flatMap((c) => ['-i', c.clipPath])];
  const delayed = timeline.map((c, i) => `[${i + 1}:a]adelay=${c.offsetMs}|${c.offsetMs}[a${i}]`);
  const mixInputs = timeline.map((_, i) => `[a${i}]`).join('');
  const filter = `${delayed.join(';')};${mixInputs}amix=inputs=${timeline.length}:duration=longest:dropout_transition=0:normalize=0[aout]`;

  execFileSync(
    'ffmpeg',
    ['-y', ...inputArgs, '-filter_complex', filter, '-map', '0:v', '-map', '[aout]', '-c:v', 'copy', '-c:a', 'aac', outPath],
    { stdio: 'inherit' }
  );
}

// ── Captura de tela cheia + overlay de monitoramento (Windows) ─────────────────────────

function getPrimaryScreenSize() {
  try {
    const out = execFileSync('powershell', [
      '-NoProfile',
      '-Command',
      'Add-Type -AssemblyName System.Windows.Forms; $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds; "$($b.Width)x$($b.Height)"',
    ])
      .toString()
      .trim();
    const [w, h] = out.split('x').map(Number);
    if (w && h) return { width: w, height: h };
  } catch (err) {
    log('WARN', `Não foi possível detectar a resolução da tela (${err.message}) — usando ${VIEWPORT.width}x${VIEWPORT.height}.`);
  }
  return VIEWPORT;
}

function startDesktopCapture(outPath, screen) {
  return spawn(
    'ffmpeg',
    [
      '-y',
      '-f', 'gdigrab',
      '-framerate', '30',
      '-rtbufsize', '150M',
      '-video_size', `${screen.width}x${screen.height}`,
      '-i', 'desktop',
      '-c:v', 'libx264',
      '-preset', 'veryfast',
      '-crf', '20',
      '-pix_fmt', 'yuv420p',
      outPath,
    ],
    { stdio: ['pipe', 'ignore', 'ignore'] }
  );
}

/** Encerra o ffmpeg mandando 'q' pelo stdin (shutdown gracioso, fecha o arquivo mp4
 * corretamente) — matar o processo (SIGKILL) deixaria o mp4 corrompido/sem moov atom. */
async function stopDesktopCapture(ff) {
  if (!ff || ff.exitCode !== null) return;
  try {
    ff.stdin.write('q');
  } catch {
    // processo já pode ter saído
  }
  await new Promise((resolve) => {
    const timer = setTimeout(() => {
      try {
        ff.kill();
      } catch {
        // ignora
      }
      resolve();
    }, 8000);
    ff.once('exit', () => {
      clearTimeout(timer);
      resolve();
    });
  });
}

// Loop de monitoramento local (docker compose) — mostrado no overlay sempre-no-topo durante a
// gravação. Ambiente-agnóstico só até certo ponto: contra um cluster real (EKS/kind) o
// equivalente seria `kubectl get pods -n gamestore -w`; ajuste esta constante se for gravar
// contra Kubernetes em vez de docker compose local.
const DOCKER_MONITOR_COMMAND =
  "while (1) { Clear-Host; " +
  "Write-Host '=== docker compose ps ===' -ForegroundColor Cyan; " +
  "docker compose ps --format 'table {{.Name}}\\t{{.Status}}'; " +
  "Write-Host ''; " +
  "Write-Host '=== docker stats ===' -ForegroundColor Cyan; " +
  "docker stats --no-stream --format 'table {{.Name}}\\t{{.CPUPerc}}\\t{{.MemUsage}}'; " +
  "Start-Sleep -Seconds 3 }";

function startMonitoringOverlay(title, command, screen) {
  try {
    const width = 620;
    const height = 360;
    const pidOut = execFileSync('powershell', [
      '-NoProfile',
      '-ExecutionPolicy', 'Bypass',
      '-File', OVERLAY_SCRIPT,
      '-Title', title,
      '-Command', command,
      '-WorkingDirectory', REPO_ROOT,
      '-X', String(20),
      '-Y', String(screen.height - height - 20),
      '-Width', String(width),
      '-Height', String(height),
    ]).toString().trim();
    const pid = Number(pidOut);
    log('MONITOR', `Overlay de monitoramento aberto (PID ${pid}).`);
    return pid;
  } catch (err) {
    log('WARN', `Não foi possível abrir o overlay de monitoramento: ${err.message}`);
    return null;
  }
}

function stopMonitoringOverlay(pid) {
  if (!pid) return;
  try {
    execFileSync('taskkill', ['/PID', String(pid), '/T', '/F'], { stdio: 'ignore' });
  } catch {
    // já pode ter encerrado sozinho
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

  // Nem toda versão do Swashbuckle gera o atributo id="auth-bearer-value" no input (a Fase 2
  // gera; a Fase 3/4 não) — aria-label="auth-bearer-value" está presente nas duas.
  const input = page.locator('input[aria-label="auth-bearer-value"]');
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
  let rawResponseText = null;
  if (await responseBody.count()) {
    rawResponseText = await responseBody.innerText();
    try {
      json = JSON.parse(rawResponseText);
    } catch {
      // resposta não é JSON (texto puro, ex.: "Usuário ativado com sucesso.") — segue sem capturar.
    }
  }
  if (!status.startsWith('2') && rawResponseText) {
    log('WARN', `${method} ${urlPath} não retornou 2xx — corpo da resposta: ${rawResponseText.slice(0, 300)}`);
  }

  await opblock.locator('.opblock-summary').click(); // recolhe antes do próximo passo
  await page.waitForTimeout(BEAT);
  return json;
}

// ── Motor de steps (register/activate/login/authorize/call) ────────────────────

function resolveMaybeFn(value, state) {
  return typeof value === 'function' ? value(state) : value;
}

async function runStep(page, currentUrlRef, services, readmeUrls, state, step, narrator) {
  const target = services[step.service];
  const swaggerUrl = swaggerUrlOf(resolveUrl(target, readmeUrls));
  if (currentUrlRef.value !== swaggerUrl) {
    log('SWAGGER', `Abrindo ${target.name} — ${swaggerUrl}`);
    await page.goto(swaggerUrl, { waitUntil: 'domcontentloaded', timeout: 30000 });
    await page.waitForSelector('.swagger-ui', { timeout: 30000 });
    await page.waitForTimeout(BEAT);
    currentUrlRef.value = swaggerUrl;
  }

  const runAction = async () => {
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
  };

  await sayAndPace(page, narrator, step.narrate, runAction);
}

async function runFlow(page, currentUrlRef, services, readmeUrls, state, steps, flowName, narrator) {
  log('FLOW', `Iniciando fluxo "${flowName}"`);
  for (const step of steps) {
    await runStep(page, currentUrlRef, services, readmeUrls, state, step, narrator);
  }
  log('FLOW', `Fluxo "${flowName}" concluído`);
}

// ── Navegação: Mensageria (RabbitMQ) ─────────────────────────────────────────

async function demoRabbitMq(page, { rabbitMqUrl }, narrator) {
  await sayAndPace(
    page,
    narrator,
    'Agora vamos abrir o painel do RabbitMQ — a fila de mensagens que conecta os serviços de forma assíncrona.',
    async () => {
      log('OBS', `RabbitMQ Management UI — ${rabbitMqUrl}`);
      await page.goto(rabbitMqUrl, { waitUntil: 'domcontentloaded', timeout: 20000 });
      await page.waitForTimeout(BEAT);
    }
  );

  const userField = page.locator('#username, input[name="username"]');
  if (await userField.count().then((c) => c > 0).catch(() => false)) {
    log('OBS', 'RabbitMQ: autenticando (guest/guest)');
    await userField.first().fill('guest');
    await page.locator('#password, input[name="password"]').first().fill('guest');
    await page.locator('input[type="submit"]').first().click();
    await page.waitForTimeout(PAUSE);
  }

  await sayAndPace(
    page,
    narrator,
    'Aqui vemos as filas ativas e a taxa de mensagens processadas em tempo real entre os microsserviços.',
    async () => {
      const queuesTab = page.locator('a:has-text("Queues")');
      if (await queuesTab.count()) {
        await queuesTab.first().click();
        await page.waitForTimeout(PAUSE);
      }
    }
  );
}

// ── Navegação: Observabilidade ───────────────────────────────────────────────

async function demoGrafanaPrometheus(page, { prometheusUrl, grafanaUrl }, narrator) {
  await sayAndPace(page, narrator, 'Vamos conferir o Prometheus, que coleta as métricas de todos os serviços.', async () => {
    log('OBS', `Prometheus targets — ${prometheusUrl}/targets`);
    await page.goto(`${prometheusUrl}/targets`, { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForTimeout(PAUSE);
  });

  await sayAndPace(page, narrator, 'E o Grafana, com os dashboards montados em cima dessas métricas.', async () => {
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
  });
}

async function demoPrometheusTargets(page, { prometheusUrl }, narrator) {
  await sayAndPace(page, narrator, 'Vamos conferir o Prometheus, que coleta as métricas de todos os serviços.', async () => {
    log('OBS', `Prometheus targets — ${prometheusUrl}/targets`);
    await page.goto(`${prometheusUrl}/targets`, { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForTimeout(PAUSE);
  });
}

async function demoJaeger(page, { jaegerUrl }, narrator) {
  await sayAndPace(page, narrator, 'Por fim, o Jaeger — o rastreamento distribuído que acompanha uma requisição por todos os serviços.', async () => {
    log('OBS', `Jaeger UI — ${jaegerUrl}/search`);
    await page.goto(`${jaegerUrl}/search`, { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForTimeout(BEAT);
  });

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
      await sayAndPace(page, narrator, 'Abrindo o trace mais recente — o grafo mostra o tempo gasto em cada serviço dentro da mesma requisição.', async () => {
        log('OBS', 'Jaeger: abrindo o trace mais recente (grafo de spans)');
        await firstTraceLink.click();
        await page.waitForTimeout(PAUSE);
      });
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
  const messaging = MESSAGING[phase] || null;
  const flows = buildFlows(phase);
  log('INIT', `Fase ${phase} — gravação iniciando`);

  fs.mkdirSync(VIDEOS_DIR, { recursive: true });
  const runTmpDir = fs.mkdtempSync(path.join(VIDEOS_DIR, '.tmp-'));
  const clipsDir = path.join(runTmpDir, 'clips');
  fs.mkdirSync(clipsDir);

  const readmeUrls = parseReadmeUrls(README_PATH);
  const summary = extractReadmeSummary(README_PATH);

  const warmUpTargets = [
    ...Object.values(services).map((t) => swaggerUrlOf(resolveUrl(t, readmeUrls))),
    ...Object.entries(observability).filter(([k]) => k !== 'type').map(([, t]) => resolveUrl(t, readmeUrls)),
    ...(messaging ? [resolveUrl(messaging.rabbitmq, readmeUrls)] : []),
  ];
  await warmUp(warmUpTargets);

  let ffmpegAvailable = false;
  try {
    execFileSync('ffmpeg', ['-version'], { stdio: 'ignore' });
    ffmpegAvailable = true;
  } catch {
    // tratado abaixo
  }
  const desktopMode = IS_WINDOWS && ffmpegAvailable;
  if (!desktopMode) {
    log(
      'WARN',
      IS_WINDOWS
        ? 'ffmpeg não encontrado no PATH — gravando só o vídeo do navegador, sem narração/overlay.'
        : 'Narração e overlay de monitoramento só estão implementados para Windows nesta versão — gravando só o vídeo do navegador.'
    );
  }

  const narrator = createNarrator({ enabled: desktopMode && NARRATE_ENABLED, clipsDir });
  const screen = desktopMode ? getPrimaryScreenSize() : VIEWPORT;

  // Chrome oferece traduzir a página (Swagger/RabbitMQ/Jaeger renderizam texto em inglês
  // dentro de um Chrome com locale pt-BR) alguns segundos após a 1ª navegação — puramente
  // cosmético (a automação não interage com o popup), mas aparece no vídeo se não suprimido.
  const launchArgs = ['--disable-features=Translate,TranslateUI'];
  if (desktopMode) launchArgs.push('--window-position=0,0', `--window-size=${screen.width},${screen.height}`, '--start-maximized');

  const browser = await chromium.launch({ headless: false, args: launchArgs });
  const context = await browser.newContext(
    desktopMode ? { viewport: null } : { viewport: VIEWPORT, recordVideo: { dir: runTmpDir, size: VIEWPORT } }
  );
  const page = await context.newPage();
  const currentUrlRef = { value: null };
  const state = {};

  let ff = null;
  let overlayPid = null;
  const desktopCapturePath = path.join(runTmpDir, 'desktop-capture.mp4');

  if (desktopMode) {
    ff = startDesktopCapture(desktopCapturePath, screen);
    narrator.start();
    await new Promise((r) => setTimeout(r, 1000)); // folga pro ffmpeg abrir o arquivo antes da 1ª narração
    if (OVERLAY_ENABLED) {
      overlayPid = startMonitoringOverlay(`GameStoreMonitor-${Date.now()}`, DOCKER_MONITOR_COMMAND, screen);
    }
  }

  try {
    log('STEP-1', 'Slide de abertura');
    await sayAndPace(
      page,
      narrator,
      `Bem-vindo à demonstração do ${summary.title}. Vamos percorrer o fluxo de administrador, o fluxo de usuário, a mensageria e o monitoramento distribuído.`,
      async () => {
        await page.goto(
          buildSlideDataUrl({
            title: summary.title,
            subtitle: 'Tech Challenge FIAP — Demonstração de Entrega',
            bullets: [summary.paragraph].filter(Boolean),
          })
        );
        await page.waitForTimeout(PAUSE);
      }
    );

    log('STEP-2', 'Fluxo A — registro -> login (admin) -> criar jogo');
    await runFlow(page, currentUrlRef, services, readmeUrls, state, flows.admin, 'admin', narrator);

    log('STEP-3', 'Fluxo B — registro -> login (usuário) -> procurar jogo -> comprar jogo');
    if (phase === 2) {
      log('INFO', 'Fase 2 não tem endpoint de compra implementado — fluxo encerra em "procurar jogo".');
    }
    await runFlow(page, currentUrlRef, services, readmeUrls, state, flows.user, 'usuário', narrator);

    if (messaging) {
      log('STEP-4', 'Mensageria (RabbitMQ)');
      await demoRabbitMq(page, { rabbitMqUrl: resolveUrl(messaging.rabbitmq, readmeUrls) }, narrator);
    }

    log('STEP-5', `Observabilidade (${observability.type})`);
    if (observability.type === 'grafana-prometheus') {
      await demoGrafanaPrometheus(
        page,
        { prometheusUrl: resolveUrl(observability.prometheus, readmeUrls), grafanaUrl: resolveUrl(observability.grafana, readmeUrls) },
        narrator
      );
    } else if (observability.type === 'prometheus-targets') {
      await demoPrometheusTargets(page, { prometheusUrl: resolveUrl(observability.prometheus, readmeUrls) }, narrator);
    } else if (observability.type === 'jaeger') {
      await demoJaeger(page, { jaegerUrl: resolveUrl(observability.jaeger, readmeUrls) }, narrator);
    }

    log('STEP-6', 'Slide de encerramento');
    await sayAndPace(page, narrator, 'Isso conclui a demonstração. Obrigado!', async () => {
      await page.goto(buildSlideDataUrl({ title: 'Fim da demonstração', subtitle: `Fase ${phase}`, bullets: [] }));
      await page.waitForTimeout(PAUSE);
    });
  } finally {
    log('CLOSE', 'Finalizando gravação...');
    await context.close();
    await browser.close();
    if (desktopMode) {
      stopMonitoringOverlay(overlayPid);
      await stopDesktopCapture(ff);
    }
  }

  const mp4Path = path.join(VIDEOS_DIR, `FCG_ENTREGA_FASE_${phase}.mp4`);

  if (desktopMode) {
    log('DONE', 'Montando a trilha de narração e mixando com o vídeo capturado...');
    try {
      muxNarrationOntoVideo(desktopCapturePath, narrator.getTimeline(), mp4Path);
      log('DONE', `Vídeo salvo em ${path.relative(REPO_ROOT, mp4Path)}`);
    } catch (err) {
      log('WARN', `Falha ao mixar narração (${err.message}) — copiando o vídeo capturado sem áudio.`);
      fs.copyFileSync(desktopCapturePath, mp4Path);
    }
  } else {
    const [recordedFile] = fs.readdirSync(runTmpDir).filter((f) => f.endsWith('.webm'));
    if (!recordedFile) {
      log('WARN', `Nenhum arquivo de vídeo encontrado em ${runTmpDir} — verifique a saída do Playwright.`);
      fs.rmSync(runTmpDir, { recursive: true, force: true });
      return;
    }
    const webmPath = path.join(VIDEOS_DIR, `FCG_ENTREGA_FASE_${phase}.webm`);
    fs.renameSync(path.join(runTmpDir, recordedFile), webmPath);

    if (ffmpegAvailable) {
      log('DONE', 'Convertendo para .mp4 via ffmpeg...');
      execFileSync('ffmpeg', ['-y', '-i', webmPath, '-c:v', 'libx264', '-preset', 'fast', '-crf', '22', mp4Path]);
      fs.unlinkSync(webmPath);
      log('DONE', `Vídeo salvo em ${path.relative(REPO_ROOT, mp4Path)}`);
    } else {
      log('WARN', 'ffmpeg não encontrado no PATH — mantendo o .webm nativo do Playwright.');
      log('DONE', `Vídeo salvo em ${path.relative(REPO_ROOT, webmPath)} (converta manualmente: ffmpeg -i FCG_ENTREGA_FASE_${phase}.webm FCG_ENTREGA_FASE_${phase}.mp4)`);
    }
  }

  fs.rmSync(runTmpDir, { recursive: true, force: true });
}

main().catch((err) => {
  console.error('[FATAL]', err);
  process.exitCode = 1;
});
