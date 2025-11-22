# TheThroneOfGames

## Visão Geral
TheThroneOfGames é uma API Web moderna e segura em ASP.NET Core para gerenciar uma plataforma digital de jogos. Este projeto foi desenvolvido como solução para o desafio Tech Challenge (ver `TheThroneOfGames.API/objetivo1.md`), atendendo todos os requisitos obrigatórios da primeira fase.

## Funcionalidades
- Registro de usuário com ativação por e-mail
- Validação de força de senha e hash seguro PBKDF2
- Autenticação JWT com claims de papel (role)
- Endpoints de administração para gerenciar jogos, promoções e usuários
- Integração com EF Core e SQL Server
- Envio automatizado de e-mails (simulado para arquivos no dev/teste)
- Testes unitários e de integração abrangentes (NUnit)
- Tratamento global de exceções e respostas ProblemDetails
- Documentação Swagger/OpenAPI

## Stack Tecnológico
- ASP.NET Core 8.0 Web API
- Entity Framework Core
- SQL Server (localdb ou completo)
- NUnit (testes unitários/integrados)
- Serilog (recomendado para logs em produção)
- Docker (opcional)

## Primeiros Passos

### Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (localdb ou completo)
- (Opcional) Docker

### Configuração
1. **Clone o repositório:**
   ```sh
   git clone <seu-repo-url>
   cd TheThroneOfGames
   ```
2. **Configure as variáveis de ambiente:**
   - Copie o `appsettings.Development.json` conforme necessário e defina seu segredo JWT e string de conexão do banco.
   - Para produção, use variáveis de ambiente ou um arquivo `.env` (veja abaixo).

3. **Restaure as dependências:**
   ```sh
   dotnet restore
   ```

### Migrações do Banco de Dados
1. **Aplique as migrações:**
   ```sh
   dotnet ef database update --project TheThroneOfGames.Infrastructure --startup-project TheThroneOfGames.API
   ```
   Isso criará o banco de dados e aplicará todas as migrações.

### Executando a Aplicação
1. **Inicie a API:**
   ```sh
   dotnet run --project TheThroneOfGames.API
   ```
   A API estará disponível em `https://localhost:5001` (ou conforme configurado).

2. **Swagger UI:**
   Acesse `https://localhost:5001/swagger` para documentação interativa e testes da API.

### Testes
1. **Execute todos os testes:**
   ```sh
   dotnet test
   ```
   Isso executará todos os testes unitários e de integração, incluindo ativação por e-mail, validação de senha, JWT e cenários de autorização.

### Docker (Opcional)
1. **Build e execução com Docker:**
   ```sh
   docker build -t thethroneofgames .
   docker run -p 5001:5001 thethroneofgames
   ```
   (Garanta que sua string de conexão e segredo JWT estejam definidos via variáveis de ambiente ou segredos Docker.)

### Testando Endpoints com Swagger
1. **Registre um usuário:**
   - No Swagger UI (`https://localhost:5001/swagger`), localize o endpoint `/api/Usuario/pre-register`
   - Use o modelo de exemplo para registrar um usuário com email e senha válidos:
     ```json
     {
       "name": "Usuário Teste",
       "email": "teste@exemplo.com",
       "password": "Senha@123",
       "role": "User"
     }
     ```
   - Verifique a pasta `Infrastructure/Outbox` para o email de ativação
   - Use o token de ativação no endpoint `/api/Usuario/activate`

2. **Obtenha um token JWT:**
   - Use o endpoint `/api/Usuario/login` com o email e senha registrados:
     ```json
     {
       "email": "teste@exemplo.com",
       "password": "Senha@123"
     }
     ```
   - Copie o token JWT retornado na resposta

3. **Use o token no Swagger:**
   - No topo da página do Swagger, clique no botão "Authorize" ou no cadeado
   - No campo "Value", digite: `Bearer seu-token-jwt`
   - Clique em "Authorize" e feche o modal
   - Agora você pode acessar endpoints protegidos

4. **Para endpoints administrativos:**
   - É necessário um usuário com role "Admin"
   - Por padrão, o primeiro usuário pode ser promovido a admin via banco de dados
   - Ou use o endpoint de promoção de usuário (requer um admin existente)

## Configuração
- **Segredo JWT:** Defina em `appsettings.json` ou como variável de ambiente `JWT_SECRET`.
- **Conexão com Banco:** Defina em `appsettings.json` ou como variável de ambiente `DB_CONNECTION`.
- **Outbox de E-mail:** Para desenvolvimento, e-mails são gravados em `Infrastructure/Outbox` como arquivos `.eml`.

## Relatório de Entrega
- **Segurança:** Senhas são validadas quanto à força e armazenadas com hash PBKDF2. Tokens JWT incluem claims de papel e expiração. Endpoints de administração são protegidos por autorização baseada em papel.
- **Testes:** O projeto inclui testes de integração e unitários abrangentes para todos os fluxos críticos, incluindo casos de borda para validação de senha, claims/expiração JWT e endpoints protegidos.
- **Extensibilidade:** A arquitetura permite fácil adição de novas funcionalidades, como papéis mais granulares, novas entidades ou provedores externos de e-mail/SMS.
- **Pronto para Produção:** Para produção, mova segredos para variáveis de ambiente ou um cofre seguro, habilite Serilog para logs e considere health checks e deploy via Docker.

## Estrutura do Projeto
- `TheThroneOfGames.API/` - Projeto principal da Web API
- `TheThroneOfGames.Application/` - Serviços de aplicação e lógica de negócio
- `TheThroneOfGames.Domain/` - Entidades de domínio e interfaces
- `TheThroneOfGames.Infrastructure/` - Acesso a dados, EF Core e serviços externos
- `Test/` - Testes unitários e de integração

## Referências
- Requisitos do desafio: veja `TheThroneOfGames.API/objetivo1.md`
- Relatório de entrega: veja `relatorio_entrega.txt`

## Contribuindo
Pull requests e issues são bem-vindos! Por favor, garanta que todos os testes passem e siga o estilo de código existente.

## Licença
Licença MIT

---

Para dúvidas ou suporte, entre em contato com o mantenedor.
