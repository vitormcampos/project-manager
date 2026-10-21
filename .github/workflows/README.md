# Publicação das imagens Docker

O workflow `docker-publish.yml` executa os testes da solução e, se forem aprovados, publica as imagens da API e da aplicação Web Blazor WebAssembly com as tags `latest` e o SHA do commit.

Configure no repositório GitHub:

## Actions → Variables

- `REGISTRY_HOST`: hostname do registry, sem protocolo nem barra final. Para Azure Container Registry, use `<nome-do-acr>.azurecr.io`.
- `API_IMAGE_NAME` (opcional): nome do repositório da imagem da API; por padrão usa `IMAGE_NAME` (configuração antiga) ou `projecthub-api`.
- `WEB_IMAGE_NAME` (opcional): nome do repositório da imagem Web; padrão `projecthub-web`.

## Actions → Secrets

- `REGISTRY_USERNAME`: usuário do registry.
- `REGISTRY_PASSWORD`: senha/token do usuário.

Para ACR, crie uma identidade de service principal com permissão de push no registry e configure o client ID como `REGISTRY_USERNAME` e o segredo como `REGISTRY_PASSWORD`. O workflow usa `docker/login-action`, então também funciona com outros registries compatíveis com login Docker. Mantenha as credenciais apenas nos GitHub Actions Secrets.
