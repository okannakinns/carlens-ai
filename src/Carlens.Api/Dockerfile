FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Carlens.Api/Carlens.Api.csproj", "src/Carlens.Api/"]
COPY ["src/Carlens.Application/Carlens.Application.csproj", "src/Carlens.Application/"]
COPY ["src/Carlens.Contracts/Carlens.Contracts.csproj", "src/Carlens.Contracts/"]
COPY ["src/Carlens.Domain/Carlens.Domain.csproj", "src/Carlens.Domain/"]
COPY ["src/Carlens.Infrastructure/Carlens.Infrastructure.csproj", "src/Carlens.Infrastructure/"]
RUN dotnet restore "src/Carlens.Api/Carlens.Api.csproj"
COPY . .
RUN dotnet publish "src/Carlens.Api/Carlens.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Carlens.Api.dll"]
