# Bipins.AI Scripts

This directory contains utility scripts for building, testing, and publishing the Bipins.AI project.

## Package Publishing Scripts

The publishing scripts support any NuGet-compatible package server (NuGet.org, GitHub Packages, Azure Artifacts, private feeds, etc.).

### PowerShell (Windows)

```powershell
# Dry run (test without publishing)
.\scripts\publish-nuget.ps1 -DryRun

# Publish to NuGet.org (default)
$env:PACKAGE_API_KEY = "your-api-key"
.\scripts\publish-nuget.ps1

# Publish to custom package server
$env:PACKAGE_API_KEY = "your-api-key"
$env:PACKAGE_SOURCE_URL = "https://your-package-server.com/v3/index.json"
.\scripts\publish-nuget.ps1

# Publish to GitHub Packages
$env:PACKAGE_API_KEY = "your-github-token"
$env:PACKAGE_SOURCE_URL = "https://nuget.pkg.github.com/OWNER/index.json"
$env:PACKAGE_SOURCE_USERNAME = "your-github-username"
.\scripts\publish-nuget.ps1

# Publish to Azure Artifacts
$env:PACKAGE_API_KEY = "your-azure-devops-pat"
$env:PACKAGE_SOURCE_URL = "https://pkgs.dev.azure.com/ORG/_packaging/FEED/nuget/v3/index.json"
.\scripts\publish-nuget.ps1

# Publish with explicit parameters
.\scripts\publish-nuget.ps1 -ApiKey "your-api-key" -SourceUrl "https://your-server.com/v3/index.json"

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

# Publish to NuGet.org (default)
export PACKAGE_API_KEY="your-api-key"
./scripts/publish-nuget.sh

# Publish to custom package server
export PACKAGE_API_KEY="your-api-key"
export PACKAGE_SOURCE_URL="https://your-package-server.com/v3/index.json"
./scripts/publish-nuget.sh

# Publish to GitHub Packages
export PACKAGE_API_KEY="your-github-token"
export PACKAGE_SOURCE_URL="https://nuget.pkg.github.com/OWNER/index.json"
export PACKAGE_SOURCE_USERNAME="your-github-username"
./scripts/publish-nuget.sh

# Publish to Azure Artifacts
export PACKAGE_API_KEY="your-azure-devops-pat"
export PACKAGE_SOURCE_URL="https://pkgs.dev.azure.com/ORG/_packaging/FEED/nuget/v3/index.json"
./scripts/publish-nuget.sh

# Publish with explicit parameters
./scripts/publish-nuget.sh --api-key "your-api-key" --source-url "https://your-server.com/v3/index.json"

# Publish specific version
./scripts/publish-nuget.sh --api-key "your-api-key" --version "1.0.0"

# Skip build step (if already built)
./scripts/publish-nuget.sh --api-key "your-api-key" --skip-build
```

## Getting API Keys

### NuGet.org
1. Go to https://www.nuget.org/account/apikeys
2. Create a new API key
3. Copy the key and use it with the scripts above

### GitHub Packages
1. Go to https://github.com/settings/tokens
2. Create a Personal Access Token (PAT) with `write:packages` and `read:packages` permissions
3. Use the token as the API key and set `PACKAGE_SOURCE_URL` to `https://nuget.pkg.github.com/YOUR_USERNAME/index.json`

### Azure Artifacts
1. Go to your Azure DevOps project
2. Create a Personal Access Token (PAT) with `Packaging (read & write)` scope
3. Use the PAT as the API key and set `PACKAGE_SOURCE_URL` to your feed URL

### Other Package Servers
- Use the appropriate authentication method for your server
- Set `PACKAGE_SOURCE_URL` to your server's NuGet v3 feed URL
- Provide credentials via `PACKAGE_API_KEY` and optionally `PACKAGE_SOURCE_USERNAME`

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
- A commit message contains `[publish-packages]` on the `master` branch
- Manual workflow dispatch with `publish_packages` set to `true`

### Required GitHub Secrets

Configure the following secrets in your GitHub repository settings (Settings → Secrets and variables → Actions):

- **`PACKAGE_API_KEY`** (required): API key or token for authenticating with the package server
- **`PACKAGE_SOURCE_URL`** (optional): Package server URL (defaults to NuGet.org if not set)
  - NuGet.org: `https://api.nuget.org/v3/index.json` (default)
  - GitHub Packages: `https://nuget.pkg.github.com/OWNER/index.json`
  - Azure Artifacts: `https://pkgs.dev.azure.com/ORG/_packaging/FEED/nuget/v3/index.json`
  - Custom server: Your server's NuGet v3 feed URL
- **`PACKAGE_SOURCE_NAME`** (optional): Name for the package source (defaults to "default")
- **`PACKAGE_SOURCE_USERNAME`** (optional): Username for package servers that require it (e.g., GitHub Packages)

### Example Secret Configuration

**For NuGet.org:**
```
PACKAGE_API_KEY: your-nuget-api-key
PACKAGE_SOURCE_URL: (leave empty or set to https://api.nuget.org/v3/index.json)
```

**For GitHub Packages:**
```
PACKAGE_API_KEY: your-github-pat-token
PACKAGE_SOURCE_URL: https://nuget.pkg.github.com/YOUR_USERNAME/index.json
PACKAGE_SOURCE_USERNAME: your-github-username
```

**For Azure Artifacts:**
```
PACKAGE_API_KEY: your-azure-devops-pat
PACKAGE_SOURCE_URL: https://pkgs.dev.azure.com/YOUR_ORG/_packaging/YOUR_FEED/nuget/v3/index.json
```
