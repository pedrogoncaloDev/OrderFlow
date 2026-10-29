# Imagem da OrderFlow.Api.
# O contexto de build é a RAIZ do repositório:
#   docker build -f src/OrderFlow.Api/Dockerfile -t orderflow-api .

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia só os .csproj antes: o restore fica em cache enquanto os pacotes não mudam.
COPY src/OrderFlow.Api/OrderFlow.Api.csproj src/OrderFlow.Api/
RUN dotnet restore src/OrderFlow.Api/OrderFlow.Api.csproj

COPY src/OrderFlow.Api/ src/OrderFlow.Api/
RUN dotnet publish src/OrderFlow.Api/OrderFlow.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Roda sem root (usuário "app" já existe na imagem).
USER $APP_UID

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Escuta na porta que a plataforma informar em PORT (Render, por exemplo); 8080 se não houver.
# Configuração por variáveis de ambiente: ConnectionStrings__Default e Jwt__Key (obrigatórias).
CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet OrderFlow.Api.dll"]
