# Navegacao Segura Backend - Fase 4

API ASP.NET Core 8 com PostgreSQL para autenticacao do responsavel, vinculacao do dispositivo Android e sincronizacao dos registros coletados.

## Rodar localmente

```powershell
cd backend
copy .env.example .env
docker compose up --build
```

API: `http://localhost:8080`

Swagger fica disponivel em ambiente `Development`: `http://localhost:8080/swagger`.

## Rodar sem Docker

```powershell
cd backend
dotnet restore
dotnet ef database update --project src/SafeNavigation.Infrastructure --startup-project src/SafeNavigation.Api
dotnet run --project src/SafeNavigation.Api
```

## Testes

```powershell
cd backend
dotnet test SafeNavigation.sln
```

## Deploy no Render

O repositorio inclui `render.yaml` para criar um Web Service Docker e um Render Postgres.

No Render:

1. Crie um Blueprint apontando para este repositorio.
2. Use o arquivo `render.yaml` na raiz.
3. O servico web usa `src/SafeNavigation.Api/Dockerfile`.
4. A variavel `DATABASE_URL` vem do banco gerenciado do Render.
5. `Jwt__SigningKey` e gerada automaticamente pelo Blueprint.
6. `Database__AutoMigrate=true` aplica migrations na inicializacao.

Health check: `/health`.

Tambem e possivel criar o Web Service manualmente:

- Runtime: Docker.
- Dockerfile path: `src/SafeNavigation.Api/Dockerfile`.
- Porta: `10000`.
- Variaveis: `ASPNETCORE_URLS=http://+:10000`, `DATABASE_URL`, `Jwt__SigningKey` e `Database__AutoMigrate=true`.

## Escopo implementado

- Cadastro, login, refresh e logout de responsavel.
- Tokens JWT separados por ator: `guardian` e `device`.
- Refresh tokens opacos com hash SHA-256 no banco e revogacao por rotacao.
- Codigo temporario de pareamento com hash no banco.
- Vinculacao de dispositivo Android e emissao de token de dispositivo.
- Listagem de dispositivos, leitura e atualizacao de configuracao.
- Sincronizacao idempotente por `deviceId + clientBatchId`.
- CRUD de regras, alertas, pedidos de desbloqueio e endpoints de privacidade.
- Alertas gerados durante sync para categorias sensiveis e tentativas bloqueadas.
- Rate limiting global, auditoria inicial e validacao via FluentValidation.

## Segurança e privacidade

- Senhas usam BCrypt.
- Tokens e codigos de pareamento nao sao persistidos em claro.
- A API nao registra payload sensivel nos logs por implementacao propria.
- O banco esta preparado com tabelas de auditoria, sync e retencao configuravel por dispositivo.
