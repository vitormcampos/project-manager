FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/ProjectHub.Api/ProjectHub.Api.csproj", "src/ProjectHub.Api/"]
COPY ["src/ProjectHub.Application/ProjectHub.Application.csproj", "src/ProjectHub.Application/"]
COPY ["src/ProjectHub.Contracts/ProjectHub.Contracts.csproj", "src/ProjectHub.Contracts/"]
COPY ["src/ProjectHub.Data/ProjectHub.Data.csproj", "src/ProjectHub.Data/"]
COPY ["src/ProjectHub.Domain/ProjectHub.Domain.csproj", "src/ProjectHub.Domain/"]
RUN dotnet restore "src/ProjectHub.Api/ProjectHub.Api.csproj"

COPY . .
RUN dotnet publish "src/ProjectHub.Api/ProjectHub.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "ProjectHub.Api.dll"]
