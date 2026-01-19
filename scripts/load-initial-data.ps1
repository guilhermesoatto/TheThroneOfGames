# Script de Carga Inicial de Dados - The Throne of Games
# Este script popula o banco de dados com dados de teste

$BaseUrl = "http://localhost:5001"  # Usuarios API
$CatalogoUrl = "http://localhost:5002"  # Catalogo API
$VendasUrl = "http://localhost:5003"  # Vendas API

Write-Host "========================================" -ForegroundColor Green
Write-Host "  THE THRONE OF GAMES - CARGA INICIAL  " -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# Função para fazer requisição com tratamento de erro
function Invoke-ApiRequest {
    param (
        [string]$Url,
        [string]$Method = "POST",
        [object]$Body = $null,
        [string]$Token = $null
    )
    
    try {
        $headers = @{
            "Content-Type" = "application/json"
        }
        
        if ($Token) {
            $headers["Authorization"] = "Bearer $Token"
        }
        
        if ($Body) {
            $jsonBody = $Body | ConvertTo-Json -Depth 10
            $response = Invoke-RestMethod -Uri $Url -Method $Method -Headers $headers -Body $jsonBody
        } else {
            $response = Invoke-RestMethod -Uri $Url -Method $Method -Headers $headers
        }
        
        return $response
    }
    catch {
        Write-Host "Erro na requisição: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# Aguardar APIs ficarem prontas
Write-Host "Aguardando APIs ficarem prontas..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

Write-Host ""
Write-Host "1. CRIANDO USUÁRIOS" -ForegroundColor Cyan
Write-Host "-------------------" -ForegroundColor Cyan

# Usuários para criar
$usuarios = @(
    @{
        nome = "Admin Geral"
        email = "admin@thethroneofgames.com"
        senha = "Admin@123"
        perfil = "Administrador"
    },
    @{
        nome = "João Silva"
        email = "joao.silva@gmail.com"
        senha = "Senha@123"
        perfil = "Cliente"
    },
    @{
        nome = "Maria Santos"
        email = "maria.santos@gmail.com"
        senha = "Senha@123"
        perfil = "Cliente"
    },
    @{
        nome = "Pedro Oliveira"
        email = "pedro.oliveira@outlook.com"
        senha = "Senha@123"
        perfil = "Cliente"
    },
    @{
        nome = "Ana Costa"
        email = "ana.costa@yahoo.com"
        senha = "Senha@123"
        perfil = "Cliente"
    }
)

$tokenAdmin = $null

foreach ($usuario in $usuarios) {
    Write-Host "  Criando usuário: $($usuario.nome)" -ForegroundColor White
    $response = Invoke-ApiRequest -Url "$BaseUrl/api/usuario/registrar" -Body $usuario
    
    if ($response) {
        Write-Host "    ✓ Usuário criado com sucesso!" -ForegroundColor Green
        
        # Se for admin, fazer login para obter token
        if ($usuario.perfil -eq "Administrador") {
            $loginData = @{
                email = $usuario.email
                senha = $usuario.senha
            }
            $loginResponse = Invoke-ApiRequest -Url "$BaseUrl/api/usuario/login" -Body $loginData
            if ($loginResponse -and $loginResponse.token) {
                $tokenAdmin = $loginResponse.token
                Write-Host "    ✓ Token admin obtido!" -ForegroundColor Green
            }
        }
    }
}

Write-Host ""
Write-Host "2. CRIANDO JOGOS NO CATÁLOGO" -ForegroundColor Cyan
Write-Host "-----------------------------" -ForegroundColor Cyan

# Jogos para criar
$jogos = @(
    @{
        nome = "The Last of Us Part II"
        descricao = "Jogo de ação e aventura em mundo pós-apocalíptico"
        preco = 249.90
        categoria = "Ação"
        plataforma = "PlayStation 5"
        quantidadeEstoque = 50
        imagemUrl = "https://image.api.playstation.com/vulcan/ap/rnd/202010/2618/qxNW7YKStvmFCF7OlKh8D4jM.png"
    },
    @{
        nome = "God of War Ragnarök"
        descricao = "Aventura épica nórdica com Kratos e Atreus"
        preco = 299.90
        categoria = "Ação/Aventura"
        plataforma = "PlayStation 5"
        quantidadeEstoque = 45
        imagemUrl = "https://image.api.playstation.com/vulcan/ap/rnd/202207/1210/4xJ8XB3bi888QTLZYdl7Oi0s.png"
    },
    @{
        nome = "Elden Ring"
        descricao = "RPG de ação em mundo aberto da FromSoftware"
        preco = 279.90
        categoria = "RPG"
        plataforma = "PC/PlayStation 5/Xbox Series X"
        quantidadeEstoque = 60
        imagemUrl = "https://image.api.playstation.com/vulcan/ap/rnd/202110/2000/aGhopp3MHppi7kooGE2Dtt8C.png"
    },
    @{
        nome = "Cyberpunk 2077"
        descricao = "RPG de ação em cidade futurística de Night City"
        preco = 199.90
        categoria = "RPG"
        plataforma = "PC/PlayStation 5/Xbox Series X"
        quantidadeEstoque = 40
        imagemUrl = "https://image.api.playstation.com/vulcan/ap/rnd/202111/3013/cKZ4tKNFj9C00giTzYtH8PF1.png"
    },
    @{
        nome = "Red Dead Redemption 2"
        descricao = "Épico faroeste em mundo aberto"
        preco = 229.90
        categoria = "Ação/Aventura"
        plataforma = "PC/PlayStation 4/Xbox One"
        quantidadeEstoque = 35
        imagemUrl = "https://image.api.playstation.com/vulcan/ap/rnd/201807/3113/wz38b0bvU0MHJqh0t1V7fpC0.png"
    },
    @{
        nome = "Horizon Forbidden West"
        descricao = "Aventura pós-apocalíptica com dinossauros robóticos"
        preco = 269.90
        categoria = "Ação/Aventura"
        plataforma = "PlayStation 5"
        quantidadeEstoque = 42
        imagemUrl = "https://image.api.playstation.com/vulcan/ap/rnd/202107/3100/ki0STHGAkIF06Q4AU8Ow4OkV.png"
    },
    @{
        nome = "Spider-Man Miles Morales"
        descricao = "Aventura de super-herói em Nova York"
        preco = 219.90
        categoria = "Ação"
        plataforma = "PlayStation 5"
        quantidadeEstoque = 38
        imagemUrl = "https://image.api.playstation.com/vulcan/ap/rnd/202008/1420/PRfYtTZQsz5trZbVwZwGvJQT.png"
    },
    @{
        nome = "Hogwarts Legacy"
        descricao = "RPG de ação no universo de Harry Potter"
        preco = 289.90
        categoria = "RPG"
        plataforma = "PC/PlayStation 5/Xbox Series X"
        quantidadeEstoque = 55
        imagemUrl = "https://image.api.playstation.com/vulcan/ap/rnd/202208/0921/dR9KJAKDW2izPbptHQr6gHwt.png"
    },
    @{
        nome = "FIFA 24"
        descricao = "Simulador de futebol mais realista"
        preco = 299.90
        categoria = "Esportes"
        plataforma = "PC/PlayStation 5/Xbox Series X"
        quantidadeEstoque = 70
        imagemUrl = "https://image.api.playstation.com/vulcan/ap/rnd/202305/2420/e0e78a53c3c242bb3ecc27e34ce9bc1ecd09d37f95a0d9e9.png"
    },
    @{
        nome = "Call of Duty Modern Warfare III"
        descricao = "FPS multiplayer de ação militar"
        preco = 319.90
        categoria = "FPS"
        plataforma = "PC/PlayStation 5/Xbox Series X"
        quantidadeEstoque = 65
        imagemUrl = "https://image.api.playstation.com/vulcan/ap/rnd/202305/3118/fb5c49031c8c0dd7b3054f3c5b2f2e1f3a1e6e5f.png"
    }
)

foreach ($jogo in $jogos) {
    Write-Host "  Criando jogo: $($jogo.nome)" -ForegroundColor White
    $response = Invoke-ApiRequest -Url "$CatalogoUrl/api/game" -Body $jogo -Token $tokenAdmin
    
    if ($response) {
        Write-Host "    ✓ Jogo criado com sucesso! ID: $($response.id)" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "3. GERANDO TRANSAÇÕES DE TESTE" -ForegroundColor Cyan
Write-Host "-------------------------------" -ForegroundColor Cyan

# Simular algumas vendas
Write-Host "  Criando pedidos de teste..." -ForegroundColor White

# Login dos clientes para obter tokens
$clienteTokens = @()

foreach ($usuario in $usuarios | Where-Object { $_.perfil -eq "Cliente" }) {
    $loginData = @{
        email = $usuario.email
        senha = $usuario.senha
    }
    $loginResponse = Invoke-ApiRequest -Url "$BaseUrl/api/usuario/login" -Body $loginData
    if ($loginResponse -and $loginResponse.token) {
        $clienteTokens += @{
            nome = $usuario.nome
            token = $loginResponse.token
        }
    }
}

# Criar alguns pedidos
for ($i = 0; $i -lt 10; $i++) {
    $cliente = $clienteTokens | Get-Random
    $numItens = Get-Random -Minimum 1 -Maximum 4
    
    $pedido = @{
        itens = @()
    }
    
    for ($j = 0; $j -lt $numItens; $j++) {
        $jogoId = Get-Random -Minimum 1 -Maximum ($jogos.Count + 1)
        $quantidade = Get-Random -Minimum 1 -Maximum 3
        
        $pedido.itens += @{
            jogoId = $jogoId
            quantidade = $quantidade
        }
    }
    
    Write-Host "    Pedido $($i + 1): Cliente $($cliente.nome) - $numItens item(ns)" -ForegroundColor White
    $response = Invoke-ApiRequest -Url "$VendasUrl/api/pedido" -Body $pedido -Token $cliente.token
    
    if ($response) {
        Write-Host "      ✓ Pedido criado! Total: R$ $($response.valorTotal)" -ForegroundColor Green
    }
    
    Start-Sleep -Milliseconds 500
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  CARGA INICIAL CONCLUÍDA COM SUCESSO! " -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

Write-Host "Estatísticas:" -ForegroundColor Yellow
Write-Host "  • $($usuarios.Count) usuários criados" -ForegroundColor White
Write-Host "  • $($jogos.Count) jogos cadastrados" -ForegroundColor White
Write-Host "  • 10 pedidos gerados" -ForegroundColor White
Write-Host ""

Write-Host "Acesse o Grafana em: http://localhost:3000" -ForegroundColor Cyan
Write-Host "  Usuário: admin" -ForegroundColor White
Write-Host "  Senha: admin" -ForegroundColor White
Write-Host ""

Write-Host "APIs disponíveis:" -ForegroundColor Cyan
Write-Host "  • Usuarios API: http://localhost:5001/swagger" -ForegroundColor White
Write-Host "  • Catalogo API: http://localhost:5002/swagger" -ForegroundColor White
Write-Host "  • Vendas API: http://localhost:5003/swagger" -ForegroundColor White
Write-Host ""

Write-Host "Prometheus: http://localhost:9090" -ForegroundColor Cyan
Write-Host "RabbitMQ Management: http://localhost:15672 (guest/guest)" -ForegroundColor Cyan
Write-Host ""
