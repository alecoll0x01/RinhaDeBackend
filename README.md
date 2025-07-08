# Payment Processor API

Este é um sistema de processamento de pagamentos desenvolvido em ASP.NET Core 8.0 com PostgreSQL e Docker, seguindo as especificações do desafio.

## Arquitetura

O sistema implementa um padrão de failover inteligente:

1. **Processador Principal (Default)**: Primeira tentativa de processamento
2. **Processador Fallback**: Usado quando o principal falha
3. **Health Check**: Verifica a saúde dos processadores antes do uso
4. **Persistência**: Salva apenas pagamentos processados com sucesso

## Funcionalidades

### Endpoints Implementados

#### POST /payments
Processa um pagamento através dos processadores externos.

**Request:**
```json
{
    "correlationId": "4a7901b8-7d26-4d9d-aa19-4dc1c7cf60b3",
    "amount": 19.90
}
```

**Response:** HTTP 200-299 com qualquer conteúdo válido

#### GET /payments-summary
Retorna resumo dos pagamentos processados.

**Parâmetros:**
- `from`: Data início (ISO 8601 UTC) - opcional
- `to`: Data fim (ISO 8601 UTC) - opcional

**Response:**
```json
{
    "default": {
        "totalRequests": 43236,
        "totalAmount": 415542345.98
    },
    "fallback": {
        "totalRequests": 423545,
        "totalAmount": 329347.34
    }
}
```

## Tecnologias Utilizadas

- **ASP.NET Core 8.0**: Framework web
- **Entity Framework Core**: ORM para banco de dados
- **PostgreSQL**: Banco de dados relacional
- **Docker**: Containerização
- **Swagger**: Documentação da API

## Configuração e Execução

### Pré-requisitos

- Docker e Docker Compose
- .NET 8.0 SDK (para desenvolvimento local)

### Execução com Docker

1. Clone o repositório
2. Execute o comando:

```bash
docker-compose up -d
```

Isso iniciará:
- API na porta 8080
- PostgreSQL na porta 5432
- Processadores de exemplo nas portas 8081 e 8082

### Execução Local (Desenvolvimento)

1. Configure o PostgreSQL local
2. Ajuste a connection string no `appsettings.json`
3. Execute as migrações:

```bash
./Scripts/create-migration.sh
```

4. Execute a aplicação:

```bash
dotnet run
```

## Configuração

### Variáveis de Ambiente

- `ConnectionStrings__DefaultConnection`: String de conexão do PostgreSQL
- `PaymentProcessors__Default`: URL do processador principal
- `PaymentProcessors__Fallback`: URL do processador fallback

### Configuração dos Processadores

No `appsettings.json` ou variáveis de ambiente:

```json
{
  "PaymentProcessors": {
    "Default": "http://default-processor:8080",
    "Fallback": "http://fallback-processor:8080"
  }
}
```

## Lógica de Processamento

1. **Validação**: Verifica se o `correlationId` já existe
2. **Health Check**: Consulta a saúde do processador padrão
3. **Processamento Primário**: Tenta processar com o processador padrão
4. **Failover**: Se falhar, tenta com o processador fallback
5. **Persistência**: Salva no banco apenas se processado com sucesso

## Características Técnicas

### Resiliência

- **Timeout**: 30 segundos para requisições HTTP
- **Retry**: Failover automático entre processadores
- **Health Check**: Rate limiting (1 chamada/5 segundos)
- **Cache**: Cache de health check para otimização

### Performance

- **Async/Await**: Operações assíncronas em toda a stack
- **Connection Pooling**: Pool de conexões do Entity Framework
- **Índices**: Índice único no `correlationId`

### Segurança

- **Validação**: Validação de entrada com Data Annotations
- **Logging**: Logs estruturados para auditoria
- **Error Handling**: Tratamento de erros centralizado
