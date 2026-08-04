FROM node:26-alpine AS client-build
WORKDIR /client
COPY ["src/Carlens.Web/ClientApp/package.json", "."]
COPY ["src/Carlens.Web/ClientApp/pnpm-lock.yaml", "."]
COPY ["src/Carlens.Web/ClientApp/pnpm-workspace.yaml", "."]
RUN corepack enable && pnpm install --frozen-lockfile
COPY ["src/Carlens.Web/ClientApp", "."]
RUN pnpm build

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Carlens.Web/Carlens.Web.csproj", "src/Carlens.Web/"]
COPY ["src/Carlens.Contracts/Carlens.Contracts.csproj", "src/Carlens.Contracts/"]
RUN dotnet restore "src/Carlens.Web/Carlens.Web.csproj"
COPY . .
COPY --from=client-build /wwwroot src/Carlens.Web/wwwroot
RUN dotnet publish "src/Carlens.Web/Carlens.Web.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Carlens.Web.dll"]
