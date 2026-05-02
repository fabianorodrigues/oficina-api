# Desenvolvimento Local

Este roteiro valida a Oficina API da Fase 3 com Docker Compose, SQL Server e smtp4dev.

## 1. Preparar ambiente

Pre-requisitos:

- Docker Desktop em execucao.
- .NET SDK 9.0.313 para comandos `dotnet` locais.

Crie o arquivo local de variaveis:

```powershell
Copy-Item docker/.env.example docker/.env
```

Revise `docker/.env` se precisar mudar portas, senha do SQL Server, JWT ou admin inicial.

## 2. Subir a stack

```powershell
docker compose --env-file docker/.env -f docker/docker-compose.yml up --build
```

Servicos publicados:

| Servico | URL |
|---|---|
| API | `http://localhost:8080` |
| Swagger | `http://localhost:8080/swagger` |
| Healthcheck | `http://localhost:8080/health` |
| smtp4dev | `http://localhost:5000` |

## 3. Validar healthcheck

```powershell
Invoke-RestMethod http://localhost:8080/health
```

Resposta esperada:

```json
{
  "status": "Healthy"
}
```

## 4. Validar Swagger

Acesse:

```text
http://localhost:8080/swagger
```

Confirme que a rota publica de autenticacao exibida e:

```text
POST /api/auth/cpf
```

## 5. Autenticar admin local

As credenciais padrao ficam em `docker/.env`.

```powershell
$body = @{
  cpf = "39053344705"
  senha = "Senha@123"
} | ConvertTo-Json

Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:8080/api/auth/cpf `
  -ContentType "application/json" `
  -Body $body
```

A resposta deve conter:

- `accessToken`;
- `expiresIn`;
- `perfil`;
- `clienteId`;
- `funcionarioId`.

## 6. Validar e-mail local

Fluxos que geram orcamento enviam e-mail para o cliente quando existe e-mail cadastrado.

Depois de executar um fluxo de OS preventiva ou diagnostico corretivo, acesse:

```text
http://localhost:5000
```

Abra o e-mail capturado e valide os links:

```text
http://localhost:8080/api/orcamentos/acoes-externas/aprovar?token=...
http://localhost:8080/api/orcamentos/acoes-externas/recusar?token=...
```

Se o SMTP estiver indisponivel, a API registra erro em log e preserva a acao principal ja gravada.

## 7. Postman

Importe:

```text
postman/OficinaAPI-cenarios.postman_collection.json
postman/OficinaAPI-cenarios.postman_environment.json
postman/OficinaAPI-seguranca.postman_collection.json
```

Confirme:

```text
baseUrl=http://localhost:8080
```

Execute a collection sequencial para criar dados, autenticar usuarios e preencher IDs/tokens automaticamente.

## 8. Testes locais

```powershell
dotnet test Oficina.sln
```

Para cobertura:

```powershell
dotnet test Oficina.sln --collect:"XPlat Code Coverage"
```

## 9. Encerrar ambiente

```powershell
docker compose --env-file docker/.env -f docker/docker-compose.yml down
```

Para remover volumes locais de banco e smtp4dev:

```powershell
docker compose --env-file docker/.env -f docker/docker-compose.yml down -v
```
