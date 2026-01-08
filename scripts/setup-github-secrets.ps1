# Script para configurar GitHub Secrets para CI/CD com GKE
# Executa: .\scripts\setup-github-secrets.ps1

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  CONFIGURACAO GITHUB SECRETS - GKE  " -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

# Verificar se a chave existe
$keyPath = "gcp-key.json"
if (-not (Test-Path $keyPath)) {
    Write-Host "`nERRO: Arquivo gcp-key.json nao encontrado!" -ForegroundColor Red
    Write-Host "Execute primeiro os comandos de criacao da service account." -ForegroundColor Yellow
    exit 1
}

# Ler conteúdo da chave
$gcpCredentials = Get-Content $keyPath -Raw

# Informações do projeto
$projectId = "project-62120210-43eb-4d93-954"

Write-Host "`n=== SECRETS NECESSARIOS ===" -ForegroundColor Yellow
Write-Host "1. GCP_CREDENTIALS" -ForegroundColor White
Write-Host "2. GCP_PROJECT_ID" -ForegroundColor White

Write-Host "`n=== INSTRUCOES PARA ADICIONAR NO GITHUB ===" -ForegroundColor Yellow
Write-Host "1. Acesse: https://github.com/seuusuario/TheThroneOfGames/settings/secrets/actions" -ForegroundColor White
Write-Host "2. Clique em 'New repository secret'" -ForegroundColor White
Write-Host "3. Adicione os secrets abaixo:" -ForegroundColor White

Write-Host "`n--- SECRET 1: GCP_CREDENTIALS ---" -ForegroundColor Cyan
Write-Host "Nome: GCP_CREDENTIALS" -ForegroundColor Green
Write-Host "Valor: (copie o conteudo abaixo - CTRL+C depois de selecionar)" -ForegroundColor Gray
Write-Host ""
Write-Host "INICIO DO JSON ====================================" -ForegroundColor Magenta
Write-Host $gcpCredentials
Write-Host "FIM DO JSON =======================================" -ForegroundColor Magenta
Write-Host ""

Write-Host "--- SECRET 2: GCP_PROJECT_ID ---" -ForegroundColor Cyan
Write-Host "Nome: GCP_PROJECT_ID" -ForegroundColor Green
Write-Host "Valor: $projectId" -ForegroundColor White

Write-Host "`n=== VERIFICACAO ===" -ForegroundColor Yellow
Write-Host "Apos adicionar os secrets, execute um push para master/main" -ForegroundColor White
Write-Host "O GitHub Actions ira:" -ForegroundColor White
Write-Host "  1. Executar testes unitarios" -ForegroundColor Gray
Write-Host "  2. Construir imagens Docker" -ForegroundColor Gray
Write-Host "  3. Executar testes de performance" -ForegroundColor Gray
Write-Host "  4. Fazer scan de seguranca" -ForegroundColor Gray
Write-Host "  5. Fazer deploy automatico no GKE" -ForegroundColor Gray

Write-Host "`n=== COMANDOS PARA VERIFICAR DEPLOY ===" -ForegroundColor Yellow
Write-Host "kubectl get pods -n thethroneofgames" -ForegroundColor Cyan
Write-Host "kubectl get services -n thethroneofgames" -ForegroundColor Cyan
Write-Host "kubectl get ingress -n thethroneofgames" -ForegroundColor Cyan
Write-Host "kubectl get hpa -n thethroneofgames" -ForegroundColor Cyan

Write-Host "`n=== SEGURANCA ===" -ForegroundColor Red
Write-Host "IMPORTANTE: A chave gcp-key.json contem credenciais sensiveis!" -ForegroundColor Red
Write-Host "- Ela esta no .gitignore (nao sera commitada)" -ForegroundColor Yellow
Write-Host "- Guarde em local seguro ou delete apos configurar secrets" -ForegroundColor Yellow
Write-Host "- Nunca compartilhe ou commite esta chave!" -ForegroundColor Yellow

Write-Host "`nPressione qualquer tecla para abrir o navegador nas configuracoes do GitHub..." -ForegroundColor Green
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Tentar abrir navegador
$repoUrl = git config --get remote.origin.url
if ($repoUrl) {
    $repoUrl = $repoUrl -replace "\.git$", ""
    $repoUrl = $repoUrl -replace "git@github.com:", "https://github.com/"
    $settingsUrl = "$repoUrl/settings/secrets/actions"
    Write-Host "Abrindo: $settingsUrl" -ForegroundColor Cyan
    Start-Process $settingsUrl
} else {
    Write-Host "Nao foi possivel determinar a URL do repositorio." -ForegroundColor Yellow
    Write-Host "Abra manualmente: https://github.com/seuusuario/TheThroneOfGames/settings/secrets/actions" -ForegroundColor White
}

Write-Host "`nScript concluido!" -ForegroundColor Green
