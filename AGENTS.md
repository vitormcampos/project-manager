# Contexto para agentes

## Visão geral

ProjectHub é uma aplicação de gestão de projetos e tarefas. A solução usa .NET 10, uma API ASP.NET Core, uma interface Blazor WebAssembly com MudBlazor e uma camada de persistência EF Core/PostgreSQL com suporte a pgvector. O repositório não tem `global.json`; os projetos declaram `net10.0`.

## Estrutura e arquitetura observada

- `ProjectHub.slnx`: solução com os cinco projetos em `src/`.
- `src/ProjectHub.Domain/`: modelos de domínio `Account`, `User` e enum `Role`; invariantes e operações do domínio ficam nos modelos. Não referencia outros projetos.
- `src/ProjectHub.Application/`: atualmente contém apenas o `.csproj`; referencia `Domain`, mas ainda não há casos de uso ou abstrações implementados.
- `src/ProjectHub.Data/`: `ProjectHubDbContext`, registro de EF Core/Npgsql/pgvector e migrations. Referencia `Application` e `Domain`; as migrations ficam aqui. Consulte `src/ProjectHub.Data/README.md` para configuração local e comandos de migration.
- `src/ProjectHub.Api/`: composição da API e dos serviços de `Data`; atualmente expõe `GET /health` e OpenAPI apenas em desenvolvimento. A inicialização exige `ConnectionStrings:DefaultConnection` e falha claramente se ela não estiver configurada.
- `src/ProjectHub.Web/`: cliente independente Blazor WebAssembly com páginas de dashboard, projetos e tarefas, componentes/layout e CSS. Usa MudBlazor. Hoje `ProjectCatalog` e `TaskCatalog` mantêm dados estáticos em memória e as páginas não consomem a API; não trate essas listas como persistência nem presuma integração Web–API já existente.
- Dependências entre projetos observadas: `Application → Domain`, `Data → Application + Domain`, `Api → Application + Data`; `Web` não tem referências a outros projetos da solução.
- `.specs/01-configuracao-api-efcore-postgresql-pgvector.md` está marcado como **rascunho**. Ele descreve direção pretendida para as camadas e requisitos de persistência; confirme o estado no código antes de considerar itens da spec implementados.

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

A API não inicia sem `ConnectionStrings:DefaultConnection`. Configure credenciais locais via User Secrets, conforme `src/ProjectHub.Data/README.md`; não versione segredos. Esse README também contém os comandos verificados para criar/aplicar migrations usando `ProjectHub.Data` como projeto e `ProjectHub.Api` como startup. Migrations não são aplicadas automaticamente no startup.

Não há projetos de teste, configuração de lint nem pipeline de CI no repositório atualmente; não invente um framework ou fluxo de validação existente. `.vscode/launch.json` contém perfis para Web, API e execução conjunta; o perfil conjunto também depende da connection string para a API.

## Convenções e cuidados

- Os projetos C# habilitam nullable reference types e implicit usings. Código observado usa nomes/identificadores em inglês e texto da interface em português (`pt-BR`); preserve esse padrão local.
- Mantenha alterações de schema e migrations em `ProjectHub.Data`. Não aplique migrations automaticamente no startup nem substitua o versionamento por `EnsureCreated`/`EnsureDeleted`; essas restrições também constam no README/spec de persistência.
- A configuração de pgvector está no `ProjectHubDbContext`/registro de `Data`; a migration inicial habilita a extensão, mas ainda não cria tabelas de negócio.
- Ao trabalhar com UI, lembre-se de que os catálogos atuais são estado estático em memória do cliente, não dados compartilhados ou duráveis.

## Skills locais

Leia o `SKILL.md` indicado quando a tarefa corresponder; estes arquivos pertencem ao projeto:

- `.agents/skills/blazor-expert/SKILL.md` — componentes, ciclo de vida, estado, formulários e navegação Blazor; consulte também os recursos específicos em `.agents/skills/blazor-expert/resources/` quando necessário.
- `.agents/skills/mudblazor/SKILL.md` — páginas e componentes MudBlazor. Atenção: o texto da skill descreve exemplos/versão 6.x de outro projeto; este repositório usa MudBlazor 8.15.0. Confirme APIs e exemplos contra a versão instalada e os componentes existentes.
- `.agents/skills/dotnet-best-practices/SKILL.md` — apoio para alterações C#/.NET, DI e qualidade. É uma orientação genérica, não evidência de que práticas/ferramentas nela citadas (por exemplo Semantic Kernel ou MSTest) façam parte desta solução; siga as dependências e convenções observadas aqui.
