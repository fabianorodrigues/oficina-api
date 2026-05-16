# oficina-api

## Visão geral

Este repositório contém a API principal da solução Oficina. A aplicação é uma API REST em .NET para clientes, veículos, serviços, peças, estoque, ordens de serviço, diagnósticos, orçamentos, autenticação e autorização JWT.

Na AWS, o workflow publica a imagem no ECR, executa migrations em um Kubernetes Job, aplica o Deployment no EKS e valida a API. A entrada pública é o API Gateway criado pelo `oficina-infra-k8s`.

## Diagrama de arquitetura

```text
GitHub Actions (deploy-api.yml)
          │
          ├─ 1. Build e testes .NET
          ├─ 2. Publicar imagem no ECR  ──►  ECR Repository
          ├─ 3. Migration Job K8s       ──►  RDS SQL Server
          ├─ 4. Apply Deployment        ──►  EKS Deployment
          │      + Service NodePort           pods: oficina-api
          │                                   nodePort: 30080
          └─ 5. Validar                 ──►  port-forward → /health
                                             SSM Listener ARN existe
```

No modo `aws_lbc`, o Service é `LoadBalancer` e o Listener ARN é descoberto e gravado no SSM pelo workflow da API. No modo padrão `terraform_nlb`, o Service é `NodePort` e o Listener ARN já foi gravado pelo Terraform.

## Tecnologias utilizadas

- .NET 10 e ASP.NET Core
- Entity Framework Core
- SQL Server
- Docker
- Kubernetes
- AWS EKS, ECR e SSM Parameter Store
- GitHub Actions
- Swagger/OpenAPI
- Postman

## Sequência de Deploy (modo padrão `terraform_nlb`)

| Passo | Repositório | O que provisiona |
|-------|-------------|-----------------|
| 1 | oficina-infra-db | VPC, subnets, RDS SQL Server |
| 2 | oficina-infra-k8s core | EKS, ECR, NLB interno |
| **3** | **oficina-api ← este** | Migrations, Deployment, Service |
| 4 | oficina-auth-lambda | Lambdas de autenticação |
| 5 | oficina-infra-k8s API Gateway | Entrada pública (HTTP API) |
| 6 | oficina-api (opcional) | Redeploy para URL pública em e-mails |

No modo `aws_lbc`, inserir `oficina-infra-k8s addons` entre os passos 2 e 3.

## Configuração necessária

Configure no GitHub Actions:

| Nome | Tipo | Uso |
| --- | --- | --- |
| `AWS_ACCESS_KEY_ID` | Secret | Autenticação AWS |
| `AWS_SECRET_ACCESS_KEY` | Secret | Autenticação AWS |
| `AWS_SESSION_TOKEN` | Secret opcional | Credenciais temporárias |
| `AWS_REGION` | Secret | Região AWS |
| `ECR_REPOSITORY_URL` | Secret | URL completa do repositório ECR (ver abaixo) |
| `EKS_CLUSTER_NAME` | Secret | Nome do cluster EKS provisionado pelo `oficina-infra-k8s` |
| `DB_CONNECTION_STRING` | Secret | String de conexão com o SQL Server |
| `JWT_SECRET` | Secret | Chave de assinatura JWT (mínimo 32 caracteres) |
| `JWT_ISSUER` | Secret | Issuer JWT |
| `JWT_AUDIENCE` | Secret | Audience JWT |
| `JWT_EXPIRATION_MINUTES` | Secret | Tempo de expiração dos tokens em minutos |
| `ADMIN_INICIAL_NOME` | Secret opcional | Nome do admin inicial |
| `ADMIN_INICIAL_CPF` | Secret opcional | CPF do admin inicial |
| `ADMIN_INICIAL_SENHA` | Secret opcional | Senha do admin inicial |
| `EMAIL_SMTP_HOST` | Variable opcional | Servidor SMTP |
| `EMAIL_SMTP_PORT` | Variable opcional | Porta SMTP |
| `EMAIL_ENABLE_SSL` | Variable opcional | `true` ou `false` |
| `EMAIL_FROM` | Variable opcional | Endereço de origem dos e-mails |
| `EMAIL_SMTP_USERNAME` | Secret opcional | Usuário de autenticação SMTP |
| `EMAIL_SMTP_PASSWORD` | Secret opcional | Senha de autenticação SMTP |
| `EMAIL_BASE_URL_APROVA_RECUSA_ORCAMENTO` | Variable opcional | URL base para links de aprovação/recusa em e-mails |
| `PROJECT_NAME` | Variable opcional | Prefixo lógico; padrão `oficina` |
| `ENVIRONMENT` | Variable opcional | Ambiente; padrão `dev` |
| `LOAD_BALANCER_PROVISIONING_MODE` | Variable opcional | `terraform_nlb` padrão ou `aws_lbc` |
| `API_NODE_PORT` | Variable opcional | NodePort esperado; padrão `30080` |

**`JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE` e `JWT_EXPIRATION_MINUTES` devem ser idênticos aos usados no `oficina-auth-lambda`.**

**SMTP**: as quatro variáveis (`EMAIL_SMTP_HOST`, `EMAIL_SMTP_PORT`, `EMAIL_ENABLE_SSL`, `EMAIL_FROM`) devem ser configuradas em conjunto com os secrets de autenticação. Se qualquer uma estiver ausente, o workflow falha na validação.

Se `EMAIL_BASE_URL_APROVA_RECUSA_ORCAMENTO` não estiver configurado, o workflow tenta ler `/${PROJECT_NAME}/${ENVIRONMENT}/api/public-base-url` no SSM. Se o parâmetro ainda não existir, o deploy segue com valor vazio.

### Obtendo o ECR_REPOSITORY_URL

Após o deploy do `oficina-infra-k8s` core, obtenha a URL do repositório ECR:

```powershell
$env:AWS_REGION="<regiao>"
$env:ECR_REPOSITORY_NAME="oficina-api"  # ou o valor configurado em ECR_REPOSITORY_NAME

aws ecr describe-repositories --repository-names $env:ECR_REPOSITORY_NAME --region $env:AWS_REGION --query "repositories[0].repositoryUri" --output text
```

Configure o valor retornado como secret `ECR_REPOSITORY_URL` no GitHub Actions.

## Como executar

Execute manualmente:

```text
GitHub Actions > Deploy API > Run workflow
```

O input `enable_initial_admin` deve ser `true` apenas na criação controlada do admin inicial em banco vazio. Nas execuções seguintes, use `false`.

No modo `terraform_nlb`, o workflow:

- aplica `service-nodeport.yaml`;
- valida que `API_NODE_PORT` bate com a porta do Target Group criado pelo Terraform;
- valida Service `NodePort`;
- valida rollout;
- valida `/health` via `kubectl port-forward`;
- valida que `/${PROJECT_NAME}/${ENVIRONMENT}/api/backend-listener-arn` existe.

No modo `aws_lbc`, o workflow usa `service.yaml`, valida o NLB interno criado pelo controller e grava o Listener ARN no SSM.

## Como validar pela AWS

Console:

- Em ECR, confirme imagem com tag do commit e tag `latest`.
- Em EKS, confirme Deployment, Pods e Service no namespace `oficina`.
- No modo `terraform_nlb`, confirme Service `NodePort`.
- No modo `aws_lbc`, confirme Load Balancer interno do tipo network.
- Em SSM Parameter Store, confirme `/${PROJECT_NAME}/${ENVIRONMENT}/api/backend-listener-arn`.

CLI:

```powershell
$env:AWS_REGION="<regiao>"
$env:ENVIRONMENT="<ambiente>"
$env:PROJECT_NAME="oficina"
$env:EKS_CLUSTER_NAME="<nome-do-cluster>"
$env:ECR_REPOSITORY_NAME="<nome-do-repositorio-ecr>"

aws ecr describe-images --repository-name $env:ECR_REPOSITORY_NAME --image-ids imageTag="<commit-sha>" --region $env:AWS_REGION --query "length(imageDetails)"
aws eks update-kubeconfig --name $env:EKS_CLUSTER_NAME --region $env:AWS_REGION
kubectl rollout status deployment/oficina-api -n oficina
kubectl get svc oficina-api -n oficina -o jsonpath='{.spec.type}{" nodePort="}{.spec.ports[0].nodePort}{"\n"}'
aws ssm get-parameter --name "/$($env:PROJECT_NAME)/$($env:ENVIRONMENT)/api/backend-listener-arn" --region $env:AWS_REGION --query "Parameter.Name"
```

### Health check e Swagger via port-forward

O Swagger não é exposto pelo API Gateway — apenas `/health` e `/api/*` são roteados publicamente. Para acessar o Swagger ou testar endpoints diretamente no pod, use `kubectl port-forward`:

**1. Abra o túnel (mantenha este terminal aberto):**

```powershell
kubectl port-forward svc/oficina-api -n oficina 18080:80
```

**2. Em outro terminal, valide o health check:**

```powershell
Invoke-RestMethod http://127.0.0.1:18080/health
```

**3. No browser, acesse o Swagger:**

```
http://localhost:18080/swagger
```

> O túnel encerra quando o terminal for fechado. Enquanto estiver aberto, a porta `18080` local aponta diretamente para o pod no EKS, sem passar pelo API Gateway.

### Testes via Postman Runner

**Pré-requisito:** passo 5 (API Gateway) concluído para usar a URL pública. Para ambiente local ou port-forward, qualquer etapa serve.

**Arquivos:**
- Collection: `postman/OficinaAPI-cenarios.postman_collection.json`
- Environment: `postman/OficinaAPI-cenarios.postman_environment.json`

**Variáveis obrigatórias — configure apenas estas três antes de rodar:**

| Variável | Descrição |
|----------|-----------|
| `baseUrl` | URL base da API (ver abaixo) |
| `adminCpf` | CPF do admin inicial (`ADMIN_INICIAL_CPF`) |
| `adminSenha` | Senha do admin inicial (`ADMIN_INICIAL_SENHA`) |

> Todas as demais variáveis (tokens, IDs, CPFs de teste) são geradas e gerenciadas automaticamente pelos scripts da collection durante a execução.

**Como obter o `baseUrl` conforme o ambiente:**

```powershell
# AWS — URL pública do API Gateway (passo 5 concluído)
aws ssm get-parameter --name "/$($env:PROJECT_NAME)/$($env:ENVIRONMENT)/api/public-base-url" --region $env:AWS_REGION --query "Parameter.Value" --output text

# AWS — via port-forward (kubectl port-forward ativo na porta 18080)
# baseUrl = http://127.0.0.1:18080

# Local — Docker Compose
# baseUrl = http://localhost:8080
```

**Como executar via Runner:**

1. Abra o Postman e importe a collection e o environment acima
2. Selecione o environment importado e preencha as três variáveis obrigatórias
3. Clique com o botão direito na collection > **Run collection**
4. Confirme que todas as requisições estão selecionadas e clique em **Run**

## Como executar localmente

Crie o arquivo local de variáveis e suba os serviços:

```powershell
Copy-Item docker/.env.example docker/.env
docker compose --profile local-db --env-file docker/.env -f docker/docker-compose.yml up -d sqlserver smtp4dev
docker compose --profile local-db --env-file docker/.env -f docker/docker-compose.yml run --rm migration
docker compose --profile local-db --env-file docker/.env -f docker/docker-compose.yml up -d api
```

## Como validar localmente

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

## Próxima etapa

Publicar `oficina-auth-lambda`. Depois, aplicar o root `terraform/api-gateway` no `oficina-infra-k8s` e executar o roteiro funcional pós-deploy.
