# Oficina API - Tech Challenge FIAP | Fase 3

API REST em .NET 10 para gestão de oficina mecânica. O repositório concentra API, regras de negócio, persistência, autenticação local, envio de e-mail em ambiente de desenvolvimento e testes automatizados.

## Visão Geral

- Cadastro e manutenção de clientes e veículos.
- Gestão de serviços, peças, insumos e estoque.
- Abertura, classificação e acompanhamento de ordens de serviço.
- Registro de diagnóstico, geração de orçamento e aprovação/recusa.
- Autenticação JWT por CPF para clientes, funcionários e admins.
- Envio local de e-mails pelo smtp4dev para validar fluxos de orçamento.

## Arquitetura e Tecnologias

| Projeto | Responsabilidade |
|---|---|
| `Oficina.Api` | Controllers, autenticação, autorização, Swagger, healthcheck e middleware de erro |
| `Oficina.Application` | Casos de uso, contratos, validações e modelos de aplicação |
| `Oficina.Domain` | Entidades, value objects, enums e regras de negócio |
| `Oficina.Infrastructure` | EF Core, repositórios, SQL Server e integrações externas |
| `Oficina.Tests` | Testes de domínio, aplicação, API, segurança e infraestrutura |

Principais tecnologias: .NET 10, ASP.NET Core, Entity Framework Core, SQL Server, JWT Bearer, FluentValidation, MailKit, smtp4dev, Swagger/OpenAPI, Docker Compose, xUnit, Moq e Coverlet.

## Pré-requisitos

- Docker Desktop.
- .NET SDK 10 para comandos locais de `dotnet`.

O SDK esperado está fixado em `global.json`, que também é usado pelo workflow de CI.

## Configuração

Crie o arquivo local de variáveis:

```powershell
Copy-Item docker/.env.example docker/.env
```

O Compose monta a connection string da API a partir das variáveis `SQLSERVER_*`.

| Variável | Uso |
|---|---|
| `SQLSERVER_HOST` | Host do SQL Server. Use `sqlserver` para container local ou endpoint do RDS |
| `SQLSERVER_PORT` | Porta do SQL Server, normalmente `1433` |
| `SQLSERVER_DATABASE` | Nome do banco |
| `SQLSERVER_USER` | Usuário do banco |
| `SQLSERVER_PASSWORD` | Senha do banco |
| `SQLSERVER_ENCRYPT` | `False` no local; normalmente `True` para RDS |
| `SQLSERVER_TRUST_SERVER_CERTIFICATE` | `True` para desenvolvimento/local |
| `RUN_MIGRATION` | Quando `true`, aplica migrations ao subir a API |
| `API_HTTP_PORT` | Porta local da API |
| `SMTP4DEV_WEB_PORT` | Porta local da interface web do smtp4dev |
| `JWT_SECRET` | Chave usada para assinar tokens JWT |
| `ADMIN_INICIAL_*` | Dados do admin inicial criado em desenvolvimento |

Não commite credenciais reais no `.env`.

## Como Rodar

### Local Completo

Use este modo para subir API, SQL Server em container e smtp4dev:

```powershell
docker compose --profile local-db --env-file docker/.env -f docker/docker-compose.yml up --build
```

No `docker/.env`, mantenha:

```text
SQLSERVER_HOST=sqlserver
SQLSERVER_USER=sa
RUN_MIGRATION=true
```

### Banco Externo ou RDS

Use este modo para subir API e smtp4dev localmente, apontando a API para um SQL Server externo:

```powershell
docker compose --env-file docker/.env -f docker/docker-compose.yml up --build api smtp4dev
```

Exemplo de variáveis para RDS SQL Server:

```text
SQLSERVER_HOST=meu-rds.xxxxxx.us-east-1.rds.amazonaws.com
SQLSERVER_PORT=1433
SQLSERVER_DATABASE=OficinaDb
SQLSERVER_USER=admin
SQLSERVER_PASSWORD=SUA_SENHA
SQLSERVER_ENCRYPT=True
SQLSERVER_TRUST_SERVER_CERTIFICATE=True
RUN_MIGRATION=false
```

Antes de usar RDS, confirme que o Security Group/firewall permite conexão na porta `1433` a partir da máquina que roda Docker.

### Acessos

| Recurso | URL |
|---|---|
| Swagger | `http://localhost:8080/swagger` |
| Healthcheck | `http://localhost:8080/health` |
| smtp4dev | `http://localhost:5000` |

Valide a API:

```powershell
Invoke-RestMethod http://localhost:8080/health
```

Resposta esperada:

```json
{
  "status": "Healthy"
}
```

## Migrations

A API executa migrations no startup somente quando `RUN_MIGRATION=true`. O EF Core aplica migrations pendentes no sentido `Up`; ele não executa os métodos `Down`.

Para RDS ou banco com dados existentes:

1. Tire snapshot ou backup do banco.
2. Comece com `RUN_MIGRATION=false`.
3. Gere e revise o script idempotente:

```powershell
dotnet ef migrations script --idempotent --project src/Oficina.Infrastructure --startup-project src/Oficina.Api --output artifacts/rds-migration.sql
```

4. Aplique migrations de forma controlada com `RUN_MIGRATION=true`.
5. Depois de validar, volte `RUN_MIGRATION=false`.

Depois da aplicação, confira a tabela `__EFMigrationsHistory`.

## Autenticação

A rota pública de login é:

| Método | Endpoint | Descrição |
|---|---|---|
| POST | `/api/auth/cpf` | Autentica cliente, funcionário ou admin por CPF |

Cliente:

```json
{
  "cpf": "52998224725"
}
```

Admin ou funcionário:

```json
{
  "cpf": "39053344705",
  "senha": "Senha@123"
}
```

Use o token retornado como `Bearer` no Swagger ou Postman. As rotas completas da API ficam disponíveis no Swagger.

## E-mail Local

O smtp4dev captura os e-mails enviados pela API em desenvolvimento. Acesse `http://localhost:5000` para validar mensagens e links de aprovação/recusa de orçamento.

## Postman

Arquivos disponíveis:

```text
postman/OficinaAPI-cenarios.postman_collection.json
postman/OficinaAPI-cenarios.postman_environment.json
postman/OficinaAPI-seguranca.postman_collection.json
```

Importe a collection e o environment, confirme `baseUrl=http://localhost:8080` e execute os cenários pelo Collection Runner.

## Testes e Validação

Restaurar e compilar:

```powershell
dotnet restore Oficina.sln
dotnet build Oficina.sln --configuration Release --no-restore
```

Executar testes:

```powershell
dotnet test Oficina.sln --configuration Release --no-build
```

Validar configuração Docker local:

```powershell
docker compose --profile local-db --env-file docker/.env -f docker/docker-compose.yml config
```

Validar configuração Docker para banco externo/RDS:

```powershell
docker compose --env-file docker/.env -f docker/docker-compose.yml config
```

Validar build da imagem:

```powershell
docker compose --env-file docker/.env -f docker/docker-compose.yml build api
```
