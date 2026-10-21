---
nome: 02 - Autenticação, Accounts e Users multi-tenant
tags:
  - spec
  - desenvolvimento
  - api
  - autenticacao
  - ddd
  - multitenancy
criado: 2026-10-20
status: em validação
---
# 02 - Autenticação, Accounts e Users multi-tenant

## Objetivo Técnico
Definir o backend para autenticação e gestão de `Accounts` e `Users`, com isolamento multi-tenant e autorização em dois níveis. O domínio deve expressar as regras de negócio sem dependência de EF Core ou ASP.NET Core; os casos de uso da camada `Application` usarão EF Core diretamente por meio de `DbContext`, sem Repository. A implementação deve seguir TDD, cobrindo caminhos válidos e inválidos tanto no domínio quanto na aplicação.

Esta especificação altera a semântica atual: `Account` deixa de representar os dados pessoais de uma pessoa e passa a representar o agrupador/tenant; os dados pessoais e as credenciais pertencem a `User`. A implementação deve ser compatível com .NET 10 e PostgreSQL já adotados pela solução.

### Estado da implementação
O backend de domínio, casos de uso, API, persistência e testes locais está implementado. A suíte PostgreSQL usa Testcontainers com pgvector e cobre migrations, unicidade de e-mail e a FK de `User.AccountId`; permanecem pendentes testes de integração para concorrência e testes explícitos de não exposição de segredos.

### Premissas de domínio e autorização
- `Account` é o agrupador multi-tenant. Um `User` pertence a exatamente uma `Account`.
- `Account.Role` e `User.Role` são conceitos distintos, com enums distintos; não reutilizar o enum `Role` atual para ambos.
- `Account.Role = Admin` identifica uma Account administrativa da plataforma: **todos os Users associados a ela têm autorização global para administrar o sistema**, independentemente do seu `User.Role`.
- `Account.Role = User` identifica uma Account convencional/tenant. Nela, `User.Role = Admin` identifica o proprietário e administrador daquela Account, responsável por gerir seus Users; `User.Role = User` identifica um membro convencional.
- A autorização global não pode ser concedida por cadastro público nem por um valor enviado pelo cliente. A criação ou alteração de uma Account para `Role = Admin` é operação de provisionamento confiável, fora dos casos de uso comuns de tenant.
- Cadastro convencional cria `Account.Role = User` e seu primeiro `User` com `User.Role = Admin`. Uma Account convencional deve sempre manter pelo menos um User ativo com papel Admin.
- O e-mail é único globalmente e normalizado para autenticação sem distinção entre maiúsculas/minúsculas. Um User não pode pertencer a várias Accounts nesta versão.

## Visão do Usuário
Uma pessoa pode criar uma Account convencional e torna-se seu primeiro administrador. O administrador dessa Account pode convidar e gerir os Users da própria organização. Os membros autenticam-se com e-mail e senha e só podem atuar no escopo permitido pela Account e pelo seu papel. Usuários ligados a uma Account administrativa da plataforma podem gerir Accounts e Users em todo o sistema.

## Cenários de Sucesso
### Domain
- [ ] Criar Account convencional válida e Users com papéis distintos (`Admin` ou `User`), cada qual associado a uma Account.
- [ ] Account recém-criada passa a ter um proprietário/Admin ativo como primeiro User.
- [ ] Um Admin de Account convencional pode gerir membros dessa Account; um User convencional não recebe automaticamente privilégios administrativos.
- [ ] User associado a Account com `Account.Role = Admin` é reconhecido como administrador global, conforme a regra de autorização acima.
- [ ] Alterar nome, papel ou estado de um User ativo mantém as invariantes do domínio; uma Account convencional continua com pelo menos um Admin ativo.

### Application
- [ ] Registrar Account convencional e seu primeiro User Admin em uma única operação atômica.
- [ ] Autenticar credenciais válidas e emitir token de acesso contendo identificadores de sujeito e Account.
- [ ] Admin de Account convencional convida um User para a própria Account; o convidado ativa a conta definindo sua senha por fluxo de convite de uso único.
- [ ] Admin gere Users da própria Account; um membro só consulta/atualiza os dados que lhe forem permitidos e não administra outros Users.
- [ ] User de Account administrativa pode executar operações globais sobre Accounts e Users.
- [ ] Desativar User ou Account impede novas autenticações e acessos protegidos.

## Cenários de Erro
### Domain
- [ ] Nome obrigatório ausente, e-mail inválido, papel não definido ou associação sem Account: rejeitar sem criar estado inválido.
- [ ] Tentar remover, desativar ou rebaixar o último Admin ativo de uma Account convencional: rejeitar e preservar o estado.
- [ ] Tentar alterar entidade desativada ou usar convite expirado, revogado ou já consumido: rejeitar.
- [ ] Tentar atribuir papel de Account inválido ou promover Account convencional a Admin por operação de domínio comum: rejeitar.

### Application
- [ ] E-mail já cadastrado, incluindo variação apenas de caixa/espaços: rejeitar sem duplicar User.
- [ ] Login com e-mail inexistente, senha incorreta, User pendente/desativado ou Account desativada: negar autenticação com resposta genérica que não revele se o e-mail existe.
- [ ] User comum tentar gerir membros, acessar outra Account ou executar ação global: negar, sem ler ou alterar dados de outro tenant.
- [ ] Admin de Account convencional tentar atuar fora da própria Account, atribuir papel global ou alterar `Account.Role`: negar.
- [ ] Dados do cliente tentarem substituir `AccountId`, papel ou identidade autenticada para obter mais privilégios: ignorar valores não confiáveis ou rejeitar a requisição; nunca autorizar com base apenas neles.
- [ ] Token de convite inválido, expirado, revogado ou reutilizado; convite para e-mail já cadastrado: rejeitar sem ativar o User.
- [ ] Concorrência ao rebaixar/desativar Admins não pode deixar Account convencional sem Admin ativo; conflitos devem ser detectados e reportados sem sucesso falso.
- [ ] Falhas de persistência não deixam Account sem User inicial, convite parcialmente consumido ou alteração parcialmente aplicada; erros não expõem senha, hash, token ou segredo.

## Esquema de Banco de Dados
A estrutura abaixo é conceitual; os nomes finais devem seguir as convenções de migrations do projeto. Criar/alterar schema somente em `ProjectHub.Data` via migrations.

```sql
CREATE TABLE Accounts (
    Id UUID PRIMARY KEY,
    Name VARCHAR(200) NOT NULL,
    Role VARCHAR(20) NOT NULL, -- Admin | User
    CreatedAt TIMESTAMPTZ NOT NULL,
    UpdatedAt TIMESTAMPTZ NULL,
    DeactivatedAt TIMESTAMPTZ NULL
);

CREATE TABLE Users (
    Id UUID PRIMARY KEY,
    AccountId UUID NOT NULL REFERENCES Accounts(Id),
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Email VARCHAR(320) NOT NULL,
    NormalizedEmail VARCHAR(320) NOT NULL,
    PasswordHash TEXT NULL, -- NULL enquanto o convite não for aceito
    Role VARCHAR(20) NOT NULL, -- Admin | User (escopo da Account)
    Status VARCHAR(20) NOT NULL, -- Pending | Active | Disabled
    CreatedAt TIMESTAMPTZ NOT NULL,
    UpdatedAt TIMESTAMPTZ NULL,
    DeactivatedAt TIMESTAMPTZ NULL
);
CREATE UNIQUE INDEX UX_Users_NormalizedEmail ON Users(NormalizedEmail);
CREATE INDEX IX_Users_AccountId_Status ON Users(AccountId, Status);

CREATE TABLE UserInvitations (
    Id UUID PRIMARY KEY,
    UserId UUID NOT NULL REFERENCES Users(Id),
    TokenHash TEXT NOT NULL,
    ExpiresAt TIMESTAMPTZ NOT NULL,
    AcceptedAt TIMESTAMPTZ NULL,
    RevokedAt TIMESTAMPTZ NULL,
    CreatedAt TIMESTAMPTZ NOT NULL
);
CREATE UNIQUE INDEX UX_UserInvitations_TokenHash ON UserInvitations(TokenHash);
```

- Armazenar apenas hash do token de convite; token em texto claro só pode ser disponibilizado uma vez ao mecanismo de entrega. Senhas nunca são persistidas em texto claro.
- `NormalizedEmail` deve ser calculado de forma consistente e a unicidade deve ser garantida também pelo banco, não apenas por uma consulta prévia.
- Usar desativação lógica para Account/User; consultas de autenticação e autorização devem excluir entidades desativadas. Definir o tratamento de dados históricos sem cascade delete destrutivo.
- A migration deve migrar ou recriar cuidadosamente o modelo atual de `Account`/`User`; não descartar dados existentes sem decisão explícita e estratégia de migração.

## Testes Esperados
- [ ] Seguir TDD: escrever primeiro o teste que falha, implementar o mínimo para fazê-lo passar, refatorar e manter a suíte verde; repetir em cada regra/caso de uso.
- [ ] Testes unitários de Domain para criação válida, validações, papéis distintos, ciclo de vida, último Admin ativo e convites de uso único; cobrir happy path e bad path.
- [ ] Testes de Application para cadastro atômico, autenticação, convites, gestão de membros, autorização por tenant e papel, e falhas de validação/persistência; cada caso de uso deve cobrir sucesso e negações relevantes.
- [x] Testes de aplicação com EF Core relacional leve ou PostgreSQL de teste para verificar consultas, transações e isolamento; não usar mocks de Repository, pois Repository não faz parte da arquitetura.
- [ ] Testes de integração com PostgreSQL para migrations, índice único de e-mail, chaves estrangeiras e comportamento de concorrência relevante.
- [x] Testes de autorização garantindo que `Account.Role = Admin` concede acesso global a todos os seus Users e que `Account.Role = User` nunca concede escopo global, mesmo para `User.Role = Admin`.
- [ ] Testes para confirmar que respostas/logs não expõem senha, hash, token de convite ou segredo de assinatura.
- [x] Criar projetos de testes necessários, pois a solução ainda não tem projetos de teste; escolher e documentar o framework ao iniciar a implementação, sem presumir ferramenta já configurada.

## O Que Deve Ser Feito
- [x] Modelar `Account` como tenant e `User` como membro/autenticação; introduzir `AccountRole` e `UserRole` separados e expressar invariantes no Domain, sem dependência de EF Core.
- [x] Definir as operações de domínio para cadastro, gestão de Users, ativação/desativação e papéis; retornar/lançar erros de domínio previsíveis e testáveis.
- [x] Implementar casos de uso na `Application` para cadastro, autenticação, convites/ativação, consulta e gestão de Accounts/Users, verificando autorização e escopo tenant em cada operação.
- [x] Adicionar referência explícita a `Microsoft.EntityFrameworkCore` em `ProjectHub.Application`; injetar `DbContext` e usar `Set<TEntity>()`, consultas EF e `SaveChanges` diretamente nos casos de uso, com transações quando a operação envolver mais de uma gravação.
- [x] Não criar `IRepository`, repositórios genéricos, implementações de Repository ou abstrações que apenas repliquem `DbSet`/`DbContext`.
- [x] Manter `ProjectHubDbContext`, configurações EF, provider PostgreSQL e migrations em `ProjectHub.Data`; registrar o contexto concreto também como `DbContext` para consumo da Application, sem fazer Application referenciar Data nem provider Npgsql.
- [x] Manter a composição de dependências na API (`Api → Application + Data`); não introduzir dependência `Application → Data`.
- [x] Implementar autenticação Bearer com JWT assinado e configuração segura por ambiente. O token deve identificar `UserId` e `AccountId`; decisões de autorização devem validar estado e papéis atuais da identidade no servidor, não confiar em `AccountId`/papéis enviados pelo cliente nem depender exclusivamente de claims potencialmente obsoletas.
- [x] Usar implementação consolidada de hashing adaptativo de senha; não implementar criptografia/hash de senha próprio. Comparar credenciais sem revelar se falhou o e-mail ou a senha e aplicar proteção contra tentativas abusivas no endpoint de login.
- [x] Implementar convite com token aleatório de uso único, armazenado como hash, expiração, revogação e ativação definindo a senha. Entrega de convite deve ser uma dependência substituível; não registrar tokens/senhas em logs.
- [x] Garantir isolamento por `AccountId` em consultas e comandos; somente papéis administrativos podem alterar usuários, e Admin de tenant convencional não pode operar em outra Account.
- [x] Versionar todas as mudanças de schema em migrations de `ProjectHub.Data`; manter migrations fora do startup automático.

## O Que Não Deve Ser Feito
- [x] Não usar um único enum `Role` para representar papel global de Account e papel local de User.
- [x] Não confundir `Account.Role = Admin` (global, herdado por todos os Users daquela Account) com `User.Role = Admin` (admin/proprietário local de Account convencional).
- [x] Não permitir que cadastro público, requisição HTTP ou User Admin de tenant atribua `Account.Role = Admin` ou privilégios globais.
- [x] Não armazenar ou transmitir senhas em texto claro, persistir tokens de convite sem hash ou incluir credenciais/segredos em logs e respostas.
- [x] Não permitir acesso a dados de outro tenant por ausência de filtro, `AccountId` controlado pelo cliente ou claims sem validação server-side.
- [x] Não adicionar EF Core, Npgsql ou detalhes de persistência ao Domain; não colocar contexto, provider ou migrations na API/Application.
- [x] Não criar Repository nem abstração de persistência que esconda EF Core dos casos de uso, conforme a decisão explícita desta spec.
- [x] Não aplicar migrations automaticamente no startup, usar `EnsureCreated`/`EnsureDeleted`, nem apagar dados existentes sem estratégia aprovada.
