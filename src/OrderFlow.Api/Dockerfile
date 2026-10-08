# Imagem da OrderFlow.Api.
# O contexto de build é a RAIZ do repositório (a API referencia outros projetos):
#   docker build -f src/OrderFlow.Api/Dockerfile -t orderflow-api .

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia só os .csproj antes: o restore fica em cache enquanto os pacotes não mudam.
COPY src/OrderFlow.Api/OrderFlow.Api.csproj src/OrderFlow.Api/
COPY src/OrderFlow.Application/OrderFlow.Application.csproj src/OrderFlow.Application/
COPY src/OrderFlow.Domain/OrderFlow.Domain.csproj src/OrderFlow.Domain/
COPY src/OrderFlow.Infrastructure/OrderFlow.Infrastructure.csproj src/OrderFlow.Infrastructure/
COPY src/OrderFlow.Contracts/OrderFlow.Contracts.csproj src/OrderFlow.Contracts/
RUN dotnet restore src/OrderFlow.Api/OrderFlow.Api.csproj

COPY src/OrderFlow.Api/ src/OrderFlow.Api/
COPY src/OrderFlow.Application/ src/OrderFlow.Application/
COPY src/OrderFlow.Domain/ src/OrderFlow.Domain/
COPY src/OrderFlow.Infrastructure/ src/OrderFlow.Infrastructure/
COPY src/OrderFlow.Contracts/ src/OrderFlow.Contracts/
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
