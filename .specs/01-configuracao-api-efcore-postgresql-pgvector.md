---
nome: 01 - Configuração da API com EF Core, PostgreSQL e pgvector
tags:
  - spec
  - desenvolvimento
  - api
  - persistencia
criado: 2026-10-20
status: rascunho
---
# 01 - Configuração da API com EF Core, PostgreSQL e pgvector

## Objetivo Técnico
Estabelecer as camadas `Application` e `Data` e configurar a persistência da solução com Entity Framework Core, PostgreSQL, Npgsql e pgvector. `Application` conterá casos de uso e poderá usar EF Core diretamente via `DbContext`, sem Repository; `Data` conterá o contexto concreto, mapeamentos, provider e migrations. A API fará a composição das dependências. A configuração deve ser segura, inicializável por ambiente e preparada para evoluir com migrations.

## Visão do Usuário
Esta configuração é infraestrutura interna e não adiciona, por si só, novas telas ou endpoints de negócio. Ela permite que as funcionalidades da API persistam e consultem dados no PostgreSQL, incluindo futuramente vetores para busca semântica.

## Cenários de Sucesso
- [ ] A API registra os serviços de persistência da camada `Data` e resolve casos de uso da camada `Application` usando a connection string configurada para o ambiente.
- [ ] A API consegue abrir conexão com uma instância PostgreSQL compatível.
- [ ] O modelo do EF Core reconhece a extensão PostgreSQL `vector` e oferece suporte ao tipo `Vector` do pgvector.
- [ ] Migrations podem ser criadas e aplicadas para manter o schema do banco versionado.
- [ ] A configuração funciona sem credenciais de banco versionadas no repositório.

## Cenários de Erro
- [ ] Connection string ausente ou inválida: a inicialização ou conexão falha com mensagem clara, sem expor senha ou outros segredos.
- [ ] PostgreSQL indisponível: a API não mascara a falha de conexão nem informa falsamente que o banco está disponível.
- [ ] Extensão pgvector ausente no servidor: a migration/configuração que depende dela falha de forma explícita, com orientação para habilitar a extensão.
- [ ] Migration incompatível com o schema atual: a aplicação informa a falha sem descartar ou recriar dados automaticamente.

## Esquema de Banco de Dados
A spec não define tabelas de negócio. Configurar o modelo para declarar a extensão pgvector e deixar a criação/alteração do schema sob controle de migrations.

```sql
-- Habilitar no banco alvo (via migration ou provisionamento, conforme suporte do ambiente):
CREATE EXTENSION IF NOT EXISTS vector;

-- Não criar tabelas ou índices de negócio nesta etapa.
```

## Testes Esperados
- [ ] Testes de registro/resolução dos serviços de Application e Data com configuração válida.
- [ ] Teste de validação quando a connection string não estiver configurada, garantindo mensagem segura.
- [ ] Teste de integração contra PostgreSQL com pgvector para verificar conexão e aplicação das migrations.
- [ ] Verificar que a migration habilita/dependa da extensão `vector` conforme a estratégia adotada.

## O Que Deve Ser Feito
- [ ] Criar os projetos `ProjectHub.Application` e `ProjectHub.Data` na solução.
- [ ] Definir dependências direcionadas: `Application → Domain + EF Core`; `Data → Application + Domain + provider PostgreSQL`; `Api → Application + Data`. `Application` usa `DbContext` diretamente e não depende de `Data`, Npgsql ou de detalhes específicos do provider.
- [ ] Manter casos de uso em `Application`; não criar Repository. Mapeamentos, provider e migrations continuam em `Data`.
- [ ] Adicionar ao projeto `Data` os pacotes compatíveis de EF Core, `Npgsql.EntityFrameworkCore.PostgreSQL` e integração pgvector (`Pgvector.EntityFrameworkCore`), alinhando as versões ao .NET 10 e entre si.
- [ ] Criar o `DbContext` e os mapeamentos de persistência em `Data`, sem colocar regras de persistência dentro das entidades de domínio.
- [ ] Registrar `DbContext`, implementações de persistência e casos de uso no container de DI a partir da API, usando `ConnectionStrings:DefaultConnection`.
- [ ] Configurar o suporte pgvector no EF Core (mapeamento do tipo `Vector` e declaração da extensão PostgreSQL `vector`) em `Data`.
- [ ] Definir como a extensão será habilitada por migration/provisionamento e documentar a pré-condição de PostgreSQL com pgvector instalado.
- [ ] Manter e executar migrations no projeto `Data`; documentar os comandos de criação/aplicação e como selecionar `Api` como startup project.
- [ ] Manter apenas um valor de exemplo não sensível em `appsettings.json`; obter credenciais reais por User Secrets em desenvolvimento ou variáveis de ambiente em implantação.
- [ ] Garantir que falhas de configuração/conexão não exponham credenciais nos logs.

## O Que Não Deve Ser Feito
- [ ] Não versionar credenciais ou connection strings com senhas reais.
- [ ] Não executar `EnsureCreated`/`EnsureDeleted` automaticamente no startup; usar migrations para evolução do schema.
- [ ] Não criar tabelas, índices vetoriais ou lógica de busca semântica nesta especificação.
- [ ] Não adicionar dependência do EF Core ou Npgsql ao `Domain`, nem Npgsql/detalhes do PostgreSQL à `Application`; o uso de EF Core em `Application` é permitido e previsto pela spec de autenticação/Accounts/Users.
- [ ] Não colocar `DbContext` ou migrations nos projetos `Api` ou `Application`.
- [ ] Não aplicar migrations automaticamente em produção sem uma decisão explícita de implantação.
