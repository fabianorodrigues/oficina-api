# Oficina API - Tech Challenge FIAP | Fase 3

API REST em .NET 10 para gestao de oficina mecanica, preparada para execucao local antes da evolucao para cloud, API Gateway e componentes serverless.

Este repositorio concentra a aplicacao base da Fase 3: API, regras de negocio, persistencia, autenticacao local unificada, envio local de e-mail e validacao em ambiente Docker Compose.

## Visao Geral

A aplicacao permite:

- cadastro e manutencao de clientes e veiculos;
- gestao de servicos, pecas, insumos e estoque;
- abertura e acompanhamento de ordens de servico;
- classificacao da OS como preventiva ou corretiva;
- registro de diagnostico;
- geracao e aprovacao/recusa de orcamentos;
- autenticacao JWT por CPF;
- consulta de recursos pelo cliente autenticado;
- aprovacao/recusa externa de orcamento por link enviado por e-mail;
- validacao local por Swagger, Postman e smtp4dev.

## Arquitetura

O projeto segue uma organizacao inspirada em Clean Architecture, DDD e Use Cases.

| Projeto | Responsabilidade |
|---|---|
| `Oficina.Api` | Controllers, autenticacao, autorizacao, Swagger, healthcheck e middleware de erro |
| `Oficina.Application` | Casos de uso, contratos, validacoes e modelos de aplicacao |
| `Oficina.Domain` | Entidades, value objects, enums e regras de negocio |
| `Oficina.Infrastructure` | EF Core, repositorios, SQL Server e integracoes externas |
| `Oficina.Tests` | Testes de dominio, aplicacao, API, seguranca e infraestrutura |

## Tecnologias

- .NET 10
- ASP.NET Core
- Entity Framework Core
- SQL Server
- JWT Bearer
- FluentValidation
- MailKit
- smtp4dev
- Swagger/OpenAPI
- Docker e Docker Compose
- xUnit, Moq e Coverlet

## Pre-requisitos

- Docker Desktop em execucao para rodar a stack local completa.
- .NET SDK 10 para comandos `dotnet` locais.

O SDK esperado esta fixado em `global.json`. Esse arquivo define qual SDK da CLI do .NET sera usado por comandos como `dotnet restore`, `dotnet build` e `dotnet test`, sem substituir o `TargetFramework` dos projetos. Ele tambem e usado pelo workflow de CI via `actions/setup-dotnet`.

## Execucao Local com Docker Compose

Crie o arquivo local de variaveis:

```powershell
Copy-Item docker/.env.example docker/.env
```

Revise `docker/.env` se precisar mudar portas, senha do SQL Server, JWT, admin inicial ou connection string do banco.

### Modo Local Completo

Use este modo para subir API, SQL Server em container e smtp4dev:

```powershell
docker compose --profile local-db --env-file docker/.env -f docker/docker-compose.yml up --build
```

Esse comando sobe:

- API;
- SQL Server local em container;
- smtp4dev.

Com `RUN_MIGRATION=true`, as migrations sao aplicadas na inicializacao para facilitar validacao local.

### Modo Banco Externo ou RDS

Use este modo para subir apenas API e smtp4dev, conectando a API em um SQL Server externo, como Amazon RDS for SQL Server.

No `docker/.env`, preencha `SQLSERVER_CONNECTION_STRING`:

```text
SQLSERVER_CONNECTION_STRING=Server=meu-rds.xxxxxx.us-east-1.rds.amazonaws.com,1433;Database=OficinaDb;User Id=admin;Password=SUA_SENHA;Encrypt=True;TrustServerCertificate=True;
RUN_MIGRATION=false
```

Suba apenas API e smtp4dev:

```powershell
docker compose --env-file docker/.env -f docker/docker-compose.yml up --build api smtp4dev
```

Para usar RDS, confirme antes:

- o RDS e SQL Server compativel;
- a porta `1433` esta liberada no Security Group/firewall para a maquina que roda Docker;
- o usuario da connection string tem permissao para ler, escrever e aplicar migrations quando `RUN_MIGRATION=true`;
- credenciais reais nao foram commitadas no repositorio.

### Acessos

| Recurso | URL |
|---|---|
| Swagger | `http://localhost:8080/swagger` |
| Healthcheck | `http://localhost:8080/health` |
| smtp4dev | `http://localhost:5000` |

### Validar healthcheck

```powershell
Invoke-RestMethod http://localhost:8080/health
```

Resposta esperada:

```json
{
  "status": "Healthy"
}
```

## Variaveis Principais

| Variavel | Uso |
|---|---|
| `SQLSERVER_CONNECTION_STRING` | Connection string completa para banco externo/RDS. Quando vazia, o Compose usa o SQL Server local em `sqlserver,1433` |
| `MSSQL_DATABASE` | Nome do banco usado pelo SQL Server local em container |
| `MSSQL_SA_PASSWORD` | Senha do usuario `sa` do SQL Server local em container |
| `SQLSERVER_PORT` | Porta local publicada pelo SQL Server em container |
| `API_HTTP_PORT` | Porta local publicada pela API |
| `SMTP4DEV_WEB_PORT` | Porta local da interface web do smtp4dev |
| `SMTP4DEV_SMTP_PORT` | Porta SMTP local do smtp4dev |
| `ConnectionStrings__SqlServer` | Conexao com SQL Server |
| `Jwt__Secret` | Chave de assinatura JWT |
| `Jwt__Issuer` | Emissor do token |
| `Jwt__Audience` | Audiencia do token |
| `Jwt__ExpirationMinutes` | Tempo de expiracao do token |
| `RUN_MIGRATION` | Executa migrations na inicializacao local |
| `AdminInicial__Nome` | Nome do admin inicial |
| `AdminInicial__Cpf` | CPF do admin inicial |
| `AdminInicial__Senha` | Senha do admin inicial |
| `EmailSettings__SmtpHost` | Host SMTP local |
| `EmailSettings__BaseUrlAprovaRecusaOrcamento` | Base URL dos links externos de orcamento |

Valores locais sao apenas para desenvolvimento. Em ambientes reais, use variaveis de ambiente, secrets ou ferramentas equivalentes.

## Migrations e Banco Externo

A API executa migrations no startup somente quando `RUN_MIGRATION=true`. O EF Core aplica apenas migrations pendentes no sentido `Up`; ele nao executa os metodos `Down`.

Para banco externo ou RDS com dados existentes, use um fluxo controlado:

1. Tire snapshot ou backup do banco.
2. Gere um script idempotente:

```powershell
dotnet ef migrations script --idempotent --project src/Oficina.Infrastructure --startup-project src/Oficina.Api --output artifacts/rds-migration.sql
```

3. Revise o script antes de aplicar.
4. Ligue `RUN_MIGRATION=true` apenas para aplicar a migration de forma controlada.
5. Depois de validar a aplicacao, volte `RUN_MIGRATION=false`.

Depois da subida com migration, valide no banco a tabela `__EFMigrationsHistory`.

## Autenticacao Local Unificada

A API expoe uma unica rota publica de login:

| Metodo | Endpoint | Descricao |
|---|---|---|
| POST | `/api/auth/cpf` | Autentica cliente, funcionario ou admin por CPF |

Clientes autenticam apenas com CPF:

```json
{
  "cpf": "52998224725"
}
```

Funcionarios e admins autenticam com CPF e senha:

```json
{
  "cpf": "39053344705",
  "senha": "Senha@123"
}
```

Resposta:

```json
{
  "accessToken": "...",
  "expiresIn": 7200,
  "perfil": "Admin",
  "clienteId": null,
  "funcionarioId": "00000000-0000-0000-0000-000000000000"
}
```

Essa rota foi desenhada para simplificar a integracao futura com API Gateway/Lambda, mantendo a autenticacao local funcional sem acoplar a aplicacao a AWS nesta etapa.

## E-mail Local com smtp4dev

O smtp4dev captura e-mails enviados pela API durante o fluxo de orcamento.

Fluxo esperado:

1. A API gera um orcamento.
2. O sistema envia um e-mail ao cliente.
3. O e-mail aparece em `http://localhost:5000`.
4. O cliente usa os links de aprovar ou recusar.
5. A API processa a acao externa por token.

Links externos gerados:

```text
/api/orcamentos/acoes-externas/aprovar?token=...
/api/orcamentos/acoes-externas/recusar?token=...
```

Falhas de SMTP sao registradas em log, mas nao derrubam a acao principal ja persistida.

## Rotas Principais

### Auth

| Metodo | Endpoint | Perfil | Descricao |
|---|---|---|---|
| POST | `/api/auth/cpf` | Publico | Login unificado por CPF |

### Cliente autenticado

| Metodo | Endpoint | Perfil | Descricao |
|---|---|---|---|
| GET | `/api/minhas-ordens-servico` | Cliente | Lista OS do cliente autenticado |
| GET | `/api/minhas-ordens-servico/{id}` | Cliente | Detalha OS propria |
| GET | `/api/minhas-ordens-servico/{id}/status` | Cliente | Consulta status da OS propria |
| GET | `/api/meus-orcamentos/{id}` | Cliente | Consulta orcamento proprio |
| POST | `/api/meus-orcamentos/{id}/aprovar` | Cliente | Aprova orcamento proprio |
| POST | `/api/meus-orcamentos/{id}/recusar` | Cliente | Recusa orcamento proprio |

### Operacao interna

| Metodo | Endpoint | Perfil | Descricao |
|---|---|---|---|
| GET/POST/PUT | `/api/clientes` | Funcionario/Admin | Cadastro de clientes |
| GET/POST/PUT | `/api/veiculos` | Funcionario/Admin | Cadastro de veiculos |
| GET/POST/PUT | `/api/servicos` | Funcionario/Admin | Catalogo de servicos |
| GET/POST/PUT | `/api/pecas` | Funcionario/Admin | Catalogo de pecas |
| GET/POST/PUT | `/api/insumos` | Funcionario/Admin | Catalogo de insumos |
| GET/POST | `/api/estoque` | Funcionario/Admin | Consulta e ajuste de estoque |
| GET/POST | `/api/ordens-servico` | Funcionario/Admin | Fluxo de OS |
| GET/POST | `/api/orcamentos` | Funcionario/Admin | Fluxo de orcamento |
| GET | `/api/relatorios/tempo-medio-execucao` | Funcionario/Admin | Relatorio operacional |

### Admin

| Metodo | Endpoint | Perfil | Descricao |
|---|---|---|---|
| GET | `/api/admin/funcionarios` | Admin | Lista funcionarios/admins |
| POST | `/api/admin/funcionarios` | Admin | Cria funcionario/admin |
| PUT/PATCH | `/api/admin/funcionarios/{id}` | Admin | Mantem usuario interno |

### Publico

| Metodo | Endpoint | Perfil | Descricao |
|---|---|---|---|
| GET | `/api/orcamentos/acoes-externas/aprovar?token=...` | Publico | Aprova orcamento por token |
| GET | `/api/orcamentos/acoes-externas/recusar?token=...` | Publico | Recusa orcamento por token |
| GET | `/health` | Publico | Status da API |

## Postman

Arquivos:

```text
postman/OficinaAPI-cenarios.postman_collection.json
postman/OficinaAPI-cenarios.postman_environment.json
postman/OficinaAPI-seguranca.postman_collection.json
```

Importe a collection e o environment, confirme `baseUrl=http://localhost:8080` e execute os cenarios pelo Collection Runner.

## Testes

Executar testes:

```powershell
dotnet test Oficina.sln --configuration Release --no-build
```

Executar testes com cobertura:

```powershell
dotnet test Oficina.sln --collect:"XPlat Code Coverage"
```

Validar a configuracao Docker com SQL Server local:

```powershell
docker compose --profile local-db --env-file docker/.env -f docker/docker-compose.yml config
```

Validar a configuracao Docker com banco externo/RDS:

```powershell
docker compose --env-file docker/.env -f docker/docker-compose.yml config
```

Validar o build da imagem da API:

```powershell
docker compose --env-file docker/.env -f docker/docker-compose.yml build api
```
