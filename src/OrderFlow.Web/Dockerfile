# Imagem da OrderFlow.Web (Blazor Server).
# O contexto de build é a RAIZ do repositório:
#   docker build -f src/OrderFlow.Web/Dockerfile -t orderflow-web .

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/OrderFlow.Web/OrderFlow.Web.csproj src/OrderFlow.Web/
RUN dotnet restore src/OrderFlow.Web/OrderFlow.Web.csproj

COPY src/OrderFlow.Web/ src/OrderFlow.Web/
RUN dotnet publish src/OrderFlow.Web/OrderFlow.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Roda sem root (usuário "app" já existe na imagem).
USER $APP_UID

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Escuta na porta que a plataforma informar em PORT; 8080 se não houver.
# Configuração por variável de ambiente: Api__BaseUrl (URL pública da API, obrigatória).
CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet OrderFlow.Web.dll"]
