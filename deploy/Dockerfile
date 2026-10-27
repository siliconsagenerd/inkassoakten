# Multi-stage build for InkassoAkten.Web (ASP.NET Core 8, Blazor Server).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution/project files first for layer caching.
COPY src/InkassoAkten.Domain/InkassoAkten.Domain.csproj src/InkassoAkten.Domain/
COPY src/InkassoAkten.Application/InkassoAkten.Application.csproj src/InkassoAkten.Application/
COPY src/InkassoAkten.Infrastructure/InkassoAkten.Infrastructure.csproj src/InkassoAkten.Infrastructure/
COPY src/InkassoAkten.Web/InkassoAkten.Web.csproj src/InkassoAkten.Web/
RUN dotnet restore src/InkassoAkten.Web/InkassoAkten.Web.csproj

# Copy the rest and publish.
COPY src/ src/
RUN dotnet publish src/InkassoAkten.Web/InkassoAkten.Web.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD wget -qO- http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "InkassoAkten.Web.dll"]
