# 📡 Fluxo de Comunicação Assíncrona - The Throne of Games

**Versão:** 1.0  
**Data:** 7 de Janeiro de 2026  
**Status:** ✅ Implementado

---

## 🎯 Visão Geral

O sistema utiliza **RabbitMQ** como broker de mensagens para garantir comunicação assíncrona entre microsserviços, permitindo escalabilidade horizontal e resiliência.

```
┌─────────────────────────────────────────────────────────────────┐
│                   FLUXO DE COMUNICAÇÃO ASSÍNCRONA               │
└─────────────────────────────────────────────────────────────────┘

┌──────────────────┐
│ USUARIOS API     │
│                  │
│ UserRegistered   │ ──────┐
│ UserActivated    │       │
│ LoginSuccessful  │       │
└──────────────────┘       │
                           ├──────────> ┌─────────────────┐
                           │            │    RABBITMQ     │
┌──────────────────┐       │            │                 │
│ CATALOGO API     │       │            │ Exchanges:      │
│                  │───────┤            │ ├─ user.events  │
│ GamePurchased    │       │            │ ├─ order.events │
│ GameListed       │       │            │ ├─ payment.    │
└──────────────────┘       │            │ └─ notify.      │
                           │            │                 │
┌──────────────────┐       │            │ Queues:         │
│ VENDAS API       │       │            │ ├─ user.*       │
│                  │───────┤            │ ├─ order.*      │
│ OrderCreated     │       │            │ ├─ payment.*    │
│ OrderFinalized   │───────┘            │ └─ dlq.*        │
└──────────────────┘                    └─────────────────┘
```

---

## 📊 Eventos Implementados

### 1. Usuários API - Eventos Publicados

#### 🔴 `user.registered`
```
Disparado: Quando usuário faz pré-registro
Exchange: user.events
Rota: user.registered

Payload:
{
  "userId": "uuid",
  "email": "user@example.com",
  "name": "User Name",
  "role": "User",
  "timestamp": "2026-01-07T10:30:00Z"
}

Consumidores:
- Notification Service (enviar email)
- Analytics Service (tracking)
```

#### 🟢 `user.activated`
```
Disparado: Quando usuário ativa conta
Exchange: user.events
Rota: user.activated

Payload:
{
  "userId": "uuid",
  "email": "user@example.com",
  "activatedAt": "2026-01-07T10:35:00Z"
}

Consumidores:
- Email Service (confirmação)
- CRM System
```

#### 🔵 `user.login`
```
Disparado: Quando usuário faz login bem-sucedido
Exchange: user.events
Rota: user.login

Payload:
{
  "userId": "uuid",
  "email": "user@example.com",
  "loginTime": "2026-01-07T10:40:00Z",
  "ipAddress": "192.168.1.1"
}

Consumidores:
- Security Service (detecção de anomalias)
- Analytics Service
```

---

### 2. Catálogo API - Eventos Publicados

#### 🟡 `game.purchased`
```
Disparado: Quando jogo é comprado através de outro serviço
Exchange: order.events
Rota: catalog.game.purchased

Payload:
{
  "gameId": "uuid",
  "userId": "uuid",
  "purchasePrice": 99.90,
  "purchaseDate": "2026-01-07T11:00:00Z"
}

Consumidores:
- Inventory Service
- Analytics Service
- Recommendation Engine
```

#### 🟠 `game.stock.low`
```
Disparado: Quando estoque de jogo fica baixo
Exchange: catalog.events
Rota: catalog.stock.low

Payload:
{
  "gameId": "uuid",
  "gameName": "Game Name",
  "currentStock": 5,
  "minStockThreshold": 10,
  "alertTime": "2026-01-07T11:05:00Z"
}

Consumidores:
- Supplier Service (repor estoque)
- Admin Notification
```

---

### 3. Vendas API - Eventos Publicados

#### 💳 `order.created`
```
Disparado: Quando novo pedido é criado
Exchange: order.events
Rota: order.created

Payload:
{
  "orderId": "uuid",
  "userId": "uuid",
  "items": [
    {
      "gameId": "uuid",
      "quantity": 1,
      "unitPrice": 99.90
    }
  ],
  "totalAmount": 99.90,
  "createdAt": "2026-01-07T11:10:00Z",
  "status": "pending"
}

Consumidores:
- Payment Service (processar pagamento)
- Email Service (confirmação)
- Analytics Service
```

#### ✅ `order.completed`
```
Disparado: Quando pedido é finalizado com sucesso
Exchange: order.events
Rota: order.completed

Payload:
{
  "orderId": "uuid",
  "userId": "uuid",
  "totalAmount": 99.90,
  "completedAt": "2026-01-07T11:15:00Z",
  "items": [...],
  "paymentId": "uuid"
}

Consumidores:
- Email Service (nota fiscal)
- Game Delivery Service
- CRM System
```

#### ❌ `order.failed`
```
Disparado: Quando pedido falha
Exchange: order.events
Rota: order.failed

Payload:
{
  "orderId": "uuid",
  "userId": "uuid",
  "failureReason": "payment_declined",
  "failedAt": "2026-01-07T11:20:00Z"
}

Consumidores:
- Email Service (notificação de falha)
- Support Service (escalação)
```

#### 🔄 `payment.processed`
```
Disparado: Quando pagamento é processado
Exchange: payment.events
Rota: payment.processed

Payload:
{
  "paymentId": "uuid",
  "orderId": "uuid",
  "userId": "uuid",
  "amount": 99.90,
  "status": "success",
  "processedAt": "2026-01-07T11:25:00Z"
}

Consumidores:
- Order Service (confirmar entrega)
- Accounting System
```

---

## 🏗️ Arquitetura de Filas

### Exchange Types

```yaml
# User Events Exchange
Name: user.events
Type: topic
Durable: true
Auto-delete: false

# Order Events Exchange
Name: order.events
Type: topic
Durable: true
Auto-delete: false

# Notification Exchange
Name: notification.events
Type: topic
Durable: true
Auto-delete: false

# Payment Exchange
Name: payment.events
Type: topic
Durable: true
Auto-delete: false
```

### Queues Configuration

```yaml
# Usuarios API Queues
user.registered.queue:
  Exchange: user.events
  Routing Key: user.registered
  TTL: 1 day
  Max Length: 10000

user.activated.queue:
  Exchange: user.events
  Routing Key: user.activated
  TTL: 1 day
  Max Length: 10000

# Vendas API Queues
order.created.queue:
  Exchange: order.events
  Routing Key: order.created
  TTL: 1 day
  Max Length: 10000

order.completed.queue:
  Exchange: order.events
  Routing Key: order.completed
  TTL: 1 day
  Max Length: 10000

# Dead Letter Queues
user.registered.dlq:
  TTL: 7 days
  Max Length: 1000

order.created.dlq:
  TTL: 7 days
  Max Length: 1000
```

---

## 🔄 Fluxos de Mensagem

### Fluxo 1: Novo Usuário Registrado

```
┌────────────────────────────────────────────────────────┐
│ USUARIO FAZE REGISTRO                                  │
└────────────────────────────────────────────────────────┘
                    ↓
┌────────────────────────────────────────────────────────┐
│ Usuarios API - POST /api/Usuario/pre-register         │
│ - Valida dados                                          │
│ - Cria usuário no banco                                │
│ - Publica evento "user.registered"                     │
└────────────────────────────────────────────────────────┘
                    ↓
┌────────────────────────────────────────────────────────┐
│ RabbitMQ - Exchange: user.events                       │
│ - Rota: user.registered                                │
│ - TTL: 1 dia                                           │
│ - DLQ: user.registered.dlq                             │
└────────────────────────────────────────────────────────┘
         ↙          ↓          ↘
        /            |            \
    [FILA 1]     [FILA 2]      [FILA 3]
    /                |               \
   ↙                 ↓                ↘
Email Service    Analytics      CRM System
(enviar email)   (log event)   (atualizar)
   |                 |               |
   └─────────────────┼───────────────┘
                     ↓
           ✅ Processo Assíncrono
              Concluído
```

---

### Fluxo 2: Criação de Pedido e Pagamento

```
┌────────────────────────────────────────────────────────┐
│ USUARIO CRIA PEDIDO                                    │
└────────────────────────────────────────────────────────┘
                    ↓
┌────────────────────────────────────────────────────────┐
│ Vendas API - POST /api/Pedido                          │
│ - Valida itens                                         │
│ - Cria pedido no banco                                 │
│ - Publica evento "order.created"                       │
└────────────────────────────────────────────────────────┘
                    ↓
┌────────────────────────────────────────────────────────┐
│ RabbitMQ - Exchange: order.events                      │
│ - Rota: order.created                                  │
└────────────────────────────────────────────────────────┘
         ↙          ↓          ↘
        /            |            \
 [Payment Q]   [Email Q]    [Analytics Q]
    /                |            \
   ↙                 ↓             ↘
Payment     Email Service      Tracking
Service    (confirmação)         Service
   |                 |             |
   ↓                 ↓             ↓
Processa       Envia email      Log evento
Pagamento      ao usuário
   |
   ├─→ Sucesso?
   │     ↓ SIM
   │   Publica "payment.processed"
   │     ↓
   │   Vendas API recebe evento
   │   Atualiza pedido: confirmed
   │   Publica "order.completed"
   │
   └─→ Falha?
         ↓ NÃO
      Publica "payment.failed"
      com retry automático
      (exponential backoff)
         ↓
      Tentativa 1: 5s depois
      Tentativa 2: 25s depois
      Tentativa 3: 125s depois
         ↓
      Max Retries?
      Movido para DLQ
      Alerta ao Admin
```

---

## 🔄 Mecanismo de Retry

### Exponential Backoff

```yaml
Retry Policy:
  Enabled: true
  Max Attempts: 3
  Initial Delay: 5 seconds
  Max Delay: 300 seconds
  Backoff Multiplier: 5

Tentativa 1:
  - Falha
  - Aguarda 5 segundos
  - Retry automático

Tentativa 2:
  - Falha
  - Aguarda 25 segundos (5 * 5)
  - Retry automático

Tentativa 3:
  - Falha
  - Aguarda 125 segundos (25 * 5)
  - Retry automático

Falha Final:
  - Movido para Dead Letter Queue
  - TTL: 7 dias
  - Alerta ao administrador
  - Email de escalação
```

### Dead Letter Queue (DLQ)

```
┌─────────────────────────────────────────┐
│ Mensagem com Falha Permanente           │
└─────────────────────────────────────────┘
                ↓
         (3 retries falharam)
                ↓
┌─────────────────────────────────────────┐
│ Movida para DLQ                         │
│ ├─ user.registered.dlq                  │
│ ├─ order.created.dlq                    │
│ └─ payment.processed.dlq                │
└─────────────────────────────────────────┘
                ↓
         (7 dias de TTL)
                ↓
┌─────────────────────────────────────────┐
│ Admin Dashboard                         │
│ - Visualiza mensagens com falha         │
│ - Retry manual                          │
│ - Análise de erro                       │
│ - Limpeza automática após TTL           │
└─────────────────────────────────────────┘
```

---

## 📊 Monitoramento de Eventos

### Métricas Disponíveis

```
# Publicação de Eventos
rabbitmq_published_messages_total
rabbitmq_published_bytes_total
rabbitmq_message_publish_latency_ms

# Consumo de Eventos
rabbitmq_consumed_messages_total
rabbitmq_consumed_bytes_total
rabbitmq_message_consume_latency_ms

# Fila
rabbitmq_queue_messages_count
rabbitmq_queue_messages_bytes
rabbitmq_queue_consumer_count

# Erros e Retries
rabbitmq_message_retry_count
rabbitmq_message_dlq_count
rabbitmq_message_error_rate
```

### Dashboard Grafana

```
Nome: RabbitMQ Event Monitoring
Painéis:
├─ Event Publication Rate
│  └─ Eventos publicados por segundo
│
├─ Event Consumption Rate
│  └─ Eventos consumidos por segundo
│
├─ Queue Depth
│  └─ Mensagens aguardando
│
├─ Message Latency
│  └─ P50, P95, P99
│
├─ Error Rate
│  └─ Mensagens com falha
│
└─ DLQ Monitor
   └─ Mensagens na DLQ
```

---

## 🚀 Exemplo de Implementação

### 1. Publicar Evento (Usuarios API)

```csharp
// UserService.cs
public class UserService
{
    private readonly IEventBus _eventBus;

    public async Task<User> RegisterUserAsync(UserRegistrationRequest request)
    {
        // Criar usuário
        var user = new User 
        { 
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email
        };
        
        await _userRepository.AddAsync(user);

        // Publicar evento
        var @event = new UserRegisteredEvent
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            Timestamp = DateTime.UtcNow
        };

        await _eventBus.PublishAsync(@event);

        return user;
    }
}
```

### 2. Consumir Evento (Email Service)

```csharp
// UserRegisteredEventHandler.cs
public class UserRegisteredEventHandler 
    : IEventHandler<UserRegisteredEvent>
{
    private readonly IEmailService _emailService;

    public async Task HandleAsync(UserRegisteredEvent @event)
    {
        try
        {
            var email = new EmailMessage
            {
                To = @event.Email,
                Subject = "Bem-vindo ao The Throne of Games",
                Body = $"Olá {@event.Name}..."
            };

            await _emailService.SendAsync(email);
        }
        catch (Exception ex)
        {
            // Log error - RabbitMQ vai fazer retry automático
            _logger.LogError(ex, "Failed to send welcome email");
            throw; // Fazer retry
        }
    }
}
```

### 3. Configurar Consumer (Startup)

```csharp
// Program.cs
services
    .AddEventBus()
    .Subscribe<UserRegisteredEvent, UserRegisteredEventHandler>()
    .Subscribe<OrderCreatedEvent, OrderNotificationHandler>()
    .Subscribe<PaymentProcessedEvent, PaymentNotificationHandler>();
```

---

## ✅ Benefícios Desta Arquitetura

| Benefício | Descrição |
|-----------|-----------|
| **Escalabilidade** | Serviços podem escalar independentemente |
| **Resiliência** | Retry automático + DLQ garante entrega |
| **Desacoplamento** | Serviços não precisam conhecer um ao outro |
| **Performance** | Operações assíncronas não bloqueiam usuário |
| **Confiabilidade** | Garantia de entrega via DLQ |
| **Observabilidade** | Métricas e logs detalhados |

---

## 🔧 Troubleshooting

### Problema: Mensagens não sendo entregues

```bash
# Verificar conexão RabbitMQ
docker logs thethroneofgames-rabbitmq | grep "connection"

# Verificar fila
rabbitmqctl list_queues name messages consumers

# Conectar ao RabbitMQ
http://localhost:15672 (guest/guest)
```

### Problema: Muitas mensagens na DLQ

```bash
# Verificar DLQ
rabbitmqctl list_queues name messages | grep dlq

# Ver causas de erro nos logs
kubectl logs -n thethroneofgames deployment/usuarios-api
```

### Problema: Memory leak em consumers

```bash
# Limpar conexões inativas
rabbitmqctl reset

# Reiniciar RabbitMQ
kubectl delete pod -n thethroneofgames pod/rabbitmq-0
```

---

**Versão:** 1.0  
**Última Atualização:** 7 de Janeiro de 2026  
**Status:** ✅ Implementado e Testado
