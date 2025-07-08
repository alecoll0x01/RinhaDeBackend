# Payment Processor API

Este � um sistema de processamento de pagamentos desenvolvido em ASP.NET Core 8.0 com PostgreSQL e Docker, seguindo as especifica��es do desafio.

## Arquitetura

O sistema implementa um padr�o de failover inteligente:

1. **Processador Principal (Default)**: Primeira tentativa de processamento
2. **Processador Fallback**: Usado quando o principal falha
3. **Health Check**: Verifica a sa�de dos processadores antes do uso
4. **Persist�ncia**: Salva apenas pagamentos processados com sucesso

## Funcionalidades

### Endpoints Implementados

#### POST /payments
Processa um pagamento atrav�s dos processadores externos.

**Request:**
```json
{
    "correlationId": "4a7901b8-7d26-4d9d-aa19-4dc1c7cf60b3",
    "amount": 19.90
}
```

**Response:** HTTP 200-299 com qualquer conte�do v�lido

#### GET /payments-summary
Retorna resumo dos pagamentos processados.

**Par�metros:**
- `from`: Data in�cio (ISO 8601 UTC) - opcional
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
- **Docker**: Containeriza��o
- **Swagger**: Documenta��o da API

## Configura��o e Execu��o

### Pr�-requisitos

- Docker e Docker Compose
- .NET 8.0 SDK (para desenvolvimento local)

### Execu��o com Docker

1. Clone o reposit�rio
2. Execute o comando:

```bash
docker-compose up -d
```

Isso iniciar�:
- API na porta 8080
- PostgreSQL na porta 5432
- Processadores de exemplo nas portas 8081 e 8082

### Execu��o Local (Desenvolvimento)

1. Configure o PostgreSQL local
2. Ajuste a connection string no `appsettings.json`
3. Execute as migra��es:

```bash
./Scripts/create-migration.sh
```

4. Execute a aplica��o:

```bash
dotnet run
```

## Configura��o

### Vari�veis de Ambiente

- `ConnectionStrings__DefaultConnection`: String de conex�o do PostgreSQL
- `PaymentProcessors__Default`: URL do processador principal
- `PaymentProcessors__Fallback`: URL do processador fallback

### Configura��o dos Processadores

No `appsettings.json` ou vari�veis de ambiente:

```json
{
  "PaymentProcessors": {
    "Default": "http://default-processor:8080",
    "Fallback": "http://fallback-processor:8080"
  }
}
```

## L�gica de Processamento

1. **Valida��o**: Verifica se o `correlationId` j� existe
2. **Health Check**: Consulta a sa�de do processador padr�o
3. **Processamento Prim�rio**: Tenta processar com o processador padr�o
4. **Failover**: Se falhar, tenta com o processador fallback
5. **Persist�ncia**: Salva no banco apenas se processado com sucesso

## Caracter�sticas T�cnicas

### Resili�ncia

- **Timeout**: 30 segundos para requisi��es HTTP
- **Retry**: Failover autom�tico entre processadores
- **Health Check**: Rate limiting (1 chamada/5 segundos)
- **Cache**: Cache de health check para otimiza��o

### Performance

- **Async/Await**: Opera��es ass�ncronas em toda a stack
- **Connection Pooling**: Pool de conex�es do Entity Framework
- **�ndices**: �ndice �nico no `correlationId`

### Seguran�a

- **Valida��o**: Valida��o de entrada com Data Annotations
- **Logging**: Logs estruturados para auditoria
- **Error Handling**: Tratamento de erros centralizado
