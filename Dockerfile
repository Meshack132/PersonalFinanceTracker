# ─── Build stage ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first (in dependency order) so Docker caches the
# restore layer across builds — only re-runs if a csproj actually changes.
COPY ["src/PersonalFinanceTracker.Web/PersonalFinanceTracker.Web.csproj", "src/PersonalFinanceTracker.Web/"]
COPY ["src/PersonalFinanceTracker.Infrastructure/PersonalFinanceTracker.Infrastructure.csproj", "src/PersonalFinanceTracker.Infrastructure/"]
COPY ["src/PersonalFinanceTracker.Application/PersonalFinanceTracker.Application.csproj", "src/PersonalFinanceTracker.Application/"]
COPY ["src/PersonalFinanceTracker.Domain/PersonalFinanceTracker.Domain.csproj", "src/PersonalFinanceTracker.Domain/"]

RUN dotnet restore "src/PersonalFinanceTracker.Web/PersonalFinanceTracker.Web.csproj"

# Now copy everything else and publish.
COPY . .
WORKDIR /src/src/PersonalFinanceTracker.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

# ─── Runtime stage ───────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# SQLite data directory — mount a volume here to persist the DB
# across container restarts (see docker-compose.yml).
RUN mkdir -p /app/data

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__Default="Data Source=/app/data/financetracker.db"

ENTRYPOINT ["dotnet", "PersonalFinanceTracker.Web.dll"]
