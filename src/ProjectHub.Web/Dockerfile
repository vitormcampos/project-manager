FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/ProjectHub.Web/ProjectHub.Web.csproj", "src/ProjectHub.Web/"]
COPY ["src/ProjectHub.Contracts/ProjectHub.Contracts.csproj", "src/ProjectHub.Contracts/"]
RUN dotnet restore "src/ProjectHub.Web/ProjectHub.Web.csproj"

COPY . .
RUN dotnet publish "src/ProjectHub.Web/ProjectHub.Web.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM nginxinc/nginx-unprivileged:alpine AS final
COPY src/ProjectHub.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot/ /usr/share/nginx/html/
EXPOSE 8080
