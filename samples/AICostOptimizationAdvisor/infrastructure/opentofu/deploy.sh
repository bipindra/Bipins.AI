#!/bin/bash

set -e

echo "Building Lambda functions..."
cd "$(dirname "$0")/../.."

# Build all Lambda functions
dotnet build src/Lambda/GetCostData/GetCostData.csproj -c Release
dotnet build src/Lambda/AnalyzeCosts/AnalyzeCosts.csproj -c Release
dotnet build src/Lambda/GetAnalysisHistory/GetAnalysisHistory.csproj -c Release

echo "Building frontend..."
cd frontend
npm install
npm run build
cd ..

echo "Deploying with OpenTofu..."
cd infrastructure/opentofu

# Initialize OpenTofu if needed
if [ ! -d ".terraform" ]; then
  tofu init
fi

# Plan and apply
tofu plan
read -p "Apply these changes? (y/n) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
  tofu apply
fi

echo "Deployment complete!"
echo "API URL: $(tofu output -raw api_url)"
