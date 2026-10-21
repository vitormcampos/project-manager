# Executar com Docker Compose

O Compose sobe a API, o cliente Blazor WebAssembly servido pelo Nginx e um PostgreSQL com pgvector. O banco usa um volume persistente. As imagens da API e do cliente são multi-stage: compiladas com o SDK .NET 10 e executadas, respectivamente, no runtime ASP.NET e no Nginx sem privilégios.

## Iniciar localmente

```bash
cp .env.example .env
```

Edite `.env` e substitua `JWT_SIGNING_KEY` por uma chave aleatória com pelo menos 32 bytes (por exemplo, gere uma com `openssl rand -base64 48`). Os valores do exemplo são apenas para desenvolvimento local; não use-os em ambientes compartilhados ou de produção. Não versione `.env`.

```bash
docker compose up --build -d
```

- Web: <http://localhost:8081>
- API e verificação de disponibilidade: <http://localhost:8080/health>
- PostgreSQL: porta local `5432` (configurável por `POSTGRES_PORT`)

A interface atual ainda usa catálogos estáticos no navegador e não consome a API. Os serviços estão expostos separadamente para desenvolvimento.

## Banco de dados

O PostgreSQL é iniciado com pgvector e a API aguarda o banco ficar disponível, mas **as migrations não são aplicadas automaticamente**. Depois de subir os serviços, aplique-as a partir de uma máquina com o SDK .NET e a ferramenta `dotnet-ef` instalados:

```bash
set -a
. ./.env
set +a
ConnectionStrings__DefaultConnection="Host=localhost;Port=${POSTGRES_PORT:-5432};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}" \
Jwt__SigningKey="$JWT_SIGNING_KEY" \
dotnet ef database update \
  --project src/ProjectHub.Data/ProjectHub.Data.csproj \
  --startup-project src/ProjectHub.Api/ProjectHub.Api.csproj
```

Para interromper os serviços sem apagar os dados, execute `docker compose down`. Para apagar também o banco persistido, use `docker compose down -v`.

## Configuração

As portas publicadas podem ser alteradas em `.env` (`API_PORT`, `WEB_PORT` e `POSTGRES_PORT`). As configurações de banco e JWT da API são montadas pelo Compose a partir de `POSTGRES_*` e `JWT_SIGNING_KEY`. Configure SMTP e demais segredos necessários à aplicação por variáveis de ambiente no serviço `api`; não os adicione ao repositório.
