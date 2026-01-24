#!/bin/bash

set -e

echo "Building Lambda functions..."
cd "$(dirname "$0")/.."

# Build all Lambda functions
dotnet build src/Lambda/GetCostData/GetCostData.csproj -c Release
dotnet build src/Lambda/AnalyzeCosts/AnalyzeCosts.csproj -c Release
dotnet build src/Lambda/GetAnalysisHistory/GetAnalysisHistory.csproj -c Release

echo "Building frontend..."
cd frontend
npm install
npm run build
cd ..

echo "Deploying with SAM..."
cd cloudformation
sam build
sam deploy --guided

echo "Deployment complete!"
