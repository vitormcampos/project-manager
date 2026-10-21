# ProjectHub.Api

A API disponibiliza autenticação Bearer/JWT, cadastro de tenants, convites e gestão de Accounts/Users. A autorização e o papel são consultados no banco em cada caso de uso; os papéis no corpo da requisição ou nos claims do cliente não concedem privilégios. Requests, responses e enums públicos ficam em `ProjectHub.Contracts`, compartilhado com o cliente Blazor.

## Configuração local

Configure os segredos com User Secrets (não os versione). A chave JWT deve ter no mínimo 32 bytes UTF-8:

```bash
dotnet user-secrets init --project src/ProjectHub.Api/ProjectHub.Api.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=projecthub;Username=<usuario>;Password=<senha>" --project src/ProjectHub.Api/ProjectHub.Api.csproj
dotnet user-secrets set "Jwt:SigningKey" "<chave-aleatoria-de-no-minimo-32-bytes>" --project src/ProjectHub.Api/ProjectHub.Api.csproj
```

Gere uma chave local com `openssl rand -base64 48`. Em outros ambientes, use um cofre de segredos/variáveis seguras, como `Jwt__SigningKey` e `ConnectionStrings__DefaultConnection`. O startup falha com mensagem segura se a connection string ou chave estiver ausente/inválida. A chave não é incluída em `appsettings.json`.

## Hash de senhas

A API usa `BCrypt.Net-Next` com fator de trabalho 12 e entropia aprimorada SHA-384. A pré-hash aprimorada evita o limite de 72 bytes do BCrypt sem truncar senhas longas. Hashes antigos do `PasswordHasher` do ASP.NET Identity não são compatíveis com BCrypt; a troca ocorre antes de haver uma migration de Users aplicada, então não há credenciais persistidas neste fluxo para migrar.

## Primeiro administrador da plataforma

Cadastro público sempre cria `Account.Role = User` e seu primeiro `User.Role = Admin`; não existe endpoint HTTP para conceder papel global. Para provisionar o primeiro administrador, configure estes valores via User Secrets (não passe senha na linha de comando):

```bash
dotnet user-secrets set "BootstrapPlatformAdmin:AccountName" "ProjectHub Platform" --project src/ProjectHub.Api/ProjectHub.Api.csproj
dotnet user-secrets set "BootstrapPlatformAdmin:FirstName" "Platform" --project src/ProjectHub.Api/ProjectHub.Api.csproj
dotnet user-secrets set "BootstrapPlatformAdmin:LastName" "Administrator" --project src/ProjectHub.Api/ProjectHub.Api.csproj
dotnet user-secrets set "BootstrapPlatformAdmin:Email" "<email-administrador>" --project src/ProjectHub.Api/ProjectHub.Api.csproj
dotnet user-secrets set "BootstrapPlatformAdmin:Password" "<senha-com-12-a-128-caracteres>" --project src/ProjectHub.Api/ProjectHub.Api.csproj
dotnet run --project src/ProjectHub.Api/ProjectHub.Api.csproj -- --bootstrap-platform-admin
dotnet user-secrets remove "BootstrapPlatformAdmin:Password" --project src/ProjectHub.Api/ProjectHub.Api.csproj
```

O comando é explícito, falha se já houver uma Account administrativa, não aplica migrations e não cria administradores automaticamente no startup. `Account.Role = Admin` concede acesso global a todos os Users dessa Account; restrinja cuidadosamente o acesso a esse comando.

## Convites por e-mail

Para enviar convites, configure `InvitationDelivery:Host`, `Port`, `Username`, `Password`, `FromEmail`, `FromName` e `WebBaseUrl` por User Secrets ou cofre/variáveis de ambiente (`InvitationDelivery__Host`, etc.). SMTP usa TLS. Convites duram 24 horas, são de uso único e somente o hash do token é persistido. O frontend deve disponibilizar a rota `/accept-invitation` em `WebBaseUrl` e chamar `POST /api/auth/invitations/accept` com o token e a senha definida pelo usuário.

## Endpoints

- `POST /api/auth/register` — cria Account convencional, proprietário e token.
- `POST /api/auth/login` — autentica por e-mail/senha; limitado a cinco tentativas por minuto por IP.
- `POST /api/auth/invitations/accept` — consome convite e ativa o usuário.
- `/api/accounts/{accountId}/users` — listagem, convite/revogação, alteração de papel e desativação; administradores de tenant só operam na própria Account.
- `/api/admin/accounts` — operações globais, disponíveis a usuários associados a uma Account administrativa.

A API não aplica migrations no startup. Configure a connection string e `Jwt:SigningKey` também ao executar comandos EF, conforme `src/ProjectHub.Data/README.md`.
