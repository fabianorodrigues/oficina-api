# oficina-api

## Visão geral

Este repositório contém a API principal da Oficina API. Ele é a **etapa 3** da implantação da solução.

A aplicação é uma API REST em .NET 10 para clientes, veículos, serviços, peças, estoque, ordens de serviço, diagnósticos, orçamentos, autenticação e autorização JWT. Nesta etapa, a imagem Docker é publicada no ECR, as migrations são executadas e a API é implantada no EKS.

## Ordem de implantação da solução

1. `oficina-infra-db`
2. `oficina-infra-k8s`
3. **`oficina-api`**
4. `oficina-auth-lambda`
5. `oficina-infra-k8s` novamente para API Gateway, quando essa etapa estiver implementada

## Responsabilidade

Este repositório é responsável por:

- manter o código da API, domínio, infraestrutura e migrations;
- executar testes automatizados;
- publicar a imagem Docker no ECR;
- publicar a tag `${GITHUB_SHA}` como tag rastreável da versão;
- publicar `latest` como alias operacional mutável;
- executar migrations com `APP_MODE=migration`;
- implantar a API no EKS quando o workflow de deploy estiver disponível.

## Pré-requisitos

- Docker Desktop para execução local.
- .NET SDK 10 conforme `global.json`.
- AWS CLI para validar ECR.
- `kubectl` para validar deploy no EKS.
- `oficina-infra-db` aplicado com outputs disponíveis.
- `oficina-infra-k8s` aplicado com ECR e EKS disponíveis.

## Configuração necessária

Configure os valores em `GitHub > Settings > Secrets and variables > Actions` para publicação/deploy.

| Nome | Tipo | Origem | Onde configurar | Uso |
|---|---|---|---|---|
| `AWS_ACCESS_KEY_ID` | Secret | Credencial AWS do usuário | GitHub Secrets deste repo | Autenticar na AWS |
| `AWS_SECRET_ACCESS_KEY` | Secret | Credencial AWS do usuário | GitHub Secrets deste repo | Autenticar na AWS |
| `AWS_SESSION_TOKEN` | Secret | Credencial temporária, se aplicável | GitHub Secrets deste repo | Autenticar com sessão temporária |
| `AWS_REGION` | Secret | Região escolhida, por exemplo `us-east-1` | GitHub Secrets deste repo | Publicar e validar imagem |
| `ECR_REPOSITORY_URL` | Secret | Output `ecr_repository_url` do `oficina-infra-k8s` | GitHub Secrets deste repo | Publicar imagem Docker |
| `EKS_CLUSTER_NAME` | Secret | Output `cluster_name` do `oficina-infra-k8s` | GitHub Secrets deste repo | Deploy no EKS |
| `DB_CONNECTION_STRING` | Secret | Montada com outputs do `oficina-infra-db` | GitHub Secrets deste repo | Conexão da API com SQL Server |
| `JWT_SECRET` | Secret | Valor definido pelo usuário | GitHub Secrets deste repo | Validar tokens JWT |
| `JWT_ISSUER` | Secret | Mesmo valor do `oficina-auth-lambda` | GitHub Secrets deste repo | Validar issuer JWT |
| `JWT_AUDIENCE` | Secret | Mesmo valor do `oficina-auth-lambda` | GitHub Secrets deste repo | Validar audience JWT |
| `JWT_EXPIRATION_MINUTES` | Secret | Mesmo valor do `oficina-auth-lambda` | GitHub Secrets deste repo | Expiração dos tokens |
| `IMAGE_ALIAS_TAG` | Variable opcional | Valor definido pelo usuário | GitHub Variables deste repo | Alias mutável da imagem; padrão `latest` |

`latest` é apenas um alias operacional mutável. A rastreabilidade da versão é feita pela tag `${GITHUB_SHA}`, que também é publicada no ECR.

Modelo de `DB_CONNECTION_STRING`:

```text
Server=<db_address>,<db_port>;Database=<db_name>;User Id=<db-user>;Password=<db-password>;Encrypt=True;TrustServerCertificate=True;
```

## Configuração local

Crie o arquivo local de variáveis:

```powershell
Copy-Item docker/.env.example docker/.env
```

O arquivo `docker/.env` configura SQL Server local, API, JWT, e-mail e admin inicial. Não versione `docker/.env`.

O envio de e-mail é best-effort: se o SMTP falhar, a operação principal continua e a falha é registrada em log. SMTP não é obrigatório para o fluxo principal.

## Como executar

### CI

O workflow `ci` roda em Pull Request e push para `main`:

- restore;
- build Release;
- testes.

### Publicar imagem no ECR

Execute manualmente:

```text
GitHub Actions > docker-build-push > Run workflow
```

O workflow publica duas tags:

- `${GITHUB_SHA}`: tag imutável e rastreável da versão;
- `latest`: alias operacional mutável.

### Execução local com Docker

Suba SQL Server e smtp4dev:

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

### Execução local com dotnet

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

### Testes

```powershell
dotnet restore Oficina.sln
dotnet build Oficina.sln --configuration Release --no-restore
dotnet test Oficina.sln --configuration Release --no-build
```

## Como validar

Valide healthcheck local:

```powershell
Invoke-RestMethod http://localhost:8080/health
```

Acesse Swagger:

```text
http://localhost:8080/swagger
```

Valide imagem no ECR:

```powershell
aws ecr describe-images --repository-name oficina-api --image-ids imageTag=latest --region <region>
aws ecr describe-images --repository-name oficina-api --image-ids imageTag=<commit-sha> --region <region>
```

Quando o deploy no EKS estiver disponível, valide:

```powershell
kubectl get pods -n oficina
kubectl get svc -n oficina
kubectl rollout status deployment/oficina-api -n oficina
```

Collections Postman:

```text
postman/OficinaAPI-cenarios.postman_collection.json
postman/OficinaAPI-cenarios.postman_environment.json
postman/OficinaAPI-seguranca.postman_collection.json
```

Configure `baseUrl=http://localhost:8080` para execução local.

## Outputs para a próxima etapa

Este repositório não gera outputs Terraform. Após a API estar publicada no EKS, os valores operacionais abaixo serão usados pelas próximas etapas.

| Valor | Usado por | Configurar como |
|---|---|---|
| URL pública ou load balancer da API | `oficina-infra-k8s` na etapa de API Gateway | `api_load_balancer_url`, quando implementado |
| Configuração JWT | `oficina-auth-lambda` | `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRATION_MINUTES` |
| Tag `${GITHUB_SHA}` | Auditoria e rollback | Referência rastreável da versão publicada |
| Tag `latest` | Operação corrente | Alias mutável da imagem mais recente |

## Problemas comuns

| Problema | Possível causa | Como resolver |
|---|---|---|
| API não conecta no banco | `DB_CONNECTION_STRING` incorreta | Monte novamente com `db_address`, `db_port` e `db_name` |
| Push no ECR falha | `ECR_REPOSITORY_URL` ausente ou incorreto | Use o output `ecr_repository_url` do `oficina-infra-k8s` |
| Tag SHA já existe | Commit já publicado | O workflow preserva a tag imutável e publica apenas o alias |
| `latest` não atualiza | Exceção mutável não configurada no ECR | Confirme `ecr_mutable_alias_tag=latest` no `oficina-infra-k8s` |
| Swagger não abre | API não iniciou | Consulte logs do container ou do pod |
| E-mail não envia | SMTP não configurado | Configure SMTP real ou use smtp4dev localmente |

## Próxima etapa

Siga para o repositório `oficina-auth-lambda` após a API publicar a imagem, executar migrations e subir no EKS.
