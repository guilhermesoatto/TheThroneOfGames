# Script simples para atualizar deployments com imagens GCR
$PROJECT_ID = "project-62120210-43eb-4d93-954"
$REGISTRY = "gcr.io/$PROJECT_ID"

Write-Host "Atualizando deployments com imagens do GCR..." -ForegroundColor Yellow

# Usuarios API
$content = Get-Content "k8s/deployments/usuarios-api.yaml" -Raw
$content = $content -replace "image: thethroneofgames-usuarios-api:.*", "image: $REGISTRY/usuarios-api:latest"
$content = $content -replace "image: gcr\.io/.*/usuarios-api:.*", "image: $REGISTRY/usuarios-api:latest"
$content | kubectl apply -f -

# Catalogo API
$content = Get-Content "k8s/deployments/catalogo-api.yaml" -Raw
$content = $content -replace "image: thethroneofgames-catalogo-api:.*", "image: $REGISTRY/catalogo-api:latest"
$content = $content -replace "image: gcr\.io/.*/catalogo-api:.*", "image: $REGISTRY/catalogo-api:latest"
$content | kubectl apply -f -

# Vendas API
$content = Get-Content "k8s/deployments/vendas-api.yaml" -Raw
$content = $content -replace "image: thethroneofgames-vendas-api:.*", "image: $REGISTRY/vendas-api:latest"
$content = $content -replace "image: gcr\.io/.*/vendas-api:.*", "image: $REGISTRY/vendas-api:latest"
$content | kubectl apply -f -

Write-Host "Deployments aplicados!" -ForegroundColor Green
