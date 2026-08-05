FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY [".config/dotnet-tools.json", ".config/"]
COPY ["src/Carlens.Api/Carlens.Api.csproj", "src/Carlens.Api/"]
COPY ["src/Carlens.Application/Carlens.Application.csproj", "src/Carlens.Application/"]
COPY ["src/Carlens.Contracts/Carlens.Contracts.csproj", "src/Carlens.Contracts/"]
COPY ["src/Carlens.Domain/Carlens.Domain.csproj", "src/Carlens.Domain/"]
COPY ["src/Carlens.Infrastructure/Carlens.Infrastructure.csproj", "src/Carlens.Infrastructure/"]
RUN dotnet restore "src/Carlens.Api/Carlens.Api.csproj" --runtime linux-x64
RUN dotnet tool restore
COPY . .
RUN dotnet publish "src/Carlens.Api/Carlens.Api.csproj" \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained false \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false
RUN dotnet ef migrations bundle \
    --project "src/Carlens.Infrastructure/Carlens.Infrastructure.csproj" \
    --startup-project "src/Carlens.Api/Carlens.Api.csproj" \
    --configuration Release \
    --target-runtime linux-x64 \
    --output /app/migrations/efbundle \
    && chmod 0555 /app/migrations/efbundle

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /app/migrations/efbundle ./efbundle
USER $APP_UID
ENTRYPOINT ["dotnet", "Carlens.Api.dll"]
