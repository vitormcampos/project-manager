# Contexto para agentes

## Visão geral

ProjectHub é uma aplicação de gestão de projetos e tarefas. A solução usa .NET 10, uma API ASP.NET Core, uma interface Blazor WebAssembly com MudBlazor e uma camada de persistência EF Core/PostgreSQL com suporte a pgvector. O repositório não tem `global.json`; os projetos declaram `net10.0`.

## Estrutura e arquitetura observada

- `ProjectHub.slnx`: seis projetos de produção em `src/` e três projetos de testes em `tests/`.
- `src/ProjectHub.Domain/`: entidades `Account`, `User`, `UserInvitation` e enums distintos `AccountRole`, `UserRole` e `UserStatus`. Regras de domínio permanecem independentes de EF Core.
- `src/ProjectHub.Contracts/`: DTOs e enums de transporte compartilhados por API e Blazor; não referencia outras camadas. Consulte `src/ProjectHub.Contracts/README.md`.
- `src/ProjectHub.Application/`: casos de uso de autenticação e gestão de Accounts/Users. Usa `DbContext`/EF Core diretamente, conforme a decisão do projeto; não há Repository. Referencia `Domain`, `Contracts` e EF Core.
- `src/ProjectHub.Data/`: `ProjectHubDbContext`, mapeamentos, registro de Npgsql/pgvector e migrations. Referencia `Application` e `Domain`; mantenha mudanças de schema aqui. Consulte `src/ProjectHub.Data/README.md`.
- `src/ProjectHub.Api/`: composição da API, autenticação JWT Bearer, hash de senhas, entrega SMTP de convites e endpoints de Auth/Accounts/Users. Exige `ConnectionStrings:DefaultConnection` e `Jwt:SigningKey`; consulte `src/ProjectHub.Api/README.md`. Expõe também `GET /health` e OpenAPI apenas em desenvolvimento.
- `src/ProjectHub.Web/`: cliente Blazor WebAssembly com páginas de dashboard, projetos e tarefas, componentes/layout e CSS. Usa MudBlazor e referencia `Contracts` para compartilhar DTOs; ainda não consome a API. `ProjectCatalog` e `TaskCatalog` mantêm dados estáticos em memória, não persistência.
- Dependências entre projetos: `Application → Contracts + Domain + EF Core`; `Data → Application + Domain + provider PostgreSQL`; `Api → Application + Contracts + Data`; `Web → Contracts`. `Application` não referencia `Data` nem Npgsql.
- `.specs/01-configuracao-api-efcore-postgresql-pgvector.md` e `.specs/02-autenticacao-accounts-users.md` são documentos de referência; confirme o estado atual pelo código e pelos testes.

## Fluxos de desenvolvimento

Requer .NET 10 SDK. Comandos de solução também aparecem em `.vscode/tasks.json`:

```bash
dotnet restore ProjectHub.slnx
dotnet build ProjectHub.slnx
dotnet publish ProjectHub.slnx
```

Executar cada aplicação:

```bash
dotnet run --project src/ProjectHub.Web/ProjectHub.Web.csproj
dotnet watch run --project src/ProjectHub.Api/ProjectHub.Api.csproj
```

A API não inicia sem `ConnectionStrings:DefaultConnection` e `Jwt:SigningKey`. Configure segredos locais por User Secrets conforme os READMEs de API/Data; não versione credenciais. O provisionamento global inicial é explícito via `--bootstrap-platform-admin`. Migrations não são aplicadas automaticamente no startup.

Os projetos `tests/ProjectHub.Domain.Tests`, `tests/ProjectHub.Application.Tests` e `tests/ProjectHub.Data.Integration.Tests` usam xUnit; execute `dotnet test ProjectHub.slnx`. Os testes de Application e Data.Integration usam Testcontainers com pgvector e requerem Docker. Não há lint nem pipeline de CI configurados. `.vscode/launch.json` contém perfis para Web, API e execução conjunta; API e perfil conjunto também dependem da connection string e da chave JWT.

## Convenções e cuidados

- Os projetos C# habilitam nullable reference types e implicit usings. Código observado usa nomes/identificadores em inglês e texto da interface em português (`pt-BR`); preserve esse padrão local.
- Mantenha alterações de schema e migrations em `ProjectHub.Data`. Não aplique migrations automaticamente no startup nem substitua o versionamento por `EnsureCreated`/`EnsureDeleted`; essas restrições também constam no README/spec de persistência.
- A configuração de pgvector está no `ProjectHubDbContext`/registro de `Data`; a migration inicial habilita a extensão e a migration `AccountsUsersAuthentication` cria as tabelas de identidade/tenant.
- Ao trabalhar com UI, lembre-se de que os catálogos atuais são estado estático em memória do cliente, não dados compartilhados ou duráveis.

## Skills locais

Leia o `SKILL.md` indicado quando a tarefa corresponder; estes arquivos pertencem ao projeto:

- `.agents/skills/blazor-expert/SKILL.md` — componentes, ciclo de vida, estado, formulários e navegação Blazor; consulte também os recursos específicos em `.agents/skills/blazor-expert/resources/` quando necessário.
- `.agents/skills/mudblazor/SKILL.md` — páginas e componentes MudBlazor. Atenção: o texto da skill descreve exemplos/versão 6.x de outro projeto; este repositório usa MudBlazor 8.15.0. Confirme APIs e exemplos contra a versão instalada e os componentes existentes.
- `.agents/skills/dotnet-best-practices/SKILL.md` — apoio para alterações C#/.NET, DI e qualidade. É uma orientação genérica; siga as dependências e convenções observadas neste repositório, incluindo xUnit nos testes.
