#!/bin/bash
# Bash script to publish Bipins.AI packages to NuGet.org
# Usage: ./scripts/publish-nuget.sh [--api-key <your-api-key>] [--version <version>] [--skip-build] [--dry-run]

set -e

API_KEY="${NUGET_API_KEY:-}"
VERSION=""
SKIP_BUILD=false
DRY_RUN=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --api-key)
            API_KEY="$2"
            shift 2
            ;;
        --version)
            VERSION="$2"
            shift 2
            ;;
        --skip-build)
            SKIP_BUILD=true
            shift
            ;;
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--api-key <key>] [--version <version>] [--skip-build] [--dry-run]"
            exit 1
            ;;
    esac
done

echo "=== Bipins.AI NuGet Publishing Script ==="

# Check if API key is provided (unless dry run)
if [ -z "$API_KEY" ] && [ "$DRY_RUN" = false ]; then
    echo "ERROR: NuGet API key is required."
    echo "Provide it via --api-key parameter or NUGET_API_KEY environment variable."
    echo "Get your API key from: https://www.nuget.org/account/apikeys"
    exit 1
fi

# Get version from git or use provided version
if [ -z "$VERSION" ]; then
    GIT_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "")
    if [ -n "$GIT_TAG" ]; then
        VERSION=$(echo "$GIT_TAG" | sed 's/^v//')
        echo "Using version from git tag: $VERSION"
    else
        VERSION="1.0.0"
        echo "No git tag found, using default version: $VERSION"
    fi
else
    echo "Using provided version: $VERSION"
fi

# Build solution if not skipped
if [ "$SKIP_BUILD" = false ]; then
    echo ""
    echo "Building solution..."
    dotnet build Bipins.AI.sln --configuration Release
fi

# Create artifacts directory
ARTIFACTS_DIR="./artifacts"
rm -rf "$ARTIFACTS_DIR"
mkdir -p "$ARTIFACTS_DIR"

# Pack all projects that should be published
echo ""
echo "Packing NuGet packages..."

PROJECTS=(
    "src/Bipins.AI.Core/Bipins.AI.Core.csproj"
    "src/Bipins.AI.Runtime/Bipins.AI.Runtime.csproj"
    "src/Bipins.AI.Ingestion/Bipins.AI.Ingestion.csproj"
    "src/Bipins.AI.Providers/Bipins.AI.Providers.OpenAI/Bipins.AI.Providers.OpenAI.csproj"
    "src/Bipins.AI.Providers/Bipins.AI.Providers.Anthropic/Bipins.AI.Providers.Anthropic.csproj"
    "src/Bipins.AI.Providers/Bipins.AI.Providers.AzureOpenAI/Bipins.AI.Providers.AzureOpenAI.csproj"
    "src/Bipins.AI.Providers/Bipins.AI.Providers.Bedrock/Bipins.AI.Providers.Bedrock.csproj"
    "src/Bipins.AI.Vectors/Bipins.AI.Vectors.Qdrant/Bipins.AI.Vectors.Qdrant.csproj"
    "src/Bipins.AI.Vectors/Bipins.AI.Vectors.Pinecone/Bipins.AI.Vectors.Pinecone.csproj"
    "src/Bipins.AI.Vectors/Bipins.AI.Vectors.Weaviate/Bipins.AI.Vectors.Weaviate.csproj"
    "src/Bipins.AI.Vectors/Bipins.AI.Vectors.Milvus/Bipins.AI.Vectors.Milvus.csproj"
)

PACKED_COUNT=0

for project in "${PROJECTS[@]}"; do
    if [ -f "$project" ]; then
        echo "  Packing $project..."
        
        PACK_ARGS=(
            "pack"
            "$project"
            "--configuration" "Release"
            "--output" "$ARTIFACTS_DIR"
            "--no-build"
        )
        
        if [ -n "$VERSION" ]; then
            PACK_ARGS+=("--version-suffix" "$VERSION")
        fi
        
        if dotnet "${PACK_ARGS[@]}"; then
            NUPKG_FILE=$(ls -t "$ARTIFACTS_DIR"/*.nupkg 2>/dev/null | head -n 1)
            if [ -n "$NUPKG_FILE" ]; then
                echo "    ✓ Packed: $(basename "$NUPKG_FILE")"
                PACKED_COUNT=$((PACKED_COUNT + 1))
            fi
        else
            echo "    ✗ Failed to pack $project"
        fi
    else
        echo "  ⚠ Skipping $project (not found)"
    fi
done

if [ $PACKED_COUNT -eq 0 ]; then
    echo ""
    echo "No packages were packed!"
    exit 1
fi

echo ""
echo "Packed $PACKED_COUNT package(s)"

# Publish to NuGet.org
if [ "$DRY_RUN" = true ]; then
    echo ""
    echo "=== DRY RUN MODE ==="
    echo "Would publish the following packages:"
    ls -1 "$ARTIFACTS_DIR"/*.nupkg 2>/dev/null | while read -r file; do
        echo "  - $(basename "$file")"
    done
    echo ""
    echo "To actually publish, run without --dry-run flag"
else
    echo ""
    echo "Publishing to NuGet.org..."
    
    for nupkg_file in "$ARTIFACTS_DIR"/*.nupkg; do
        if [ -f "$nupkg_file" ]; then
            echo "  Publishing $(basename "$nupkg_file")..."
            
            if dotnet nuget push "$nupkg_file" \
                --api-key "$API_KEY" \
                --source https://api.nuget.org/v3/index.json \
                --skip-duplicate; then
                echo "    ✓ Published successfully"
            else
                echo "    ✗ Failed to publish"
            fi
        fi
    done
    
    echo ""
    echo "=== Publishing Complete ==="
    echo "View packages at: https://www.nuget.org/profiles/$USER"
fi
