# ProjectHub.Data

Camada de persistência: contém `ProjectHubDbContext`, configuração do PostgreSQL/pgvector e migrations. `ProjectHub.Application` não referencia esta camada; a API registra os serviços de Data na composição.

## Connection string de desenvolvimento

Configure credenciais locais com User Secrets (não versione credenciais reais):

```bash
dotnet user-secrets init --project src/ProjectHub.Api/ProjectHub.Api.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=projecthub;Username=<usuario>;Password=<senha>" --project src/ProjectHub.Api/ProjectHub.Api.csproj
```

Em outros ambientes, configure a variável `ConnectionStrings__DefaultConnection` no ambiente de execução. O valor em `appsettings.json` é apenas um placeholder local.

## Migrations

Criar uma migration (arquivos ficam neste projeto):

```bash
dotnet ef migrations add <NomeDaMigration> \
  --project src/ProjectHub.Data/ProjectHub.Data.csproj \
  --startup-project src/ProjectHub.Api/ProjectHub.Api.csproj \
  --output-dir Migrations
```

Aplicar migrations no banco configurado:

```bash
dotnet ef database update \
  --project src/ProjectHub.Data/ProjectHub.Data.csproj \
  --startup-project src/ProjectHub.Api/ProjectHub.Api.csproj
```

O servidor PostgreSQL deve ter pgvector instalado para que a migration possa habilitar a extensão `vector`. Migrations não são aplicadas automaticamente no startup da API.
