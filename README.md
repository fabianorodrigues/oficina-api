# oficina-api

## Visão Geral

Este repositório contém a API principal da solução Oficina. A aplicação é uma API REST em .NET para clientes, veículos, serviços, peças, estoque, ordens de serviço, diagnósticos, orçamentos, autenticação e autorização JWT.

Na AWS, a imagem Docker é publicada no ECR, as migrations são executadas por um Kubernetes Job e a API é implantada no EKS atrás de um NLB interno. A entrada pública oficial é o API Gateway HTTP API criado pelo `oficina-infra-k8s`.

## Responsabilidade

- Manter código, domínio, infraestrutura da aplicação e migrations.
- Executar build e testes automatizados.
- Publicar imagem Docker no ECR.
- Executar migrations com `APP_MODE=migration`.
- Implantar a API no EKS usando `${ECR_REPOSITORY_URL}:${GITHUB_SHA}`.
- Criar Service Kubernetes com NLB interno via AWS Load Balancer Controller.
- Gravar o Listener ARN validado no SSM para uso do API Gateway.

A tag `latest` existe apenas como alias operacional no ECR. Kubernetes usa a imagem com `${GITHUB_SHA}`.

## Ordem De Implantação

1. `oficina-infra-db`
2. `oficina-infra-k8s` core
3. `oficina-infra-k8s` addons
4. `oficina-api`
5. `oficina-auth-lambda`
6. `oficina-infra-k8s` API Gateway
7. Novo deploy da `oficina-api` com `EMAIL_BASE_URL_APROVA_RECUSA_ORCAMENTO`

## Configuração Necessária

Configure no GitHub Actions:

| Nome | Tipo | Uso |
| --- | --- | --- |
| `AWS_ACCESS_KEY_ID` | Secret | Autenticação AWS |
| `AWS_SECRET_ACCESS_KEY` | Secret | Autenticação AWS |
| `AWS_SESSION_TOKEN` | Secret opcional | Credenciais temporárias |
| `AWS_REGION` | Secret | Região AWS |
| `ECR_REPOSITORY_URL` | Secret | Repositório ECR da API |
| `EKS_CLUSTER_NAME` | Secret | Cluster EKS |
| `DB_CONNECTION_STRING` | Secret | Conexão com SQL Server |
| `JWT_SECRET` | Secret | Validação de tokens |
| `JWT_ISSUER` | Secret | Issuer JWT |
| `JWT_AUDIENCE` | Secret | Audience JWT |
| `JWT_EXPIRATION_MINUTES` | Secret | Expiração dos tokens |
| `ADMIN_INICIAL_NOME` | Secret opcional | Admin inicial |
| `ADMIN_INICIAL_CPF` | Secret opcional | Admin inicial |
| `ADMIN_INICIAL_SENHA` | Secret opcional | Admin inicial |
| `EMAIL_SMTP_USERNAME` | Secret opcional | Usuário SMTP |
| `EMAIL_SMTP_PASSWORD` | Secret opcional | Senha SMTP |
| `EMAIL_BASE_URL_APROVA_RECUSA_ORCAMENTO` | Variable opcional | URL pública para links de orçamento |

`JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE` e `JWT_EXPIRATION_MINUTES` devem ser iguais aos usados no `oficina-auth-lambda`.

As credenciais AWS do deploy precisam manter as permissões já usadas para ECR/EKS e também `ec2:DescribeVpcs`, `elasticloadbalancing:DescribeLoadBalancers`, `elasticloadbalancing:DescribeListeners`, `ssm:PutParameter` e `ssm:GetParameter`.

Quando `EMAIL_BASE_URL_APROVA_RECUSA_ORCAMENTO` não estiver configurada como GitHub Variable, o deploy tenta ler `/oficina/{environment}/api/public-base-url` no SSM. Se o parâmetro não existir, segue com valor vazio.

## Como Executar Na AWS

Execute manualmente:

```text
GitHub Actions > Deploy API > Run workflow
```

O workflow valida a configuração, executa build e testes, publica a imagem no ECR, executa migrations e faz o rollout da API.

Antes de gravar `/oficina/{environment}/api/backend-listener-arn`, o workflow valida de forma segura que o LoadBalancer criado é `Type=network`, `Scheme=internal`, possui listener `80` e possui target group associado. Se qualquer validação falhar, o parâmetro SSM não é gravado.

Em ambiente já provisionado, mudanças no tipo ou no scheme do Service devem ser feitas com recriação controlada do Service, porque o LoadBalancer é gerenciado pelo AWS Load Balancer Controller.

O input `enable_initial_admin` deve ser usado apenas para criar o admin inicial em banco vazio. Depois, execute novo deploy com `enable_initial_admin=false`.

## Como Validar Na AWS

Antes do API Gateway, valide a API com `kubectl port-forward` para o Service:

```powershell
kubectl port-forward svc/oficina-api -n oficina 18080:80
Invoke-RestMethod http://127.0.0.1:18080/health
```

Depois do root de API Gateway, valide pela URL pública do API Gateway:

- `GET /health`
- `POST /api/auth/cpf`
- Uma rota protegida sem token
- Uma rota protegida com token válido

Não valide a API pelo DNS do LoadBalancer. O LoadBalancer deve ser interno.

## Como Executar Localmente

Crie o arquivo local de variáveis e suba os serviços:

```powershell
Copy-Item docker/.env.example docker/.env
docker compose --profile local-db --env-file docker/.env -f docker/docker-compose.yml up -d sqlserver smtp4dev
docker compose --profile local-db --env-file docker/.env -f docker/docker-compose.yml run --rm migration
docker compose --profile local-db --env-file docker/.env -f docker/docker-compose.yml up -d api
```

## Como Validar Localmente

```powershell
Invoke-RestMethod http://localhost:8080/health
Invoke-RestMethod http://localhost:8080/ready
dotnet restore Oficina.sln
dotnet build Oficina.sln --configuration Release --no-restore
dotnet test Oficina.sln --configuration Release --no-build
```

Swagger local:

```text
http://localhost:8080/swagger
```

## Valores Consumidos

| Valor | Origem | Uso |
| --- | --- | --- |
| `ECR_REPOSITORY_URL` | `oficina-infra-k8s` | Publicar e implantar imagem |
| `EKS_CLUSTER_NAME` | `oficina-infra-k8s` | Configurar acesso ao cluster |
| `DB_CONNECTION_STRING` | GitHub Secret | Conectar ao SQL Server |
| Configuração JWT | GitHub Secrets | Validar tokens |
| `/oficina/{environment}/api/public-base-url` | SSM opcional | Links públicos depois do API Gateway |

## Valores Gerados

| Valor | Destino | Uso |
| --- | --- | --- |
| Imagem Docker com `${GITHUB_SHA}` | ECR | Deploy rastreável |
| `latest` | ECR | Alias operacional |
| `/oficina/{environment}/api/backend-listener-arn` | SSM | Backend privado do API Gateway |

## Próxima Etapa

Depois do deploy da API, publique o `oficina-auth-lambda`, aplique o root `api-gateway` do `oficina-infra-k8s` e execute novo deploy da API para consumir a URL pública final.
