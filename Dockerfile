# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/DotNet10Template.API/DotNet10Template.API.csproj", "src/DotNet10Template.API/"]
COPY ["src/DotNet10Template.Infrastructure/DotNet10Template.Infrastructure.csproj", "src/DotNet10Template.Infrastructure/"]
COPY ["src/DotNet10Template.Application/DotNet10Template.Application.csproj", "src/DotNet10Template.Application/"]
COPY ["src/DotNet10Template.Domain/DotNet10Template.Domain.csproj", "src/DotNet10Template.Domain/"]
RUN dotnet restore "src/DotNet10Template.API/DotNet10Template.API.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/src/DotNet10Template.API"
RUN dotnet build "DotNet10Template.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DotNet10Template.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "DotNet10Template.API.dll"]
