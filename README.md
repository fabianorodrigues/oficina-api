# oficina-api

## Visão geral

Este repositório contém a API principal da Oficina API. Ele é a **etapa 3** da implantação da solução.

A aplicação é uma API REST em .NET 10 para clientes, veículos, serviços, peças, estoque, ordens de serviço, diagnósticos, orçamentos, autenticação e autorização JWT. Nesta etapa, a imagem Docker é publicada no ECR, as migrations são executadas por um Kubernetes Job e a API é implantada no EKS.

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
- publicar `latest` apenas como alias operacional mutável;
- validar que `latest` aponta para o mesmo `imageDigest` da tag `${GITHUB_SHA}`;
- executar migrations com `APP_MODE=migration`;
- implantar a API no EKS.

Kubernetes nunca usa `latest`. O Migration Job e o Deployment usam sempre:

```text
${ECR_REPOSITORY_URL}:${GITHUB_SHA}
```

## Pré-requisitos

- Docker Desktop para execução local.
- .NET SDK 10 conforme `global.json`.
- AWS CLI para validar ECR e EKS.
- `kubectl` para validar deploy no EKS.
- `oficina-infra-db` aplicado com outputs disponíveis.
- `oficina-infra-k8s` aplicado com ECR e EKS disponíveis.

## Configuração necessária

Configure os valores em `GitHub > Settings > Secrets and variables > Actions`.

| Nome | Tipo | Origem | Uso |
|---|---|---|---|
| `AWS_ACCESS_KEY_ID` | Secret | Credencial AWS do usuário | Autenticar na AWS |
| `AWS_SECRET_ACCESS_KEY` | Secret | Credencial AWS do usuário | Autenticar na AWS |
| `AWS_SESSION_TOKEN` | Secret opcional | Credencial temporária, se aplicável | Autenticar com sessão temporária |
| `AWS_REGION` | Secret | Região escolhida, por exemplo `us-east-1` | ECR, EKS e AWS CLI |
| `ECR_REPOSITORY_URL` | Secret | Output `ecr_repository_url` do `oficina-infra-k8s` | Publicar imagem Docker |
| `EKS_CLUSTER_NAME` | Secret | Output `cluster_name` do `oficina-infra-k8s` | Deploy no EKS |
| `DB_CONNECTION_STRING` | Secret | Montada com outputs do `oficina-infra-db` | Conexão da API com SQL Server |
| `JWT_SECRET` | Secret | Valor definido pelo usuário | Validar tokens JWT |
| `JWT_ISSUER` | Secret | Mesmo valor do `oficina-auth-lambda` | Validar issuer JWT |
| `JWT_AUDIENCE` | Secret | Mesmo valor do `oficina-auth-lambda` | Validar audience JWT |
| `JWT_EXPIRATION_MINUTES` | Secret | Mesmo valor do `oficina-auth-lambda` | Expiração dos tokens |
| `ADMIN_INICIAL_NOME` | Secret opcional | Valor definido pelo usuário | Usado somente com `enable_initial_admin=true` |
| `ADMIN_INICIAL_CPF` | Secret opcional | Valor definido pelo usuário | Usado somente com `enable_initial_admin=true` |
| `ADMIN_INICIAL_SENHA` | Secret opcional | Valor definido pelo usuário | Usado somente com `enable_initial_admin=true` |
| `EMAIL_SMTP_USERNAME` | Secret opcional | Usuário do provedor SMTP | Usado somente se o SMTP exigir autenticação |
| `EMAIL_SMTP_PASSWORD` | Secret opcional | Senha do provedor SMTP | Usado somente se o SMTP exigir autenticação |

`AWS_SESSION_TOKEN` é opcional. Quando estiver preenchido, o workflow configura credenciais AWS com session token. Quando estiver vazio, usa apenas `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY` e `AWS_REGION`.

Configure também GitHub Variables opcionais para SMTP em cloud:

| Nome | Tipo | Uso |
|---|---|---|
| `EMAIL_SMTP_HOST` | Variable opcional | Host SMTP em cloud |
| `EMAIL_SMTP_PORT` | Variable opcional | Porta SMTP; precisa ser maior que `0` quando SMTP estiver configurado |
| `EMAIL_ENABLE_SSL` | Variable opcional | `true` ou `false`; padrão `false` |
| `EMAIL_FROM` | Variable opcional | Remetente usado nos e-mails |
| `EMAIL_BASE_URL_APROVA_RECUSA_ORCAMENTO` | Variable opcional | URL pública da API usada nos links de aprovação/recusa |

Se SMTP estiver configurado, informe `EMAIL_SMTP_HOST`, `EMAIL_SMTP_PORT`, `EMAIL_FROM` e `EMAIL_BASE_URL_APROVA_RECUSA_ORCAMENTO`. Se o provedor exigir autenticação, configure `EMAIL_SMTP_USERNAME` e `EMAIL_SMTP_PASSWORD` juntos.

`EMAIL_BASE_URL_APROVA_RECUSA_ORCAMENTO` representa a URL pública da própria API:

- localmente: `http://localhost:8080`;
- no EKS antes do API Gateway: URL pública do LoadBalancer;
- após API Gateway: URL pública do API Gateway.

No deploy Kubernetes esse valor não é hardcoded; ele vem somente da GitHub Variable.

Modelo de `DB_CONNECTION_STRING`:

```text
Server=<db_address>,<db_port>;Database=<db_name>;User Id=<db-user>;Password=<db-password>;Encrypt=True;TrustServerCertificate=True;
```

## Deploy manual no EKS

Execute manualmente:

```text
GitHub Actions > deploy-api > Run workflow
```

O input `enable_initial_admin` controla a criação do admin inicial:

- `false`: padrão recomendado para execuções normais.
- `true`: habilita `AdminInicial__Enabled=true` e exige `ADMIN_INICIAL_NOME`, `ADMIN_INICIAL_CPF` e `ADMIN_INICIAL_SENHA`.

Use `enable_initial_admin=true` somente quando precisar preparar a primeira autenticação em um banco vazio. O Secret Kubernetes é recriado a cada deploy; quando esse input estiver `false`, as chaves de admin inicial não ficam preservadas no Secret.

O workflow executa:

- validação de secrets obrigatórios sem imprimir valores;
- validação segura da configuração SMTP opcional;
- restore, build e testes da solution;
- login no ECR antes de qualquer `docker push`;
- publicação da tag `${GITHUB_SHA}` somente se ela ainda não existir;
- atualização de `latest` como alias da mesma imagem da tag `${GITHUB_SHA}`;
- validação de existência das tags `${GITHUB_SHA}` e `latest`;
- comparação de `imageDigest` entre `${GITHUB_SHA}` e `latest`;
- configuração do kubeconfig do EKS;
- aplicação do namespace `oficina`;
- validação de permissões Kubernetes;
- criação/atualização de ConfigMap sem dados sensíveis;
- recriação do Secret da API sem versionar valores, incluindo credenciais SMTP somente quando configuradas em par;
- execução do Kubernetes Job de migration;
- deploy da API com Service `LoadBalancer`;
- validação de rollout e `/health` com retry.

Se a migration falhar ou der timeout, o workflow coleta diagnóstico com:

```powershell
kubectl describe job oficina-api-migration -n oficina
kubectl get pods -n oficina -l job-name=oficina-api-migration
kubectl logs -n oficina -l job-name=oficina-api-migration --tail=200
```

Se o LoadBalancer ou `/health` falhar, o workflow coleta:

```powershell
kubectl describe svc oficina-api -n oficina
kubectl get svc oficina-api -n oficina -o yaml
kubectl get endpoints oficina-api -n oficina
kubectl get pods -n oficina -l app=oficina-api
```

## Configuração local

Crie o arquivo local de variáveis:

```powershell
Copy-Item docker/.env.example docker/.env
```

O arquivo `docker/.env` configura SQL Server local, API, JWT, e-mail e admin inicial. Não versione `docker/.env`.

O `smtp4dev` é usado apenas no Docker local. O arquivo `docker/.env.example` mantém valores explícitos para admin inicial local:

```text
ADMIN_INICIAL_ENABLED=true
ADMIN_INICIAL_NOME=Admin Local
ADMIN_INICIAL_CPF=39053344705
ADMIN_INICIAL_SENHA=Senha@123
```

O envio de e-mail é best-effort: se SMTP estiver ausente ou falhar, a operação principal continua. Quando SMTP não estiver configurado, o sender registra log seguro informando que o e-mail não foi enviado e não tenta conexão. SMTP real não é obrigatório para subir a API.

Após usar `enable_initial_admin=true` na primeira subida em cloud, execute um novo deploy com `enable_initial_admin=false`. Isso remove `AdminInicial__Nome`, `AdminInicial__Cpf` e `AdminInicial__Senha` do Secret Kubernetes recriado pelo workflow.

O bootstrap do admin inicial é idempotente: se o CPF já existir, ele não recria o admin, não sobrescreve senha e não falha o startup.

## Execução local com Docker

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

## Testes

```powershell
dotnet restore Oficina.sln
dotnet build Oficina.sln --configuration Release --no-restore
dotnet test Oficina.sln --configuration Release --no-build
```

## Validação

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

Valide deploy no EKS:

```powershell
aws eks update-kubeconfig --name <cluster_name> --region <region>
kubectl get pods -n oficina
kubectl get svc oficina-api -n oficina
kubectl get endpoints oficina-api -n oficina
kubectl rollout status deployment/oficina-api -n oficina --timeout=300s
```

Quando o Service receber hostname ou IP, valide:

```powershell
Invoke-RestMethod http://<load-balancer>/health
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
| Tag SHA já existe | Commit já publicado | O workflow preserva a tag imutável e atualiza apenas `latest` |
| Digest de `latest` diverge do SHA | Alias apontando para outra imagem | Reexecute o workflow e valide a configuração de mutabilidade do ECR |
| `latest` não atualiza | Exceção mutável não configurada no ECR | Confirme `ecr_mutable_alias_tag=latest` no `oficina-infra-k8s` |
| Workflow sem permissão no EKS | Usuário/role AWS sem RBAC no cluster | Ajuste permissões antes de executar o deploy |
| Swagger não abre | API não iniciou | Consulte logs do pod |
| E-mail não envia | SMTP não configurado | O envio é best-effort; configure SMTP apenas quando necessário |

## Próxima etapa

Siga para o repositório `oficina-auth-lambda` após a API publicar a imagem, executar migrations e subir no EKS.
