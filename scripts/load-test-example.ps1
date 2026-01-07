# Exemplo de Uso - Script de Teste de Carga
# The Throne of Games

Write-Host "=================================" -ForegroundColor Cyan
Write-Host "EXEMPLO DE USO - TESTE DE CARGA" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "1. TESTE RAPIDO (Sanidade)" -ForegroundColor Yellow
Write-Host "   Valida que todos os endpoints estao funcionando" -ForegroundColor Gray
Write-Host "   .\load-test.ps1 -NumUsuarios 5 -NumJogos 10 -NumPedidos 10 -ConcurrentUsers 2" -ForegroundColor White
Write-Host ""

Write-Host "2. TESTE PADRAO" -ForegroundColor Yellow
Write-Host "   Configuracao default para testes de carga normais" -ForegroundColor Gray
Write-Host "   .\load-test.ps1" -ForegroundColor White
Write-Host "   (50 usuarios, 100 jogos, 200 pedidos, 10 threads)" -ForegroundColor Gray
Write-Host ""

Write-Host "3. TESTE MEDIO" -ForegroundColor Yellow
Write-Host "   Para simular carga realista" -ForegroundColor Gray
Write-Host "   .\load-test.ps1 -NumUsuarios 100 -NumJogos 200 -NumPedidos 500 -ConcurrentUsers 20 -GenerateReport" -ForegroundColor White
Write-Host ""

Write-Host "4. TESTE DE ESTRESSE" -ForegroundColor Yellow
Write-Host "   Para identificar limites do sistema" -ForegroundColor Gray
Write-Host "   .\load-test.ps1 -NumUsuarios 500 -NumJogos 1000 -NumPedidos 2000 -ConcurrentUsers 50 -GenerateReport" -ForegroundColor White
Write-Host ""

Write-Host "5. TESTE APENAS DE LEITURA" -ForegroundColor Yellow
Write-Host "   Pula criacao de dados, apenas testa endpoints" -ForegroundColor Gray
Write-Host "   .\load-test.ps1 -SkipDataCreation -ConcurrentUsers 20" -ForegroundColor White
Write-Host ""

Write-Host "=================================" -ForegroundColor Cyan
Write-Host "INTERPRETAR RESULTADOS" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Taxa de Sucesso: " -ForegroundColor Yellow -NoNewline
Write-Host "> 95% = Excelente | 90-95% = Bom | < 90% = Investigar" -ForegroundColor White

Write-Host "Tempo Medio: " -ForegroundColor Yellow -NoNewline
Write-Host "< 200ms = Otimo | 200-500ms = Aceitavel | > 500ms = Atencao" -ForegroundColor White

Write-Host "P95: " -ForegroundColor Yellow -NoNewline
Write-Host "< 500ms = Otimo | 500-1000ms = Aceitavel | > 1000ms = Problema" -ForegroundColor White

Write-Host "P99: " -ForegroundColor Yellow -NoNewline
Write-Host "< 1000ms = Otimo | 1000-2000ms = Aceitavel | > 2000ms = Critico" -ForegroundColor White
Write-Host ""

Write-Host "=================================" -ForegroundColor Cyan
Write-Host "COBERTURA DE ENDPOINTS" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

$endpoints = @(
    @{ API = "Usuarios"; Method = "POST"; Path = "/api/Usuario/pre-register"; Status = "[OK]" },
    @{ API = "Usuarios"; Method = "POST"; Path = "/api/Usuario/activate"; Status = "[OK]" },
    @{ API = "Usuarios"; Method = "POST"; Path = "/api/Usuario/login"; Status = "[OK]" },
    @{ API = "Catalogo"; Method = "POST"; Path = "/api/Game"; Status = "[OK]" },
    @{ API = "Catalogo"; Method = "GET"; Path = "/api/Game"; Status = "[OK]" },
    @{ API = "Catalogo"; Method = "GET"; Path = "/api/Game/{id}"; Status = "[OK]" },
    @{ API = "Vendas"; Method = "POST"; Path = "/api/Pedido"; Status = "[OK]" },
    @{ API = "Vendas"; Method = "GET"; Path = "/api/Pedido"; Status = "[OK]" }
)

foreach ($ep in $endpoints) {
    $color = if ($ep.Status -eq "[OK]") { "Green" } else { "Red" }
    Write-Host "$($ep.Status) " -ForegroundColor $color -NoNewline
    Write-Host "$($ep.API.PadRight(10)) " -NoNewline
    Write-Host "$($ep.Method.PadRight(6)) " -ForegroundColor Cyan -NoNewline
    Write-Host "$($ep.Path)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Total: 8 endpoints testados | 100% de cobertura" -ForegroundColor Green
Write-Host ""

Write-Host "=================================" -ForegroundColor Cyan
Write-Host "PROXIMOS PASSOS" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "1. Execute o teste:" -ForegroundColor Yellow
Write-Host "   .\load-test.ps1 -GenerateReport" -ForegroundColor White
Write-Host ""

Write-Host "2. Monitore no Grafana:" -ForegroundColor Yellow
Write-Host "   http://localhost:3000" -ForegroundColor White
Write-Host ""

Write-Host "3. Analise o relatorio gerado:" -ForegroundColor Yellow
Write-Host "   load-test-report-[timestamp].txt" -ForegroundColor White
Write-Host ""

Write-Host "4. Compare metricas:" -ForegroundColor Yellow
Write-Host "   - Taxa de sucesso" -ForegroundColor Gray
Write-Host "   - Tempo medio de resposta" -ForegroundColor Gray
Write-Host "   - P95 e P99" -ForegroundColor Gray
Write-Host "   - Gargalos por endpoint" -ForegroundColor Gray
Write-Host ""

Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""
