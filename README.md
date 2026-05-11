# oficina-api

## Visão geral

Este repositório contém a API principal da solução Oficina API. A aplicação é uma API REST em .NET 10 para clientes, veículos, serviços, peças, estoque, ordens de serviço, diagnósticos, orçamentos, autenticação e autorização JWT.

Na implantação em AWS, a imagem Docker é publicada no ECR, as migrations são executadas por um Kubernetes Job e a API é implantada no EKS.

## Arquitetura e ordem de implantação

1. `oficina-infra-db`: cria VPC, subnets, security groups e RDS.
2. `oficina-infra-k8s`: cria ECR, EKS e node group.
3. **`oficina-api`**: publica a imagem no ECR, executa migrations e sobe no EKS.
4. `oficina-auth-lambda`: publica as Lambdas de autenticação e autorização.
5. `oficina-infra-k8s`: etapa futura para API Gateway.

## Responsabilidade deste repositório

- Manter o código da API, domínio, infraestrutura e migrations.
- Executar testes automatizados.
- Publicar a imagem Docker no ECR.
- Publicar a tag `${GITHUB_SHA}` como versão rastreável.
- Usar `latest` apenas como alias operacional mutável.
- Executar migrations com `APP_MODE=migration`.
- Implantar a API no EKS.

Kubernetes nunca usa `latest`. O Migration Job e o Deployment usam sempre:

```text
${ECR_REPOSITORY_URL}:${GITHUB_SHA}
```

## Integração com os outros repositórios

Este repositório consome banco, ECR e EKS já provisionados. Ele não gera outputs Terraform.

### Valores consumidos

| Valor | Origem | Uso |
|---|---|---|
| `ECR_REPOSITORY_URL` | Output `ecr_repository_url` do `oficina-infra-k8s` | Publicar e implantar a imagem Docker |
| `EKS_CLUSTER_NAME` | Output `cluster_name` do `oficina-infra-k8s` | Configurar kubeconfig e fazer deploy no EKS |
| `DB_CONNECTION_STRING` | Outputs `db_address`, `db_port` e `db_name` do `oficina-infra-db`, mais usuário e senha do banco | Conectar a API ao SQL Server |
| `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRATION_MINUTES` | Mesmos valores configurados no `oficina-auth-lambda` | Validar os tokens emitidos pelas Lambdas |

### Valores gerados

| Valor | Usado por | Uso |
|---|---|---|
| URL pública ou load balancer da API | `oficina-infra-k8s`, na etapa futura de API Gateway | Integração pública da API |
| Tag `${GITHUB_SHA}` | Auditoria e rollback | Referência rastreável da versão publicada |
| Tag `latest` | Operação corrente | Alias mutável da imagem mais recente no ECR |

Modelo de `DB_CONNECTION_STRING`:

```text
Server=<db_address>,<db_port>;Database=<db_name>;User Id=<db-user>;Password=<db-password>;Encrypt=True;TrustServerCertificate=True;
```

## Configuração necessária

Configure os valores em `GitHub > Settings > Secrets and variables > Actions`.

| Nome | Tipo | Uso |
|---|---|---|
| `AWS_ACCESS_KEY_ID` | Secret | Autenticar na AWS |
| `AWS_SECRET_ACCESS_KEY` | Secret | Autenticar na AWS |
| `AWS_SESSION_TOKEN` | Secret opcional | Autenticar com credencial temporária |
| `AWS_REGION` | Secret | Região AWS usada por ECR, EKS e AWS CLI |
| `ECR_REPOSITORY_URL` | Secret | URL completa do repositório ECR |
| `EKS_CLUSTER_NAME` | Secret | Nome do cluster EKS |
| `DB_CONNECTION_STRING` | Secret | Conexão da API com SQL Server |
| `JWT_SECRET` | Secret | Validar tokens JWT |
| `JWT_ISSUER` | Secret | Issuer JWT |
| `JWT_AUDIENCE` | Secret | Audience JWT |
| `JWT_EXPIRATION_MINUTES` | Secret | Expiração dos tokens |
| `ADMIN_INICIAL_NOME` | Secret opcional | Usado somente com `enable_initial_admin=true` |
| `ADMIN_INICIAL_CPF` | Secret opcional | Usado somente com `enable_initial_admin=true` |
| `ADMIN_INICIAL_SENHA` | Secret opcional | Usado somente com `enable_initial_admin=true` |
| `EMAIL_SMTP_USERNAME` | Secret opcional | Usuário SMTP, quando necessário |
| `EMAIL_SMTP_PASSWORD` | Secret opcional | Senha SMTP, quando necessário |

O `JWT_SECRET` deve ser o mesmo usado pelo `oficina-auth-lambda` e ter pelo menos 32 caracteres. Para gerar um valor forte no PowerShell:

```powershell
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | ForEach-Object {[char]$_})
```

Variables opcionais para SMTP em cloud:

| Nome | Tipo | Uso |
|---|---|---|
| `EMAIL_SMTP_HOST` | Variable opcional | Host SMTP |
| `EMAIL_SMTP_PORT` | Variable opcional | Porta SMTP |
| `EMAIL_ENABLE_SSL` | Variable opcional | `true` ou `false`; padrão `false` |
| `EMAIL_FROM` | Variable opcional | Remetente dos e-mails |
| `EMAIL_BASE_URL_APROVA_RECUSA_ORCAMENTO` | Variable opcional | URL pública da API nos links de aprovação/recusa |

Se SMTP estiver configurado, informe `EMAIL_SMTP_HOST`, `EMAIL_SMTP_PORT`, `EMAIL_FROM` e `EMAIL_BASE_URL_APROVA_RECUSA_ORCAMENTO`. Se o provedor exigir autenticação, configure `EMAIL_SMTP_USERNAME` e `EMAIL_SMTP_PASSWORD` juntos.

## Como executar

### Deploy no EKS

Execute manualmente:

```text
GitHub Actions > Deploy API > Run workflow
```

O input `enable_initial_admin` controla a criação do admin inicial:

- `false`: padrão recomendado para execuções normais;
- `true`: cria o admin inicial e exige `ADMIN_INICIAL_NOME`, `ADMIN_INICIAL_CPF` e `ADMIN_INICIAL_SENHA`.

Use `enable_initial_admin=true` apenas na primeira subida de um banco vazio. Depois, execute novo deploy com `enable_initial_admin=false` para remover os dados do Secret Kubernetes recriado pelo workflow.

### Execução local com Docker

Crie o arquivo local de variáveis:

```powershell
Copy-Item docker/.env.example docker/.env
```

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
Remove-Item Env:APP_MODE
```

Executar testes:

```powershell
dotnet restore Oficina.sln
dotnet build Oficina.sln --configuration Release --no-restore
dotnet test Oficina.sln --configuration Release --no-build
```

## Como validar

Valide a API local:

```powershell
Invoke-RestMethod http://localhost:8080/health
```

Swagger local:

```text
http://localhost:8080/swagger
```

Valide a imagem no ECR:

```powershell
aws ecr describe-images --repository-name oficina-api --image-ids imageTag=latest --region <region>
aws ecr describe-images --repository-name oficina-api --image-ids imageTag=<commit-sha> --region <region>
```

Valide o deploy no EKS:

```powershell
aws eks update-kubeconfig --name <cluster_name> --region <region>
kubectl get pods -n oficina
kubectl get svc oficina-api -n oficina
kubectl rollout status deployment/oficina-api -n oficina --timeout=300s
```

Quando o Service receber hostname ou IP:

```powershell
Invoke-RestMethod http://<load-balancer>/health
Invoke-RestMethod http://<load-balancer>/ready
```

Collections Postman:

```text
postman/OficinaAPI-cenarios.postman_collection.json
postman/OficinaAPI-cenarios.postman_environment.json
postman/OficinaAPI-seguranca.postman_collection.json
```

Configure `baseUrl=http://localhost:8080` para execução local.

## Problemas comuns

| Problema | Possível causa | Como resolver |
|---|---|---|
| API não conecta no banco | `DB_CONNECTION_STRING` incorreta | Monte novamente com `db_address`, `db_port` e `db_name` |
| Push no ECR falha | `ECR_REPOSITORY_URL` ausente ou incorreto | Use `ecr_repository_url` do `oficina-infra-k8s` |
| Authorizer nega token válido | JWT diferente do configurado nas Lambdas | Alinhe `JWT_SECRET`, `JWT_ISSUER` e `JWT_AUDIENCE` |
| Tag SHA já existe | Commit já publicado | O workflow preserva a tag imutável e atualiza apenas `latest` |
| `latest` não atualiza | Exceção mutável não configurada no ECR | Confirme `ecr_mutable_alias_tag=latest` no `oficina-infra-k8s` |
| Rollout timeout | Pod não ficou `Ready` ou ambiente está lento | Consulte logs e eventos do deployment no job `deploy-api` |
| E-mail não envia | SMTP não configurado | O envio é best-effort; configure SMTP apenas quando necessário |

## Próxima etapa

Siga para o repositório `oficina-auth-lambda` após a API publicar a imagem, executar migrations e subir no EKS.
