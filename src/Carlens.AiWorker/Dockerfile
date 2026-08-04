FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Carlens.AiWorker/Carlens.AiWorker.csproj", "src/Carlens.AiWorker/"]
COPY ["src/Carlens.Application/Carlens.Application.csproj", "src/Carlens.Application/"]
COPY ["src/Carlens.Contracts/Carlens.Contracts.csproj", "src/Carlens.Contracts/"]
COPY ["src/Carlens.Domain/Carlens.Domain.csproj", "src/Carlens.Domain/"]
COPY ["src/Carlens.Infrastructure/Carlens.Infrastructure.csproj", "src/Carlens.Infrastructure/"]
RUN dotnet restore "src/Carlens.AiWorker/Carlens.AiWorker.csproj"
COPY . .
RUN dotnet publish "src/Carlens.AiWorker/Carlens.AiWorker.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0-noble AS final
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
WORKDIR /app
COPY --chown=app:app --from=build /app/publish .
RUN ./.playwright/node/linux-x64/node \
        ./.playwright/package/cli.js \
        install --with-deps chromium \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
ENTRYPOINT ["dotnet", "Carlens.AiWorker.dll"]
