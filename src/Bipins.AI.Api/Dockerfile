# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY Directory.Build.props ./
COPY Directory.Build.targets ./
COPY src/Bipins.AI.Api/*.csproj ./src/Bipins.AI.Api/
COPY src/Bipins.AI.Core/*.csproj ./src/Bipins.AI.Core/
COPY src/Bipins.AI.Runtime/*.csproj ./src/Bipins.AI.Runtime/
COPY src/Bipins.AI.Ingestion/*.csproj ./src/Bipins.AI.Ingestion/

# Restore dependencies
RUN dotnet restore Bipins.AI.sln

# Copy all source files
COPY . .

# Build the application
WORKDIR /src/src/Bipins.AI.Api
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published files
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl --fail http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "Bipins.AI.Api.dll"]
