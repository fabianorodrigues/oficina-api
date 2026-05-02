# Oficina API - Tech Challenge FIAP | Fase 3

API REST em .NET 9 para gestao de oficina mecanica, preparada para execucao local antes da evolucao para cloud, API Gateway e componentes serverless.

Este repositorio concentra a aplicacao base da Fase 3: API, regras de negocio, persistencia, autenticacao local unificada, envio local de e-mail e documentacao para validacao em ambiente Docker Compose.

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

- .NET 9
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

## Execucao Local Recomendada

### Pre-requisitos

- Docker Desktop
- .NET SDK 9.0.313, quando for rodar comandos `dotnet` localmente

O SDK esperado esta fixado em `global.json`.

### Subir a stack local

```powershell
Copy-Item docker/.env.example docker/.env
docker compose --env-file docker/.env -f docker/docker-compose.yml up --build
```

Esse comando sobe:

- API;
- SQL Server;
- smtp4dev.

Com `RUN_MIGRATION=true`, as migrations sao aplicadas na inicializacao para facilitar validacao local.

### Acessos

| Recurso | URL |
|---|---|
| Swagger | `http://localhost:8080/swagger` |
| Healthcheck | `http://localhost:8080/health` |
| smtp4dev | `http://localhost:5000` |

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

## Variaveis Principais

| Variavel | Uso |
|---|---|
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

```powershell
dotnet test Oficina.sln
```

Com cobertura:

```powershell
dotnet test Oficina.sln --collect:"XPlat Code Coverage"
```

## Mais Detalhes

O roteiro operacional completo esta em:

```text
docs/local-development.md
```
