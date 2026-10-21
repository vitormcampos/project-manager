# ProjectHub.Data

Camada de persistência: contém `ProjectHubDbContext`, configuração do PostgreSQL/pgvector e migrations. `ProjectHub.Application` não referencia esta camada; a API registra os serviços de Data na composição.

## Connection string de desenvolvimento

Configure connection string e chave JWT localmente com User Secrets (não versione credenciais reais). Veja `src/ProjectHub.Api/README.md` para a configuração completa de autenticação e o provisionamento explícito do primeiro administrador:

```bash
dotnet user-secrets init --project src/ProjectHub.Api/ProjectHub.Api.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=projecthub;Username=<usuario>;Password=<senha>" --project src/ProjectHub.Api/ProjectHub.Api.csproj
dotnet user-secrets set "Jwt:SigningKey" "<chave-aleatoria-com-pelo-menos-32-bytes>" --project src/ProjectHub.Api/ProjectHub.Api.csproj
```

Em outros ambientes, configure `ConnectionStrings__DefaultConnection` e `Jwt__SigningKey` em um cofre/variáveis seguras. Não há connection string nem chave de assinatura versionadas em `appsettings.json`.

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

## Testes PostgreSQL

A suíte usa xUnit. `tests/ProjectHub.Application.Tests` e `tests/ProjectHub.Data.Integration.Tests` usam Testcontainers com PostgreSQL e pgvector (`pgvector/pgvector:pg16`). Os testes de Application usam o `ProjectHubDbContext` de produção, aplicam as migrations e limpam as tabelas entre testes; os testes de Data verificam migrations, unicidade global de e-mail e integridade da FK de `User.AccountId`. Docker é necessário para ambos os projetos e para baixar a imagem na primeira execução.

Execute cada projeto ou a suíte completa:

```bash
dotnet test tests/ProjectHub.Application.Tests/ProjectHub.Application.Tests.csproj
dotnet test tests/ProjectHub.Data.Integration.Tests/ProjectHub.Data.Integration.Tests.csproj
dotnet test ProjectHub.slnx
```

A solução também inclui testes de domínio, que não dependem de PostgreSQL.
