#!/bin/bash
# Bash script to publish Bipins.AI packages to a NuGet package server
# Usage: ./scripts/publish-nuget.sh [--api-key <key>] [--source-url <url>] [--source-name <name>] [--source-username <username>] [--version <version>] [--skip-build] [--dry-run]

set -e

API_KEY="${PACKAGE_API_KEY:-}"
SOURCE_URL="${PACKAGE_SOURCE_URL:-}"
SOURCE_NAME="${PACKAGE_SOURCE_NAME:-}"
SOURCE_USERNAME="${PACKAGE_SOURCE_USERNAME:-}"
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
        --source-url)
            SOURCE_URL="$2"
            shift 2
            ;;
        --source-name)
            SOURCE_NAME="$2"
            shift 2
            ;;
        --source-username)
            SOURCE_USERNAME="$2"
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
            echo "Usage: $0 [--api-key <key>] [--source-url <url>] [--source-name <name>] [--source-username <username>] [--version <version>] [--skip-build] [--dry-run]"
            exit 1
            ;;
    esac
done

echo "=== Bipins.AI Package Publishing Script ==="

# Set default source URL if not provided
if [ -z "$SOURCE_URL" ]; then
    SOURCE_URL="https://api.nuget.org/v3/index.json"
    echo "Using default NuGet.org source"
else
    echo "Using package source: $SOURCE_URL"
fi

# Set default source name if not provided
if [ -z "$SOURCE_NAME" ]; then
    SOURCE_NAME="default"
fi

# Check if API key is provided (unless dry run)
if [ -z "$API_KEY" ] && [ "$DRY_RUN" = false ]; then
    echo "ERROR: Package API key is required."
    echo "Provide it via --api-key parameter or PACKAGE_API_KEY environment variable."
    echo "For NuGet.org, get your API key from: https://www.nuget.org/account/apikeys"
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

# Pack the main Bipins.AI package (which includes all projects)
echo ""
echo "Packing NuGet package..."

PROJECTS=(
    "src/Bipins.AI/Bipins.AI.csproj"
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

# Add custom source if needed (and not NuGet.org)
if [ "$DRY_RUN" = false ] && [ "$SOURCE_URL" != "https://api.nuget.org/v3/index.json" ] && [ -n "$SOURCE_URL" ]; then
    echo ""
    echo "Adding package source: $SOURCE_URL"
    
    ADD_SOURCE_ARGS=(
        "nuget" "add" "source" "$SOURCE_URL"
        "--name" "$SOURCE_NAME"
    )
    
    if [ -n "$API_KEY" ]; then
        ADD_SOURCE_ARGS+=("--password" "$API_KEY")
    fi
    
    if [ -n "$SOURCE_USERNAME" ]; then
        ADD_SOURCE_ARGS+=("--username" "$SOURCE_USERNAME")
    fi
    
    ADD_SOURCE_ARGS+=("--store-password-in-clear-text")
    
    dotnet "${ADD_SOURCE_ARGS[@]}" 2>/dev/null || true
fi

# Publish packages
if [ "$DRY_RUN" = true ]; then
    echo ""
    echo "=== DRY RUN MODE ==="
    echo "Would publish the following packages to $SOURCE_URL:"
    ls -1 "$ARTIFACTS_DIR"/*.nupkg 2>/dev/null | while read -r file; do
        echo "  - $(basename "$file")"
    done
    echo ""
    echo "To actually publish, run without --dry-run flag"
else
    echo ""
    echo "Publishing to $SOURCE_URL..."
    
    for nupkg_file in "$ARTIFACTS_DIR"/*.nupkg; do
        if [ -f "$nupkg_file" ]; then
            echo "  Publishing $(basename "$nupkg_file")..."
            
            PUSH_ARGS=(
                "nuget" "push" "$nupkg_file"
                "--source" "$SOURCE_URL"
                "--skip-duplicate"
            )
            
            if [ -n "$API_KEY" ]; then
                PUSH_ARGS+=("--api-key" "$API_KEY")
            fi
            
            if dotnet "${PUSH_ARGS[@]}"; then
                echo "    ✓ Published successfully"
            else
                echo "    ✗ Failed to publish"
            fi
        fi
    done
    
    echo ""
    echo "=== Publishing Complete ==="
    if [ "$SOURCE_URL" = "https://api.nuget.org/v3/index.json" ]; then
        echo "View packages at: https://www.nuget.org/profiles/$USER"
    else
        echo "Packages published to: $SOURCE_URL"
    fi
fi
