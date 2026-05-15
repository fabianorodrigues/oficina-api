# oficina-api

## Visão Geral

Este repositório contém a API principal da solução Oficina. A aplicação é uma API REST em .NET para clientes, veículos, serviços, peças, estoque, ordens de serviço, diagnósticos, orçamentos, autenticação e autorização JWT.

Na AWS, a imagem Docker é publicada no ECR, as migrações são executadas por um Kubernetes Job e a API é implantada no EKS atrás de um NLB interno. A entrada pública é criada depois pelo API Gateway do `oficina-infra-k8s`.

## Responsabilidade Deste Repositório

- Manter código, domínio, infraestrutura da aplicação e migrações.
- Executar build e testes automatizados.
- Publicar a imagem Docker no ECR com tag do commit.
- Executar migrações com `APP_MODE=migration`.
- Implantar a API no EKS.
- Criar o Service Kubernetes que gera o NLB interno.
- Validar o NLB interno e gravar o Listener ARN no SSM para o API Gateway.

## Integração com os Outros Repositórios

Valores consumidos:

| Valor | Origem | Uso |
| --- | --- | --- |
| `ECR_REPOSITORY_URL` | `oficina-infra-k8s` | Compilação e envio da imagem |
| `EKS_CLUSTER_NAME` | `oficina-infra-k8s` | Acesso ao cluster |
| `DB_CONNECTION_STRING` | `oficina-infra-db` e GitHub Secret | Acesso ao SQL Server |
| Configuração JWT | GitHub Secrets compartilhados com `oficina-auth-lambda` | Validação de tokens |
| SMTP e URL pública de e-mail | GitHub Variables/Secrets opcionais | Envio de e-mails quando configurado |

Valores gerados:

| Valor | Consumido por | Uso |
| --- | --- | --- |
| Imagem Docker com tag do commit | EKS | Implantação rastreável |
| Tag `latest` | ECR | Alias operacional da imagem mais recente |
| NLB interno | `oficina-infra-k8s` API Gateway | Backend privado da API |
| `/oficina/{environment}/api/backend-listener-arn` | `oficina-infra-k8s` API Gateway | Integração privada do API Gateway |

## Ordem de Implantação

1. `oficina-infra-db`
2. `oficina-infra-k8s` core
3. `oficina-infra-k8s` addons
4. `oficina-api`
5. `oficina-auth-lambda`
6. `oficina-infra-k8s` API Gateway

## Configuração Necessária

Configure no GitHub Actions:

| Nome | Tipo | Uso |
| --- | --- | --- |
| `AWS_ACCESS_KEY_ID` | Secret | Autenticação AWS |
| `AWS_SECRET_ACCESS_KEY` | Secret | Autenticação AWS |
| `AWS_SESSION_TOKEN` | Secret opcional | Credenciais temporárias |
| `AWS_REGION` | Secret | Região AWS |
| `ECR_REPOSITORY_URL` | Secret | URL do repositório ECR |
| `EKS_CLUSTER_NAME` | Secret | Nome do cluster EKS |
| `DB_CONNECTION_STRING` | Secret | Conexão com SQL Server |
| `JWT_SECRET` | Secret | Chave de assinatura e validação JWT |
| `JWT_ISSUER` | Secret | Issuer JWT |
| `JWT_AUDIENCE` | Secret | Audience JWT |
| `JWT_EXPIRATION_MINUTES` | Secret | Tempo de expiração dos tokens |
| `ADMIN_INICIAL_NOME` | Secret opcional | Admin inicial quando habilitado |
| `ADMIN_INICIAL_CPF` | Secret opcional | Admin inicial quando habilitado |
| `ADMIN_INICIAL_SENHA` | Secret opcional | Admin inicial quando habilitado |
| `EMAIL_SMTP_HOST`, `EMAIL_SMTP_PORT`, `EMAIL_ENABLE_SSL`, `EMAIL_FROM` | Variables opcionais | Configuração SMTP |
| `EMAIL_SMTP_USERNAME`, `EMAIL_SMTP_PASSWORD` | Secrets opcionais | Autenticação SMTP |
| `EMAIL_BASE_URL_APROVA_RECUSA_ORCAMENTO` | Variable opcional | URL pública para links de e-mail |

`JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE` e `JWT_EXPIRATION_MINUTES` devem ser iguais aos usados no `oficina-auth-lambda`. Se a URL pública de e-mail não for configurada, o workflow tenta ler `/oficina/{environment}/api/public-base-url` no SSM; se o parâmetro ainda não existir, o deploy segue com valor vazio.

## Como Executar

Execute manualmente:

```text
GitHub Actions > Deploy API > Run workflow
```

O input `enable_initial_admin` deve ficar `true` apenas na criação controlada do admin inicial em banco vazio. Nas execuções seguintes, use `false`.

O workflow valida a configuração, executa build e testes, publica a imagem, executa as migrações, aplica os manifests no EKS, valida o rollout, confirma que o Load Balancer é NLB interno e grava `/oficina/{environment}/api/backend-listener-arn` no SSM.

## Como Validar na AWS

Console:

- Em ECR, confirme a imagem com tag do commit e a tag `latest`.
- Em EKS, confirme deployment, pods e service no namespace `oficina`.
- Em EC2 Load Balancers, confirme que o Load Balancer da API é do tipo network e scheme internal.
- Em SSM Parameter Store, confirme que `/oficina/{environment}/api/backend-listener-arn` existe.

CLI:

```powershell
$env:AWS_REGION="<regiao>"
$env:ENVIRONMENT="<ambiente>"
$env:EKS_CLUSTER_NAME="<nome-do-cluster>"
$env:ECR_REPOSITORY_NAME="<nome-do-repositorio-ecr>"

aws ecr describe-images --repository-name $env:ECR_REPOSITORY_NAME --image-ids imageTag="<commit-sha>" --region $env:AWS_REGION --query "imageDetails[0].{Tags:imageTags,PushedAt:imagePushedAt}"
aws eks update-kubeconfig --name $env:EKS_CLUSTER_NAME --region $env:AWS_REGION
kubectl rollout status deployment/oficina-api -n oficina
kubectl get pods -n oficina -l app=oficina-api
kubectl get svc oficina-api -n oficina -o jsonpath='{.spec.type}{"\n"}'
aws elbv2 describe-load-balancers --region $env:AWS_REGION --query "LoadBalancers[?Type=='network' && Scheme=='internal'].{Type:Type,Scheme:Scheme,State:State.Code}"
aws ssm get-parameter --name "/oficina/$($env:ENVIRONMENT)/api/backend-listener-arn" --region $env:AWS_REGION --query "Parameter.Name"
```

Health Check da API:

```powershell
kubectl port-forward svc/oficina-api -n oficina 18080:80
Invoke-RestMethod http://127.0.0.1:18080/health
Invoke-RestMethod http://127.0.0.1:18080/ready
```

Após o API Gateway, valide a URL pública em ambiente autenticado:

- `GET /health`
- `POST /api/auth/cpf`
- Uma rota protegida sem token
- Uma rota protegida com token válido

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

## Próxima Etapa

Publicar o `oficina-auth-lambda`. Depois, aplicar o root `api-gateway` no `oficina-infra-k8s`.
