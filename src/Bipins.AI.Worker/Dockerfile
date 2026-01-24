# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY Directory.Build.props ./
COPY Directory.Build.targets ./
COPY src/Bipins.AI.Worker/*.csproj ./src/Bipins.AI.Worker/
COPY src/Bipins.AI.Core/*.csproj ./src/Bipins.AI.Core/
COPY src/Bipins.AI.Runtime/*.csproj ./src/Bipins.AI.Runtime/
COPY src/Bipins.AI.Ingestion/*.csproj ./src/Bipins.AI.Ingestion/

# Restore dependencies
RUN dotnet restore Bipins.AI.sln

# Copy all source files
COPY . .

# Build the application
WORKDIR /src/src/Bipins.AI.Worker
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published files
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "Bipins.AI.Worker.dll"]
