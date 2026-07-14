FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/SafeNavigation.Api/SafeNavigation.Api.csproj", "src/SafeNavigation.Api/"]
COPY ["src/SafeNavigation.Application/SafeNavigation.Application.csproj", "src/SafeNavigation.Application/"]
COPY ["src/SafeNavigation.Domain/SafeNavigation.Domain.csproj", "src/SafeNavigation.Domain/"]
COPY ["src/SafeNavigation.Infrastructure/SafeNavigation.Infrastructure.csproj", "src/SafeNavigation.Infrastructure/"]
RUN dotnet restore "src/SafeNavigation.Api/SafeNavigation.Api.csproj"
COPY . .
RUN dotnet publish "src/SafeNavigation.Api/SafeNavigation.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SafeNavigation.Api.dll"]
