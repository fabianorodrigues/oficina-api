# Oficina API - Tech Challenge FIAP | Fase 3

![Coverage](.github/badges/badge_combined.svg)

## Visão geral

API REST em .NET 10 para gestão de oficina mecânica. A aplicação é responsável por clientes, veículos, serviços, peças, estoque, ordens de serviço, diagnóstico, orçamento, aprovação/recusa e autenticação JWT.

Nesta fase, a API gera uma imagem Docker para publicação no Amazon ECR. As migrations são executadas de forma dedicada com `APP_MODE=migration`.

## Tecnologias

- .NET 10
- ASP.NET Core
- Entity Framework Core
- SQL Server
- JWT Bearer
- FluentValidation
- MailKit
- Swagger/OpenAPI
- Docker Compose
- xUnit, Moq e Coverlet

## Relação com os outros repositórios

| Repositório | Responsabilidade |
|---|---|
| `oficina-api` | API, domínio, EF Core, migrations, Docker e testes |
| `oficina-infra-db` | RDS SQL Server, VPC, subnets e security groups |
| `oficina-auth-lambda` | Lambda Auth por CPF e Lambda Authorizer JWT |
| `oficina-infra-k8s` | ECR, EKS, Kubernetes e API Gateway |

## Pré-requisitos

- Docker Desktop.
- .NET SDK 10.
- AWS CLI, somente para publicação e validação de imagens no ECR.

O SDK esperado está definido em `global.json`.

## Publicação da imagem no ECR

O ECR é criado no repositório `oficina-infra-k8s`. O output `ecr_repository_url` deve ser cadastrado como secret `ECR_REPOSITORY_URL` neste repositório.

O workflow `docker-build-push` apenas builda e publica a imagem da API. Ele não executa Docker Compose, API, SQL Server, smtp4dev ou migrations.

Secrets obrigatórios no GitHub:

| Secret | Descrição |
|---|---|
| `AWS_ACCESS_KEY_ID` | Access Key da AWS |
| `AWS_SECRET_ACCESS_KEY` | Secret Key da AWS |
| `AWS_SESSION_TOKEN` | Token temporário da AWS Academy |
| `AWS_REGION` | Região AWS, exemplo `us-east-1` |
| `ECR_REPOSITORY_URL` | URL completa do ECR criada pelo `oficina-infra-k8s` |

Como executar:

```text
GitHub Actions > docker-build-push > Run workflow
```

Tags publicadas:

- `<commit-sha>`;
- `demo-latest`.

## Validação da publicação no ECR

Validação pelo console AWS:

```text
ECR > Private repositories > oficina-api > Images
```

Validação opcional pela AWS CLI:

```powershell
aws ecr describe-images --repository-name oficina-api --image-ids imageTag=demo-latest --region us-east-1
```

## Testar imagem publicada localmente

Faça login no ECR:

```powershell
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <account-id>.dkr.ecr.us-east-1.amazonaws.com
```

No `docker/.env`, configure a imagem publicada:

```text
API_IMAGE_REPOSITORY=<ECR_REPOSITORY_URL>
API_IMAGE_TAG=demo-latest
```

Suba o banco e o smtp4dev:

```powershell
docker compose --profile local-db --env-file docker/.env -f docker/docker-compose.yml up -d sqlserver smtp4dev
```

Puxe a imagem publicada:

```powershell
docker compose --env-file docker/.env -f docker/docker-compose.yml pull api
```

Execute as migrations:

```powershell
docker compose --profile local-db --env-file docker/.env -f docker/docker-compose.yml run --rm --pull never migration
```

Suba a API usando a imagem publicada:

```powershell
docker compose --profile local-db --env-file docker/.env -f docker/docker-compose.yml up -d --no-build api
```

Valide:

```powershell
Invoke-RestMethod http://localhost:8080/health
```

## Autenticação e Postman

A rota pública de login é `POST /api/auth/cpf`.

Payload para cliente:

```json
{
  "cpf": "52998224725"
}
```

Payload para funcionário ou admin:

```json
{
  "cpf": "39053344705",
  "senha": "Senha@123"
}
```

Use o token retornado como `Bearer` no Swagger ou no Postman.

Collections disponíveis:

```text
postman/OficinaAPI-cenarios.postman_collection.json
postman/OficinaAPI-cenarios.postman_environment.json
postman/OficinaAPI-seguranca.postman_collection.json
```

Importe a collection e o environment, confirme `baseUrl=http://localhost:8080` e execute os cenários pelo Collection Runner.

## E-mail

Localmente, a API usa smtp4dev. Em cloud, um SMTP real pode ser configurado por variáveis de ambiente, ConfigMap ou Secret.

SMTP não é obrigatório neste estágio. Se o envio falhar, a falha é logada e a operação principal continua. `EmailSettings__BaseUrlAprovaRecusaOrcamento` deve apontar para a URL pública da API quando estiver em cloud.

Exemplo mínimo:

```text
EmailSettings__SmtpHost=<smtp-host>
EmailSettings__SmtpPort=<smtp-port>
EmailSettings__From=<remetente>
EmailSettings__BaseUrlAprovaRecusaOrcamento=<url-publica>
```

## Testes

Restaurar dependências:

```powershell
dotnet restore Oficina.sln
```

Compilar:

```powershell
dotnet build Oficina.sln --configuration Release --no-restore
```

Executar testes:

```powershell
dotnet test Oficina.sln --configuration Release --no-build
```

Executar testes com cobertura:

```powershell
dotnet test Oficina.sln --collect:"XPlat Code Coverage"
```

##
## Configurações e Execuções Local da API

Crie o arquivo local de variáveis:

```powershell
Copy-Item docker/.env.example docker/.env
```

O arquivo `docker/.env` configura SQL Server, API, JWT, e-mail local e admin inicial. Para execução local padrão, os valores de `docker/.env.example` já servem como base.

Grupos principais de configuração:

- `SQLSERVER_*`: conexão com SQL Server local ou RDS.
- `JWT_*`: geração e validação de tokens.
- `EMAIL_*`: remetente e SMTP local/cloud.
- `ADMIN_INICIAL_*`: admin inicial de desenvolvimento.
- `API_*`: porta e imagem usada pelo Docker Compose.

## Execução local com Docker

Suba o SQL Server e o smtp4dev:

```powershell
docker compose --profile local-db --env-file docker/.env -f docker/docker-compose.yml up -d sqlserver smtp4dev
```

Execute as migrations:

```powershell
docker compose --profile local-db --env-file docker/.env -f docker/docker-compose.yml run --rm migration
```

Suba a API:

```powershell
docker compose --profile local-db --env-file docker/.env -f docker/docker-compose.yml up -d api
```

O serviço `migration` usa `APP_MODE=migration` e encerra após aplicar as migrations. A API normal não aplica migrations automaticamente. O smtp4dev é usado apenas para desenvolvimento local.

## Execução local com dotnet

Rodar a API:

```powershell
dotnet run --project src/Oficina.Api/Oficina.Api.csproj
```

Executar migrations:

```powershell
$env:APP_MODE="migration"; dotnet run --project src/Oficina.Api/Oficina.Api.csproj
```

Limpar a variável:

```powershell
Remove-Item Env:APP_MODE
```

## Validação local

| Recurso | URL |
|---|---|
| Swagger | `http://localhost:8080/swagger` |
| Healthcheck | `http://localhost:8080/health` |
| smtp4dev | `http://localhost:5000` |
