# Bipins.AI Scripts

This directory contains utility scripts for building, testing, and publishing the Bipins.AI project.

## NuGet Publishing Scripts

### PowerShell (Windows)

```powershell
# Dry run (test without publishing)
.\scripts\publish-nuget.ps1 -DryRun

# Publish with API key from environment variable
$env:NUGET_API_KEY = "your-api-key"
.\scripts\publish-nuget.ps1

# Publish with explicit API key
.\scripts\publish-nuget.ps1 -ApiKey "your-api-key"

# Publish specific version
.\scripts\publish-nuget.ps1 -ApiKey "your-api-key" -Version "1.0.0"

# Skip build step (if already built)
.\scripts\publish-nuget.ps1 -ApiKey "your-api-key" -SkipBuild
```

### Bash (Linux/macOS)

```bash
# Make script executable (first time only)
chmod +x scripts/publish-nuget.sh

# Dry run (test without publishing)
./scripts/publish-nuget.sh --dry-run

# Publish with API key from environment variable
export NUGET_API_KEY="your-api-key"
./scripts/publish-nuget.sh

# Publish with explicit API key
./scripts/publish-nuget.sh --api-key "your-api-key"

# Publish specific version
./scripts/publish-nuget.sh --api-key "your-api-key" --version "1.0.0"

# Skip build step (if already built)
./scripts/publish-nuget.sh --api-key "your-api-key" --skip-build
```

## Getting Your NuGet API Key

1. Go to https://www.nuget.org/account/apikeys
2. Create a new API key
3. Copy the key and use it with the scripts above

## Packages Published

The following packages will be published to NuGet.org:

- `Bipins.AI.Core` - Core abstractions and models
- `Bipins.AI.Runtime` - Runtime components (pipelines, policies, RAG)
- `Bipins.AI.Ingestion` - Document ingestion and chunking
- `Bipins.AI.Providers.OpenAI` - OpenAI provider
- `Bipins.AI.Providers.Anthropic` - Anthropic Claude provider
- `Bipins.AI.Providers.AzureOpenAI` - Azure OpenAI provider
- `Bipins.AI.Providers.Bedrock` - AWS Bedrock provider
- `Bipins.AI.Vectors.Qdrant` - Qdrant vector database
- `Bipins.AI.Vectors.Pinecone` - Pinecone vector database
- `Bipins.AI.Vectors.Weaviate` - Weaviate vector database
- `Bipins.AI.Vectors.Milvus` - Milvus vector database

## Version Management

The scripts will automatically detect the version from git tags (e.g., `v1.0.0`). If no tag is found, it defaults to `1.0.0`. You can override this with the `--version` parameter.

## GitHub Actions

The CI workflow will automatically publish packages when:
- A commit message contains `[publish-nuget]` on the `master` branch
- Manual workflow dispatch with `publish_to_nuget` set to `true`

Make sure to set the `NUGET_API_KEY` secret in your GitHub repository settings.
