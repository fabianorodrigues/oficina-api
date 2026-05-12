# oficina-api

## Visão geral

Este repositório contém a API principal da solução Oficina API. A aplicação é uma API REST em .NET 10 para clientes, veículos, serviços, peças, estoque, ordens de serviço, diagnósticos, orçamentos, autenticação e autorização JWT.

Na AWS, a imagem Docker é publicada no ECR, as migrations são executadas por um Kubernetes Job e a API é implantada no EKS.

A solução completa é composta por quatro repositórios, nesta ordem de implantação:

1. `oficina-infra-db`: rede, security groups e RDS.
2. `oficina-infra-k8s`: ECR, EKS e node group.
3. `oficina-api`: imagem Docker, migrations e deploy da API no EKS.
4. `oficina-auth-lambda`: Lambdas de autenticação por CPF e autorização JWT.

## Papel deste repositório

- Manter o código da API, domínio, infraestrutura e migrations.
- Executar build e testes automatizados.
- Publicar a imagem Docker no ECR.
- Executar migrations com `APP_MODE=migration`.
- Implantar a API no EKS usando a tag rastreável `${GITHUB_SHA}`.

Kubernetes usa sempre a imagem `${ECR_REPOSITORY_URL}:${GITHUB_SHA}`. A tag `latest` existe apenas como alias operacional no ECR.

## Integração e dependências

Este repositório consome infraestrutura criada por `oficina-infra-db` e `oficina-infra-k8s`. Ele não gera outputs Terraform. Os outputs dos repositórios de infraestrutura facilitam o provisionamento, a integração entre repositórios e a avaliação acadêmica do projeto como portfólio, mas não devem ser publicados em logs; consulte-os em ambiente autenticado e configure os valores necessários como GitHub Secrets.

| Valor consumido | Origem | Uso |
|---|---|---|
| `ECR_REPOSITORY_URL` | Output `ecr_repository_url` do `oficina-infra-k8s` | Publicar e implantar a imagem Docker |
| `EKS_CLUSTER_NAME` | Output `cluster_name` do `oficina-infra-k8s` | Configurar kubeconfig e fazer deploy no EKS |
| `DB_CONNECTION_STRING` | Outputs `db_address`, `db_port` e `db_name` do `oficina-infra-db`, mais usuário e senha do banco | Conectar a API ao SQL Server |
| `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRATION_MINUTES` | Mesma configuração usada no `oficina-auth-lambda` | Validar os tokens emitidos pelas Lambdas |

Modelo de connection string:

```text
Server=<db_address>,<db_port>;Database=<db_name>;User Id=<db-user>;Password=<db-password>;Encrypt=True;TrustServerCertificate=True;
```

## Configuração necessária

Configure os valores em `GitHub > Settings > Secrets and variables > Actions`.

| Nome | Tipo | Uso |
|---|---|---|
| `AWS_ACCESS_KEY_ID` | Secret | Autenticar na AWS |
| `AWS_SECRET_ACCESS_KEY` | Secret | Autenticar na AWS |
| `AWS_SESSION_TOKEN` | Secret opcional | Usar credenciais temporárias |
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

O `JWT_SECRET` deve ser igual ao usado pelo `oficina-auth-lambda` e ter pelo menos 32 caracteres.

Variáveis opcionais para SMTP:

| Nome | Tipo | Uso |
|---|---|---|
| `EMAIL_SMTP_HOST` | Variable opcional | Host SMTP |
| `EMAIL_SMTP_PORT` | Variable opcional | Porta SMTP |
| `EMAIL_ENABLE_SSL` | Variable opcional | `true` ou `false`; padrão `false` |
| `EMAIL_FROM` | Variable opcional | Remetente dos e-mails |
| `EMAIL_BASE_URL_APROVA_RECUSA_ORCAMENTO` | Variable opcional | URL pública da API nos links de aprovação/recusa |

## Como executar e validar na AWS

Execute manualmente:

```text
GitHub Actions > Deploy API > Run workflow
```

O workflow valida a configuração, executa build e testes, publica a imagem no ECR, executa migrations no EKS e faz o rollout da API.

O input `enable_initial_admin` controla a criação do admin inicial:

- `false`: padrão para execuções normais;
- `true`: cria o admin inicial e exige `ADMIN_INICIAL_NOME`, `ADMIN_INICIAL_CPF` e `ADMIN_INICIAL_SENHA`.

Use `enable_initial_admin=true` somente na primeira subida de um banco vazio. Depois, execute novo deploy com `enable_initial_admin=false` para recriar o Secret Kubernetes sem esses dados.

Para validar manualmente:

```powershell
aws ecr describe-images --repository-name oficina-api --image-ids imageTag=<commit-sha> --region <region>
aws ecr describe-images --repository-name oficina-api --image-ids imageTag=latest --region <region>
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

## Problemas comuns

| Problema | Possível causa | Como resolver |
|---|---|---|
| API não conecta no banco | `DB_CONNECTION_STRING` incorreta | Monte novamente com outputs do `oficina-infra-db` |
| Push no ECR falha | `ECR_REPOSITORY_URL` ausente ou incorreto | Use `ecr_repository_url` do `oficina-infra-k8s` |
| Token válido é negado | JWT diferente do configurado nas Lambdas | Alinhe `JWT_SECRET`, `JWT_ISSUER` e `JWT_AUDIENCE` |
| Rollout expira | Pod não ficou `Ready` | Consulte logs e eventos do deployment |
| E-mail não envia | SMTP não configurado | Configure SMTP apenas quando esse fluxo for necessário |

## Como executar e validar localmente

Crie o arquivo local de variáveis:

```powershell
cd oficina-api
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

Valide:

```powershell
Invoke-RestMethod http://localhost:8080/health
Invoke-RestMethod http://localhost:8080/ready
```

Swagger local:

```text
http://localhost:8080/swagger
```

Para build e testes locais:

```powershell
dotnet restore Oficina.sln
dotnet build Oficina.sln --configuration Release --no-restore
dotnet test Oficina.sln --configuration Release --no-build
```

Collections Postman:

```text
postman/OficinaAPI-cenarios.postman_collection.json
postman/OficinaAPI-cenarios.postman_environment.json
postman/OficinaAPI-seguranca.postman_collection.json
```

Use `baseUrl=http://localhost:8080` para execução local.
