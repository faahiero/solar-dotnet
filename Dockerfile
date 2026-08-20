# ==============================================================================
# SOLAR LMS 2.0 - DOCKERFILE MULTI-STAGE (.NET 10 + REACT 19 VITE)
# ==============================================================================

# ----------------------------------------------------
# STAGE 1: Build do Front-end (React 19 + TypeScript + Vite)
# ----------------------------------------------------
FROM node:22-alpine AS client-build
WORKDIR /src/client

# Instala dependências do NPM
COPY src/Solar.Client/package*.json ./
RUN npm ci

# Compila o bundle estático otimizado
COPY src/Solar.Client/ ./
RUN npm run build

# ----------------------------------------------------
# STAGE 2: Build do Back-end (.NET 10 SDK)
# ----------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src

# Copia arquivos de projeto para cache eficiente de camadas Docker
COPY *.sln ./
COPY src/Solar.Domain/*.csproj src/Solar.Domain/
COPY src/Solar.Application/*.csproj src/Solar.Application/
COPY src/Solar.Infrastructure/*.csproj src/Solar.Infrastructure/
COPY src/Solar.WebApi/*.csproj src/Solar.WebApi/
COPY tests/Solar.Domain.Tests/*.csproj tests/Solar.Domain.Tests/
COPY tests/Solar.WebApi.Tests/*.csproj tests/Solar.WebApi.Tests/

RUN dotnet restore Solar.sln

# Copia o código-fonte completo
COPY . .

# Copia o bundle compilado do React diretamente para o wwwroot da WebApi
COPY --from=client-build /src/client/dist/ ./src/Solar.WebApi/wwwroot/

# Executa testes automatizados durante a compilação
RUN dotnet test Solar.sln -c Release --no-restore --logger "console;verbosity=normal"

# Publica os binários otimizados da WebApi
RUN dotnet publish src/Solar.WebApi/Solar.WebApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ----------------------------------------------------
# STAGE 3: Imagem Final de Runtime (Distroless com Suporte a ICU/i18n)
# ----------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS final
WORKDIR /app

# Copia artefato publicado
COPY --from=backend-build /app/publish .

# Cria diretório para uploads com permissão para o usuário não-root 'app'
USER app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "Solar.WebApi.dll"]
